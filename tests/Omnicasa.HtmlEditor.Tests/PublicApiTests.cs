using Omnicasa.HtmlEditor;
using Xunit;

namespace Omnicasa.HtmlEditor.Tests;

public class PublicApiTests
{
    [Fact]
    public void Options_HaveSensibleDefaults()
    {
        var options = new HtmlEditorOptions();

        Assert.Equal(string.Empty, options.InitialHtml);
        Assert.Equal("Editor", options.Title);
        Assert.Equal("Save", options.SaveText);
        Assert.Equal("Discard", options.DiscardText);
        Assert.False(string.IsNullOrEmpty(options.Placeholder));
    }

    [Fact]
    public void Result_CarriesSavedHtml()
    {
        var result = new HtmlEditorResult { Saved = true, Html = "<p>x</p>" };

        Assert.True(result.Saved);
        Assert.Equal("<p>x</p>", result.Html);
    }

    [Fact]
    public void OpenEditor_OnUnsupportedPlatform_ThrowsFast()
    {
        // The platform-neutral net9.0 build has no native page, so it must fail fast.
        Assert.Throws<PlatformNotSupportedException>(() =>
        {
            _ = HtmlEditor.OpenEditorAsync("<p>x</p>");
        });
    }
}
