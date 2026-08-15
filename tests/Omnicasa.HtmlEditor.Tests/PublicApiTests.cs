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
    public void Options_HaveNoTagsByDefault()
    {
        // The tag picker is opt-in: an editor opened without tags must look exactly as it did
        // before the feature existed.
        var options = new HtmlEditorOptions();

        Assert.Empty(options.Tags);
        Assert.False(string.IsNullOrEmpty(options.TagButtonText));
        Assert.False(string.IsNullOrEmpty(options.TagPickerTitle));
        Assert.False(string.IsNullOrEmpty(options.TagSearchPlaceholder));
        Assert.False(string.IsNullOrEmpty(options.TagEmptyText));
    }

    [Fact]
    public void Tag_FallsBackToValueAndInsertedText()
    {
        var bare = new HtmlEditorTag { Value = "ContactName" };
        var tagged = new HtmlEditorTag
        {
            Label = "Contact",
            Value = "ContactName",
            InsertText = "[*ContactName*]",
        };

        Assert.Equal("ContactName", bare.EffectiveText);
        Assert.Equal("ContactName", bare.EffectiveLabel);
        Assert.Equal("[*ContactName*]", tagged.EffectiveText);
        Assert.Equal("Contact", tagged.EffectiveLabel);
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
