# Omnicasa.HtmlEditor

[![NuGet](https://img.shields.io/nuget/v/Omnicasa.HtmlEditor.svg?logo=nuget&label=NuGet)](https://www.nuget.org/packages/Omnicasa.HtmlEditor)
[![NuGet Downloads](https://img.shields.io/nuget/dt/Omnicasa.HtmlEditor.svg)](https://www.nuget.org/packages/Omnicasa.HtmlEditor)

A tiny **.NET 9 MAUI** library that opens a **native** rich‑text HTML editor page on
mobile and gives you back the edited HTML. The editor itself is powered by
[**Quill 2**](https://quilljs.com) (a powerful, MIT‑licensed HTML/JS WYSIWYG editor),
bundled **offline** inside the assembly — no CDN, no internet required.

- **iOS** → presents a native `UIViewController` (inside a `UINavigationController`) hosting a `WKWebView`.
- **Android** → starts a native `Activity` hosting a `WebView`.
- Each page has **Save** and **Discard** actions.

Target frameworks: `net9.0` (platform-neutral, for unit testing), `net9.0-android`, `net9.0-ios`.

## Repository layout

```
src/Omnicasa.HtmlEditor          The library (net9.0; net9.0-android; net9.0-ios)
samples/Omnicasa.HtmlEditor.Sample   A MAUI app that opens the editor (C# UI, no XAML)
tests/Omnicasa.HtmlEditor.Tests      xUnit tests for the shared logic (net9.0)
Directory.Build.props + stylecop.json   Omnicasa.Analyzers conventions, applied solution-wide
global.json                       Pins the .NET SDK to 9.0.313
```

## The whole API

There is essentially **one** call. It opens the editor and returns what the user did.

```csharp
using Omnicasa.HtmlEditor;

// Open empty:
HtmlEditorResult result = await HtmlEditor.OpenEditorAsync();

// Or pre-load existing HTML:
HtmlEditorResult result = await HtmlEditor.OpenEditorAsync("<p>Hello <b>world</b></p>");

// Or with full options:
var result = await HtmlEditor.OpenEditorAsync(new HtmlEditorOptions
{
    InitialHtml  = "<h2>Listing description</h2><p>Bright apartment…</p>",
    Title        = "Description",
    Placeholder  = "Describe the property…",
    SaveText     = "Save",
    DiscardText  = "Cancel",
});

if (result.Saved)
{
    string html = result.Html;   // the edited HTML
}
else
{
    // user discarded
}
```

`OpenEditorAsync` completes only after the user taps **Save** (`Saved == true`, with `Html`)
or **Discard** / back‑gesture (`Saved == false`).

### Types

| Type | Purpose |
|------|---------|
| `HtmlEditor.OpenEditorAsync(...)` | Opens the native editor page and awaits the result. |
| `HtmlEditorOptions` | `InitialHtml`, `Title`, `Placeholder`, `SaveText`, `DiscardText`. |
| `HtmlEditorResult` | `bool Saved`, `string Html`. |

## Install / reference

Add a project reference (or the NuGet package once published):

```xml
<ItemGroup>
  <ProjectReference Include="..\src\Omnicasa.HtmlEditor\Omnicasa.HtmlEditor.csproj" />
</ItemGroup>
```

No platform setup, manifest entries, or permissions are required — the Android
`EditorActivity` is registered automatically through its `[Activity]` attribute, and
nothing touches the network.

> The library locates the current page via MAUI's `Platform.CurrentActivity` (Android) and
> the key window's top `UIViewController` (iOS), so call it from a normal MAUI app context
> (e.g. a button handler / view model command).

## How it works

1. `EditorHtmlBuilder` reads three embedded resources — `editor.html`, `quill.js`,
   `quill.snow.css` — and inlines them into a single self‑contained HTML document, injecting
   your `InitialHtml` (base64‑encoded, UTF‑8 safe) and options.
2. The platform layer loads that document into a `WKWebView` / `WebView`.
3. On **Save**, native code evaluates `window.__getHtml()` to read the current document, then
   closes the page and resolves the task.

## Building & testing

The SDK is pinned to **9.0.313** via `global.json`.

```bash
dotnet build Omnicasa.HtmlEditor.slnx -c Release          # library + sample + tests
dotnet test  tests/Omnicasa.HtmlEditor.Tests              # run the unit tests
dotnet build samples/Omnicasa.HtmlEditor.Sample -f net9.0-android   # run the sample
```

### Sample app

`samples/Omnicasa.HtmlEditor.Sample` is a minimal .NET MAUI app (UI written in C#, no XAML)
with an "Open editor" button that calls `HtmlEditor.OpenEditorAsync` and shows the returned HTML.

### Tests

`tests/Omnicasa.HtmlEditor.Tests` (xUnit, `net9.0`) covers the shared logic — Quill/CSS inlining,
token replacement, base64 encoding of initial HTML, placeholder escaping, option defaults, and the
platform-neutral fast-fail. The library exposes its internals to the test assembly via
`InternalsVisibleTo`.

## Coding conventions

Every project references [**Omnicasa.Analyzers**](https://www.nuget.org/packages/Omnicasa.Analyzers)
(wired once in `Directory.Build.props` with `stylecop.json`), which brings StyleCop + SonarAnalyzer,
the company ruleset, and `TreatWarningsAsErrors`. The library and tests build with **zero warnings**.
The sample and test projects relax documentation (SA1600) in a local `.editorconfig`, since an app
and test code are not a published API surface.

## Licensing

This library is MIT. It bundles Quill 2.0.3 (BSD‑3‑Clause). See `THIRD-PARTY-NOTICES.md`.
