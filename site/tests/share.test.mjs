import test from 'node:test';
import assert from 'node:assert/strict';
import { createSharing } from '../src/lib/share.mjs';

const title = 'Statecraft: Israel';
const canonical = 'https://example.test/statecraft-israel/en/';
const deferred = () => {
  let resolve, reject;
  const promise = new Promise((yes, no) => { resolve = yes; reject = no; });
  return { promise, resolve, reject };
};
const namedError = (name) => Object.assign(new Error(name), { name });

for (const url of [
  'https://example.test/statecraft-israel/en/',
  'https://example.test/statecraft-israel/he/',
  'https://example.test/en/',
  'https://example.test/he/',
]) {
  test(`canonical language/base path retained without query or fragment: ${url}`, () => {
    const controller = createSharing({ url: `${url}?token=private-sentinel#private-fragment`, title });
    assert.equal(controller.url, url);
    assert.ok(!controller.url.includes('private-sentinel'));
    assert.ok(!controller.url.includes('private-fragment'));
  });
}

test('rejects a non-HTTP canonical instead of putting it into a share payload', () => {
  assert.throws(() => createSharing({ url: 'javascript:alert(1)', title }), TypeError);
});

test('native sharing receives the existing canonical and title once, and is available only with an API', async () => {
  const payloads = [], states = [];
  const controller = createSharing({ url: canonical, title, share: async (payload) => payloads.push(payload) }, (state) => states.push(state));
  assert.equal(controller.canShare, true);
  await controller.share();
  assert.deepEqual(payloads, [{ url: canonical, title }]);
  assert.deepEqual(states, ['sharing', 'idle']);
  assert.equal(controller.busy, false);
  assert.equal(createSharing({ url: canonical, title }).canShare, false);
});

test('a pending native request prevents a second share and returns to idle on resolution', async () => {
  const pending = deferred(); let calls = 0;
  const controller = createSharing({ url: canonical, title, share: () => { calls++; return pending.promise; } });
  const first = controller.share();
  assert.equal(controller.busy, true);
  await controller.share();
  assert.equal(calls, 1);
  pending.resolve(); await first;
  assert.equal(controller.busy, false);
});

test('AbortError stays quiet and never automatically copies or reports success', async () => {
  let copies = 0; const states = [];
  const controller = createSharing({ url: canonical, title, share: async () => { throw namedError('AbortError'); }, writeText: async () => { copies++; } }, (state) => states.push(state));
  await controller.share();
  assert.deepEqual(states, ['sharing', 'idle']);
  assert.equal(copies, 0);
  assert.equal(controller.busy, false);
});

for (const name of ['NotAllowedError', 'DataError']) {
  test(`native ${name} leaves an explicit copy action usable without auto-copy`, async () => {
    const copied = [], states = [];
    const controller = createSharing({ url: canonical, title, share: () => { throw namedError(name); }, writeText: async (url) => copied.push(url) }, (state) => states.push(state));
    await controller.share();
    assert.deepEqual(states, ['sharing', 'share-unavailable']);
    assert.deepEqual(copied, []);
    assert.equal(controller.busy, false);
    await controller.copy();
    assert.deepEqual(copied, [canonical]);
    assert.equal(states.at(-1), 'copied');
  });
}

test('Copied is emitted only after clipboard resolves, with the sanitized canonical', async () => {
  const pending = deferred(), copied = [], states = [];
  const controller = createSharing({ url: `${canonical}?token=private-sentinel#fragment`, title, writeText: (url) => { copied.push(url); return pending.promise; } }, (state) => states.push(state));
  const copying = controller.copy();
  assert.deepEqual(copied, [canonical]);
  assert.ok(!states.includes('copied'));
  assert.equal(controller.busy, true);
  pending.resolve(); await copying;
  assert.equal(states.at(-1), 'copied');
  assert.equal(controller.busy, false);
});

test('denied clipboard selects a manual-copy state without a false success', async () => {
  const states = [];
  const controller = createSharing({ url: canonical, title, writeText: async () => { throw namedError('NotAllowedError'); } }, (state) => states.push(state));
  await controller.copy();
  assert.equal(states.at(-1), 'manual-copy');
  assert.ok(!states.includes('copied'));
  assert.equal(controller.url, canonical);
  assert.equal(controller.busy, false);
});

test('missing clipboard keeps the canonical available for manual selection', async () => {
  const states = [];
  const controller = createSharing({ url: canonical, title }, (state) => states.push(state));
  await controller.copy();
  assert.deepEqual(states, ['manual-copy']);
  assert.equal(controller.url, canonical);
});

test('insecure contexts do not invoke restricted APIs and retain manual selection', async () => {
  let shares = 0, copies = 0; const states = [];
  const controller = createSharing({ url: canonical, title, secure: false, share: async () => { shares++; }, writeText: async () => { copies++; } }, (state) => states.push(state));
  assert.equal(controller.canShare, false);
  await controller.share(); await controller.copy();
  assert.equal(shares, 0); assert.equal(copies, 0);
  assert.equal(states.at(-1), 'manual-copy');
});

test('a pending copy prevents duplicate writes and is recoverable after failure', async () => {
  const pending = deferred(); let writes = 0; const states = [];
  const controller = createSharing({ url: canonical, title, writeText: () => { writes++; return pending.promise; } }, (state) => states.push(state));
  const copying = controller.copy(); await controller.copy();
  assert.equal(writes, 1);
  pending.reject(namedError('NotAllowedError')); await copying;
  assert.equal(controller.busy, false);
  assert.equal(states.at(-1), 'manual-copy');
});
