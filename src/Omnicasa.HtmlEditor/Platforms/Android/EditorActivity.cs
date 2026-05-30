using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Views;
using Android.Webkit;
using Android.Widget;
using AndroidX.Core.View;
using AGraphics = Android.Graphics;
using Button = Android.Widget.Button;
using LinearLayout = Android.Widget.LinearLayout;
using TextView = Android.Widget.TextView;
using View = Android.Views.View;
using ViewGroup = Android.Views.ViewGroup;
using WebView = Android.Webkit.WebView;

namespace Omnicasa.HtmlEditor;

/// <summary>
/// Full-screen native Android page that hosts the Quill editor inside a <see cref="WebView"/>,
/// with Discard / Save actions in a top bar. Registered automatically via the [Activity] attribute.
/// </summary>
[Activity(
    Theme = "@android:style/Theme.Material.Light.NoActionBar",
    ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.UiMode,
    WindowSoftInputMode = SoftInput.AdjustResize)]
internal sealed class EditorActivity : Activity
{
    private WebView webView = null!;
    private bool completed;

    /// <summary>EvaluateJavascript returns a JSON-encoded value; turn it back into a plain string.</summary>
    /// <param name="jsonEncoded">The JSON-encoded value returned by the WebView.</param>
    /// <returns>The decoded HTML string.</returns>
    private static string DecodeJsString(string? jsonEncoded)
    {
        if (string.IsNullOrEmpty(jsonEncoded) || jsonEncoded == "null")
        {
            return string.Empty;
        }

        try
        {
            return System.Text.Json.JsonSerializer.Deserialize<string>(jsonEncoded!) ?? string.Empty;
        }
        catch
        {
            return jsonEncoded!;
        }
    }

    /// <inheritdoc/>
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);

        // Drive insets ourselves so the bars do not overlap the content (edge-to-edge on Android 15+).
        WindowCompat.SetDecorFitsSystemWindows(Window!, false);

        var options = EditorBridge.Options;

        var root = new LinearLayout(this) { Orientation = Orientation.Vertical };
        root.SetBackgroundColor(AGraphics.Color.White);
        root.AddView(BuildTopBar(options));

        webView = new WebView(this)
        {
            LayoutParameters = new LinearLayout.LayoutParams(
                ViewGroup.LayoutParams.MatchParent, 0, 1f),
        };
        var settings = webView.Settings;
        settings.JavaScriptEnabled = true;
        settings.DomStorageEnabled = true;
        webView.SetBackgroundColor(AGraphics.Color.White);
        root.AddView(webView);

        SetContentView(root);

        // Pad for the status bar (top) and the navigation bar / keyboard (bottom).
        ViewCompat.SetOnApplyWindowInsetsListener(root, new SystemBarsInsetsListener());

        var html = EditorHtmlBuilder.Build(options);

        // The document is fully self-contained, so no base URL is required.
        webView.LoadDataWithBaseURL(null, html, "text/html", "utf-8", null);
    }

    /// <inheritdoc/>
    public override void OnBackPressed() => Finish(HtmlEditorResult.Discard());

    /// <inheritdoc/>
    protected override void OnDestroy()
    {
        // If the activity goes away without an explicit choice, report a discard.
        if (!completed)
        {
            completed = true;
            EditorBridge.Complete(HtmlEditorResult.Discard());
        }

        base.OnDestroy();
    }

    private View BuildTopBar(HtmlEditorOptions options)
    {
        var bar = new LinearLayout(this)
        {
            Orientation = Orientation.Horizontal,
            LayoutParameters = new LinearLayout.LayoutParams(
                ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent),
        };
        bar.SetBackgroundColor(AGraphics.Color.ParseColor("#FAFAFA"));
        bar.SetGravity(GravityFlags.CenterVertical);
        var pad = Dp(8);
        bar.SetPadding(pad, pad, pad, pad);

        var discard = new Button(this) { Text = options.DiscardText };
        discard.SetAllCaps(false);
        discard.Click += (_, _) => Finish(HtmlEditorResult.Discard());
        bar.AddView(discard);

        var title = new TextView(this)
        {
            Text = options.Title,
            LayoutParameters = new LinearLayout.LayoutParams(0, ViewGroup.LayoutParams.WrapContent, 1f),
            Gravity = GravityFlags.Center,
        };
        title.SetTextColor(AGraphics.Color.ParseColor("#1A1A1A"));
        title.SetTextSize(Android.Util.ComplexUnitType.Sp, 17);
        title.SetSingleLine(true);
        bar.AddView(title);

        var save = new Button(this) { Text = options.SaveText };
        save.SetAllCaps(false);
        save.SetTextColor(AGraphics.Color.ParseColor("#0A66FF"));
        save.Click += (_, _) => SaveAndFinish();
        bar.AddView(save);

        return bar;
    }

    private void SaveAndFinish()
    {
        webView.EvaluateJavascript(
            "(window.__getHtml ? window.__getHtml() : '')",
            new JsResultCallback(value =>
            {
                var html = DecodeJsString(value);
                Finish(HtmlEditorResult.Save(html));
            }));
    }

    private void Finish(HtmlEditorResult result)
    {
        if (completed)
        {
            return;
        }

        completed = true;
        EditorBridge.Complete(result);
        Finish();
    }

    private int Dp(int value) => (int)(value * Resources!.DisplayMetrics!.Density);

    private sealed class JsResultCallback : Java.Lang.Object, IValueCallback
    {
        private readonly Action<string?> onValue;

        public JsResultCallback(Action<string?> onValue) => this.onValue = onValue;

        public void OnReceiveValue(Java.Lang.Object? value) => onValue(value?.ToString());
    }

    private sealed class SystemBarsInsetsListener : Java.Lang.Object, IOnApplyWindowInsetsListener
    {
        public WindowInsetsCompat? OnApplyWindowInsets(View? v, WindowInsetsCompat? insets)
        {
            if (v is null || insets is null)
            {
                return insets;
            }

            var bars = insets.GetInsets(WindowInsetsCompat.Type.SystemBars());
            var ime = insets.GetInsets(WindowInsetsCompat.Type.Ime());
            var bottom = System.Math.Max(bars?.Bottom ?? 0, ime?.Bottom ?? 0);
            v.SetPadding(bars?.Left ?? 0, bars?.Top ?? 0, bars?.Right ?? 0, bottom);
            return insets;
        }
    }
}
