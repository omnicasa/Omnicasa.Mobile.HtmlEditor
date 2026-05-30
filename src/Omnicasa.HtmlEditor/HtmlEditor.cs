namespace Omnicasa.HtmlEditor;

/// <summary>
/// Entry point of the library. Opens a native rich-text HTML editor page
/// (a <c>UIViewController</c> on iOS, an <c>Activity</c> on Android) powered by Quill,
/// and returns the edited HTML when the user taps Save (or a discarded result otherwise).
/// </summary>
public static partial class HtmlEditor
{
    /// <summary>
    /// Opens the editor with the supplied <paramref name="options"/> and waits for the user
    /// to Save or Discard.
    /// </summary>
    /// <returns>A <see cref="HtmlEditorResult"/> describing what the user did.</returns>
    public static Task<HtmlEditorResult> OpenEditorAsync(HtmlEditorOptions? options = null)
        => OpenEditorPlatformAsync(options ?? new HtmlEditorOptions());

    /// <summary>
    /// Convenience overload: opens the editor pre-loaded with <paramref name="initialHtml"/>.
    /// </summary>
    public static Task<HtmlEditorResult> OpenEditorAsync(string initialHtml)
        => OpenEditorAsync(new HtmlEditorOptions { InitialHtml = initialHtml ?? string.Empty });

    // Implemented per platform under Platforms/.
    private static partial Task<HtmlEditorResult> OpenEditorPlatformAsync(HtmlEditorOptions options);
}
