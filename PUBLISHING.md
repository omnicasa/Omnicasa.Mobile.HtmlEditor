# Publishing `Omnicasa.HtmlEditor` to NuGet

This is a step-by-step manual for the **first** upload of the package to
[nuget.org](https://www.nuget.org). Later releases are just steps 4–6 with a new version.

The package is produced from `src/Omnicasa.HtmlEditor/Omnicasa.HtmlEditor.csproj` and contains
three target frameworks (`net9.0`, `net9.0-android`, `net9.0-ios`) with the Quill assets embedded.

---

## 1. Prerequisites

- The .NET SDK pinned by `global.json` (**9.0.313**). Check with `dotnet --version`.
- A free account on <https://www.nuget.org> (sign in with a Microsoft account).
- The account must be allowed to publish the `Omnicasa.HtmlEditor` ID.
  The `Omnicasa.*` prefix is already used by [`Omnicasa.Analyzers`](https://www.nuget.org/packages/Omnicasa.Analyzers).
  If the prefix is **reserved**, publish from an account that owns it (or is a co-owner),
  otherwise NuGet will reject a new ID under the reserved prefix. See step 7.

---

## 2. Create a NuGet API key (one time)

1. Sign in to nuget.org → click your avatar → **API Keys** → **+ Create**.
2. Fill in:
   - **Key Name**: `Omnicasa.HtmlEditor push`
   - **Expiration**: 90–365 days (NuGet no longer allows non-expiring keys).
   - **Select Scopes**: `Push` → *Push new packages and package versions*.
   - **Glob Pattern**: `Omnicasa.HtmlEditor` (or `Omnicasa.*` to reuse for the whole prefix).
3. **Create**, then **Copy** the key now — it is shown only once.
4. Keep it secret. Prefer an environment variable over pasting it into commands:

   ```bash
   export NUGET_API_KEY="oy2x...your-key..."
   ```

---

## 3. Set the version

The version lives in the csproj:

```xml
<Version>1.0.0</Version>
```

Use [SemVer](https://semver.org): `MAJOR.MINOR.PATCH`. For a pre-release, use a suffix such as
`1.0.0-preview.1`. **A version can never be re-uploaded or overwritten**, so bump it for every push.

---

## 4. Build the package (Release)

From the repository root:

```bash
rm -rf artifacts
dotnet pack src/Omnicasa.HtmlEditor/Omnicasa.HtmlEditor.csproj -c Release -o artifacts
```

This creates, under `artifacts/`:

- `Omnicasa.HtmlEditor.1.0.0.nupkg`   — the library
- `Omnicasa.HtmlEditor.1.0.0.snupkg`  — the debug symbols

Optional sanity check of the contents:

```bash
unzip -l artifacts/Omnicasa.HtmlEditor.1.0.0.nupkg
```

You should see `lib/net9.0/…`, `lib/net9.0-android35.0/…`, `lib/net9.0-ios18.0/…`, plus `README.md`.

---

## 5. (Recommended) Validate before pushing

Test the package locally against the sample app to be sure it restores and works:

```bash
# Add the artifacts folder as a temporary local feed
dotnet nuget add source "$(pwd)/artifacts" -n local-omnicasa

# In a throwaway test app:
#   dotnet add package Omnicasa.HtmlEditor --source local-omnicasa --version 1.0.0

# Remove it again when done
dotnet nuget remove source local-omnicasa
```

---

## 6. Push to nuget.org

```bash
dotnet nuget push artifacts/Omnicasa.HtmlEditor.1.0.0.nupkg \
  --api-key "$NUGET_API_KEY" \
  --source https://api.nuget.org/v3/index.json
```

Notes:

- The matching `.snupkg` is **pushed automatically** when it sits next to the `.nupkg`.
  (To push it explicitly: repeat the command with the `.snupkg` path.)
- The first time, NuGet runs validation and virus scanning; the package appears within a few
  minutes but can take up to ~15 minutes to be fully **indexed** and installable.
- Watch the status at: `https://www.nuget.org/packages/Omnicasa.HtmlEditor`

---

## 7. First-upload specifics (ID prefix reservation)

- If you get **403 / "The package ID is reserved"**, the `Omnicasa.*` prefix is owned by another
  account. Resolve by either:
  - pushing from / adding your account as an **owner** of an existing `Omnicasa.*` package, or
  - requesting an ID prefix reservation for your account at
    <https://learn.microsoft.com/nuget/nuget-org/id-prefix-reservation>.
- If you get **409 / "already exists"**, that exact version is taken — bump `<Version>` and re-pack.
- After it is live, add co-owners on the package page (**Manage Owners**) so the team can publish
  future versions.

---

## 8. Releasing future versions

1. Bump `<Version>` in the csproj (and update `<PackageReleaseNotes>`).
2. Commit and tag: `git tag v1.0.1 && git push --tags`.
3. `dotnet pack … -c Release -o artifacts`
4. `dotnet nuget push artifacts/Omnicasa.HtmlEditor.<new-version>.nupkg --api-key "$NUGET_API_KEY" --source https://api.nuget.org/v3/index.json`

---

## Automated publishing (GitHub Actions)

Two workflows live in `.github/workflows/`:

| Workflow | Trigger | What it does |
|----------|---------|--------------|
| `pr-build-test.yml` | Pull request → `main` | Builds the library (all TFMs) + sample (Android) and runs the unit tests. |
| `publish-nuget.yml` | Push/merge to `main` touching `src/**` | Tests, packs with version `yyyy.mm.dd.<run_number>`, pushes to NuGet.org, and creates a `v<version>` GitHub Release with the `.nupkg` attached. |

Both run on **macOS** runners (required because the library multi-targets `net9.0-ios`) and use the
SDK pinned by `global.json`.

**One-time setup for the publish workflow:**

1. Create the API key on nuget.org (steps 2 above).
2. In GitHub: **Settings → Secrets and variables → Actions → New repository secret**
   - Name: `NUGET_API_KEY`
   - Value: the key.

After that, every merge to `main` that changes `src/**` publishes a new version automatically — no
manual `dotnet pack`/`push` needed. The very first version still has to satisfy the ID-prefix rules
in step 7 (so it can be worth doing the first push manually, then letting CI take over).

The version is computed at run time, e.g. a merge on 30 May 2026 as run #7 → `2026.5.30.7`.
To publish on **every** merge (not only `src/**` changes), delete the `paths:` filter in
`publish-nuget.yml`.

## Alternative: GitHub Packages (private feed)

To publish to the org's GitHub Packages feed instead of nuget.org:

```bash
dotnet nuget push artifacts/Omnicasa.HtmlEditor.1.0.0.nupkg \
  --api-key "$GITHUB_TOKEN" \
  --source https://nuget.pkg.github.com/omnicasa/index.json
```

`GITHUB_TOKEN` needs the `write:packages` scope. Consumers then add that source with a PAT that has
`read:packages`.
