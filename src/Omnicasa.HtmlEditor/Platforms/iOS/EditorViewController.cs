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
    /// <summary>The bar and page surface, matching the document's own chrome.</summary>
    private static readonly UIColor Surface = UIColor.FromRGB(250, 250, 252);

    /// <summary>Hairline under the bar.</summary>
    private static readonly UIColor Separator = UIColor.FromRGBA(60, 60, 67, 46);

    /// <summary>Title colour, matching the page.</summary>
    private static readonly UIColor TitleColor = UIColor.FromRGB(26, 26, 26);

    /// <summary>Action colour, the same blue the document's own buttons use.</summary>
    private static readonly UIColor Tint = UIColor.FromRGB(10, 102, 255);

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

        // The editor's own surface, not the system's: the document inside is a light page whatever
        // the device is set to, so a dark view behind it would frame white content in black.
        OverrideUserInterfaceStyle = UIUserInterfaceStyle.Light;
        View!.BackgroundColor = Surface;

        // Keep the web view below the navigation bar instead of under it.
        EdgesForExtendedLayout = UIRectEdge.None;
        ExtendedLayoutIncludesOpaqueBars = false;

        StyleNavigationBar();

        // A back chevron rather than a word: this page is entered from somewhere, and every other
        // page in the app leaves the same way. The discard text stays as its accessibility name.
        var chevron = UIImage.GetSystemImage("chevron.backward") ?? new UIImage();
        var back = new UIBarButtonItem(
            chevron,
            UIBarButtonItemStyle.Plain,
            (_, _) => Finish(HtmlEditorResult.Discard()))
        {
            AccessibilityLabel = options.DiscardText,
        };

        NavigationItem.LeftBarButtonItem = back;

        NavigationItem.RightBarButtonItem = new UIBarButtonItem(
            options.SaveText, UIBarButtonItemStyle.Done, (_, _) => SaveAndFinish());

        var config = new WKWebViewConfiguration
        {
            DefaultWebpagePreferences = new WKWebpagePreferences { AllowsContentJavaScript = true },
        };

        // The page posts here when a caller-supplied action is tapped.
        var content = new WKUserContentController();
        content.AddScriptMessageHandler(new ActionBridge(this), EditorActionRunner.BridgeName);
        config.UserContentController = content;

        webView = new WKWebView(CGRect.Empty, config)
        {
            Opaque = false,

            // Matches the document, so there is no dark flash before it paints.
            BackgroundColor = UIColor.White,
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

    /// <summary>
    /// Paints the navigation bar to match the document.
    /// </summary>
    /// <remarks>
    /// Set on this page's own navigation item, never on the shared bar: the host app configures
    /// <c>UINavigationBar.Appearance</c> for its own branding, and a library that wrote there
    /// would restyle the app. Reading it is the other half of the problem — inheriting a dark
    /// branded bar is what framed this white page in black — so every slot is declared here,
    /// including the scroll-edge ones that iOS otherwise leaves transparent.
    /// </remarks>
    private void StyleNavigationBar()
    {
        var appearance = new UINavigationBarAppearance();
        appearance.ConfigureWithOpaqueBackground();
        appearance.BackgroundColor = Surface;
        appearance.ShadowColor = Separator;
        appearance.TitleTextAttributes = new UIStringAttributes
        {
            ForegroundColor = TitleColor,
            Font = UIFont.SystemFontOfSize(17, UIFontWeight.Semibold),
        };

        NavigationItem.StandardAppearance = appearance;
        NavigationItem.ScrollEdgeAppearance = appearance;
        NavigationItem.CompactAppearance = appearance;

        if (OperatingSystem.IsIOSVersionAtLeast(15))
        {
            NavigationItem.CompactScrollEdgeAppearance = appearance;
        }
    }

    /// <inheritdoc/>
    public override void ViewWillAppear(bool animated)
    {
        base.ViewWillAppear(animated);

        // The tint lives on the bar itself, so it is applied once this page is inside one. The
        // navigation controller is this page's own, created alongside it.
        if (NavigationController?.NavigationBar is { } bar)
        {
            bar.TintColor = Tint;
            bar.Translucent = false;
        }
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

    /// <summary>Runs a caller action posted by the page and hands the answer back to it.</summary>
    private async void RunAction(string? payload)
    {
        var script = await EditorActionRunner.RunAsync(options, payload);
        try
        {
            await webView.EvaluateJavaScriptAsync(script);
        }
        catch
        {
            // The page went away mid-action; nothing to apply it to.
        }
    }

    /// <summary>Receives <c>window.webkit.messageHandlers.ocAction.postMessage(...)</c>.</summary>
    private sealed class ActionBridge : NSObject, IWKScriptMessageHandler
    {
        private readonly EditorViewController owner;

        public ActionBridge(EditorViewController owner) => this.owner = owner;

        public void DidReceiveScriptMessage(
            WKUserContentController userContentController,
            WKScriptMessage message)
            => owner.RunAction(message?.Body?.ToString());
    }
}
