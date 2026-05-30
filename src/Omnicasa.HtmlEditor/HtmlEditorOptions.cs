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
}
