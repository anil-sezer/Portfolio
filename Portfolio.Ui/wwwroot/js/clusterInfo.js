const COUNTER_IDS = ['pods-count', 'services-count', 'deployments-count', 'cronjobs-count', 'nodes-count'];
const DURATION_MS = 1000;

let currentFrame = null;

function animateCounters(duration = DURATION_MS) {
    cancel(); // in case a previous run is still in flight

    // Targets come from the server-rendered text content — no interop needed.
    const counters = COUNTER_IDS
        .map(id => document.getElementById(id))
        .filter(el => el !== null)
        .map(el => ({ el, target: parseInt(el.textContent, 10) }))
        .filter(c => Number.isInteger(c.target) && c.target > 0);

    if (counters.length === 0) {
        return; // Stats service unreachable, or not on a page with the section.
    }

    // Reset to 0 synchronously so there's no flash of the final SSR value.
    for (const { el } of counters) {
        el.textContent = '0';
    }

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

if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', () => animateCounters(), { once: true });
} else {
    animateCounters();
}
