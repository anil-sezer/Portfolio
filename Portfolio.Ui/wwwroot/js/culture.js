(function () {
    const SCROLL_KEY = 'portfolio_scroll_pos';

    function restoreScroll() {
        const savedScroll = sessionStorage.getItem(SCROLL_KEY);
        if (savedScroll !== null) {
            const targetY = parseInt(savedScroll, 10);
            if (!isNaN(targetY)) {
                window.scrollTo({ top: targetY, left: 0, behavior: 'instant' });
            }
        }
    }

    // Attempt restoration as soon as DOM is ready
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', restoreScroll);
    } else {
        restoreScroll();
    }

    // Re-check on window load after fonts and images render to counter any layout shifts
    window.addEventListener('load', function () {
        restoreScroll();
        setTimeout(function () {
            sessionStorage.removeItem(SCROLL_KEY);
        }, 100);
    });

    window.setCulture = async function (culture) {
        if (!culture) return;

        // If the clicked language is already active, do nothing
        const activeBtn = document.querySelector('.language-switcher .btn.btn-light');
        if (activeBtn && activeBtn.getAttribute('lang') && culture.toLowerCase().startsWith(activeBtn.getAttribute('lang').toLowerCase())) {
            return;
        }

        // Save current scroll position before reload
        sessionStorage.setItem(SCROLL_KEY, window.scrollY.toString());

        // Set the ASP.NET Core culture cookie directly on the client
        const cookieValue = `c=${culture}|uic=${culture}`;
        document.cookie = `.AspNetCore.Culture=${encodeURIComponent(cookieValue)};path=/;max-age=31536000;SameSite=Lax`;

        // Sync with server endpoint
        try {
            await fetch(`/SetCulture?culture=${encodeURIComponent(culture)}`, {
                headers: { 'X-Requested-With': 'XMLHttpRequest' }
            });
        } catch {
            // Ignore network failures since client cookie is already set
        }

        // Reload page in place to re-render localized content without resetting position
        window.location.reload();
    };
})();
