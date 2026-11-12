"""Execute the game's actual WebGL commit observer with held IDBFS callbacks; no browser or Unity needed."""
import shutil
import subprocess
from pathlib import Path

import pytest

PLUGIN = Path(__file__).resolve().parents[2] / "unity/Assets/Scripts/UI/AutosavePersistence.jslib"
pytestmark = pytest.mark.skipif(shutil.which("node") is None, reason="needs node to execute the game's jslib")


def run(body):
    driver = r'''
const assert = require("node:assert/strict");
const mount = {}, other = {}, calls = [], logs = [], errors = [], callbacks = [], messages = [];
const Module = { __unityIdbfsMount: { mount }, autoSyncPersistentDataPath: true };
const LibraryManager = { library: {} };
function mergeInto(target, value) { Object.assign(target, value); }
function UTF8ToString(value) { return value; }
function SendMessage(target, method, value) { messages.push([target, method, value]); }
const console = { log: value => logs.push(value), error: value => errors.push(value) };
const IDBFS = { syncfs(target, populate, callback) {
  assert.equal(this, IDBFS);
  calls.push({ target, populate, callback });
  return "existing sync result";
} };
''' + PLUGIN.read_text() + r'''
const StatecraftAutosave = LibraryManager.library.$StatecraftAutosave;
const request = LibraryManager.library.StatecraftPersistAutosave;
const one = "1 1948-06-15 turn 1 hash aa", two = "2 1948-07-15 turn 2 hash bb";
function save(value, target = "UI") { request("save " + value, target); }
function flush(target = mount, populate = false) {
  return IDBFS.syncfs(target, populate, error => callbacks.push(error));
}
''' + body
    return subprocess.run(["node", "-e", driver], capture_output=True, text=True, check=True)


def test_save_ack_waits_for_the_existing_sync_callback_and_starts_no_sync():
    run(r'''
save(one);
assert.equal(calls.length, 0);  // file close already queued Unity's timer; the observer must not launch a competing sync
assert.deepEqual(logs, []);
assert.equal(flush(), "existing sync result");
assert.deepEqual(logs, []);
calls[0].callback(null);
assert.deepEqual(logs, ["AUTOSAVE OK " + one]);
assert.deepEqual(messages, [["UI", "OnSavePersistenceResult", "ok save " + one]]);
assert.deepEqual(callbacks, [null]);
assert.deepEqual(errors, []);
''')


def test_a_first_request_during_an_older_sync_waits_for_the_followup():
    run(r'''
flush();  // Unity was already syncing before our observer was installed
save(one);
calls[0].callback(null);
assert.deepEqual(logs, []);
flush();
calls[1].callback(null);
assert.deepEqual(logs, ["AUTOSAVE OK " + one]);
assert.deepEqual(messages, [["UI", "OnSavePersistenceResult", "ok save " + one]]);
assert.equal(calls.length, 2);
''')


def test_each_generation_belongs_to_the_snapshot_that_started_after_its_request():
    run(r'''
save(one);
IDBFS.syncfs(mount, false, error => { callbacks.push(error); flush(); }); // Unity's 'again' callback starts the next sync
save(two);  // writes made while the first snapshot is in flight
calls[0].callback(null);
assert.equal(calls.length, 2);
assert.deepEqual(logs, []);  // a superseded snapshot must not claim either exact file state
assert.deepEqual(messages, []);
calls[1].callback(null);
assert.deepEqual(logs, ["AUTOSAVE OK " + two]);
assert.deepEqual(messages, [["UI", "OnSavePersistenceResult", "ok save " + two]]);
assert.deepEqual(callbacks, [null, null]);
''')


def test_sync_errors_are_not_success_and_do_not_poison_the_next_save():
    run(r'''
save(one);
flush();
const error = new Error("storage denied");
calls[0].callback(error);
assert.deepEqual(logs, []);
assert.deepEqual(errors, ["AUTOSAVE FAIL " + one]);
assert.deepEqual(messages, [["UI", "OnSavePersistenceResult", "fail save " + one]]);
assert.equal(callbacks[0], error);
save(two);
flush();
calls[1].callback(null);
assert.deepEqual(logs, ["AUTOSAVE OK " + two]);
assert.deepEqual(messages[1], ["UI", "OnSavePersistenceResult", "ok save " + two]);
''')


