
'use strict';

/* ─── Global helpers ─── */
function showToast(msg, cls) {
    var t = document.getElementById('toast');
    var m = document.getElementById('toast-msg');
    if (!t || !m) return;
    t.className = 'toast ' + (cls || 't-lime');
    m.textContent = msg;
    t.classList.add('show');
    setTimeout(function () { t.classList.remove('show'); }, 3200);
}


/* ─── Cursor ─── */
const dot = document.getElementById('cursor-dot');
const ring = document.getElementById('cursor-ring');
let mx = 0, my = 0, rx = 0, ry = 0;
document.addEventListener('mousemove', e => { mx = e.clientX; my = e.clientY; dot.style.left = mx + 'px'; dot.style.top = my + 'px'; });
(function animRing() {
    rx += (mx - rx) * .12; ry += (my - ry) * .12;
    ring.style.left = rx + 'px'; ring.style.top = ry + 'px';
    requestAnimationFrame(animRing);
})();
document.addEventListener('mousedown', () => document.body.classList.add('cursor-click'));
document.addEventListener('mouseup', () => document.body.classList.remove('cursor-click'));
document.addEventListener('mouseleave', () => { dot.style.opacity = '0'; ring.style.opacity = '0'; });
document.addEventListener('mouseenter', () => { dot.style.opacity = '1'; ring.style.opacity = ''; });
document.querySelectorAll('a,button,[onclick],.portal-card,.job-row,.appl-row,.alert-row,.sb-link,.tab-btn,.prof-btn').forEach(el => {
    el.addEventListener('mouseenter', () => document.body.classList.add('cursor-hover'));
    el.addEventListener('mouseleave', () => document.body.classList.remove('cursor-hover'));
});

const liveEvents = [
    { t: '🆕 New registration', d: 'A student from COMSATS just registered and completed profile setup.', dot: 'live-dot-register' },
    { t: '📋 Application submitted', d: 'New application to Frontend Dev with 72% match score.', dot: 'live-dot-apply' },
    { t: '💼 Job updated', d: 'Arbisoft extended deadline for Software Engineer Intern by 2 weeks.', dot: 'live-dot-job' },
    { t: '✅ Status change', d: 'Applicant shortlisted by NetSol for React Native Developer role.', dot: 'live-dot-status' },
    { t: '🏢 Company registered', d: 'New company PixelForge submitted for admin approval.', dot: 'live-dot-company' },
    { t: '📈 Score improved', d: 'Student rank jumped from #18 to #11 after adding Docker skill.', dot: 'live-dot-score' },
    { t: '💼 Job posted', d: 'Systems Ltd posted "Senior Frontend Developer" — 200+ students match.', dot: 'live-dot-job' },
];
setInterval(() => {
    const feed = document.getElementById('live-feed'); if (!feed) return;
    const e = liveEvents[Math.floor(Math.random() * liveEvents.length)];
    const row = document.createElement('div'); row.className = 'live-row';
    row.style.opacity = '0'; row.style.transition = 'opacity .4s';
    row.innerHTML = `<div class="live-dot ${e.dot}"></div><div><div class="live-t">${e.t}</div><div class="live-d">${e.d}</div><div class="live-ts">JUST NOW</div></div>`;
    feed.insertBefore(row, feed.firstChild);
    setTimeout(() => row.style.opacity = '1', 20);
    if (feed.children.length > 8) feed.removeChild(feed.lastChild);
}, 5000);

/* ─── Job card filter ─── */
function filterJobs(type, btn) {
    document.querySelectorAll('.jp-filter-btn').forEach(b => b.classList.remove('jpf-on'));
    btn.classList.add('jpf-on');
    document.querySelectorAll('#jp-cards-grid .jp-card').forEach(card => {
        if (type === 'all') {
            card.style.display = '';
            card.style.animation = 'fadeUp .35s ease both';
        } else {
            const match = card.dataset.type === type;
            card.style.display = match ? '' : 'none';
            if (match) card.style.animation = 'fadeUp .35s ease both';
        }
    });
}

