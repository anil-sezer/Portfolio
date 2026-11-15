(function () {
    function toggleCard(trigger) {
        const card = trigger.closest('.tech-stack-card');
        if (!card) {
            return;
        }

        const isOpen = !card.classList.contains('is-open');
        card.classList.toggle('is-open', isOpen);
        trigger.setAttribute('aria-expanded', String(isOpen));
    }

    function init() {
        const grid = document.getElementById('tech-stack-grid');
        if (!grid) {
            return;
        }

        grid.addEventListener('click', function (e) {
            const trigger = e.target.closest('.tech-accordion-trigger');
            if (!trigger) {
                return;
            }
            toggleCard(trigger);
        });
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', init);
    } else {
        init();
    }
})();
