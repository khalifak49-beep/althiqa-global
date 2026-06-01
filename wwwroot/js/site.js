// ===== PWA Service Worker registration =====
if ('serviceWorker' in navigator) {
    window.addEventListener('load', () => {
        navigator.serviceWorker.register('/sw.js').catch(() => { });
    });
}

// ===== Install prompt (Add to Home Screen) =====
(function () {
    let deferredPrompt = null;
    window.addEventListener('beforeinstallprompt', (e) => {
        e.preventDefault();
        deferredPrompt = e;
        const btn = document.getElementById('pwaInstallBtn');
        if (btn) {
            btn.classList.remove('d-none');
            btn.addEventListener('click', async () => {
                btn.classList.add('d-none');
                deferredPrompt.prompt();
                await deferredPrompt.userChoice;
                deferredPrompt = null;
            });
        }
    });
})();

(function () {
    // ===== Theme toggle (light / dark) =====
    const html = document.documentElement;
    const saved = localStorage.getItem('hm-theme');
    if (saved) html.setAttribute('data-bs-theme', saved);
    const btn = document.getElementById('themeToggle');
    function syncIcon() {
        if (!btn) return;
        const i = btn.querySelector('i');
        if (!i) return;
        i.className = html.getAttribute('data-bs-theme') === 'dark' ? 'bi bi-sun' : 'bi bi-moon-stars';
    }
    syncIcon();
    if (btn) {
        btn.addEventListener('click', () => {
            const next = html.getAttribute('data-bs-theme') === 'dark' ? 'light' : 'dark';
            html.setAttribute('data-bs-theme', next);
            localStorage.setItem('hm-theme', next);
            syncIcon();
        });
    }

    // ===== Notifications poller =====
    const badge = document.getElementById('notifBadge');
    if (badge) {
        async function poll() {
            try {
                const r = await fetch('/Notifications/Unread', { credentials: 'include' });
                if (!r.ok) return;
                const data = await r.json();
                if (data.count > 0) {
                    badge.classList.remove('d-none');
                    badge.textContent = data.count > 9 ? '9+' : data.count;
                } else {
                    badge.classList.add('d-none');
                }
            } catch (e) { /* ignore */ }
        }
        poll();
        setInterval(poll, 60000);
    }

    // ===== Live chat widget =====
    const chatToggle = document.getElementById('chatToggle');
    const chatWin = document.getElementById('chatWindow');
    if (chatToggle && chatWin) {
        chatToggle.addEventListener('click', () => chatWin.classList.toggle('open'));
        const send = document.getElementById('chatSend');
        const input = document.getElementById('chatInput');
        const body = document.getElementById('chatBody');
        function reply(text) {
            const lower = text.toLowerCase();
            if (lower.includes('سعر') || lower.includes('price')) return 'الأسعار تبدأ من 30 ريال للساعة، وتختلف حسب العاملة.';
            if (lower.includes('حجز') || lower.includes('book')) return 'تستطيع الحجز من صفحة العاملات بالضغط على زر "احجز الآن".';
            if (lower.includes('إلغاء') || lower.includes('cancel')) return 'يمكن الإلغاء من صفحة الحجز قبل 6 ساعات من الموعد.';
            if (lower.includes('دفع') || lower.includes('pay')) return 'ندعم Visa, MasterCard, Apple Pay, Google Pay و PayPal.';
            return 'شكراً لتواصلك! سيرد عليك أحد ممثلي خدمة العملاء في أقرب وقت.';
        }
        function append(role, text) {
            const div = document.createElement('div');
            div.className = 'msg ' + (role === 'me' ? 'text-start' : '');
            div.textContent = (role === 'me' ? 'أنت: ' : 'الدعم: ') + text;
            body.appendChild(div);
            body.scrollTop = body.scrollHeight;
        }
        function go() {
            const v = (input.value || '').trim();
            if (!v) return;
            append('me', v); input.value = '';
            setTimeout(() => append('bot', reply(v)), 500);
        }
        send && send.addEventListener('click', go);
        input && input.addEventListener('keydown', e => { if (e.key === 'Enter') go(); });
    }

    // ===== Favorite toggle =====
    document.querySelectorAll('.fav-btn[data-worker]').forEach(btn => {
        btn.addEventListener('click', async (e) => {
            e.preventDefault();
            const id = btn.getAttribute('data-worker');
            const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;
            try {
                const r = await fetch('/Workers/ToggleFavorite', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/x-www-form-urlencoded', 'RequestVerificationToken': token || '' },
                    body: 'workerId=' + id,
                    credentials: 'include'
                });
                if (r.status === 401) { location.href = '/Account/Login'; return; }
                const data = await r.json();
                const icon = btn.querySelector('i');
                if (icon) icon.className = data.isFavorite ? 'bi bi-heart-fill' : 'bi bi-heart';
            } catch (err) { console.warn(err); }
        });
    });

    // ===== Live booking quote on Create page =====
    const quoteBox = document.getElementById('priceQuote');
    if (quoteBox) {
        const wid = quoteBox.getAttribute('data-worker');
        const hoursEl = document.getElementById('Hours');
        const couponEl = document.getElementById('CouponCode');
        const msgEl = document.getElementById('couponMsg');
        const btnApply = document.getElementById('btnApplyCoupon');
        async function refresh() {
            const hours = parseInt(hoursEl?.value || '0', 10);
            if (!hours) return;
            const url = `/Bookings/Quote?workerId=${wid}&hours=${hours}&code=${encodeURIComponent(couponEl?.value || '')}`;
            const r = await fetch(url);
            if (!r.ok) return;
            const q = await r.json();
            quoteBox.querySelector('[data-f="sub"]').textContent = q.subTotal.toFixed(3);
            quoteBox.querySelector('[data-f="disc"]').textContent = q.discount.toFixed(3);
            quoteBox.querySelector('[data-f="tax"]').textContent = q.tax.toFixed(3);
            quoteBox.querySelector('[data-f="total"]').textContent = q.total.toFixed(3);
            if (msgEl) {
                const status = q.couponStatus || 'none';
                msgEl.textContent = q.couponMessage || '';
                msgEl.className = 'small mt-1 ' + (
                    status === 'applied' ? 'text-success fw-bold' :
                    status === 'min_amount' ? 'text-warning' :
                    (status === 'invalid' || status === 'expired') ? 'text-danger' : 'text-muted'
                );
            }
        }
        hoursEl && hoursEl.addEventListener('change', refresh);
        couponEl && couponEl.addEventListener('input', () => clearTimeout(window.__cTO) || (window.__cTO = setTimeout(refresh, 400)));
        btnApply && btnApply.addEventListener('click', refresh);
        refresh();
    }

    // ===== Available start-time loader =====
    const slotsEl = document.getElementById('StartTime');
    if (slotsEl && slotsEl.dataset.dynamic === '1') {
        const wid = slotsEl.dataset.worker;
        const dateEl = document.getElementById('BookingDate');
        const hoursEl = document.getElementById('Hours');
        async function loadSlots() {
            const date = dateEl?.value, hours = hoursEl?.value;
            if (!date || !hours) return;
            const r = await fetch(`/Bookings/AvailableTimes?workerId=${wid}&date=${date}&hours=${hours}`);
            if (!r.ok) return;
            const items = await r.json();
            slotsEl.innerHTML = '';
            if (!items.length) {
                const opt = document.createElement('option'); opt.disabled = true; opt.textContent = 'لا توجد أوقات متاحة';
                slotsEl.appendChild(opt); return;
            }
            for (const it of items) {
                const opt = document.createElement('option');
                opt.value = it.value; opt.textContent = it.text;
                slotsEl.appendChild(opt);
            }
        }
        dateEl && dateEl.addEventListener('change', loadSlots);
        hoursEl && hoursEl.addEventListener('change', loadSlots);
        loadSlots();
    }

    // ===== Star rating selector =====
    document.querySelectorAll('.rate-stars').forEach(host => {
        const input = document.querySelector(host.dataset.target);
        const stars = host.querySelectorAll('i');
        function paint(n) { stars.forEach((s, i) => s.classList.toggle('active', i < n)); }
        stars.forEach((s, i) => {
            s.addEventListener('mouseover', () => stars.forEach((x, j) => x.classList.toggle('hover', j <= i)));
            s.addEventListener('mouseout', () => stars.forEach(x => x.classList.remove('hover')));
            s.addEventListener('click', () => { input.value = i + 1; paint(i + 1); });
        });
        paint(parseInt(input.value || '5', 10));
    });
})();
