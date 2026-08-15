namespace Omnicasa.HtmlEditor;

/// <summary>
/// A button the caller adds to the editor's toolbar — Translate, Clear, Rewrite with AI, anything
/// the app can do to the text. Tapping it runs
/// <see cref="HtmlEditorOptions.OnAction"/> and applies whatever that returns.
/// </summary>
public sealed class HtmlEditorAction
{
    /// <summary>Gets or sets the id handed back to the handler, so one handler can serve them all.</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the text shown on the button. Always required: it names the action for screen
    /// readers even when <see cref="IconOnly"/> hides it.
    /// </summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets an optional icon, as a complete <c>&lt;svg&gt;</c> element. Draw it with
    /// <c>currentColor</c> and it picks up the button's colour, including while busy.
    /// </summary>
    /// <remarks>
    /// Only shapes survive: script, foreign content and event-handler attributes are stripped
    /// before the icon reaches the page. Sized by the editor, so the svg's own width/height are
    /// ignored — a <c>viewBox</c> is what matters.
    /// </remarks>
    public string? Icon { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether only the icon is shown, with the label kept for
    /// accessibility. Ignored when there is no <see cref="Icon"/> — a button has to show
    /// something.
    /// </summary>
    public bool IconOnly { get; set; }
}

/// <summary>
/// What the user tapped, and the document as it stands.
/// </summary>
public sealed class HtmlEditorActionContext
{
    /// <summary>Gets the <see cref="HtmlEditorAction.Id"/> of the button that was tapped.</summary>
    public string ActionId { get; init; } = string.Empty;

    /// <summary>Gets the current document, merge tags written out as their raw text.</summary>
    public string Html { get; init; } = string.Empty;
}

/// <summary>
/// What the editor should do with a handler's answer.
/// </summary>
public sealed class HtmlEditorActionResult
{
    /// <summary>How the returned html is applied.</summary>
    internal enum ResultKind
    {
        /// <summary>Leave the document as it is.</summary>
        None,

        /// <summary>Replace the whole document.</summary>
        Replace,

        /// <summary>Insert at the caret, replacing the selection if there is one.</summary>
        Insert,
    }

    /// <summary>Gets a result that leaves the document untouched — a cancelled or failed action.</summary>
    public static HtmlEditorActionResult None { get; } =
        new HtmlEditorActionResult(ResultKind.None, string.Empty);

    /// <summary>Gets the kind of change to apply.</summary>
    internal ResultKind Kind { get; }

    /// <summary>Gets the html the change carries.</summary>
    internal string Html { get; }

    private HtmlEditorActionResult(ResultKind kind, string html)
    {
        Kind = kind;
        Html = html ?? string.Empty;
    }

    /// <summary>
    /// Replaces the whole document — a translation, a rewrite, or an empty string to clear it.
    /// </summary>
    /// <param name="html">The new document.</param>
    /// <returns>A replace result.</returns>
    public static HtmlEditorActionResult Replace(string html) =>
        new HtmlEditorActionResult(ResultKind.Replace, html);

    /// <summary>Inserts at the caret, replacing the selection if there is one.</summary>
    /// <param name="html">The html to insert.</param>
    /// <returns>An insert result.</returns>
    public static HtmlEditorActionResult Insert(string html) =>
        new HtmlEditorActionResult(ResultKind.Insert, html);
}
