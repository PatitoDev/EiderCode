using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;


namespace EiderCode.LSP.RPC;


// JSON RPC 2.0 with LSP header
public class JsonRPCClient
{
    private const string VERSION = "2.0";
    private readonly StreamWriter _stdIn;
    private readonly Stream _stdOut;
    private CancellationTokenSource _readCancellationTokenSource;

    private Task? _streamReaderTask;

    private Dictionary<string, string> _resultStore = new();

    public JsonRPCClient(StreamWriter stdIn, Stream stdOut)
    {
        _stdIn = stdIn;
        _stdOut = stdOut;
        _readCancellationTokenSource = new();

        /*
        var content = @"{ ""jsonrpc"" : ""2.0"", ""id"" : ""1"", ""method"" : ""initialize"", ""params"": {} }";
        var length = Encoding.ASCII.GetByteCount(content);
        var msg = $"Content-Length: {length}\r\n\r\n{content}";

        _process.StandardInput.Write(msg);

        Task.Run(() =>
        {
            Task.Delay(1000);
            var content = @"{ ""jsonrpc"" : ""2.0"", ""id"" : ""1"", ""method"" : ""initialize"", ""params"": {} }";
            var length = Encoding.ASCII.GetByteCount(content);
            var msg = $"Content-Length: {length}\r\n\r\n{content}";

            _process.StandardInput.Write(msg);
        });
        */
    }

    public async Task<RpcResponseMessage<Result>?> RequestMethod<Result, MethodParams>(string method, MethodParams? paramObj)
    {
        return await Task.Run(() =>
        {
            var timeout = 1000 * 10;
            var id = Guid.NewGuid().ToString();
            var request = new RpcRequestMsg<MethodParams>()
            {
                Id = id,
                Jsonrpc = VERSION,
                Method = method,
                Params = paramObj
            };

            Send(request);

            string? response = null;
            var timer = new Stopwatch();
            timer.Start();

            while (!_resultStore.TryGetValue(id, out response))
            {
                if (timer.ElapsedMilliseconds >= timeout) return null;
            }

            timer.Stop();

            var parsedResponse = JsonSerializer
                .Deserialize<RpcResponseMessage<Result>>(
                    response, _jsonDeserializationOptions
                );

            _resultStore.Remove(id);

            if (parsedResponse == null) return null;
            return parsedResponse;
        });
    }

    private void Send<T>(RpcRequestMsg<T> requestMsg)
    {
        var content = JsonSerializer.Serialize(requestMsg, new JsonSerializerOptions()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        });

        var length = Encoding.UTF8.GetByteCount(content);
        var payloadAsString = $"Content-Length: {length}\r\n\r\n{content}";
        var payload = Encoding.UTF8.GetBytes(payloadAsString);
        //var header = $"Content-Length: {length}\r\n\r\n";
        //var headerEncoded = Encoding.ASCII.GetBytes(header);
        //var contentEncoded = Encoding.UTF8.GetBytes(content);
        //var payload = headerEncoded.Concat(contentEncoded);

        Godot.GD.Print("SEND:", payloadAsString);

        _stdIn.Write(payloadAsString);
    }

    public void SendJsonString(string content)
    {
        var length = Encoding.UTF8.GetByteCount(content);
        var payloadAsString = $"Content-Length: {length}\r\n\r\n{content}";
        var payload = Encoding.UTF8.GetBytes(payloadAsString);
        Godot.GD.Print("SEND: ", payloadAsString);

        _stdIn.Write(payloadAsString);
    }

    public void StartReading()
    {
        if (_streamReaderTask?.Status == TaskStatus.Running)
            throw new Exception("Reader already started");

        var cancellationToken = _readCancellationTokenSource.Token;
        _streamReaderTask = Task.Run(async () =>
        {
            await ReadStreamAsync(_stdOut, cancellationToken);
        });
    }

    public void StopReading()
    {
        _readCancellationTokenSource.Cancel();
    }

    private JsonSerializerOptions _jsonDeserializationOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private void OnMessageReceived(string message)
    {
        var parsed = System.Text.Json.JsonDocument.Parse(message);
        Godot.GD.Print("RECEIVED : ",
            System.Text.Json.JsonSerializer
            .Serialize(parsed, new System.Text.Json.JsonSerializerOptions()
            {
                WriteIndented = true
            })
        );

        try
        {
            var rpcMessageParsed = JsonSerializer.Deserialize<ResponseOrNotificationMessage>(
                message,
                _jsonDeserializationOptions
            );

            if (
                rpcMessageParsed == null ||
                rpcMessageParsed.Jsonrpc != VERSION
            ) return;

            // if message has an Id then its a response otherwiser its a notification
            var isNotification = String.IsNullOrWhiteSpace(rpcMessageParsed.Id);

            if (isNotification)
            {
                // handle
                return;
            }

            _resultStore.Add(rpcMessageParsed.Id!, message);
        }
        catch (JsonException)
        {
            return;
        }
    }

    private async Task ReadStreamAsync(Stream stdOut, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested) return;

        var headerCount = 0;
        var contentLine = "";
        var contentLength = 0;

        while (stdOut.CanRead)
        {
            var charByte = new Byte[1];
            await stdOut.ReadExactlyAsync(charByte, 0, 1, cancellationToken);

            var lastByte = charByte[0];
            var lastChar = Encoding.ASCII.GetString([lastByte]);
            contentLine += lastChar;

            if (lastChar != "\n") continue;
            // first line should be Content-Length: x\r\rn
            // second line is optional and could be Content-Type: utf-8
            headerCount += 1;

            if (headerCount == 1)
            {
                // read content length
                contentLength = int.Parse(
                    contentLine
                    .ReplaceLineEndings("")
                    .Replace("Content-Length: ", "")
                  );
                contentLine = ""; // reset and read next line
            }

            if (headerCount == 2)
            {
                // content type is optional
                if (!string.IsNullOrWhiteSpace(contentLine))
                {
                    // if its there then it must be utf8 or utf-8
                    var contentType = contentLine.Replace("Content-Type: ", "").ReplaceLineEndings("");
                    if (contentType != "utf8" && contentType != "utf-8")
                    {
                        // if its not utf-8 read and ignore message
                        var bytesToDiscard = new Byte[contentLength];
                        await stdOut.ReadExactlyAsync(bytesToDiscard, 0, contentLength, cancellationToken);
                        contentLength = 0;
                        contentLine = "";
                        headerCount = 0;
                        continue;
                    }
                }

                var contentBytes = new Byte[contentLength];
                await stdOut.ReadExactlyAsync(contentBytes, 0, contentLength, cancellationToken);
                var content = Encoding.UTF8.GetString(contentBytes);
                OnMessageReceived(content);
                contentLength = 0;
                contentLine = "";
                headerCount = 0;
            }
        }
    }
}
