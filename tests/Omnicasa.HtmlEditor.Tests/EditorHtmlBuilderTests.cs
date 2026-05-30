using System.Text;
using Omnicasa.HtmlEditor;
using Xunit;

namespace Omnicasa.HtmlEditor.Tests;

public class EditorHtmlBuilderTests
{
    [Fact]
    public void Build_Default_ProducesSelfContainedDocument()
    {
        var html = EditorHtmlBuilder.Build(new HtmlEditorOptions());

        Assert.StartsWith("<!DOCTYPE html>", html);
        Assert.Contains("Quill", html);       // Quill JS was inlined.
        Assert.Contains(".ql-editor", html);  // Quill CSS was inlined.
    }

    [Fact]
    public void Build_ReplacesAllTokens()
    {
        var html = EditorHtmlBuilder.Build(new HtmlEditorOptions());

        Assert.DoesNotContain("/*__QUILL_JS__*/", html);
        Assert.DoesNotContain("/*__QUILL_CSS__*/", html);
        Assert.DoesNotContain("__INITIAL_B64__", html);
        Assert.DoesNotContain("__PLACEHOLDER__", html);
    }

    [Fact]
    public void Build_EncodesInitialHtmlAsBase64()
    {
        var content = "<p>Hello <b>world</b></p>";
        var expected = Convert.ToBase64String(Encoding.UTF8.GetBytes(content));

        var html = EditorHtmlBuilder.Build(new HtmlEditorOptions { InitialHtml = content });

        Assert.Contains(expected, html);
    }

    [Fact]
    public void Build_DoesNotInjectRawInitialHtml()
    {
        // A script payload in the content must not appear verbatim (it is base64 encoded).
        var html = EditorHtmlBuilder.Build(new HtmlEditorOptions
        {
            InitialHtml = "</script><script>alert(1)</script>",
        });

        Assert.DoesNotContain("alert(1)", html);
    }

    [Fact]
    public void Build_EscapesPlaceholderQuotes()
    {
        var html = EditorHtmlBuilder.Build(new HtmlEditorOptions { Placeholder = "O'Brien \"x\"" });

        Assert.Contains("O\\'Brien", html);
        Assert.Contains("\\\"x\\\"", html);
    }
}
