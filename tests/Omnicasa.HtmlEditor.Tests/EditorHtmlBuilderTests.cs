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

    [Fact]
    public void Build_ReplacesTagToken()
    {
        var html = EditorHtmlBuilder.Build(new HtmlEditorOptions());

        Assert.DoesNotContain("__TAGS_B64__", html);
    }

    [Fact]
    public void Build_EncodesTagConfigAsBase64()
    {
        var options = new HtmlEditorOptions
        {
            Tags =
            {
                new HtmlEditorTag
                {
                    Label = "Contact name",
                    Value = "ContactName",
                    InsertText = "[*ContactName*]",
                },
            },
        };
        var expected = Convert.ToBase64String(
            Encoding.UTF8.GetBytes(EditorHtmlBuilder.BuildTagConfig(options)));

        var html = EditorHtmlBuilder.Build(options);

        Assert.Contains(expected, html);
    }

    [Fact]
    public void Build_DoesNotInjectRawTagLabels()
    {
        // A label is caller-supplied text, so it must never reach the page as source.
        var html = EditorHtmlBuilder.Build(new HtmlEditorOptions
        {
            Tags = { new HtmlEditorTag { Label = "</script><script>alert(2)</script>", Value = "X" } },
        });

        Assert.DoesNotContain("alert(2)", html);
    }

    [Fact]
    public void TagConfig_CarriesLabelsAndStrings()
    {
        var json = EditorHtmlBuilder.BuildTagConfig(new HtmlEditorOptions
        {
            TagButtonText = "Veld",
            TagPickerTitle = "Veld invoegen",
            TagSearchPlaceholder = "Zoeken",
            TagEmptyText = "Niets",
            Tags =
            {
                new HtmlEditorTag
                {
                    Label = "Contact name",
                    Value = "ContactName",
                    Group = "Person",
                    InsertText = "[*ContactName*]",
                },
            },
        });

        Assert.Contains("\"buttonText\":\"Veld\"", json);
        Assert.Contains("\"title\":\"Veld invoegen\"", json);
        Assert.Contains("\"searchPlaceholder\":\"Zoeken\"", json);
        Assert.Contains("\"emptyText\":\"Niets\"", json);
        Assert.Contains("\"label\":\"Contact name\"", json);
        Assert.Contains("\"value\":\"ContactName\"", json);
        Assert.Contains("\"text\":\"[*ContactName*]\"", json);
        Assert.Contains("\"group\":\"Person\"", json);
    }

    [Fact]
    public void TagConfig_WithoutInsertText_FallsBackToValue()
    {
        var json = EditorHtmlBuilder.BuildTagConfig(new HtmlEditorOptions
        {
            Tags = { new HtmlEditorTag { Label = "Name", Value = "{{name}}" } },
        });

        Assert.Contains("\"text\":\"{{name}}\"", json);
    }

    [Fact]
    public void TagConfig_WithoutLabel_FallsBackToInsertedText()
    {
        var json = EditorHtmlBuilder.BuildTagConfig(new HtmlEditorOptions
        {
            Tags = { new HtmlEditorTag { Value = "ContactName", InsertText = "[*ContactName*]" } },
        });

        Assert.Contains("\"label\":\"[*ContactName*]\"", json);
    }

    [Fact]
    public void TagConfig_SkipsTagsWithNothingToInsert()
    {
        var json = EditorHtmlBuilder.BuildTagConfig(new HtmlEditorOptions
        {
            Tags =
            {
                new HtmlEditorTag { Label = "Empty" },
                new HtmlEditorTag { Label = "Real", Value = "R" },
            },
        });

        Assert.DoesNotContain("Empty", json);
        Assert.Contains("Real", json);
    }

    [Fact]
    public void TagConfig_EscapesJsonSpecials()
    {
        var json = EditorHtmlBuilder.BuildTagConfig(new HtmlEditorOptions
        {
            Tags = { new HtmlEditorTag { Label = "He said \"hi\"\\\n", Value = "V" } },
        });

        Assert.Contains("\"label\":\"He said \\\"hi\\\"\\\\\\n\"", json);
    }

    [Fact]
    public void TagConfig_CarriesThePreviewValue()
    {
        var json = EditorHtmlBuilder.BuildTagConfig(new HtmlEditorOptions
        {
            Tags =
            {
                new HtmlEditorTag
                {
                    Label = "Contact name",
                    Value = "ContactName",
                    InsertText = "[*ContactName*]",
                    PreviewValue = "John Doe",
                },
            },
        });

        Assert.Contains("\"preview\":\"John Doe\"", json);

        // The document still carries the token: a preview value is shown, never saved.
        Assert.Contains("\"text\":\"[*ContactName*]\"", json);
    }

    [Fact]
    public void TagConfig_WithoutPreviewValue_SendsAnEmptyOne()
    {
        var json = EditorHtmlBuilder.BuildTagConfig(new HtmlEditorOptions
        {
            Tags = { new HtmlEditorTag { Label = "Contact name", Value = "ContactName" } },
        });

        Assert.Contains("\"preview\":\"\"", json);
    }

    [Fact]
    public void TagConfig_WithNoTags_IsAnEmptyList()
    {
        var json = EditorHtmlBuilder.BuildTagConfig(new HtmlEditorOptions());

        Assert.Contains("\"tags\":[]", json);
    }
}
