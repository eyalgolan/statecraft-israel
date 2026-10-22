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
const mount = {}, other = {}, calls = [], logs = [], errors = [], callbacks = [];
const Module = { __unityIdbfsMount: { mount }, autoSyncPersistentDataPath: true };
const LibraryManager = { library: {} };
function mergeInto(target, value) { Object.assign(target, value); }
function UTF8ToString(value) { return value; }
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
function flush(target = mount, populate = false) {
  return IDBFS.syncfs(target, populate, error => callbacks.push(error));
}
''' + body
    return subprocess.run(["node", "-e", driver], capture_output=True, text=True, check=True)


def test_save_ack_waits_for_the_existing_sync_callback_and_starts_no_sync():
    run(r'''
request(one);
assert.equal(calls.length, 0);  // file close already queued Unity's timer; the observer must not launch a competing sync
assert.deepEqual(logs, []);
assert.equal(flush(), "existing sync result");
assert.deepEqual(logs, []);
calls[0].callback(null);
assert.deepEqual(logs, ["AUTOSAVE OK " + one]);
assert.deepEqual(callbacks, [null]);
assert.deepEqual(errors, []);
''')


def test_a_first_request_during_an_older_sync_waits_for_the_followup():
    run(r'''
flush();  // Unity was already syncing before our observer was installed
request(one);
calls[0].callback(null);
assert.deepEqual(logs, []);
flush();
calls[1].callback(null);
assert.deepEqual(logs, ["AUTOSAVE OK " + one]);
assert.equal(calls.length, 2);
''')


def test_each_generation_belongs_to_the_snapshot_that_started_after_its_request():
    run(r'''
request(one);
IDBFS.syncfs(mount, false, error => { callbacks.push(error); flush(); }); // Unity's 'again' callback starts the next sync
request(two);  // writes made while the first snapshot is in flight
calls[0].callback(null);
assert.equal(calls.length, 2);
assert.deepEqual(logs, []);  // a superseded snapshot must not claim either exact file state
calls[1].callback(null);
assert.deepEqual(logs, ["AUTOSAVE OK " + two]);
assert.deepEqual(callbacks, [null, null]);
''')


def test_sync_errors_are_not_success_and_do_not_poison_the_next_save():
    run(r'''
request(one);
flush();
const error = new Error("storage denied");
calls[0].callback(error);
assert.deepEqual(logs, []);
assert.deepEqual(errors, ["AUTOSAVE FAIL " + one]);
assert.equal(callbacks[0], error);
request(two);
flush();
calls[1].callback(null);
assert.deepEqual(logs, ["AUTOSAVE OK " + two]);
''')


def test_boot_populate_and_other_mounts_pass_through_without_taking_save_requests():
    run(r'''
request(one);
assert.equal(flush(mount, true), "existing sync result");
flush(other);
calls[0].callback(null); calls[1].callback(null);
assert.deepEqual(logs, []);
flush(); calls[2].callback(null);
assert.deepEqual(logs, ["AUTOSAVE OK " + one]);
assert.equal(calls[0].populate, true);
assert.equal(calls[1].target, other);
''')


@pytest.mark.parametrize("disable", ["Module.autoSyncPersistentDataPath = false;", "delete Module.__unityIdbfsMount;"])
def test_missing_autosync_fails_instead_of_announcing_durability(disable):
    run(disable + r'''
request(one);
assert.deepEqual(errors, ["AUTOSAVE FAIL " + one]);
assert.deepEqual(logs, []);
assert.equal(calls.length, 0);
''')


def test_multiple_writes_before_one_flush_acknowledge_only_the_latest_file_contents():
    run(r'''
request(one);
request(two);  // one file path now contains two's bytes, not one's
flush();
calls[0].callback(null);
assert.deepEqual(logs, ["AUTOSAVE OK " + two]);
''')
