using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Views;
using Android.Webkit;
using Android.Widget;
using AndroidX.Core.View;
using AGraphics = Android.Graphics;
using Button = Android.Widget.Button;
using ImageButton = Android.Widget.ImageButton;
using ImageView = Android.Widget.ImageView;
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
    /// <summary>The bar and page surface, matching the document's own chrome.</summary>
    private static readonly AGraphics.Color SurfaceColor = AGraphics.Color.ParseColor("#FAFAFA");

    /// <summary>Title colour, matching the page.</summary>
    private static readonly AGraphics.Color BarTitleColor = AGraphics.Color.ParseColor("#1A1A1A");

    /// <summary>Action colour, the same blue the document's own buttons use.</summary>
    private static readonly AGraphics.Color TintColor = AGraphics.Color.ParseColor("#0A66FF");

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

        // The page is light whatever the app or the device is set to, so the status bar icons have
        // to be dark — white-on-white left them invisible over this page's own bar.
        var insetsController = WindowCompat.GetInsetsController(Window!, Window!.DecorView!);
        if (insetsController != null)
        {
            insetsController.AppearanceLightStatusBars = true;
        }

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

        // The page posts here when a caller-supplied action is tapped. Safe to expose: the
        // document is one we built ourselves from embedded assets and never loads remote script.
        webView.AddJavascriptInterface(new ActionBridge(this), EditorActionRunner.BridgeName);
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
        bar.SetBackgroundColor(SurfaceColor);
        bar.SetGravity(GravityFlags.CenterVertical);
        var pad = Dp(8);
        bar.SetPadding(pad, pad, pad, pad);

        // A back arrow rather than a word, so leaving this page looks like leaving any other.
        // The discard text stays as its accessibility name.
        var back = new ImageButton(this)
        {
            LayoutParameters = new LinearLayout.LayoutParams(Dp(40), Dp(40)),
            ContentDescription = options.DiscardText,
        };
        back.SetImageDrawable(SystemBackIndicator() ?? new BackArrowDrawable(BarTitleColor, Dp(2)));
        back.SetBackgroundColor(AGraphics.Color.Transparent);
        back.SetScaleType(ImageView.ScaleType.FitCenter);
        back.SetPadding(Dp(10), Dp(10), Dp(10), Dp(10));
        back.Click += (_, _) => Finish(HtmlEditorResult.Discard());
        bar.AddView(back);

        var title = new TextView(this)
        {
            Text = options.Title,
            LayoutParameters = new LinearLayout.LayoutParams(0, ViewGroup.LayoutParams.WrapContent, 1f),
            Gravity = GravityFlags.Center,
        };
        title.SetTextColor(BarTitleColor);
        title.SetTextSize(Android.Util.ComplexUnitType.Sp, 17);
        title.SetTypeface(null, AGraphics.TypefaceStyle.Bold);
        title.SetSingleLine(true);
        bar.AddView(title);

        var save = new Button(this) { Text = options.SaveText };
        save.SetAllCaps(false);
        save.SetTextColor(TintColor);
        save.SetBackgroundColor(AGraphics.Color.Transparent);
        save.Click += (_, _) => SaveAndFinish();
        bar.AddView(save);

        return bar;
    }

    /// <summary>
    /// The platform's own up indicator, tinted to this bar.
    /// </summary>
    /// <returns>The themed drawable, or <c>null</c> when the theme declares none.</returns>
    /// <remarks>
    /// Read from the theme rather than drawn, so the arrow is whatever this Android version and
    /// this device's theme say "back" looks like — the point of a back button is that it matches
    /// every other one on the device, not the one on the other platform.
    /// </remarks>
    private Android.Graphics.Drawables.Drawable? SystemBackIndicator()
    {
        try
        {
            var value = new Android.Util.TypedValue();
            var resolved = Theme?.ResolveAttribute(
                Android.Resource.Attribute.HomeAsUpIndicator, value, true) == true;
            if (!resolved || value.ResourceId == 0)
            {
                return null;
            }

            var drawable = AndroidX.Core.Content.ContextCompat.GetDrawable(this, value.ResourceId);
            if (drawable == null)
            {
                return null;
            }

            // Mutate first: the theme hands back a shared instance, and tinting it in place would
            // recolour every other use of the same drawable in the host app.
            var tinted = AndroidX.Core.Graphics.Drawable.DrawableCompat.Wrap(drawable.Mutate());
            AndroidX.Core.Graphics.Drawable.DrawableCompat.SetTint(tinted, BarTitleColor);
            return tinted;
        }
        catch (Java.Lang.Exception)
        {
            // A theme without the attribute is not an error; the fallback covers it.
            return null;
        }
    }

    /// <summary>
    /// Fallback back arrow, for a theme that declares no up indicator. Drawn rather than shipped:
    /// a drawable resource in a library has to be merged into the host app's resources.
    /// </summary>
    private sealed class BackArrowDrawable : Android.Graphics.Drawables.Drawable
    {
        private readonly AGraphics.Paint paint;

        public BackArrowDrawable(AGraphics.Color color, float strokeWidth)
        {
            paint = new AGraphics.Paint(AGraphics.PaintFlags.AntiAlias)
            {
                Color = color,
                StrokeWidth = strokeWidth,
                StrokeCap = AGraphics.Paint.Cap.Round,
                StrokeJoin = AGraphics.Paint.Join.Round,
            };
            paint.SetStyle(AGraphics.Paint.Style.Stroke);
        }

        public override int Opacity => (int)AGraphics.Format.Translucent;

        public override void Draw(AGraphics.Canvas canvas)
        {
            // An arrow, not a chevron: Android points back with a shaft and a head.
            var bounds = Bounds;
            float midY = bounds.CenterY();
            float left = bounds.Left + (bounds.Width() * 0.18f);
            float right = bounds.Right - (bounds.Width() * 0.18f);
            float reach = bounds.Height() * 0.20f;

            using var path = new AGraphics.Path();
            path.MoveTo(left, midY);
            path.LineTo(right, midY);
            path.MoveTo(left + reach, midY - reach);
            path.LineTo(left, midY);
            path.LineTo(left + reach, midY + reach);
            canvas.DrawPath(path, paint);
        }

        public override void SetAlpha(int alpha) => paint.Alpha = alpha;

        public override void SetColorFilter(AGraphics.ColorFilter? colorFilter) =>
            paint.SetColorFilter(colorFilter);
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

    /// <summary>Runs a caller action posted by the page and hands the answer back to it.</summary>
    private async void RunAction(string? payload)
    {
        var script = await EditorActionRunner.RunAsync(EditorBridge.Options, payload);

        // Back to the UI thread: the post arrives on the WebView's JS thread, and evaluating a
        // script is only legal on the thread the view was created on.
        RunOnUiThread(() =>
        {
            if (!completed)
            {
                webView.EvaluateJavascript(script, null);
            }
        });
    }

    /// <summary>Receives <c>window.ocAction.post(...)</c>.</summary>
    private sealed class ActionBridge : Java.Lang.Object
    {
        private readonly EditorActivity owner;

        public ActionBridge(EditorActivity owner) => this.owner = owner;

        [Java.Interop.Export("post")]
        [global::Android.Webkit.JavascriptInterface]
        public void Post(string payload) => owner.RunAction(payload);
    }

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
