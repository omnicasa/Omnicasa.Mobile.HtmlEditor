using System.Text;
using System.Text.Json;

namespace Omnicasa.HtmlEditor;

/// <summary>
/// The platform-neutral half of the action round trip: the page posts a request, the caller's
/// handler answers, and the answer goes back as a script for the page to run. Both native hosts
/// share this so the contract lives in one place.
/// </summary>
internal static class EditorActionRunner
{
    /// <summary>Name of the bridge object the page posts through, on both platforms.</summary>
    public const string BridgeName = "ocAction";

    /// <summary>
    /// Runs the caller's handler for a request posted by the page.
    /// </summary>
    /// <param name="options">The editor configuration, carrying the handler.</param>
    /// <param name="payload">The JSON the page posted: <c>{ id, html }</c>.</param>
    /// <returns>The script to evaluate in the page, applying whatever the handler decided.</returns>
    public static async Task<string> RunAsync(HtmlEditorOptions options, string? payload)
    {
        var actionId = string.Empty;
        try
        {
            using var document = JsonDocument.Parse(payload ?? "{}");
            var root = document.RootElement;
            actionId = Read(root, "id");
            var html = Read(root, "html");

            if (options.OnAction is null)
            {
                return ApplyScript(actionId, HtmlEditorActionResult.None);
            }

            var result = await options.OnAction(
                new HtmlEditorActionContext { ActionId = actionId, Html = html })
                .ConfigureAwait(false);

            return ApplyScript(actionId, result ?? HtmlEditorActionResult.None);
        }
        catch
        {
            // A handler that threw must not leave the button spinning forever: the page is told
            // the action finished and changed nothing. The caller owns reporting the failure.
            return ApplyScript(actionId, HtmlEditorActionResult.None);
        }
    }

    private static string Read(JsonElement root, string name) =>
        root.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString() ?? string.Empty
            : string.Empty;

    /// <summary>Builds the call into the page. The html travels base64 so no quoting can break it.</summary>
    private static string ApplyScript(string actionId, HtmlEditorActionResult result)
    {
        var kind = result.Kind switch
        {
            HtmlEditorActionResult.ResultKind.Replace => "replace",
            HtmlEditorActionResult.ResultKind.Insert => "insert",
            _ => "none",
        };

        var idB64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(actionId));
        var htmlB64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(result.Html));

        return $"window.__applyAction && window.__applyAction('{idB64}','{kind}','{htmlB64}')";
    }
}
