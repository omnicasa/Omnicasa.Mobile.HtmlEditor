namespace Omnicasa.HtmlEditor;

/// <summary>
/// A tag the user can drop into the document from the editor's tag picker — typically a merge
/// field such as <c>[*ContactName*]</c> that a server resolves when the message is rendered.
/// </summary>
/// <remarks>
/// The library never fetches tags: the caller owns both the list and the syntax, so the same
/// editor serves merge fields, shortcodes or any other placeholder scheme.
/// </remarks>
public sealed class HtmlEditorTag
{
    /// <summary>Gets or sets the text shown in the picker and on the inserted chip.</summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the tag's identity. It is shown beside the label in the picker, so two
    /// same-named tags from different groups can be told apart before inserting.
    /// </summary>
    public string Value { get; set; } = string.Empty;

    /// <summary>Gets or sets the optional heading this tag is listed under in the picker.</summary>
    public string? Group { get; set; }

    /// <summary>
    /// Gets or sets the literal text written into the document. Defaults to <see cref="Value"/>,
    /// so a caller whose placeholder syntax is the raw value can leave it unset.
    /// </summary>
    public string? InsertText { get; set; }

    /// <summary>Gets the text actually written into the document.</summary>
    internal string EffectiveText => string.IsNullOrEmpty(InsertText) ? Value : InsertText!;

    /// <summary>Gets the text actually shown on the chip and in the picker row.</summary>
    internal string EffectiveLabel => string.IsNullOrEmpty(Label) ? EffectiveText : Label;
}
