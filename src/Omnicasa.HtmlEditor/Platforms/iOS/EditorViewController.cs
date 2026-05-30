using Foundation;
using UIKit;
using WebKit;

namespace Omnicasa.HtmlEditor;

/// <summary>
/// Native iOS page that hosts the Quill editor inside a <see cref="WKWebView"/>, presented inside a
/// navigation controller with Discard (left) and Save (right) bar button items.
/// </summary>
internal sealed class EditorViewController : UIViewController
{
    private readonly HtmlEditorOptions options;
    private readonly Action<HtmlEditorResult> complete;
    private WKWebView webView = null!;
    private bool completed;

    /// <summary>Initializes a new instance of the <see cref="EditorViewController"/> class.</summary>
    /// <param name="options">The editor configuration.</param>
    /// <param name="complete">Callback invoked with the outcome when the page closes.</param>
    public EditorViewController(HtmlEditorOptions options, Action<HtmlEditorResult> complete)
    {
        this.options = options;
        this.complete = complete;
    }

    /// <inheritdoc/>
    public override void ViewDidLoad()
    {
        base.ViewDidLoad();

        Title = options.Title;
        View!.BackgroundColor = UIColor.SystemBackground;

        NavigationItem.LeftBarButtonItem = new UIBarButtonItem(
            options.DiscardText, UIBarButtonItemStyle.Plain, (_, _) => Finish(HtmlEditorResult.Discard()));
        NavigationItem.RightBarButtonItem = new UIBarButtonItem(
            options.SaveText, UIBarButtonItemStyle.Done, (_, _) => SaveAndFinish());

        var config = new WKWebViewConfiguration
        {
            DefaultWebpagePreferences = new WKWebpagePreferences { AllowsContentJavaScript = true },
        };

        webView = new WKWebView(View!.Bounds, config)
        {
            AutoresizingMask = UIViewAutoresizing.FlexibleWidth | UIViewAutoresizing.FlexibleHeight,
            Opaque = false,
            BackgroundColor = UIColor.SystemBackground,
        };
        webView.ScrollView.KeyboardDismissMode = UIScrollViewKeyboardDismissMode.Interactive;
        View!.AddSubview(webView);

        var html = EditorHtmlBuilder.Build(options);
        webView.LoadHtmlString(new NSString(html), NSBundle.MainBundle.BundleUrl);
    }

    private async void SaveAndFinish()
    {
        var html = string.Empty;
        try
        {
            var result = await webView.EvaluateJavaScriptAsync(
                "(window.__getHtml ? window.__getHtml() : '')");
            html = result?.ToString() ?? string.Empty;
        }
        catch
        {
            // Fall back to an empty document if the script bridge is unavailable.
        }

        Finish(HtmlEditorResult.Save(html));
    }

    private void Finish(HtmlEditorResult result)
    {
        if (completed)
        {
            return;
        }

        completed = true;
        DismissViewController(true, () => complete(result));
    }
}
