using System.Collections.Generic;
using System.Threading;
using StreamJsonRpc;
using System.Threading.Tasks;
using System;

namespace EiderCode.LSP;

public enum TraceValue
{
  Off,
  Messages,
  Verbose
}

public record ClientInfo
{
  public required string Name { get; init; }
  public required string Version { get; init; }
}

public record TextDocumentClientCapabilities
{

  /**
  * Capabilities specific to the `textDocument/completion` request.
  */
  public CompletionClientCapabilities? Completion { get; set; }
}


public record ClientCapabilities
{
  /**
  * Text document specific client capabilities.
  */
  public TextDocumentClientCapabilities? TextDocument { get; init; }

}

public record WorkDoneProgressParams
{
  /**
   * An optional token that a server can use to report work done progress.
   */
   // string | integer
  public required string? ProgressToken { get; init; }
}

public record InitializeActionParams
{
  public required int? ProcessId { get; set; }
  public ClientInfo? ClientInfo { get; set; }
  /**
   * The locale the client is currently showing the user interface
   * in. This must not necessarily be the locale of the operating
   * system.
   *
   * Uses IETF language tags as the value's syntax
   * (See https://en.wikipedia.org/wiki/IETF_language_tag)
   *
   * @since 3.16.0
   */
  public  string? Locale { get; set; }
  /**
   * User provided initialization options.
   */
  // InitializationOptions // lsp any?
  public required ClientCapabilities ClientCapabilities { get; init; }
  public TraceValue? Trace { get; set; }
  /**
   * The workspace folders configured in the client when the server starts.
   * This property is only available if the client supports workspace folders.
   * It can be `null` if the client supports workspace folders but none are
   * configured.
   *
   * @since 3.6.0
   */
  public List<string>? WorkspaceFolders { get; set; }
}
