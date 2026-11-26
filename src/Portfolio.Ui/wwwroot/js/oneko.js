// Pixel cat that chases the cursor (desktop) or runs to taps (touch).
// Derived from oneko.js by adryd325 (MIT): https://github.com/adryd325/oneko.js
(function () {
    'use strict';

    const SPRITE_SIZE = 32;
    const HALF_SPRITE = SPRITE_SIZE / 2;
    const TICK_MS = 100;
    const SPEED_PX = 10;
    const REST_DISTANCE_PX = 48;
    const DIRECTION_THRESHOLD = Math.sin(Math.PI / 8);
    const SETTLE_TICKS = 10;
    const IDLE_ANIMATION_MIN_DELAY_MS = 10000;
    const IDLE_ANIMATION_MAX_DELAY_MS = 30000;
    const STORAGE_KEY = 'oneko';
    const STORAGE_VERSION = 1;

    const SPRITES = {
        idle: [[-3, -3]],
        alert: [[-7, -3]],
        scratchSelf: [[-5, 0], [-6, 0], [-7, 0]],
        scratchWallN: [[0, 0], [0, -1]],
        scratchWallS: [[-7, -1], [-6, -2]],
        scratchWallE: [[-2, -2], [-2, -3]],
        scratchWallW: [[-4, 0], [-4, -1]],
        tired: [[-3, -2]],
        sleeping: [[-2, 0], [-2, -1]],
        N: [[-1, -2], [-1, -3]],
        NE: [[0, -2], [0, -3]],
        E: [[-3, 0], [-3, -1]],
        SE: [[-5, -1], [-5, -2]],
        S: [[-6, -3], [-7, -2]],
        SW: [[-5, -3], [-6, -1]],
        W: [[-4, -2], [-4, -3]],
        NW: [[-1, 0], [-1, -1]]
    };

    // document.currentScript is only available during synchronous script evaluation.
    const spriteUrl = document.currentScript.dataset.cat;
    if (!spriteUrl) {
        throw new Error('oneko: the script tag requires a data-cat attribute with the sprite URL.');
    }

    const reducedMotionQuery = window.matchMedia('(prefers-reduced-motion: reduce)');

    let header = null;
    let el = null;
    let x = 0;
    let y = 0;
    let targetX = 0;
    let targetY = 0;
    let frameCount = 0;
    let idleTicks = 0;
    let idleAnimation = null;
    let idleAnimationFrame = 0;
    let tickIntervalId = null;
    let idleAnimationTimeoutId = null;

    function clamp(value, min, max) {
        return Math.min(Math.max(value, min), max);
    }

    // Playable area: the viewport minus the scrollbar and the part covered by the top header.
    function getBounds() {
        const viewport = document.documentElement;
        const headerBottom = Math.max(0, header.getBoundingClientRect().bottom);
        return {
            minX: HALF_SPRITE,
            maxX: viewport.clientWidth - HALF_SPRITE,
            minY: headerBottom + HALF_SPRITE,
            maxY: viewport.clientHeight - HALF_SPRITE
        };
    }

    function render() {
        el.style.transform = `translate3d(${Math.round(x - HALF_SPRITE)}px, ${Math.round(y - HALF_SPRITE)}px, 0)`;
    }

    function setSprite(name, frame) {
        const frames = SPRITES[name];
        const sprite = frames[frame % frames.length];
        el.style.backgroundPosition = `${sprite[0] * SPRITE_SIZE}px ${sprite[1] * SPRITE_SIZE}px`;
    }

    function startLoop() {
        if (el === null || document.visibilityState !== 'visible') {
            return;
        }
        if (tickIntervalId === null) {
            tickIntervalId = window.setInterval(tick, TICK_MS);
        }
    }

    function stopLoop() {
        window.clearInterval(tickIntervalId);
        tickIntervalId = null;
    }

    function cancelIdleAnimation() {
        window.clearTimeout(idleAnimationTimeoutId);
        idleAnimationTimeoutId = null;
    }

    function scheduleIdleAnimation() {
        if (idleAnimationTimeoutId !== null) {
            return;
        }
        const delay = IDLE_ANIMATION_MIN_DELAY_MS +
            Math.random() * (IDLE_ANIMATION_MAX_DELAY_MS - IDLE_ANIMATION_MIN_DELAY_MS);
        idleAnimationTimeoutId = window.setTimeout(startIdleAnimation, delay);
    }

    function startIdleAnimation() {
        idleAnimationTimeoutId = null;
        const bounds = getBounds();
        const candidates = ['sleeping', 'scratchSelf'];
        if (x - bounds.minX <= HALF_SPRITE) {
            candidates.push('scratchWallW');
        }
        if (y - bounds.minY <= HALF_SPRITE) {
            candidates.push('scratchWallN');
        }
        if (bounds.maxX - x <= HALF_SPRITE) {
            candidates.push('scratchWallE');
        }
        if (bounds.maxY - y <= HALF_SPRITE) {
            candidates.push('scratchWallS');
        }
        idleAnimation = candidates[Math.floor(Math.random() * candidates.length)];
        idleAnimationFrame = 0;
        startLoop();
    }

    function rest() {
        idleTicks += 1;

        switch (idleAnimation) {
            case null:
                setSprite('idle', 0);
                if (idleTicks > SETTLE_TICKS) {
                    // Nothing changes until input arrives or the next idle animation starts.
                    stopLoop();
                    scheduleIdleAnimation();
                }
                return;
            case 'sleeping':
                if (idleAnimationFrame < 8) {
                    setSprite('tired', 0);
                } else {
                    setSprite('sleeping', Math.floor(idleAnimationFrame / 4));
                }
                if (idleAnimationFrame > 192) {
                    idleAnimation = null;
                }
                break;
            default:
                setSprite(idleAnimation, idleAnimationFrame);
                if (idleAnimationFrame > 9) {
                    idleAnimation = null;
                }
                break;
        }
        idleAnimationFrame += 1;
    }

    function tick() {
        frameCount += 1;

        const bounds = getBounds();
        x = clamp(x, bounds.minX, bounds.maxX);
        y = clamp(y, bounds.minY, bounds.maxY);

        const clampedTargetX = clamp(targetX, bounds.minX, bounds.maxX);
        const clampedTargetY = clamp(targetY, bounds.minY, bounds.maxY);
        const diffX = x - clampedTargetX;
        const diffY = y - clampedTargetY;
        const distance = Math.hypot(diffX, diffY);
        const cursorDistance = Math.hypot(x - targetX, y - targetY);

        if (cursorDistance < REST_DISTANCE_PX || distance < 1) {
            rest();
            render();
            return;
        }

        cancelIdleAnimation();
        idleAnimation = null;
        idleAnimationFrame = 0;

        if (idleTicks > 1) {
            // Wake up and stay alert for a few ticks before running.
            setSprite('alert', 0);
            idleTicks = Math.min(idleTicks, 7) - 1;
            render();
            return;
        }
        idleTicks = 0;

        let direction = '';
        if (diffY / distance > DIRECTION_THRESHOLD) {
            direction += 'N';
        } else if (diffY / distance < -DIRECTION_THRESHOLD) {
            direction += 'S';
        }
        if (diffX / distance > DIRECTION_THRESHOLD) {
            direction += 'W';
        } else if (diffX / distance < -DIRECTION_THRESHOLD) {
            direction += 'E';
        }
        setSprite(direction, frameCount);

        const step = Math.min(SPEED_PX, distance);
        x = clamp(x - (diffX / distance) * step, bounds.minX, bounds.maxX);
        y = clamp(y - (diffY / distance) * step, bounds.minY, bounds.maxY);
        render();
    }

    function setTarget(clientX, clientY) {
        targetX = clientX;
        targetY = clientY;
        startLoop();
    }

    function onPointerMove(event) {
        // Touch pointers only move during drags/scrolls; those are handled by pointerdown instead.
        if (event.pointerType !== 'touch') {
            setTarget(event.clientX, event.clientY);
        }
    }

    function onPointerDown(event) {
        if (event.pointerType === 'touch') {
            setTarget(event.clientX, event.clientY);
        }
    }

    function onVisibilityChange() {
        if (document.visibilityState === 'visible') {
            startLoop();
        } else {
            stopLoop();
            cancelIdleAnimation();
        }
    }

    function loadPosition() {
        const raw = window.localStorage.getItem(STORAGE_KEY);
        if (raw === null) {
            return null;
        }
        const stored = JSON.parse(raw);
        const isValid = stored !== null &&
            typeof stored === 'object' &&
            stored.v === STORAGE_VERSION &&
            Number.isFinite(stored.x) &&
            Number.isFinite(stored.y);
        if (!isValid) {
            // Written by an older version of this script.
            window.localStorage.removeItem(STORAGE_KEY);
            return null;
        }
        return stored;
    }

    function savePosition() {
        if (el === null) {
            return;
        }
        window.localStorage.setItem(STORAGE_KEY, JSON.stringify({
            v: STORAGE_VERSION,
            x: Math.round(x),
            y: Math.round(y)
        }));
    }

    function create() {
        header = document.getElementById('header');
        if (header === null) {
            throw new Error('oneko: #header element not found.');
        }

        const bounds = getBounds();
        const stored = loadPosition();
        // Default spawn: bottom-left of the content area, away from the header and the back-to-top button.
        x = clamp(stored === null ? bounds.minX + HALF_SPRITE : stored.x, bounds.minX, bounds.maxX);
        y = clamp(stored === null ? bounds.maxY - HALF_SPRITE : stored.y, bounds.minY, bounds.maxY);
        // Start resting so the cat does not run toward a stale pointer position.
        targetX = x;
        targetY = y;
        frameCount = 0;
        idleTicks = 0;
        idleAnimation = null;
        idleAnimationFrame = 0;

        el = document.createElement('div');
        el.id = 'oneko';
        el.setAttribute('aria-hidden', 'true');
        el.style.backgroundImage = `url("${spriteUrl}")`;
        setSprite('idle', 0);
        render();
        document.body.appendChild(el);

        startLoop();
    }

    function destroy() {
        stopLoop();
        cancelIdleAnimation();
        el.remove();
        el = null;
    }

    function onReducedMotionChange() {
        if (reducedMotionQuery.matches) {
            savePosition();
            destroy();
        } else {
            create();
        }
    }

    function init() {
        document.addEventListener('pointermove', onPointerMove, { passive: true });
        document.addEventListener('pointerdown', onPointerDown, { passive: true });
        document.addEventListener('visibilitychange', onVisibilityChange);
        window.addEventListener('resize', startLoop, { passive: true });
        window.addEventListener('pagehide', savePosition);
        reducedMotionQuery.addEventListener('change', onReducedMotionChange);

        if (!reducedMotionQuery.matches) {
            create();
        }
    }

    // Defer creation until the page is idle so the cat never competes with first paint.
    function scheduleInit() {
        if ('requestIdleCallback' in window) {
            window.requestIdleCallback(init, { timeout: 2000 });
        } else {
            window.setTimeout(init, 200);
        }
    }

    if (document.readyState === 'complete') {
        scheduleInit();
    } else {
        window.addEventListener('load', scheduleInit, { once: true });
    }
})();
