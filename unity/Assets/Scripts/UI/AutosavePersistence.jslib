// Observe Unity 6000.3's existing autosync, never start a concurrent FS.syncfs. SaveStore has already closed and
// renamed the file; autoPersist queued that mount. Capture requests when its next flush starts, so a request made
// during an older in-flight flush cannot be acknowledged by the older snapshot. TURN remains a sim diagnostic.
mergeInto(LibraryManager.library, {
  $StatecraftAutosave: { pending: null, latest: null, installed: false },
  StatecraftPersistAutosave__deps: ["$StatecraftAutosave", "$IDBFS"],
  StatecraftPersistAutosave: function (receiptPtr) {
    var receipt = UTF8ToString(receiptPtr);
    var mount = Module.__unityIdbfsMount && Module.__unityIdbfsMount.mount;
    if (!mount || !Module.autoSyncPersistentDataPath) {
      console.error("AUTOSAVE FAIL " + receipt);
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
              if (error) console.error("AUTOSAVE FAIL " + saved);
              else if (saved === saves.latest) console.log("AUTOSAVE OK " + saved);
            }
          }
        });
      };
      saves.installed = true;
    }
    // Multiple writes before the queued flush overwrite the same autosave path: only its latest bytes can be durable.
    saves.pending = saves.latest = receipt;
  },
});
