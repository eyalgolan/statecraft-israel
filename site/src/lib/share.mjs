// Share only a rendered canonical: never the visitor's query or fragment.
export function createSharing({ url, title, share, writeText, secure = true }, onState = () => {}) {
  const canonical = new URL(url);
  if (!['https:', 'http:'].includes(canonical.protocol)) throw new TypeError('Expected an HTTP canonical URL');
  canonical.search = '';
  canonical.hash = '';
  const payload = { url: canonical.href, title };
  const canShare = secure && typeof share === 'function';
  let busy = false;

  return {
    url: payload.url,
    canShare,
    get busy() { return busy; },
    async share() {
      if (busy || !canShare) return;
      busy = true;
      onState('sharing');
      let result = 'idle';
      try {
        await share(payload);
      } catch (error) {
        // AbortError also covers no available targets; don't claim a reason or copy automatically.
        if (error?.name !== 'AbortError') result = 'share-unavailable';
      } finally {
        busy = false;
        onState(result);
      }
    },
    async copy() {
      if (busy) return;
      if (!secure || typeof writeText !== 'function') { onState('manual-copy'); return; }
      busy = true;
      onState('copying');
      let result = 'copied';
      try {
        await writeText(payload.url);
      } catch {
        result = 'manual-copy';
      } finally {
        busy = false;
        onState(result);
      }
    },
  };
}
