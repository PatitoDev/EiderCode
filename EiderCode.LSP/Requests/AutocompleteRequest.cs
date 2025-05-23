using System.Text.Json.Serialization;
using EiderCode.LSP;

public record AutocompleteParams: IRequestParams
{
  public required DocumentUri TextDocument { get; init; }
  public required Position Position { get; init; }
}

public record AutocompleteResult // extends work donepreogress params and partial result params
{

}

public record CompletionItem {

  /**
   * The label of this completion item.
   *
   * The label property is also by default the text that
   * is inserted when selecting this completion.
   *
   * If label details are provided the label itself should
   * be an unqualified name of the completion item.
   */
  public required string Label { get; init; }

  /**
   * Additional details for the label
   *
   * @since 3.17.0
   */
  public required CompletionItemLabelDetails? LabelDetails { get; init; }


  /**
   * The kind of this completion item. Based of the kind
   * an icon is chosen by the editor. The standardized set
   * of available values is defined in `CompletionItemKind`.
   */
  public required CompletionItemKind? Kind { get; init; }

  /**
   * Tags for this completion item.
   *
   * @since 3.15.0
   */
  public required Tag[]? Tags { get; init; }

  /**
   * A human-readable string with additional information
   * about this item, like type or symbol information.
   */
  public required string? Detail { get; init; }

  /**
   * A human-readable string that represents a doc-comment.
   */
  //documentation?: string | MarkupContent;

  /**
   * Indicates if this item is deprecated.
   *
   * @deprecated Use `tags` instead if supported.
   */
  public required bool? Deprecated { get; init; }

  /**
   * Select this item when showing.
   *
   * *Note* that only one completion item can be selected and that the
   * tool / client decides which item that is. The rule is that the *first*
   * item of those that match best is selected.
   */
  public required bool? Preselect { get; init; }

  /**
   * A string that should be used when comparing this item
   * with other items. When omitted the label is used
   * as the sort text for this item.
   */
  public required string? SortText { get; init; }

  /**
   * A string that should be used when filtering a set of
   * completion items. When omitted the label is used as the
   * filter text for this item.
   */
  public required string? FilterText { get; init; }

  /**
   * A string that should be inserted into a document when selecting
   * this completion. When omitted the label is used as the insert text
   * for this item.
   *
   * The `insertText` is subject to interpretation by the client side.
   * Some tools might not take the string literally. For example
   * VS Code when code complete is requested in this example
   * `con<cursor position>` and a completion item with an `insertText` of
   * `console` is provided it will only insert `sole`. Therefore it is
   * recommended to use `textEdit` instead since it avoids additional client
   * side interpretation.
   */
  public required string? InsertText { get; init; }

  /**
   * The format of the insert text. The format applies to both the
   * `insertText` property and the `newText` property of a provided
   * `textEdit`. If omitted defaults to `InsertTextFormat.PlainText`.
   *
   * Please note that the insertTextFormat doesn't apply to
   * `additionalTextEdits`.
   */
  public required InsertTextFormat? InsertTextFormat { get; init; }

  /**
   * How whitespace and indentation is handled during completion
   * item insertion. If not provided the client's default value depends on
   * the `textDocument.completion.insertTextMode` client capability.
   *
   * @since 3.16.0
   * @since 3.17.0 - support for `textDocument.completion.insertTextMode`
   */
  public required InsertTextMode? InsertTextMode { get; init; }

  /**
   * An edit which is applied to a document when selecting this completion.
   * When an edit is provided the value of `insertText` is ignored.
   *
   * *Note:* The range of the edit must be a single line range and it must
   * contain the position at which completion has been requested.
   *
   * Most editors support two different operations when accepting a completion
   * item. One is to insert a completion text and the other is to replace an
   * existing text with a completion text. Since this can usually not be
   * predetermined by a server it can report both ranges. Clients need to
   * signal support for `InsertReplaceEdit`s via the
   * `textDocument.completion.completionItem.insertReplaceSupport` client
   * capability property.
   *
   * *Note 1:* The text edit's range as well as both ranges from an insert
   * replace edit must be a [single line] and they must contain the position
   * at which completion has been requested.
   * *Note 2:* If an `InsertReplaceEdit` is returned the edit's insert range
   * must be a prefix of the edit's replace range, that means it must be
   * contained and starting at the same position.
   *
   * @since 3.16.0 additional type `InsertReplaceEdit`
   */
  //textEdit?: TextEdit | InsertReplaceEdit;

