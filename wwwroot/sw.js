// Al Thiqa PWA service worker — minimal install + static asset caching.
const CACHE = 'al-thiqa-v3';

self.addEventListener('install', () => self.skipWaiting());

self.addEventListener('activate', (event) => {
    event.waitUntil((async () => {
        const keys = await caches.keys();
        await Promise.all(keys.filter(k => k !== CACHE).map(k => caches.delete(k)));
        await self.clients.claim();
    })());
});

self.addEventListener('fetch', (event) => {
    const req = event.request;
    if (req.method !== 'GET') return;
    const url = new URL(req.url);
    if (url.origin !== self.location.origin) return;

    // Cache-first for static assets, network-first for everything else.
    if (/\.(css|js|svg|png|jpe?g|webp|woff2?|ico)$/i.test(url.pathname)) {
        event.respondWith((async () => {
            const cache = await caches.open(CACHE);
            const cached = await cache.match(req);
            if (cached) {
                fetch(req).then(r => { if (r.ok) cache.put(req, r); }).catch(() => { });
                return cached;
            }
            try {
                const fresh = await fetch(req);
                if (fresh.ok) cache.put(req, fresh.clone());
                return fresh;
            } catch (e) {
                return new Response('', { status: 503 });
            }
        })());
    }
});
