using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using EiderCode.LSP.RPC;

namespace EiderCode.LSP;


public class LSPClient : IDisposable
{

    private Process? _process;
    private JsonRPCClient? _rpcClient;
    private CancellationTokenSource? _readCancellationTokenSource;

    public LSPClient()
    {
    }

    public void StartClient(string workspacePath)
    {
        //var pathToLspExe = "D:\\lsps\\quick-lint-js\\bin\\quick-lint-js.exe";
        var pathToLspExe = "D:\\lsps\\omnisharp-win-x64\\OmniSharp.exe";
        System.Console.WriteLine($"Started LSP Client: {pathToLspExe}");

        _readCancellationTokenSource?.Cancel();
        _process?.Kill();
        _process = new Process();
        _readCancellationTokenSource = new();

        _process.StartInfo.Arguments = "-lsp";
        //_process.StartInfo.Arguments = "--lsp-server";
        _process.StartInfo.CreateNoWindow = true;
        _process.StartInfo.FileName = pathToLspExe;
        _process.StartInfo.UseShellExecute = false;
       // _process.StartInfo.WorkingDirectory = "D:/";
        _process.StartInfo.WorkingDirectory = workspacePath;
        _process.StartInfo.RedirectStandardInput = true;
        _process.StartInfo.RedirectStandardOutput = true;
        _process.StartInfo.RedirectStandardError = true;

        _process.Start();

        var stdOut = _process.StandardOutput.BaseStream;
        var stdIn = _process.StandardInput;

        var readCancellationToken = _readCancellationTokenSource.Token;

        Task.Delay(500);

        _rpcClient = new(stdIn, stdOut);
        _rpcClient.StartReading();

        Task.Run(async () => {
          await Initialize();
        });
    }

    public void GetAutocomplete(string documentPath, int line, int character)
    {
      if (_rpcClient == null) return;

      var uri = new Uri(documentPath);
      var autocompleteParams = new AutocompleteParams()
      {
        TextDocument = new (){
          Uri = uri.AbsoluteUri
        },
        Position = new (){
          Line = line,
          Character = character
        },
      };

      _rpcClient
        .RequestMethod<object, AutocompleteParams>("textDocument/completion", autocompleteParams)
        .Wait();
    }

    public void Dispose()
    {
      _readCancellationTokenSource?.Cancel();
      _rpcClient?.StopReading();
      _process?.Kill();
    }

    public async Task Initialize()
    {
      if (_rpcClient == null) return;


      /*
      var paramObj = new InitializeActionParams(){
        ProcessId = Godot.OS.GetProcessId(),
        ClientCapabilities = new()
        {
        }
      };

      var response = await _rpcClient.RequestMethod<object, InitializeActionParams>("initialize", paramObj);
      */

      _rpcClient.SendJsonString(
@"
{
  ""jsonrpc"": ""2.0"",
  ""id"": ""init-id"",
  ""method"": ""initialize"",
  ""params"": {
        ""workDoneToken"": null,
        ""processId"" : null,
        ""capabilities"": {
            ""workspace"": null,
            ""textDocument"": {
              ""completion"": {
              ""dynamicRegistration"": true,
              ""completionItem"": {
              ""snippetSupport"": true,
              ""commitCharactersSupport"": true,
              ""documentationFormat"": [""markdown""],
              ""deprecatedSupport"": true,
              ""preselectSupport"": false,
              ""tagSupport"": {
                  ""valueSet"": [1]
              },
              ""insertReplaceSupport"": true,
              ""resolveSupport"": {
                 ""properties"": []
               },
               ""insertTextModeSupport"": {
                 ""valueSet"": [2]
               },
               ""labelDetailsSupport"": true
             },
             ""completionItemKind"": {
               ""valueSet"": [1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,17,18,19,20,21,22,23,24,25]
             },
             ""contextSupport"": true,
             ""insertTextMode"": 2,
             ""completionList"":null
           }
          },
          ""notebookDocument"": null,
          ""window"" : null,
          ""general"": null,
          ""experimental"": null
        }
  }
}
");

}
}
