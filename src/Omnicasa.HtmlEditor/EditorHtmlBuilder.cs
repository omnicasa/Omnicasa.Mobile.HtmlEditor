using System.Globalization;
using System.Reflection;
using System.Text;

namespace Omnicasa.HtmlEditor;

/// <summary>
/// Builds the self-contained editor HTML document by inlining the bundled Quill assets
/// together with the user's options. The produced string has no external dependencies,
/// so it works fully offline inside a WebView / WKWebView.
/// </summary>
internal static class EditorHtmlBuilder
{
    private static string? quillJs;
    private static string? quillCss;
    private static string? template;

    /// <summary>Builds the self-contained editor HTML document for the given options.</summary>
    /// <param name="options">The editor configuration.</param>
    /// <returns>A complete HTML document with Quill and the user's content inlined.</returns>
    public static string Build(HtmlEditorOptions options)
    {
        var asm = typeof(EditorHtmlBuilder).Assembly;
        template ??= ReadResource(asm, "editor.html");
        quillCss ??= ReadResource(asm, "quill.snow.css");
        quillJs ??= ReadResource(asm, "quill.js");

        var initialB64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(options.InitialHtml ?? string.Empty));

        // The tag picker's whole configuration travels as one base64 JSON blob: labels come from
        // the caller, and a quoted-string token would put their apostrophes and angle brackets in
        // the page's source. Base64 keeps them data.
        var tagsB64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(BuildTagConfig(options)));

        // Replace the small tokens first, then inline the large blobs, so the blobs are
        // never scanned for token text.
        return template
            .Replace("__PLACEHOLDER__", JsEscape(options.Placeholder))
            .Replace("__INITIAL_B64__", initialB64)
            .Replace("__TAGS_B64__", tagsB64)
            .Replace("/*__QUILL_CSS__*/", quillCss)
            .Replace("/*__QUILL_JS__*/", quillJs);
    }

    /// <summary>Serializes the tag picker's strings and tags as JSON.</summary>
    /// <param name="options">The editor configuration.</param>
    /// <returns>A JSON object the editor script parses at start-up.</returns>
    /// <remarks>
    /// Hand-written rather than <c>JsonSerializer</c>-driven: reflection-based serialization of a
    /// public type is what the trimmer warns about, and the shape here is three strings and a list.
    /// </remarks>
    internal static string BuildTagConfig(HtmlEditorOptions options)
    {
        var sb = new StringBuilder(256);
        sb.Append("{\"buttonText\":");
        AppendJsonString(sb, options.TagButtonText);
        sb.Append(",\"title\":");
        AppendJsonString(sb, options.TagPickerTitle);
        sb.Append(",\"searchPlaceholder\":");
        AppendJsonString(sb, options.TagSearchPlaceholder);
        sb.Append(",\"emptyText\":");
        AppendJsonString(sb, options.TagEmptyText);
        sb.Append(",\"tags\":[");

        var first = true;
        foreach (var tag in options.Tags ?? Array.Empty<HtmlEditorTag>())
        {
            // A tag with nothing to insert has no behaviour, and it would render as an empty row.
            if (tag is null || string.IsNullOrEmpty(tag.EffectiveText))
            {
                continue;
            }

            if (!first)
            {
                sb.Append(',');
            }

            first = false;

            sb.Append("{\"label\":");
            AppendJsonString(sb, tag.EffectiveLabel);
            sb.Append(",\"value\":");
            AppendJsonString(sb, tag.Value);
            sb.Append(",\"text\":");
            AppendJsonString(sb, tag.EffectiveText);
            sb.Append(",\"group\":");
            AppendJsonString(sb, tag.Group ?? string.Empty);
            sb.Append(",\"preview\":");
            AppendJsonString(sb, tag.PreviewValue ?? string.Empty);
            sb.Append('}');
        }

        sb.Append("],\"actions\":[");

        // No handler means no way to answer a tap, so the buttons are dropped rather than shown
        // dead. The list is the caller's, in the caller's order.
        var firstAction = true;
        foreach (var action in options.OnAction is null
            ? Array.Empty<HtmlEditorAction>()
            : options.Actions ?? Array.Empty<HtmlEditorAction>())
        {
            if (action is null || string.IsNullOrEmpty(action.Id) || string.IsNullOrEmpty(action.Label))
            {
                continue;
            }

            if (!firstAction)
            {
                sb.Append(',');
            }

            firstAction = false;

            sb.Append("{\"id\":");
            AppendJsonString(sb, action.Id);
            sb.Append(",\"label\":");
            AppendJsonString(sb, action.Label);
            sb.Append(",\"icon\":");
            AppendJsonString(sb, action.Icon ?? string.Empty);

            // Only meaningful with an icon: a button showing nothing at all is not a button.
            sb.Append(",\"iconOnly\":");
            sb.Append(action.IconOnly && !string.IsNullOrEmpty(action.Icon) ? "true" : "false");
            sb.Append('}');
        }

        sb.Append("]}");
        return sb.ToString();
    }

    /// <summary>Appends a JSON-escaped string literal, quotes included.</summary>
    private static void AppendJsonString(StringBuilder sb, string? value)
    {
        sb.Append('"');
        foreach (var c in value ?? string.Empty)
        {
            switch (c)
            {
                case '"': sb.Append("\\\""); break;
                case '\\': sb.Append("\\\\"); break;
                case '\b': sb.Append("\\b"); break;
                case '\f': sb.Append("\\f"); break;
                case '\n': sb.Append("\\n"); break;
                case '\r': sb.Append("\\r"); break;
                case '\t': sb.Append("\\t"); break;
                default:
                    if (c < ' ')
                    {
                        sb.Append("\\u").Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
                    }
                    else
                    {
                        sb.Append(c);
                    }

                    break;
            }
        }

        sb.Append('"');
    }

    private static string ReadResource(Assembly asm, string logicalName)
    {
        using var stream = asm.GetManifestResourceStream(logicalName)
            ?? throw new InvalidOperationException(
                $"Embedded resource '{logicalName}' was not found. Available: " +
                string.Join(", ", asm.GetManifestResourceNames()));
        using var reader = new StreamReader(stream, Encoding.UTF8);
        return reader.ReadToEnd();
    }

    /// <summary>Escapes a string so it is safe inside a single-quoted JavaScript literal.</summary>
    private static string JsEscape(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        var sb = new StringBuilder(value!.Length + 8);
        foreach (var c in value)
        {
            switch (c)
            {
                case '\\': sb.Append("\\\\"); break;
                case '\'': sb.Append("\\'"); break;
                case '"': sb.Append("\\\""); break;
                case '\n': sb.Append("\\n"); break;
                case '\r': sb.Append("\\r"); break;
                case '\t': sb.Append("\\t"); break;
                case '<': sb.Append("\\u003c"); break;
                case '>': sb.Append("\\u003e"); break;
                default: sb.Append(c); break;
            }
        }

        return sb.ToString();
    }
}
