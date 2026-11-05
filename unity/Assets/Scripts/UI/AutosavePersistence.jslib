// Observe Unity 6000.3's existing autosync, never start a concurrent FS.syncfs. SaveStore has already closed and
// renamed the file; autoPersist queued that mount. Capture requests when its next flush starts, so a request made
// during an older in-flight flush cannot be acknowledged by the older snapshot. Report the result to the requesting
// GameRoot as well as the console. TURN remains a sim diagnostic.
mergeInto(LibraryManager.library, {
  $StatecraftAutosave: { pending: null, latest: null, installed: false },
  StatecraftPersistAutosave__deps: ["$StatecraftAutosave", "$IDBFS"],
  StatecraftPersistAutosave: function (requestPtr, targetPtr) {
    var request = {
      value: UTF8ToString(requestPtr),
      target: UTF8ToString(targetPtr),
    };
    function report(saved, error) {
      var save = saved.value.indexOf("save ") === 0;
      var receipt = save ? saved.value.substring(5) : null;
      if (save) {
        if (error) console.error("AUTOSAVE FAIL " + receipt);
        else console.log("AUTOSAVE OK " + receipt);
      }
      SendMessage(saved.target, "OnSavePersistenceResult", (error ? "fail " : "ok ") + saved.value);
    }
    var mount = Module.__unityIdbfsMount && Module.__unityIdbfsMount.mount;
    if (!mount || !Module.autoSyncPersistentDataPath) {
      report(request, new Error("IndexedDB autosync is unavailable"));
      return;
    }
    var saves = StatecraftAutosave;
    if (!saves.installed) {
      var sync = IDBFS.syncfs;
      IDBFS.syncfs = function (target, populate, callback) {
        if (populate || target !== mount) return sync.call(this, target, populate, callback);
        var saved = saves.pending;
        saves.pending = null;
        return sync.call(this, target, populate, function (error) {
          // Unity's callback owns its idb/again state and starts a queued follow-up flush. Leave it intact.
          try { callback(error); }
          finally {
            if (saved) {
              if (error) report(saved, error);
              else if (saved === saves.latest) report(saved, null);
            }
          }
        });
      };
      saves.installed = true;
    }
    // Multiple writes before the queued flush overwrite the same autosave path: only its latest bytes can be durable.
    saves.pending = saves.latest = request;
  },
});
