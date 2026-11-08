(function () {
    const MAX_SHIFT_X_PX = 5.5;
    const MAX_SHIFT_Y_PX = 2.0;
    const MAX_ROTATE_Z_DEG = 2.2;
    const MAX_ROTATE_Y_DEG = 4.5;
    const MAX_ROTATE_X_DEG = 3.0;
    const MAX_SHADOW_X_PX = 8.0;

    let rafId = null;
    let isListeningMouse = false;
    let lastClientX = 0;
    let lastClientY = 0;

    function getElements() {
        return {
            section: document.getElementById('about'),
            trigger: document.getElementById('about-pic-trigger'),
            dialog: document.getElementById('about-pic-lightbox')
        };
    }

    function canAnimateTilt() {
        return window.matchMedia('(pointer: fine)').matches &&
               window.matchMedia('(prefers-reduced-motion: no-preference)').matches;
    }

    function resetTilt(trigger) {
        if (!trigger) {
            return;
        }
        trigger.style.setProperty('--pic-tx', '0px');
        trigger.style.setProperty('--pic-ty', '0px');
        trigger.style.setProperty('--pic-rz', '0deg');
        trigger.style.setProperty('--pic-ry', '0deg');
        trigger.style.setProperty('--pic-rx', '0deg');
        trigger.style.setProperty('--pic-shadow-x', '0px');
    }

    function applyTilt() {
        rafId = null;
        const { trigger, dialog } = getElements();
        if (!trigger || (dialog && dialog.open)) {
            return;
        }

        const rect = trigger.getBoundingClientRect();
        if (rect.width === 0 || rect.height === 0) {
            return;
        }

        const centerX = rect.left + rect.width * 0.5;
        const centerY = rect.top + rect.height * 0.5;

        const rangeX = Math.max(window.innerWidth * 0.45, 360);
        const rangeY = Math.max(window.innerHeight * 0.45, 360);

        const dx = Math.max(-1, Math.min(1, (lastClientX - centerX) / rangeX));
        const dy = Math.max(-1, Math.min(1, (lastClientY - centerY) / rangeY));

        trigger.style.setProperty('--pic-tx', `${(dx * MAX_SHIFT_X_PX).toFixed(2)}px`);
        trigger.style.setProperty('--pic-ty', `${(dy * MAX_SHIFT_Y_PX).toFixed(2)}px`);
        trigger.style.setProperty('--pic-rz', `${(dx * MAX_ROTATE_Z_DEG).toFixed(2)}deg`);
        trigger.style.setProperty('--pic-ry', `${(dx * MAX_ROTATE_Y_DEG).toFixed(2)}deg`);
        trigger.style.setProperty('--pic-rx', `${(-dy * MAX_ROTATE_X_DEG).toFixed(2)}deg`);
        trigger.style.setProperty('--pic-shadow-x', `${(-dx * MAX_SHADOW_X_PX).toFixed(2)}px`);
    }

    function onMouseMove(e) {
        lastClientX = e.clientX;
        lastClientY = e.clientY;
        if (rafId === null) {
            rafId = window.requestAnimationFrame(applyTilt);
        }
    }

    function onMouseLeave() {
        if (rafId !== null) {
            window.cancelAnimationFrame(rafId);
            rafId = null;
        }
        const { trigger } = getElements();
        resetTilt(trigger);
    }

    function enableMouseTracking() {
        if (isListeningMouse || !canAnimateTilt()) {
            return;
        }
        isListeningMouse = true;
        window.addEventListener('mousemove', onMouseMove, { passive: true });
        document.addEventListener('mouseleave', onMouseLeave, { passive: true });
    }

    function disableMouseTracking() {
        if (!isListeningMouse) {
            return;
        }
        isListeningMouse = false;
        window.removeEventListener('mousemove', onMouseMove);
        document.removeEventListener('mouseleave', onMouseLeave);
        onMouseLeave();
    }

    function setupTiltObserver() {
        const { section } = getElements();
        if (!section || !('IntersectionObserver' in window)) {
            return;
        }

        const observer = new IntersectionObserver((entries) => {
            entries.forEach((entry) => {
                if (entry.isIntersecting) {
                    enableMouseTracking();
                } else {
                    disableMouseTracking();
                }
            });
        }, {
            threshold: 0.05
        });

        observer.observe(section);
    }

    function closeLightbox(dialog) {
        if (!dialog || !dialog.open || dialog.classList.contains('is-closing')) {
            return;
        }

        if (window.matchMedia('(prefers-reduced-motion: reduce)').matches) {
            dialog.close();
            return;
        }

        dialog.classList.add('is-closing');
        const content = dialog.querySelector('.about-lightbox-content');
        let finished = false;

        const finishClose = () => {
            if (finished) {
                return;
            }
            finished = true;
            dialog.classList.remove('is-closing');
            if (dialog.open) {
                dialog.close();
            }
        };

        if (content) {
            content.addEventListener('animationend', finishClose, { once: true });
        }
        window.setTimeout(finishClose, 220);
    }

    function setupLightbox() {
        const { trigger, dialog } = getElements();
        if (!trigger || !dialog) {
            return;
        }

        trigger.addEventListener('click', () => {
            resetTilt(trigger);
            if (typeof dialog.showModal === 'function' && !dialog.open) {
                dialog.classList.remove('is-closing');
                dialog.showModal();
                document.body.style.overflow = 'hidden';
            }
        });

        dialog.addEventListener('click', (e) => {
            const closeBtn = e.target.closest('.about-lightbox-close');
            const clickedImg = e.target.closest('.about-lightbox-img');
            if (closeBtn || !clickedImg) {
                closeLightbox(dialog);
            }
        });

        dialog.addEventListener('cancel', (e) => {
            e.preventDefault();
            closeLightbox(dialog);
        });

        dialog.addEventListener('close', () => {
            dialog.classList.remove('is-closing');
            document.body.style.overflow = '';
        });
    }

    function init() {
        setupLightbox();
        setupTiltObserver();
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', init);
    } else {
        init();
    }
})();
