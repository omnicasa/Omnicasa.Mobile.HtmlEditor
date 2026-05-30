namespace Omnicasa.HtmlEditor.Sample;

public class MainPage : ContentPage
{
    private const string EmptyPlaceholder = "(no content yet)";

    private readonly Label preview;

    public MainPage()
    {
        Title = "HtmlEditor Sample";

        var openButton = new Button { Text = "Open editor" };
        openButton.Clicked += OnOpenClicked;

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
                    openButton,
                    new Label { Text = "Returned HTML:", FontAttributes = FontAttributes.Bold },
                    preview,
                },
            },
        };
    }

    private async void OnOpenClicked(object? sender, EventArgs e)
    {
        var current = preview.Text == EmptyPlaceholder ? string.Empty : preview.Text;

        var result = await HtmlEditor.OpenEditorAsync(new HtmlEditorOptions
        {
            InitialHtml = current,
            Title = "Description",
            Placeholder = "Describe the property…",
        });

        if (result.Saved)
        {
            preview.Text = string.IsNullOrEmpty(result.Html) ? "(empty)" : result.Html;
        }
    }
}
