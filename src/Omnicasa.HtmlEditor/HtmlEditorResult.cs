namespace Omnicasa.HtmlEditor;

/// <summary>
/// The outcome of an editor session.
/// </summary>
public sealed class HtmlEditorResult
{
    /// <summary><c>true</c> if the user tapped Save; <c>false</c> if the user discarded.</summary>
    public bool Saved { get; init; }

    /// <summary>The edited HTML. Only meaningful when <see cref="Saved"/> is <c>true</c>.</summary>
    public string Html { get; init; } = string.Empty;

    /// <summary>Creates a saved result carrying the edited HTML.</summary>
    /// <param name="html">The edited HTML.</param>
    /// <returns>A saved <see cref="HtmlEditorResult"/>.</returns>
    internal static HtmlEditorResult Save(string html) =>
        new HtmlEditorResult { Saved = true, Html = html ?? string.Empty };

    /// <summary>Creates a discarded result.</summary>
    /// <returns>A discarded <see cref="HtmlEditorResult"/>.</returns>
    internal static HtmlEditorResult Discard() =>
        new HtmlEditorResult { Saved = false, Html = string.Empty };
}
