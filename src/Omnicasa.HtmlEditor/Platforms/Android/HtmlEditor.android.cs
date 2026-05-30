using Android.Content;
using Microsoft.Maui.ApplicationModel;

namespace Omnicasa.HtmlEditor;

public static partial class HtmlEditor
{
    private static partial Task<HtmlEditorResult> OpenEditorPlatformAsync(HtmlEditorOptions options)
    {
        var activity = Platform.CurrentActivity
            ?? throw new InvalidOperationException(
                "No current Activity. Initialize MAUI (or call Platform.Init) before opening the editor.");

        var tcs = new TaskCompletionSource<HtmlEditorResult>();
        EditorBridge.Begin(options, tcs);

        var intent = new Intent(activity, typeof(EditorActivity));
        activity.StartActivity(intent);

        return tcs.Task;
    }
}

/// <summary>
/// Hands the active <see cref="HtmlEditorOptions"/> and completion source to the
/// <see cref="EditorActivity"/> it launches, and routes the result back. The editor is
/// modal, so at most one session is pending at a time.
/// </summary>
internal static class EditorBridge
{
    private static readonly object Gate = new object();
    private static HtmlEditorOptions? pendingOptions;
    private static TaskCompletionSource<HtmlEditorResult>? pendingCompletion;

    /// <summary>Gets the options for the session currently being opened.</summary>
    public static HtmlEditorOptions Options
    {
        get
        {
            lock (Gate)
            {
                return pendingOptions ?? new HtmlEditorOptions();
            }
        }
    }

    /// <summary>Registers a new editor session and its completion source.</summary>
    /// <param name="options">Options for the session.</param>
    /// <param name="tcs">The completion source resolved when the session ends.</param>
    public static void Begin(HtmlEditorOptions options, TaskCompletionSource<HtmlEditorResult> tcs)
    {
        lock (Gate)
        {
            // If something was already pending (e.g. the activity was killed), discard it.
            pendingCompletion?.TrySetResult(HtmlEditorResult.Discard());
            pendingOptions = options;
            pendingCompletion = tcs;
        }
    }

    /// <summary>Resolves the pending session with the given result.</summary>
    /// <param name="result">The outcome to report back to the caller.</param>
    public static void Complete(HtmlEditorResult result)
    {
        TaskCompletionSource<HtmlEditorResult>? tcs;
        lock (Gate)
        {
            tcs = pendingCompletion;
            pendingCompletion = null;
            pendingOptions = null;
        }

        tcs?.TrySetResult(result);
    }
}
