using System.Text.Json.Serialization;

namespace EiderCode.LSP;

/**
 * Completion item tags are extra annotations that tweak the rendering of a
 * completion item.
 *
 * @since 3.15.0
 */
 [JsonConverter(typeof(JsonNumberEnumConverter<Tag>))]
public enum Tag
{
  /**
  * Render a completion as obsolete, usually using a strike-out.
  */
  Deprecated = 1,
}