/* ─── Animate jp-progress-fills on entry ─── */
function animateJpFills() {
    document.querySelectorAll('.jp-progress-fill').forEach(fill => {
        const w = fill.style.width;
        fill.style.width = '0%';
        setTimeout(() => { fill.style.width = w; }, 80);
    });
}
// Hook into cPanel to trigger animation when Jobs panel opens
const _origCPanel = window.cPanel;
window.cPanel = function (id, el) {
    _origCPanel && _origCPanel(id, el);
    if (id === 'jobs') setTimeout(animateJpFills, 150);
};
window.addEventListener('load', () => {
    // also trigger if jobs panel is already active
    if (document.querySelector('#cp-jobs.on')) animateJpFills();
});

/* ─── Animate application rings & bars when Applications panel opens ─── */
function animateApplicationCards() {
    // Animate bars
    document.querySelectorAll('.acr-bar-fill').forEach((fill, i) => {
        const target = fill.dataset.targetWidth;
        fill.style.width = '0%';
        setTimeout(() => { fill.style.width = target; }, 100 + i * 80);
    });
    // Animate rings
    document.querySelectorAll('.ring-animate').forEach((circle, i) => {
        const offset = parseFloat(circle.dataset.offset);
        const totalDash = 113;
        circle.style.strokeDashoffset = String(totalDash);
        setTimeout(() => {
            circle.style.transition = 'stroke-dashoffset 1.3s cubic-bezier(.4,0,.2,1)';
            circle.style.strokeDashoffset = String(offset);
        }, 150 + i * 120);
    });
}

/* ─── Animate match rings on overview panel (on-load) ─── */
function animateOverviewRings() {
    const rings = [
        { id: 'ring-ov1', pct: 87, color: '#1a7a3a' }
    ];
    document.querySelectorAll('#sp-overview .match-ring-wow svg circle.ring-fill-high, #sp-overview .match-ring-wow svg circle.ring-fill-mid, #sp-overview .match-ring-wow svg circle.ring-fill-low').forEach((c, i) => {
        const originalOffset = c.getAttribute('stroke-dashoffset');
        c.setAttribute('stroke-dashoffset', '145');
        setTimeout(() => {
            c.style.transition = 'stroke-dashoffset 1.2s cubic-bezier(.4,0,.2,1)';
            c.setAttribute('stroke-dashoffset', originalOffset);
        }, 300 + i * 200);
    });
    // Also animate browse jobs rings
    document.querySelectorAll('#sp-jobs .match-ring-wow svg circle.ring-fill-high, #sp-jobs .match-ring-wow svg circle.ring-fill-mid, #sp-jobs .match-ring-wow svg circle.ring-fill-low').forEach((c, i) => {
        const originalOffset = c.getAttribute('stroke-dashoffset');
        c.setAttribute('stroke-dashoffset', '145');
        setTimeout(() => {
            c.style.transition = 'stroke-dashoffset 1.2s cubic-bezier(.4,0,.2,1)';
            c.setAttribute('stroke-dashoffset', originalOffset);
        }, 400 + i * 150);
    });
}

/* ─── Hook sPanel to trigger animations ─── */
const _origSPanel = window.sPanel;
window.sPanel = function (id, el) {
    _origSPanel && _origSPanel(id, el);
    if (id === 'applications') setTimeout(animateApplicationCards, 150);
    if (id === 'overview') setTimeout(animateOverviewRings, 150);
    if (id === 'jobs') setTimeout(() => {
        document.querySelectorAll('#sp-jobs .match-ring-wow svg circle.ring-fill-high, #sp-jobs .match-ring-wow svg circle.ring-fill-mid, #sp-jobs .match-ring-wow svg circle.ring-fill-low').forEach((c, i) => {
            const o = c.getAttribute('stroke-dashoffset');
            c.setAttribute('stroke-dashoffset', '145');
            setTimeout(() => { c.style.transition = 'stroke-dashoffset 1.2s cubic-bezier(.4,0,.2,1)'; c.setAttribute('stroke-dashoffset', o); }, 100 + i * 120);
        });
    }, 150);
};

