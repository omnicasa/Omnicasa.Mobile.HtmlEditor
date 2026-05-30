using Foundation;
using UIKit;

namespace Omnicasa.HtmlEditor;

public static partial class HtmlEditor
{
    private static partial Task<HtmlEditorResult> OpenEditorPlatformAsync(HtmlEditorOptions options)
    {
        var tcs = new TaskCompletionSource<HtmlEditorResult>();

        void Present()
        {
            var presenter = TopViewController()
                ?? throw new InvalidOperationException("No view controller available to present the editor.");

            var editor = new EditorViewController(options, result => tcs.TrySetResult(result));
            var nav = new UINavigationController(editor)
            {
                ModalPresentationStyle = UIModalPresentationStyle.FullScreen,
            };
            presenter.PresentViewController(nav, true, null);
        }

        if (NSThread.IsMain)
            Present();
        else
            UIApplication.SharedApplication.InvokeOnMainThread(Present);

        return tcs.Task;
    }

    private static UIViewController? TopViewController()
    {
        var window = UIApplication.SharedApplication.ConnectedScenes
            .OfType<UIWindowScene>()
            .SelectMany(s => s.Windows)
            .FirstOrDefault(w => w.IsKeyWindow)
            ?? UIApplication.SharedApplication.ConnectedScenes
                .OfType<UIWindowScene>()
                .SelectMany(s => s.Windows)
                .FirstOrDefault();

        var vc = window?.RootViewController;
        while (vc?.PresentedViewController is { } presented)
            vc = presented;
        return vc;
    }
}
