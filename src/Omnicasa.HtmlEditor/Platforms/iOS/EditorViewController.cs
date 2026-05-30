using CoreGraphics;
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

        // Keep the web view below the navigation bar instead of under it.
        EdgesForExtendedLayout = UIRectEdge.None;
        ExtendedLayoutIncludesOpaqueBars = false;

        NavigationItem.LeftBarButtonItem = new UIBarButtonItem(
            options.DiscardText, UIBarButtonItemStyle.Plain, (_, _) => Finish(HtmlEditorResult.Discard()));
        NavigationItem.RightBarButtonItem = new UIBarButtonItem(
            options.SaveText, UIBarButtonItemStyle.Done, (_, _) => SaveAndFinish());

        var config = new WKWebViewConfiguration
        {
            DefaultWebpagePreferences = new WKWebpagePreferences { AllowsContentJavaScript = true },
        };

        webView = new WKWebView(CGRect.Empty, config)
        {
            Opaque = false,
            BackgroundColor = UIColor.SystemBackground,
            TranslatesAutoresizingMaskIntoConstraints = false,
        };
        webView.ScrollView.KeyboardDismissMode = UIScrollViewKeyboardDismissMode.Interactive;
        webView.ScrollView.ContentInsetAdjustmentBehavior = UIScrollViewContentInsetAdjustmentBehavior.Never;
        View!.AddSubview(webView);

        // Pin to the safe area on top/sides and to the keyboard guide on the bottom, so the editor
        // shrinks above the keyboard (the toolbar stays put) instead of being pushed off-screen.
        var constraints = new[]
        {
            webView.TopAnchor.ConstraintEqualTo(View.SafeAreaLayoutGuide.TopAnchor),
            webView.LeadingAnchor.ConstraintEqualTo(View.LeadingAnchor),
            webView.TrailingAnchor.ConstraintEqualTo(View.TrailingAnchor),
            webView.BottomAnchor.ConstraintEqualTo(View.KeyboardLayoutGuide.TopAnchor),
        };
        NSLayoutConstraint.ActivateConstraints(constraints);

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
