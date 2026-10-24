- Unity EditMode runs start with a warm-up launch (`UnityMcpGuard.WarmUp`, in CONTRIBUTING.md and the post-merge job),
  so the Unity-MCP plugin is loaded when the tests start, and the `UnityMcpGuardTests` that need it now fail instead of
  passing when it is missing.
