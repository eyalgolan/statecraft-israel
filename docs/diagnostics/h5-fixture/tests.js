"use strict";

// Synthetic output-only checks. Not Unity integration or screen-reader tests.
window.runH5Checks = async function (target) {
  const doc = target.document;
  const host = doc.querySelector("#mount");
  const outside = doc.querySelector("#outside");
  const passes = [];
  function check(value, name) {
    if (!value) throw new Error(name);
    passes.push(name);
  }
  const frame = () => new Promise(resolve => target.requestAnimationFrame(() => target.requestAnimationFrame(resolve)));
  const initial = {
    version: 1, epoch: 1, sequence: 0, language: "en", focus: "End Turn",
    title: "Synthetic decision", body: '<img src=x onerror="window.injected=true">',
    options: ["<script>bad</" + "script>", "Second option"], announcement: "Not done: synthetic refusal"
  };
  let bridge = target.createH5Fixture(host, 1);
  try {
    outside.focus();
    check(bridge.receive(initial), "accept versioned synthetic payload");
    await frame();
    check(doc.activeElement === outside, "announcements never steal focus");
    check(!host.querySelector("img,script") && host.querySelector("h3+p").textContent === initial.body &&
      !target.injected, "untrusted markup rendered literally");
    check(host.querySelector("[role=status]").textContent === initial.announcement, "status receives refusal text");
    check([...host.querySelectorAll("li")].map(x => x.textContent).join("|") === initial.options.join("|"),
      "source option order retained");
    const before = host.innerHTML;
    check(!bridge.receive({...initial, sequence: 1, title: "must not appear", options: ["ok", null]}),
      "malformed payload rejected");
    check(host.innerHTML === before, "invalid payload leaves every DOM field unchanged");
    check(!bridge.receive(initial), "duplicate sequence rejected");
    check(!bridge.receive({...initial, epoch: 0, sequence: 99}), "old session rejected");
    check(!bridge.receive({...initial, version: 2, sequence: 99}), "unknown schema rejected");
    check(bridge.receive({...initial, sequence: 1, language: "he", focus: "סיום תור", body: "טקסט בדיקה"}),
      "Hebrew payload accepted");
    await frame();
    check(host.firstElementChild.lang === "he" && host.firstElementChild.dir === "rtl", "Hebrew language and direction exposed");
    check(host.querySelector("[role=status]").textContent === initial.announcement, "same refusal repopulates region");
    check(bridge.receive({...initial, sequence: 2, announcement: "stale callback"}), "queued announcement accepted");
    const disposed = bridge;
    bridge.dispose();
    bridge.dispose();
    check(!disposed.receive({...initial, sequence: 999}), "disposed root rejects late payload");
    bridge = target.createH5Fixture(host, 2);
    check(bridge.receive({...initial, epoch: 2, title: "New synthetic session"}), "new session starts at sequence zero");
    await frame();
    check(!host.textContent.includes("stale callback") && host.querySelectorAll("section").length === 1,
      "teardown cancels pending callback without duplicate region");
    check(doc.activeElement === outside, "new session preserves unrelated focus");
    bridge.receive({...initial, epoch: 2, sequence: 1, announcement: "superseded"});
    bridge.receive({...initial, epoch: 2, sequence: 2, announcement: "newest"});
    await frame();
    check(host.querySelector("[role=status]").textContent === "newest", "latest update cancels superseded announcement");
    let duplicateRejected = false;
    try { target.createH5Fixture(host, 3); } catch { duplicateRejected = true; }
    check(duplicateRejected && host.querySelectorAll("section").length === 1, "duplicate mount rejected without extra region");
    return passes;
  } finally {
    bridge.dispose();
  }
};

const run = document.querySelector("#run");
const results = document.querySelector("#results");
const fixture = document.querySelector("iframe");
let runNumber = 0;
fixture.addEventListener("load", () => { run.disabled = false; });
run.addEventListener("click", async () => {
  run.disabled = true;
  const iteration = ++runNumber;
  results.textContent = "Running isolated synthetic checks…";
  try {
    const passes = await window.runH5Checks(fixture.contentWindow);
    results.dataset.result = "passed";
    results.textContent = "Run " + iteration + ": " + passes.length + " isolated checks passed.\n" + passes.join("\n") +
      "\nNot a screen-reader, Unity-transport or game-operability test.";
  } catch (error) {
    results.dataset.result = "failed";
    results.textContent = "FAILED: " + error;
  } finally {
    run.disabled = false;
  }
});
