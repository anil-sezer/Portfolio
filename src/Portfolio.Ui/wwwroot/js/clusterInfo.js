const METRIC_MAP = [
    { id: 'pods-count', key: 'activePodCount' },
    { id: 'services-count', key: 'serviceCount' },
    { id: 'deployments-count', key: 'deploymentCount' },
    { id: 'cronjobs-count', key: 'cronJobCount' },
    { id: 'nodes-count', key: 'nodeCount' }
];

const INITIAL_DURATION_MS = 1000;
const UPDATE_DURATION_MS = 600;
const REFRESH_INTERVAL_MS = 30000;

let currentFrame = null;
let refreshTimer = null;
let lastRefreshTime = performance.now();
let abortController = null;
let isFetching = false;
let hasAppeared = false;
let isVisibilityListenerAttached = false;
const currentTargets = new Map();

function animateCounters(counters, duration = INITIAL_DURATION_MS) {
    cancel(); // in case a previous run is still in flight

    const start = performance.now();

    function tick(now) {
        const progress = Math.min((now - start) / duration, 1);

        for (const { el, from, to } of counters) {
            const val = Math.floor(from + (to - from) * progress);
            el.textContent = val.toString();
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

function isPageActive() {
    return document.visibilityState === 'visible';
}

async function fetchStats() {
    if (isFetching || !isPageActive()) {
        return;
    }

    const clusterSection = document.getElementById('clusterInfo');
    const statsUrl = clusterSection?.dataset?.statsUrl || '/api/cluster/stats';

    try {
        isFetching = true;
        abortController = new AbortController();
        const response = await fetch(statsUrl, {
            signal: abortController.signal,
            headers: {
                'Accept': 'application/json'
            }
        });

        if (!response.ok) {
            return;
        }

        const data = await response.json();
        lastRefreshTime = performance.now();
        updateCounters(data);
    } catch (err) {
        if (err.name !== 'AbortError') {
            // Silently ignore network failures to avoid noisy console errors on transient issues
        }
    } finally {
        isFetching = false;
        abortController = null;
    }
}

function updateCounters(data) {
    const prefersReducedMotion = window.matchMedia('(prefers-reduced-motion: reduce)').matches;
    const toAnimate = [];

    for (const { id, key } of METRIC_MAP) {
        const el = document.getElementById(id);
        const newTarget = data[key];
        if (!el || typeof newTarget !== 'number') {
            continue;
        }

        currentTargets.set(id, newTarget);

        // If the user hasn't scrolled to the cluster section yet, keep '0' until revealed
        if (!hasAppeared && !prefersReducedMotion) {
            continue;
        }

        const currentDisplayed = parseInt(el.textContent, 10);

        if (isNaN(currentDisplayed) || prefersReducedMotion) {
            el.textContent = newTarget.toString();
            continue;
        }

        if (currentDisplayed !== newTarget) {
            toAnimate.push({ el, from: currentDisplayed, to: newTarget });
        }
    }

    if (toAnimate.length > 0) {
        animateCounters(toAnimate, UPDATE_DURATION_MS);
    }
}

function triggerInitialAnimation() {
    hasAppeared = true;
    const counters = [];
    for (const [id, target] of currentTargets) {
        const el = document.getElementById(id);
        if (el && target > 0) {
            counters.push({ el, from: 0, to: target });
        }
    }

    if (counters.length > 0) {
        animateCounters(counters, INITIAL_DURATION_MS);
    }
}

function startLiveRefresh() {
    stopLiveRefresh();
    refreshTimer = setInterval(() => {
        if (isPageActive()) {
            fetchStats();
        }
    }, REFRESH_INTERVAL_MS);
}

function stopLiveRefresh() {
    if (refreshTimer !== null) {
        clearInterval(refreshTimer);
        refreshTimer = null;
    }
    if (abortController !== null) {
        abortController.abort();
        abortController = null;
    }
}

function setupVisibilityListener() {
    if (isVisibilityListenerAttached) {
        return;
    }
    isVisibilityListenerAttached = true;

    document.addEventListener('visibilitychange', () => {
        if (isPageActive()) {
            const elapsed = performance.now() - lastRefreshTime;
            if (elapsed >= REFRESH_INTERVAL_MS) {
                fetchStats();
            }
            startLiveRefresh();
        } else {
            stopLiveRefresh();
        }
    });

    window.addEventListener('beforeunload', () => {
        stopLiveRefresh();
    });
}

function setupClusterCounters() {
    const clusterSection = document.getElementById('clusterInfo');
    if (!clusterSection) {
        return; // Not on a page with the stats section.
    }

    const prefersReducedMotion = window.matchMedia('(prefers-reduced-motion: reduce)').matches;

    // Targets come from the server-rendered text content — no interop needed.
    for (const { id } of METRIC_MAP) {
        const el = document.getElementById(id);
        if (el) {
            const target = parseInt(el.textContent, 10);
            if (Number.isInteger(target) && target > 0) {
                currentTargets.set(id, target);
            }
        }
    }

    if (currentTargets.size === 0) {
        return; // Stats service unreachable, or not on a page with the section.
    }

    if (prefersReducedMotion) {
        hasAppeared = true;
    } else {
        // Set counters to 0 in preparation for the scroll-triggered animation.
        for (const [id] of currentTargets) {
            const el = document.getElementById(id);
            if (el) {
                el.textContent = '0';
            }
        }

        const targetSection = document.getElementById('stats-list') || clusterSection;

        if (!('IntersectionObserver' in window)) {
            triggerInitialAnimation();
        } else {
            const observer = new IntersectionObserver((entries, obs) => {
                for (const entry of entries) {
                    if (entry.isIntersecting) {
                        obs.disconnect();
                        triggerInitialAnimation();
                        break;
                    }
                }
            }, {
                threshold: 0.15
            });

            observer.observe(targetSection);
        }
    }

    startLiveRefresh();
    setupVisibilityListener();
}

if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', setupClusterCounters, { once: true });
} else {
    setupClusterCounters();
}