/* ─── On first page load, animate overview ─── */
window.addEventListener('load', () => {
    setTimeout(animateOverviewRings, 600);
    // Init sidebar mini ring (74%)
    const sbRing = document.getElementById('sb-mini-ring');
    if (sbRing) {
        sbRing.style.strokeDashoffset = '75.4';
        setTimeout(() => {
            sbRing.style.transition = 'stroke-dashoffset 1.4s cubic-bezier(.4,0,.2,1)';
            sbRing.style.strokeDashoffset = '20'; // 74% of 75.4
        }, 800);
    }
});


window.addEventListener('scroll', () => {
    document.getElementById('topbar').classList.toggle('scrolled', window.scrollY > 10);
});

/* ─── Keyboard backlight glow on outside click ─── */
document.addEventListener('click', (e) => {
    const grid = document.querySelector('.portals-grid');
    if (!grid) return;
    // Only fire when clicking OUTSIDE the portal cards
    if (!e.target.closest('.portal-card')) {
        const cards = document.querySelectorAll('.portal-card');
        const glowClasses = ['kb-glow-s', 'kb-glow-c', 'kb-glow-a'];
        cards.forEach((c, i) => {
            c.classList.remove(...glowClasses);
            // stagger the glow slightly like a keyboard wave
            setTimeout(() => {
                c.classList.add(glowClasses[i]);
                setTimeout(() => c.classList.remove(glowClasses[i]), 700);
            }, i * 80);
        });
    }
});

/* ─── Scroll progress bar ─── */
(function () {
    const bar = document.createElement('div');
    bar.id = 'scroll-progress';
    bar.style.cssText = `
    position:fixed; top:0; left:0; z-index:9999;
    height:2px; width:0%;
    background:linear-gradient(to right, var(--lime-d), var(--lime));
    transition:width .1s linear;
    pointer-events:none;
    box-shadow:0 0 8px rgba(143,212,0,.5);
  `;
    document.body.appendChild(bar);
    window.addEventListener('scroll', () => {
        const scrolled = window.scrollY;
        const max = document.documentElement.scrollHeight - window.innerHeight;
        bar.style.width = max > 0 ? (scrolled / max * 100) + '%' : '0%';
    });
})();

/* ─── Intersection observer: fade-up stat cells & feature cells ─── */
(function () {
    const targets = document.querySelectorAll('.stat-cell, .feature-cell, .portal-card, .profile-section-block');
    const obs = new IntersectionObserver((entries) => {
        entries.forEach(e => {
            if (e.isIntersecting) {
                e.target.style.opacity = '1';
                e.target.style.transform = 'translateY(0)';
                obs.unobserve(e.target);
            }
        });
    }, { threshold: 0.1 });
    targets.forEach(t => {
        t.style.opacity = '0';
        t.style.transform = 'translateY(18px)';
        t.style.transition = 'opacity .5s ease, transform .5s cubic-bezier(.4,0,.2,1)';
        obs.observe(t);
    });
})();

/* ─── Button ripple effect ─── */
document.addEventListener('click', function (e) {
    const btn = e.target.closest('.btn');
    if (!btn) return;
    const ripple = document.createElement('span');
    const rect = btn.getBoundingClientRect();
    const size = Math.max(rect.width, rect.height) * 1.4;
    ripple.style.cssText = `
    position:absolute; width:${size}px; height:${size}px;
    left:${e.clientX - rect.left - size / 2}px;
    top:${e.clientY - rect.top - size / 2}px;
    background:rgba(255,255,255,.2); border-radius:50%;
    transform:scale(0); animation:rippleOut .5s ease-out forwards;
    pointer-events:none; z-index:10;
  `;
    btn.style.position = 'relative';
    btn.style.overflow = 'hidden';
    btn.appendChild(ripple);
    setTimeout(() => ripple.remove(), 500);
});
const rs = document.createElement('style');
rs.textContent = '@keyframes rippleOut{to{transform:scale(1);opacity:0}}';
document.head.appendChild(rs);
