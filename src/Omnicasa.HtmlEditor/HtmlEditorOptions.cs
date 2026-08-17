namespace Omnicasa.HtmlEditor;

/// <summary>
/// Options that configure a single editor session opened by
/// <see cref="HtmlEditor.OpenEditorAsync(HtmlEditorOptions?)"/>.
/// </summary>
public sealed class HtmlEditorOptions
{
    /// <summary>HTML content to pre-load into the editor. Defaults to empty.</summary>
    public string InitialHtml { get; set; } = string.Empty;

    /// <summary>Title shown in the editor page navigation/title bar.</summary>
    public string Title { get; set; } = "Editor";

    /// <summary>Placeholder text shown while the editor is empty.</summary>
    public string Placeholder { get; set; } = "Start writing…";

    /// <summary>Label for the save action. Defaults to "Save".</summary>
    public string SaveText { get; set; } = "Save";

    /// <summary>Label for the discard action. Defaults to "Discard".</summary>
    public string DiscardText { get; set; } = "Discard";

    /// <summary>
    /// Gets or sets the tags offered by the "insert tag" picker. Empty — the default — hides the
    /// toolbar button entirely, so an editor that has no tags looks exactly as it did before.
    /// </summary>
    public IList<HtmlEditorTag> Tags { get; set; } = new List<HtmlEditorTag>();

    /// <summary>Gets or sets the label of the toolbar button that opens the tag picker.</summary>
    public string TagButtonText { get; set; } = "Insert field";

    /// <summary>Gets or sets the title of the tag picker panel.</summary>
    public string TagPickerTitle { get; set; } = "Insert field";

    /// <summary>Gets or sets the placeholder of the tag picker's search box.</summary>
    public string TagSearchPlaceholder { get; set; } = "Search fields…";

    /// <summary>Gets or sets the text shown when the search matches no tag.</summary>
    public string TagEmptyText { get; set; } = "No fields found";

    /// <summary>
    /// Gets or sets a JavaScript regular expression matching what a tag looks like in this
    /// caller's documents — for example <c>\[\*[^*\]]+\*\]</c>.
    /// </summary>
    /// <remarks>
    /// Without it, only tags the caller listed in <see cref="Tags"/> are recognised, and anything
    /// else in the document stays raw <c>[*LIKE_THIS*]</c> text. A document usually knows tags the
    /// caller does not — one endpoint lists the fields you may insert, another renders fields it
    /// never advertises — and those still have to read as fields rather than as syntax.
    /// </remarks>
    public string? TagPattern { get; set; }

    /// <summary>
    /// Gets or sets extra toolbar buttons the app provides — Translate, Clear, Rewrite, anything.
    /// Each one runs <see cref="OnAction"/>; without a handler they are not rendered, since a
    /// button that cannot do anything is worse than no button.
    /// </summary>
    public IList<HtmlEditorAction> Actions { get; set; } = new List<HtmlEditorAction>();

    /// <summary>
    /// Gets or sets the handler run when an action is tapped. It receives the document as it
    /// stands and returns what to do with the result — replace it, insert at the caret, or
    /// nothing. The editor stays open and shows the button as busy while it runs, so the handler
    /// is free to await a network call.
    /// </summary>
    public Func<HtmlEditorActionContext, Task<HtmlEditorActionResult>>? OnAction { get; set; }
}
