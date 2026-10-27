# Draft feature request for IvanMurzak/Unity-MCP

For the owner to file on https://github.com/IvanMurzak/Unity-MCP/issues (not filed by the project's agents). It was
drafted in the Unity-MCP adoption review of 2026-10-10.

## Title

Gate the main asmdefs without writing scripting defines into ProjectSettings.asset

## Body

We use Unity-MCP 0.93.3 from OpenUPM in a free game and git-ignore `Assets/Plugins/NuGet`, so the resolver
downloads the DLLs on each machine's first open. That part works well.

On every healthy resolve, `RecompileGate.EnsureReadyDefine()` writes `UNITY_MCP_READY;UNITY_MCP_DEPS_7` into the
scripting defines of every build target group in `ProjectSettings/ProjectSettings.asset`. For a team that does not commit
the NuGet DLLs, this leaves a permanent local change in a tracked file. That change must never be committed: a fresh clone
then gets the defines before the DLLs, the plugin's assemblies compile against missing references (about 2,000
`error CS0234`), and batch mode aborts. We now guard against it with a policy test. We found no setting, env var or menu
item that changes this.

Proposal: gate the main asmdefs with `versionDefines` keyed on a resolver-generated local marker package (for example
`Packages/com.ivanmurzak.unity.mcp.nuget-ready`, a git-ignorable embedded package whose version encodes the deps
generation), instead of writing `UNITY_MCP_READY` and `UNITY_MCP_DEPS_n` into ProjectSettings. That keeps the same
static, pre-code-execution gate (#957, #966) while leaving `ProjectSettings.asset` untouched for teams that don't commit
`Assets/Plugins/NuGet`. Alternatively, add an opt-in that writes the gate only when absent and never on every resolve.

Related, smaller observations from the same adoption, in case they are useful:

- With `CI=true` in the environment and the DLLs not committed, the resolver skips the download but still adds the gate
  defines, so a fresh clone fails to compile. A check that the DLLs exist before `EnsureReadyDefine()` on the CI path
  would avoid that.
- In batch mode the first-run window (`MainWindowInitializer`) opens, and its server panel can start the local server
  during a test run. Skipping it when `Application.isBatchMode` would help teams that run EditMode tests headless.