  /**
   * The edit text used if the completion item is part of a CompletionList and
   * CompletionList defines an item default for the text edit range.
   *
   * Clients will only honor this property if they opt into completion list
   * item defaults using the capability `completionList.itemDefaults`.
   *
   * If not provided and a list's default range is provided the label
   * property is used as a text.
   *
   * @since 3.17.0
   */
  public required string? TextEditText { get; init; }

  /**
   * An optional array of additional text edits that are applied when
   * selecting this completion. Edits must not overlap (including the same
   * insert position) with the main edit nor with themselves.
   *
   * Additional text edits should be used to change text unrelated to the
   * current cursor position (for example adding an import statement at the
   * top of the file if the completion item will insert an unqualified type).
   */
  public required TextEdit[]? AdditionalTextEdits { get; init; }

  /**
   * An optional set of characters that when pressed while this completion is
   * active will accept it first and then type that character. *Note* that all
   * commit characters should have `length=1` and that superfluous characters
   * will be ignored.
   */
  public required string[]? CommitCharacters { get; init; }

  /**
   * An optional command that is executed *after* inserting this completion.
   * *Note* that additional modifications to the current document should be
   * described with the additionalTextEdits-property.
   */
  public required LspCommand? Command { get; init; }

  /**
   * A data entry field that is preserved on a completion item between
   * a completion and a completion resolve request.
   */
  //data?: LSPAny;
}

public record LspCommand {
  /**
   * Title of the command, like `save`.
   */
  public required string Title { get; init; }
  /**
   * The identifier of the actual command handler.
   */
  public required string Command { get; init; }
  /**
   * Arguments that the command handler should be
   * invoked with.
   */
   // lspany[]
  public required object[]? Arguments { get; init; }
}


public record TextEdit {
  /**
   * The range of the text document to be manipulated. To insert
   * text into a document create a range where start === end.
   */
  public required Range Range { get; init; }

  /**
   * The string to be inserted. For delete operations use an
   * empty string.
   */
  public required string NewText { get; init; }
}


/**
 * Additional details for a completion item label.
 *
 * @since 3.17.0
 */
public record CompletionItemLabelDetails {

  /**
   * An optional string which is rendered less prominently directly after
   * {@link CompletionItem.label label}, without any spacing. Should be
   * used for function signatures or type annotations.
   */
  public required string? Detail { get; init; }

  /**
   * An optional string which is rendered less prominently after
   * {@link CompletionItemLabelDetails.detail}. Should be used for fully qualified
   * names or file path.
   */
  public required string? Description { get; init; }
}

/**
 * A special text edit to provide an insert and a replace operation.
 *
 * @since 3.16.0
 */
public record InsertReplaceEdit
{
  /**
  * The string to be inserted.
  */
  public required string NewText { get; init; }

  /**
   * The range if the insert is requested
   */
  public required Range Insert { get; init; }

  /**
   * The range if the replace is requested.
   */
  public required Range Replace { get; init; }
}


[JsonConverter(typeof(JsonNumberEnumConverter<CompletionItemKind>))]
public enum CompletionItemKind {
  Text = 1,
  Method = 2,
  Function = 3,
  Constructor = 4,
  Field = 5,
  Variable = 6,
  Class = 7,
  Interface = 8,
  Module = 9,
  Property = 10,
  Unit = 11,
  Value = 12,
  Enum = 13,
  Keyword = 14,
  Snippet = 15,
  Color = 16,
  File = 17,
  Reference = 18,
  Folder = 19,
  EnumMember = 20,
  Constant = 21,
  Struct = 22,
  Event = 23,
  Operator = 24,
  TypeParameter = 25,
}