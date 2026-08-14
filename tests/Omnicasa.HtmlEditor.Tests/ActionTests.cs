using System.Text;
using Omnicasa.HtmlEditor;
using Xunit;

namespace Omnicasa.HtmlEditor.Tests;

public class ActionTests
{
    [Fact]
    public void Options_HaveNoActionsByDefault()
    {
        var options = new HtmlEditorOptions();

        Assert.Empty(options.Actions);
        Assert.Null(options.OnAction);
    }

    [Fact]
    public void TagConfig_CarriesActions()
    {
        var json = EditorHtmlBuilder.BuildTagConfig(Options(
            new HtmlEditorAction { Id = "translate", Label = "Translate" },
            new HtmlEditorAction { Id = "clear", Label = "Clear" }));

        Assert.Contains("\"actions\":[{\"id\":\"translate\",\"label\":\"Translate\"", json);
        Assert.Contains("{\"id\":\"clear\",\"label\":\"Clear\"", json);
    }

    [Fact]
    public void TagConfig_WithoutHandler_DropsActions()
    {
        // A button with nothing behind it would just be a dead control.
        var options = new HtmlEditorOptions
        {
            Actions = { new HtmlEditorAction { Id = "translate", Label = "Translate" } },
        };

        var json = EditorHtmlBuilder.BuildTagConfig(options);

        Assert.Contains("\"actions\":[]", json);
    }

    [Fact]
    public void TagConfig_SkipsActionsMissingIdOrLabel()
    {
        var json = EditorHtmlBuilder.BuildTagConfig(Options(
            new HtmlEditorAction { Id = "no-label" },
            new HtmlEditorAction { Label = "No id" },
            new HtmlEditorAction { Id = "ok", Label = "Real" }));

        Assert.DoesNotContain("no-label", json);
        Assert.DoesNotContain("No id", json);
        Assert.Contains("\"id\":\"ok\"", json);
    }

    [Fact]
    public void TagConfig_CarriesTheIcon()
    {
        var json = EditorHtmlBuilder.BuildTagConfig(Options(new HtmlEditorAction
        {
            Id = "clear",
            Label = "Clear",
            Icon = "<svg viewBox=\"0 0 16 16\"><path d=\"M3 4h10\"/></svg>",
            IconOnly = true,
        }));

        Assert.Contains("\\\"0 0 16 16\\\"", json);
        Assert.Contains("\"iconOnly\":true", json);
    }

    [Fact]
    public void TagConfig_WithoutIcon_IsNeverIconOnly()
    {
        // A button with the label hidden and no icon would render as an empty capsule.
        var json = EditorHtmlBuilder.BuildTagConfig(Options(new HtmlEditorAction
        {
            Id = "clear",
            Label = "Clear",
            IconOnly = true,
        }));

        Assert.Contains("\"iconOnly\":false", json);
        Assert.Contains("\"icon\":\"\"", json);
    }

    [Fact]
    public async Task Run_ReplacesWithHandlerHtml()
    {
        var options = Options(new HtmlEditorAction { Id = "translate", Label = "Translate" });
        options.OnAction = context => Task.FromResult(
            HtmlEditorActionResult.Replace($"<p>{context.ActionId}:{context.Html}</p>"));

        var script = await EditorActionRunner.RunAsync(
            options, "{\"id\":\"translate\",\"html\":\"<p>hi</p>\"}");

        Assert.Contains("'replace'", script);
        Assert.Contains(B64("<p>translate:<p>hi</p></p>"), script);
    }

    [Fact]
    public async Task Run_PassesTheDocumentToTheHandler()
    {
        HtmlEditorActionContext? seen = null;
        var options = Options(new HtmlEditorAction { Id = "clear", Label = "Clear" });
        options.OnAction = context =>
        {
            seen = context;
            return Task.FromResult(HtmlEditorActionResult.Replace(string.Empty));
        };

        await EditorActionRunner.RunAsync(options, "{\"id\":\"clear\",\"html\":\"<p>[*Tag*]</p>\"}");

        Assert.Equal("clear", seen!.ActionId);
        Assert.Equal("<p>[*Tag*]</p>", seen.Html);
    }

    [Fact]
    public async Task Run_WithoutHandler_ChangesNothing()
    {
        var script = await EditorActionRunner.RunAsync(
            new HtmlEditorOptions(), "{\"id\":\"x\",\"html\":\"\"}");

        Assert.Contains("'none'", script);
    }

    [Fact]
    public async Task Run_WhenHandlerThrows_ReleasesTheButton()
    {
        // The action must always answer, or the button spins forever.
        var options = Options(new HtmlEditorAction { Id = "boom", Label = "Boom" });
        options.OnAction = _ => throw new InvalidOperationException("no network");

        var script = await EditorActionRunner.RunAsync(options, "{\"id\":\"boom\",\"html\":\"\"}");

        Assert.Contains("__applyAction", script);
        Assert.Contains("'none'", script);
    }

    [Fact]
    public async Task Run_WithMalformedPayload_ChangesNothing()
    {
        var options = Options(new HtmlEditorAction { Id = "x", Label = "X" });
        options.OnAction = _ => Task.FromResult(HtmlEditorActionResult.Replace("<p>never</p>"));

        var script = await EditorActionRunner.RunAsync(options, "not json");

        Assert.Contains("'none'", script);
        Assert.DoesNotContain(B64("<p>never</p>"), script);
    }

    [Fact]
    public async Task Run_InsertCarriesTheInsertKind()
    {
        var options = Options(new HtmlEditorAction { Id = "sig", Label = "Signature" });
        options.OnAction = _ => Task.FromResult(HtmlEditorActionResult.Insert("<b>x</b>"));

        var script = await EditorActionRunner.RunAsync(options, "{\"id\":\"sig\",\"html\":\"\"}");

        Assert.Contains("'insert'", script);
        Assert.Contains(B64("<b>x</b>"), script);
    }

    private static HtmlEditorOptions Options(params HtmlEditorAction[] actions)
    {
        var options = new HtmlEditorOptions
        {
            Actions = actions,
            OnAction = _ => Task.FromResult(HtmlEditorActionResult.None),
        };
        return options;
    }

    private static string B64(string value) =>
        Convert.ToBase64String(Encoding.UTF8.GetBytes(value));
}
