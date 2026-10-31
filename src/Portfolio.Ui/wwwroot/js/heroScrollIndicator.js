(function () {
    const IDLE_DELAY_MS = 6000;
    const DISMISS_SCROLL_RATIO = 0.20; // Dismiss once 20% of viewport/hero is scrolled

    let idleTimer = null;
    let isPermanentlyDismissed = false;
    let isHeroIntersecting = true;
    let heroObserver = null;

    function getElements() {
        return {
            indicator: document.getElementById('heroScrollIndicator'),
            hero: document.getElementById('hero')
        };
    }

    function getScrollThreshold() {
        return window.innerHeight * DISMISS_SCROLL_RATIO;
    }

    function showIndicator() {
        if (isPermanentlyDismissed) {
            return;
        }

        const { indicator } = getElements();
        if (indicator && window.scrollY < getScrollThreshold() && isHeroIntersecting) {
            indicator.classList.add('is-visible');
        }
    }

    function hideIndicator() {
        const { indicator } = getElements();
        if (indicator) {
            indicator.classList.remove('is-visible');
        }
    }

    function cancelIdleTimer() {
        if (idleTimer) {
            clearTimeout(idleTimer);
            idleTimer = null;
        }
    }

    function dismissIndicator() {
        isPermanentlyDismissed = true;
        cancelIdleTimer();
        hideIndicator();

        // Clean up listeners and observer since indicator will never appear again
        window.removeEventListener('scroll', handleScroll);
        if (heroObserver) {
            heroObserver.disconnect();
            heroObserver = null;
        }
    }

    function startIdleTimer() {
        cancelIdleTimer();
        if (isPermanentlyDismissed || window.scrollY >= getScrollThreshold()) {
            return;
        }

        idleTimer = setTimeout(() => {
            showIndicator();
        }, IDLE_DELAY_MS);
    }

    function handleScroll() {
        if (isPermanentlyDismissed) {
            return;
        }

        const currentScrollY = window.scrollY;

        // Permanently dismiss if user scrolls down past 20% of the hero
        if (currentScrollY >= getScrollThreshold()) {
            dismissIndicator();
        }
    }

    function initIntersectionObserver() {
        const { hero } = getElements();
        if (!hero || !('IntersectionObserver' in window)) {
            return;
        }

        // Trigger when 20% or more of hero scrolls out of view (intersectionRatio < 0.8)
        heroObserver = new IntersectionObserver((entries) => {
            entries.forEach((entry) => {
                isHeroIntersecting = entry.isIntersecting && entry.intersectionRatio >= 0.8;
                if (!isHeroIntersecting && window.scrollY >= getScrollThreshold()) {
                    dismissIndicator();
                }
            });
        }, {
            threshold: [0, 0.8]
        });

        heroObserver.observe(hero);
    }

    function setupClickHandler() {
        document.addEventListener('click', function (e) {
            const link = e.target.closest('#heroScrollIndicator');
            if (link) {
                e.preventDefault();
                dismissIndicator();

                const target = document.getElementById('main') || document.querySelector('main');
                if (target) {
                    target.scrollIntoView({ behavior: 'smooth' });
                } else {
                    window.scrollTo({
                        top: window.innerHeight,
                        behavior: 'smooth'
                    });
                }
            }
        });
    }

    function init() {
        setupClickHandler();

        // If user loaded the page already scrolled past 20%, dismiss immediately
        if (window.scrollY >= getScrollThreshold()) {
            isPermanentlyDismissed = true;
            return;
        }

        initIntersectionObserver();
        startIdleTimer();
        window.addEventListener('scroll', handleScroll, { passive: true });
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', init);
    } else {
        init();
    }
})();
