const COUNTER_IDS = ['pods-count', 'services-count', 'deployments-count', 'cronjobs-count', 'nodes-count'];
const DURATION_MS = 1000;

let currentFrame = null;

function animateCounters(counters, duration = DURATION_MS) {
    cancel(); // in case a previous run is still in flight

    const start = performance.now();

    function tick(now) {
        const progress = Math.min((now - start) / duration, 1);

        for (const { el, target } of counters) {
            el.textContent = Math.floor(target * progress).toString();
        }

        currentFrame = progress < 1 ? requestAnimationFrame(tick) : null;
    }

    currentFrame = requestAnimationFrame(tick);
}

function cancel() {
    if (currentFrame !== null) {
        cancelAnimationFrame(currentFrame);
        currentFrame = null;
    }
}

function setupClusterCounters() {
    const prefersReducedMotion = window.matchMedia('(prefers-reduced-motion: reduce)').matches;

    // Targets come from the server-rendered text content — no interop needed.
    const counters = COUNTER_IDS
        .map(id => document.getElementById(id))
        .filter(el => el !== null)
        .map(el => ({ el, target: parseInt(el.textContent, 10) }))
        .filter(c => Number.isInteger(c.target) && c.target > 0);

    if (counters.length === 0) {
        return; // Stats service unreachable, or not on a page with the section.
    }

    // If user prefers reduced motion, leave the server-rendered final values as-is.
    if (prefersReducedMotion) {
        return;
    }

    // Set counters to 0 in preparation for the scroll-triggered animation.
    for (const { el } of counters) {
        el.textContent = '0';
    }

    const targetSection = document.getElementById('stats-list') || counters[0].el;

    if (!('IntersectionObserver' in window)) {
        animateCounters(counters);
        return;
    }

    const observer = new IntersectionObserver((entries, obs) => {
        for (const entry of entries) {
            if (entry.isIntersecting) {
                obs.disconnect();
                animateCounters(counters);
                break;
            }
        }
    }, {
        threshold: 0.15
    });

    observer.observe(targetSection);
}

if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', setupClusterCounters, { once: true });
} else {
    setupClusterCounters();
}
