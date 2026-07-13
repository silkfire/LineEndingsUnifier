## What this is

A Visual Studio extension (VSIX) that unifies line endings (and optionally strips trailing
whitespace / adds a final newline) across a solution, project, folder, or single file, either
on-demand from Solution Explorer context menus or automatically on document save. The whole
extension is a single project targeting **.NET Framework 4.8**, packaged as an
`AsyncPackage`-based VSPackage.

## Build

There is no test suite and no separate lint step — analyzers run as part of the build.

- The `.sln` is at the repo root, but the `.csproj` is nested one folder deeper
  (`LineEndingsUnifier\LineEndingsUnifier.csproj`). Pointing MSBuild at the wrong one yields
  `MSB1009`, which is a path mistake, not a build failure.
- Resolve MSBuild install-agnostically via `vswhere` rather than hardcoding a path:

  ```powershell
  $mb = & "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe" -latest -products * -requires Microsoft.Component.MSBuild -find MSBuild\**\Bin\MSBuild.exe
  & $mb "LineEndingsUnifier.sln" /t:Restore,Build /p:Configuration=Debug /v:minimal /nologo
  ```

- Output VSIX: `LineEndingsUnifier\bin\Debug\LineEndingsUnifier.vsix`.
- For a clean, parseable warning/error list, use console-logger params (ANSI color codes, not
  verbosity, are what break line-by-line parsing) and `/t:Rebuild` so analyzers re-emit:

  ```powershell
  & $mb "LineEndingsUnifier.sln" /t:Rebuild /p:Configuration=Debug "/clp:WarningsOnly;NoSummary;DisableConsoleColor" /nologo
  ```

- Debugging launches a VS **experimental instance** (`devenv /rootsuffix Exp`), configured in the
  `.csproj` StartAction — F5 from Visual Studio installs and runs the extension there.

## Language / framework constraints

- **Because this targets .NET Framework 4.8, the C# language version is capped at 7.3.** No
  `LangVersion` is set, so the compiler defaults to 7.3 for this TFM. C# 8+ features will not
  compile — e.g. `is not` patterns, switch expressions, nullable reference types, `using`
  declarations, target-typed `new`. ReSharper/IDE may *suggest* newer syntax as hints; those
  suggestions are not usable here. Use the 7.3-compatible form (`!(x is T y)` instead of
  `x is not T y`, classic `switch`, etc.).
- VSSDK constraints apply throughout: shell/automation calls must run on the UI thread — call
  `ThreadHelper.ThrowIfNotOnUIThread()` and hop with `JoinableTaskFactory.SwitchToMainThreadAsync()`.
  Do **not** use `ConfigureAwait(false)` (continuations must return to the main thread); this is
  enforced by disabling CA2007 in `.editorconfig`.

## Analyzers

- `.editorconfig` promotes *every* analyzer diagnostic to **warning** so they surface in the Error
  List, but `TreatWarningsAsErrors` is not set, so warnings are non-fatal. A few noisy/inappropriate
  rules are dialed back there (CA1031 broad-catch, CA2007, several IDE* formatting rules).
- `Microsoft.CodeAnalysis.BannedApiAnalyzers` + `BannedSymbols.txt` ban specific APIs (RS0030 is
  raised to warning). This exists to prevent a previously-fixed COM-resolution bug from returning —
  check `BannedSymbols.txt` before reintroducing a banned symbol.

## Architecture

Everything lives in the `LineEndingsUnifier` namespace. Rough layering:

- **`LineEndingsUnifierPackage.cs`** — the `AsyncPackage` entry point and orchestrator. Registers the
  four Solution Explorer commands (Solution / Project / Folder / File), resolves VS services in
  `InitializeAsync`, wires the save listener, and contains all the traversal + reporting logic. This
  is the file to start from. The package auto-loads on `SolutionExistsAndFullyLoaded`.
- **`LineEndingsChanger.cs`** — the core text transformation. Defines the `LineEnding` enum (Windows /
  Linux / Macintosh / **Dominant** / None) and `ChangeLineEndings`, which finds "unexpected" line
  endings and replaces them inside a single undo transaction, preserving caret position. `Dominant`
  classifies existing endings in one pass and converts to the majority style (Linux is the tie-break
  default).
- **`LineEndingSearchPattern.cs`** — the regex patterns (target styles + "non-X" complements + a
  trailing-whitespace pattern). The actual find/replace is delegated to VS's `IFindService` finders.
- **`LineEndingFinderFactoryProvider.cs`** — precreates and hands out the `IFinderFactory` instances
  for each pattern (built once from `IFindService`).
- **`OptionsPage.cs`** — the Tools > Options page (`DialogPage`). All user-configurable behavior lives
  here: default line ending, supported file *formats* vs. supported *filenames* (two separate
  semicolon-lists; formats match by suffix, filenames match whole-name), force-on-save,
  save-after-unify, report-to-output, unify-only-open-files, add-final-newline, remove-trailing-
  whitespace, and track-changes. Setting `TrackChanges` to false deletes the change-log file.
- **`DocumentSaveListener.cs`** — thin `IVsRunningDocTableEvents3` wrapper that raises a `BeforeSave`
  event; the package uses it to implement force-on-save unification. A `_isUnifyingLocked` flag in the
  package guards against the save we trigger ourselves re-entering this path.
- **`ChangesManager` / `LastChanges`** — the "track changes" persistence layer. When enabled, records
  per-file (UTC ticks + line-ending style) in an XML change log named `<solution>.leu` **inside the
  solution folder**. On the next run a file is skipped unless its style differs or it was modified
  after the recorded time. A corrupt/unreadable log is treated as empty (safe: files just get
  re-unified). UTC ticks are used deliberately so comparison against `File.GetLastWriteTimeUtc` is
  DST-safe.
- **`Extensions.cs`** — solution/project traversal helpers (recurses into solution folders) and the
  `EndsWithAny` / `EqualsAny` matchers used to decide whether a file is in scope. Filename matching is
  `OrdinalIgnoreCase` (Windows filenames are case-insensitive; avoids the Turkish-I problem).
- **`Utilities.cs`**, **`GuidList.cs` / `PkgCmdID.cs`** — newline-string helper and the command/GUID
  identifiers that pair with `LineEndingsUnifier.vsct` (the command-table definition).

### Key flows

- **Manual unify** (context-menu command → `UnifyLineEndingsFromSolutionExplorerMenuCommand`): pops the
  `LineEndingChoice` WPF dialog, then runs the unify operation on a `JoinableTaskFactory` UI-thread
  task, optionally opening/closing/saving files, writing an output-window report, and updating the
  change log.
- **On-save unify** (`DocumentSaveListener_BeforeSave`): only when `ForceDefaultLineEndingOnSave` is
  on; unifies the just-saved document to the default line ending in place. Getting a text buffer can
  legitimately return null (non-text views), which is treated as "nothing to do".
- **Whole-document processing** (`UnifyLineEndingsInDocument`): runs up to three edits **in order** —
  remove trailing whitespace, unify line endings, add final newline — each re-reading
  `CurrentSnapshot`. This ordering is load-bearing; don't reorder or cache a snapshot/span across the
  steps or later edits operate on stale spans.

## Conventions

- Namespace-scoped `using` directives (inside the `namespace` block), and `using static` for
  `LineEndingsChanger` in the package.
- Broad `catch (Exception)` around VS shell/automation calls is intentional and expected (see the
  CA1031 dial-back) — many DTE/automation calls throw for benign reasons (no selection, non-file
  node, closed document), and the correct response is usually "hide the command / skip the file".
