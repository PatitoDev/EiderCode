namespace EiderCode.LSP;


public enum MarkupKind
{
    PlainText, // plaintext
    Markdown // markdown
}

public record ResolveSupport
{
    /**
    * The properties that a client can resolve lazily.
    */
    public required string[] Properties { get; set; }
}

public record CompletitionItem
{
    /**
     * Client supports snippets as insert text.
     *
     * A snippet can define tab stops and placeholders with `$1`, `$2`
     * and `${3:foo}`. `$0` defines the final tab stop, it defaults to
     * the end of the snippet. Placeholders with equal identifiers are
     * linked, that is typing in one will update others too.
     */
    public bool? SnippetSupport { get; set; }

    /**
     * Client supports commit characters on a completion item.
     */
    public bool? CommitCharactersSupport { get; set; }

    /**
     * Client supports the follow content formats for the documentation
     * property. The order describes the preferred format of the client.
     */
    public MarkupKind[]? DocumentationFormat { get; set; }

    /**
     * Client supports the deprecated property on a completion item.
     */
    public bool? DeprecatedSupport { get; set; }

    /**
     * Client supports the preselect property on a completion item.
     */
    public bool? PreselectSupport { get; set; }

    /**
     * Client supports the tag property on a completion item. Clients
     * supporting tags have to handle unknown tags gracefully. Clients
     * especially need to preserve unknown tags when sending a completion
     * item back to the server in a resolve call.
     *
     * @since 3.15.0
     */
    public ValueSetArray<int>? TagSupport { get; set; }

    /**
     * Client supports insert replace edit to control different behavior if
     * a completion item is inserted in the text or should replace text.
     *
     * @since 3.16.0
     */
    public bool? InsertReplaceSupport { get; set; }

    /**
     * Indicates which properties a client can resolve lazily on a
     * completion item. Before version 3.16.0 only the predefined properties
     * `documentation` and `detail` could be resolved lazily.
     *
     * @since 3.16.0
     */
    public ResolveSupport? ResolveSupport { get; set; }

    /**
     * The client supports the `insertTextMode` property on
     * a completion item to override the whitespace handling mode
     * as defined by the client (see `insertTextMode`).
     *
     * @since 3.16.0
     */
    public ValueSetArray<int>? InsertTextModeSupport { get; set; }

    /**
     * The client has support for completion item label
     * details (see also `CompletionItemLabelDetails`).
     *
     * @since 3.17.0
     */
    public bool? LabelDetailsSupport { get; set; }
}

public record CompletionList
{
    /**
   * The client supports the following itemDefaults on
   * a completion list.
   *
   * The value lists the supported property names of the
   * `CompletionList.itemDefaults` object. If omitted
   * no properties are supported.
   *
   * @since 3.17.0
   */
    public string[]? ItemDefaults { get; set; }
}

public record CompletionClientCapabilities
{

    /**
     * Whether completion supports dynamic registration.
     */
    public bool? DynamicRegistration { get; set; }

    /**
     * The client supports the following `CompletionItem` specific
     * capabilities.
     */
    public CompletitionItem? CompletitionItem { get; set; }

    /**
    * The completion item kind values the client supports. When this
    * property exists the client also guarantees that it will
    * handle values outside its set gracefully and falls back
    * to a default value when unknown.
    *
    * If this property is not present the client only supports
    * the completion items kinds from `Text` to `Reference` as defined in
    * the initial version of the protocol.
    */
    public ValueSetArray<int>? CompletionItemKind { get; set; }

    /**
     * The client supports to send additional context information for a
     * `textDocument/completion` request.
     */
    public bool? ContextSupport { get; set; }

    /**
     * The client's default when the completion item doesn't provide a
     * `insertTextMode` property.
     *
     * @since 3.17.0
     */
    public int? InsertTextMode { get; set; }

    /**
     * The client supports the following `CompletionList` specific
     * capabilities.
     *
     * @since 3.17.0
     */
    public CompletionList? CompletionList { get; set; }
}
