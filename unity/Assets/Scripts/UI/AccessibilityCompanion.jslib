// #1119 B: only Unity-to-page text. The template mounts after createUnityInstance resolves.
mergeInto(LibraryManager.library, {
  StatecraftCompanionPush: function (jsonPtr) {
    if (typeof window === "undefined" || typeof window.statecraftCompanionPush !== "function") return 0;
    return window.statecraftCompanionPush(UTF8ToString(jsonPtr)) ? 1 : 0;
  },
});
