

namespace EiderCode.LSP.RPC;


public record RpcError
{
    public required int Code;
    public required string Message;
    // data? any
}

public abstract record RpcMessageBase
{
    public required string Jsonrpc { get; init; }
}

public record ResponseOrNotificationMessage: RpcMessageBase
{
  public required string? Id { get; init; }
}

public record RpcResponseMessage<T>: RpcMessageBase
{
    // string | int | null
    public required string Id { get; init; }
    public T? Result { get; init; }
    public RpcError? Error { get; init; }
}

public record RpcNotification<T>: RpcMessageBase
{
  public required string Method { get; init; }
  // can be array or object
  public required T? Params { get; init; }
}

public record RpcRequestMsg<T>: RpcMessageBase
{
    // string | number
    public required string Id { get; init; }
    public required string Method { get; init; }
    public required T? Params { get; init; }
}

// actual message content

// Cancel request notification
// $/cancelRequest
public record CancelParams
{
  public required string Id { get; init; }
}

// Progress
// $/progress
public record ProgressParams<T>
{
  // string | number - different to id
  public required string Token { get; init; }
  public required T Value { get; init; }
}

// errors
public static class RpcErrorCodes
{
  public const int ParseError     = -32700;
  public const int InvalidRequest = -32600;
  public const int MethodNotFound = -32601;
  public const int InvalidParams  = -32602;
  public const int InternalError  = -32603;

  /* Invalid range but here for backwards compatibility */
  public const int ServerNotInitialized = -32002;
  public const int UnknownErrorCode     = -32001;


  /**
   * A request failed but it was syntactically correct, e.g the
   * method name was known and the parameters were valid. The error
   * message should contain human readable information about why
   * the request failed.
   *
   * @since 3.17.0
   */
  public const int RequestFailed = -32803;

  /**
   * The server cancelled the request. This error code should
   * only be used for requests that explicitly support being
   * server cancellable.
   *
   * @since 3.17.0
   */
  public const int ServerCancelled = -32802;

  /**
   * The server detected that the content of a document got
   * modified outside normal conditions. A server should
   * NOT send this error code if it detects a content change
   * in its unprocessed messages. The result even computed
   * on an older state might still be useful for the client.
   *
   * If a client decides that a result is not of any use anymore
   * the client should cancel the request.
   */
  public const int ContentModified = -32801;

  /**
   * The client has canceled a request and a server has detected
   * the cancel.
   */
  public const int RequestCancelled = -32800;
}