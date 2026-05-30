#if !ANDROID && !IOS
namespace Omnicasa.HtmlEditor;

/// <summary>
/// Fallback implementation used by the platform-neutral <c>net9.0</c> target. The editor
/// requires a native page, so it is only functional on Android and iOS; this exists so the
/// shared logic (for example <see cref="EditorHtmlBuilder"/>) can be referenced and unit tested.
/// </summary>
public static partial class HtmlEditor
{
    private static partial Task<HtmlEditorResult> OpenEditorPlatformAsync(HtmlEditorOptions options)
    {
        _ = options;
        throw new PlatformNotSupportedException(
            "Omnicasa.HtmlEditor only opens an editor on Android and iOS.");
    }
}
#endif