def test_boot_populate_and_other_mounts_pass_through_without_taking_save_requests():
    run(r'''
save(one);
assert.equal(flush(mount, true), "existing sync result");
flush(other);
calls[0].callback(null); calls[1].callback(null);
assert.deepEqual(logs, []);
flush(); calls[2].callback(null);
assert.deepEqual(logs, ["AUTOSAVE OK " + one]);
assert.deepEqual(messages, [["UI", "OnSavePersistenceResult", "ok save " + one]]);
assert.equal(calls[0].populate, true);
assert.equal(calls[1].target, other);
''')


@pytest.mark.parametrize("disable", ["Module.autoSyncPersistentDataPath = false;", "delete Module.__unityIdbfsMount;"])
def test_missing_autosync_fails_instead_of_announcing_durability(disable):
    run(disable + r'''
save(one);
assert.deepEqual(errors, ["AUTOSAVE FAIL " + one]);
assert.deepEqual(logs, []);
assert.deepEqual(messages, [["UI", "OnSavePersistenceResult", "fail save " + one]]);
assert.equal(calls.length, 0);
''')


def test_multiple_writes_before_one_flush_acknowledge_only_the_latest_file_contents():
    run(r'''
save(one);
save(two);  // one file path now contains two's bytes, not one's
flush();
calls[0].callback(null);
assert.deepEqual(logs, ["AUTOSAVE OK " + two]);
assert.deepEqual(messages, [["UI", "OnSavePersistenceResult", "ok save " + two]]);
''')


def test_delete_success_reports_to_its_requesting_game_root_without_an_autosave_receipt():
    run(r'''
request("delete", "GameRoot");
flush();
calls[0].callback(null);
assert.deepEqual(logs, []);
assert.deepEqual(errors, []);
assert.deepEqual(messages, [["GameRoot", "OnSavePersistenceResult", "ok delete"]]);
''')


def test_delete_failure_reports_to_its_requesting_game_root_without_an_autosave_receipt():
    run(r'''
request("delete", "GameRoot");
flush();
calls[0].callback(new Error("storage denied"));
assert.deepEqual(logs, []);
assert.deepEqual(errors, []);
assert.deepEqual(messages, [["GameRoot", "OnSavePersistenceResult", "fail delete"]]);
''')


def test_a_delete_before_the_flush_replaces_the_pending_save_and_only_the_delete_reports():
    run(r'''
save(one);
request("delete", "GameRoot");  // New Game deletes the file before the queued flush starts
flush();
calls[0].callback(null);
assert.deepEqual(logs, []);  // the save's bytes are gone, so its receipt must not be announced as durable
assert.deepEqual(errors, []);
assert.deepEqual(messages, [["GameRoot", "OnSavePersistenceResult", "ok delete"]]);
''')


def test_a_save_flush_failing_after_a_delete_request_still_reports_the_save_failure():
    run(r'''
save(one);
flush();  // the save's snapshot is in flight
request("delete", "GameRoot");
const error = new Error("storage denied");
calls[0].callback(error);
assert.deepEqual(errors, ["AUTOSAVE FAIL " + one]);
assert.deepEqual(messages, [["UI", "OnSavePersistenceResult", "fail save " + one]]);
flush();
calls[1].callback(null);
assert.deepEqual(messages[1], ["GameRoot", "OnSavePersistenceResult", "ok delete"]);
assert.deepEqual(logs, []);
''')


