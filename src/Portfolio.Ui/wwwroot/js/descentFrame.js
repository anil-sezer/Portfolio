(function () {
    let rafId = null;

    function updateScrollProgress() {
        rafId = null;
        const frame = document.getElementById('page-descent-frame');
        if (!frame) {
            return;
        }

        const maxScroll = Math.max(1, document.documentElement.scrollHeight - window.innerHeight);
        const pct = Math.max(0, Math.min(100, (window.scrollY / maxScroll) * 100));
        frame.style.setProperty('--page-scroll-pct', `${pct.toFixed(2)}%`);
    }

    function scheduleUpdate() {
        if (rafId === null) {
            rafId = window.requestAnimationFrame(updateScrollProgress);
        }
    }

    function init() {
        const frame = document.getElementById('page-descent-frame');
        if (!frame || document.querySelectorAll('section[id]').length < 2) {
            return;
        }

        frame.hidden = false;
        updateScrollProgress();

        window.addEventListener('scroll', scheduleUpdate, { passive: true });
        window.addEventListener('resize', scheduleUpdate, { passive: true });
        window.addEventListener('load', scheduleUpdate);
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', init);
    } else {
        init();
    }
})();
