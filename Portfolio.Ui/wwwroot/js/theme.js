(function () {
    const STORAGE_KEY = 'theme-preference';

    function getPreferredTheme() {
        const storedTheme = localStorage.getItem(STORAGE_KEY);
        if (storedTheme) {
            return storedTheme;
        }
        // Default to dark theme for all visitors
        return 'dark';
    }

    function applyTheme(theme) {
        document.documentElement.setAttribute('data-theme', theme);
        document.documentElement.setAttribute('data-bs-theme', theme);
    }

    // Apply theme immediately to prevent FOUC
    const initialTheme = getPreferredTheme();
    applyTheme(initialTheme);

    window.themeManager = {
        getTheme: function () {
            return document.documentElement.getAttribute('data-theme') || getPreferredTheme();
        },
        setTheme: function (theme) {
            localStorage.setItem(STORAGE_KEY, theme);
            applyTheme(theme);
            window.dispatchEvent(new CustomEvent('themechanged', { detail: { theme: theme } }));
        },
        toggleTheme: function () {
            const current = this.getTheme();
            const next = current === 'dark' ? 'light' : 'dark';
            this.setTheme(next);
            return next;
        }
    };

    // Sync across system preference changes if no manual preference is saved
    window.matchMedia('(prefers-color-scheme: dark)').addEventListener('change', function (e) {
        if (!localStorage.getItem(STORAGE_KEY)) {
            applyTheme(e.matches ? 'dark' : 'light');
        }
    });
})();