# Unity 6000.3.25f1's IDBFS as shipped in the Web build's framework.js, cut down to what decides the callback: syncfs ends in
# IDBFS.reconcile, which opens one readwrite transaction, calls back on the first request error or on `complete`, and
# sets no `onabort`. Unity's queuePersist (same file) starts a flush from a 0 ms timer and restarts it on 'again'.
UNITY_IDBFS = r'''
const txs = [], timers = [], requestErrors = [], completions = [];
class FakeTransaction {
  constructor() { this.error = null; this.aborts = []; }
  addEventListener(type, listener) { if (type === "abort") this.aborts.push(listener); }
  complete() { this.oncomplete({ target: this }); }
  abort(error = null) {  // a commit that fails after every put succeeded fires only `abort`
    this.error = error;
    for (const listener of this.aborts) listener({ target: this });
    if (this.onabort) this.onabort({ target: this });
  }
}
class FakeDatabase { transaction() { const tx = new FakeTransaction(); txs.push(tx); return tx; } }
const db = new FakeDatabase();
IDBFS.syncfs = function (target, populate, callback) {
  calls.push({ target, populate, callback });
  IDBFS.reconcile({ type: "local", entries: {} }, { type: "remote", db, entries: {} }, callback);
};
IDBFS.reconcile = function (src, dst, callback) {
  let errored = false;
  const transaction = (src.type === "remote" ? src.db : dst.db).transaction(["FILE_DATA"], "readwrite");
  function done(err) { if (err && !errored) { errored = true; return callback(err); } }
  transaction.onerror = e => { done(e.target.error); };
  transaction.oncomplete = () => { if (!errored) callback(null); };
  requestErrors.push(done);  // storeRemoteEntry's try/catch reports a throwing put through done
};
function queuePersist() {  // what a closed or renamed file under /idbfs calls
  function onPersistComplete(error) {
    completions.push(error);
    if (mount.idbPersistState === "again") startPersist(); else mount.idbPersistState = 0;
  }
  function startPersist() { mount.idbPersistState = "idb"; IDBFS.syncfs(mount, false, onPersistComplete); }
  if (!mount.idbPersistState) { mount.idbPersistState = "timer"; timers.push(startPersist); }
  else if (mount.idbPersistState === "idb") mount.idbPersistState = "again";
}
function autosave(value) { queuePersist(); save(value); }  // SaveStore's rename queues the flush, then GameStart asks
'''


def test_an_abort_at_commit_fails_the_save_and_releases_unitys_persist_queue():
    run(UNITY_IDBFS + r'''
autosave(one);
timers.shift()();
assert.equal(txs.length, 1);
txs[0].abort();  // a script abort leaves transaction.error null; a real quota abort carries QuotaExceededError
assert.deepEqual(errors, ["AUTOSAVE FAIL " + one]);
assert.deepEqual(messages, [["UI", "OnSavePersistenceResult", "fail save " + one]]);
assert.equal(completions.length, 1);
assert.ok(completions[0] instanceof Error);
assert.equal(mount.idbPersistState, 0);  // without a callback Unity stays at 'idb' and never flushes again
autosave(two);
assert.equal(timers.length, 1);
timers.shift()();
txs[1].complete();
assert.deepEqual(logs, ["AUTOSAVE OK " + two]);
assert.deepEqual(messages[1], ["UI", "OnSavePersistenceResult", "ok save " + two]);
assert.equal(Object.prototype.hasOwnProperty.call(db, "transaction"), false);  // the database is left as found
''')


def test_an_abort_carries_the_transactions_own_error_and_a_queued_followup_still_runs():
    run(UNITY_IDBFS + r'''
autosave(one);
timers.shift()();
autosave(two);  // written while the first snapshot is in flight: Unity marks 'again'
const quota = new Error("QuotaExceededError");
txs[0].abort(quota);
assert.deepEqual(errors, ["AUTOSAVE FAIL " + one]);
assert.deepEqual(completions, [quota]);
assert.equal(txs.length, 2);  // Unity's 'again' restarted the flush for the newer bytes
txs[1].complete();
assert.deepEqual(logs, ["AUTOSAVE OK " + two]);
assert.deepEqual(completions, [quota, null]);
''')


def test_an_abort_after_idbfs_already_failed_the_flush_does_not_call_back_twice():
    run(UNITY_IDBFS + r'''
autosave(one);
timers.shift()();
const thrown = new Error("put threw");
requestErrors[0](thrown);  // the path that already worked: a throwing put calls back at once
txs[0].abort(thrown);      // then the transaction aborts as well
assert.deepEqual(completions, [thrown]);
assert.deepEqual(errors, ["AUTOSAVE FAIL " + one]);
assert.equal(messages.length, 1);
''')


def test_a_committed_flush_reports_once_and_a_late_abort_listener_stays_silent():
    run(UNITY_IDBFS + r'''
autosave(one);
timers.shift()();
txs[0].complete();
assert.deepEqual(logs, ["AUTOSAVE OK " + one]);
assert.deepEqual(completions, [null]);
assert.equal(txs[0].aborts.length, 1);
txs[0].aborts[0]({ target: txs[0] });  // IndexedDB never fires both, but a stray abort must not add a FAIL
assert.deepEqual(errors, []);
assert.deepEqual(completions, [null]);
''')
