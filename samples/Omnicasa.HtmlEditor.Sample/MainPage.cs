namespace Omnicasa.HtmlEditor.Sample;

public class MainPage : ContentPage
{
    private const string EmptyPlaceholder = "(no content yet)";

    // Drawn with currentColor, so they follow the button — including while it is busy.
    private const string SvgOpen =
        "<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 16 16\">";

    private const string TranslateIcon =
        SvgOpen +
        "<path d=\"M2 4h8M6 2v2M4 4c0 4 2 6 5 7M8 4c0 3-3 5-6 6\" fill=\"none\" " +
        "stroke=\"currentColor\" stroke-width=\"1.5\" stroke-linecap=\"round\"/></svg>";

    private const string TrashIcon =
        SvgOpen +
        "<path d=\"M3 4h10M6.5 4V2.5h3V4M5 4l.7 9h4.6L11 4\" fill=\"none\" " +
        "stroke=\"currentColor\" stroke-width=\"1.4\" stroke-linecap=\"round\" " +
        "stroke-linejoin=\"round\"/></svg>";

    // A stand-in for the merge fields a real caller fetches from its own backend.
    private static readonly HtmlEditorTag[] SampleTags =
    {
        Tag("Contact name", "ContactName", "Person"),
        Tag("Contact e-mail", "ContactEmail", "Person"),
        Tag("Date", "Date", "Appointment"),
        Tag("Time", "Time", "Appointment"),
        Tag("Property link", "PropertyLink", "Property"),
    };

    private readonly Label preview;
    private readonly Switch tagsSwitch;

    public MainPage()
    {
        Title = "HtmlEditor Sample";

        var openButton = new Button { Text = "Open editor" };
        openButton.Clicked += OnOpenClicked;

        tagsSwitch = new Switch { IsToggled = true };

        preview = new Label { Text = EmptyPlaceholder };

        Content = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Padding = 20,
                Spacing = 16,
                Children =
                {
                    new Label { Text = "Tap to open the native HTML editor (Quill):" },
                    new HorizontalStackLayout
                    {
                        Spacing = 8,
                        Children =
                        {
                            tagsSwitch,
                            new Label { Text = "Offer merge fields", VerticalOptions = LayoutOptions.Center },
                        },
                    },
                    openButton,
                    new Label { Text = "Returned HTML:", FontAttributes = FontAttributes.Bold },
                    preview,
                },
            },
        };
    }

    /// <summary>
    /// One handler serves every action; the id says which was tapped. A real app would call its
    /// translation service here — the editor stays open and shows the button busy meanwhile.
    /// </summary>
    private static async Task<HtmlEditorActionResult> HandleActionAsync(HtmlEditorActionContext context)
    {
        switch (context.ActionId)
        {
            case "translate":
                // Stands in for a round trip to a translation API.
                await Task.Delay(1200);
                return HtmlEditorActionResult.Replace(context.Html.Replace("Dear", "Beste"));

            case "signature":
                return HtmlEditorActionResult.Insert("<p>—<br><b>Omnicasa</b></p>");

            case "clear":
                return HtmlEditorActionResult.Replace(string.Empty);

            default:
                return HtmlEditorActionResult.None;
        }
    }

    private static HtmlEditorTag Tag(string label, string value, string group) =>
        new HtmlEditorTag
        {
            Label = label,
            Value = value,
            Group = group,
            InsertText = $"[*{value}*]",
        };

    private async void OnOpenClicked(object? sender, EventArgs e)
    {
        var current = preview.Text == EmptyPlaceholder ? string.Empty : preview.Text;

        var options = new HtmlEditorOptions
        {
            InitialHtml = current,
            Title = "Description",
            Placeholder = "Describe the property…",
        };

        // Off shows the editor exactly as it is without the feature: no Insert field button.
        if (tagsSwitch.IsToggled)
        {
            options.Tags = SampleTags;
        }

        // Stand-ins for what a real app would do — a translation service, a confirm dialog.
        options.Actions = new List<HtmlEditorAction>
        {
            new HtmlEditorAction { Id = "translate", Label = "Translate", Icon = TranslateIcon },
            new HtmlEditorAction { Id = "signature", Label = "Signature" },
            new HtmlEditorAction { Id = "clear", Label = "Clear", Icon = TrashIcon, IconOnly = true },
        };

        options.OnAction = HandleActionAsync;

        var result = await HtmlEditor.OpenEditorAsync(options);

        if (result.Saved)
        {
            preview.Text = string.IsNullOrEmpty(result.Html) ? "(empty)" : result.Html;
        }
    }
}
