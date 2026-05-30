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

        // Replace the small tokens first, then inline the large blobs, so the blobs are
        // never scanned for token text.
        return template
            .Replace("__PLACEHOLDER__", JsEscape(options.Placeholder))
            .Replace("__INITIAL_B64__", initialB64)
            .Replace("/*__QUILL_CSS__*/", quillCss)
            .Replace("/*__QUILL_JS__*/", quillJs);
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
