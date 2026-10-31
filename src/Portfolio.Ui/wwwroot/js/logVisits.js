import { HttpMethod, HttpHeaders } from './constants.js';

function getClientInfo() {
    return {
        userAgent: navigator.userAgent || "",
        language: navigator.languages ? navigator.languages.toString() : "",
        platform: navigator.platform || "",
        webdriver: navigator.webdriver ? "true" : "false",
        deviceMemory: navigator.deviceMemory ? navigator.deviceMemory.toString() : "",
        hardwareConcurrency: navigator.hardwareConcurrency ? navigator.hardwareConcurrency.toString() : "",
        maxTouchPoints: navigator.maxTouchPoints !== undefined ? navigator.maxTouchPoints.toString() : "-1",
        doNotTrack: (navigator.doNotTrack !== null && navigator.doNotTrack !== undefined) ? navigator.doNotTrack.toString() : "",
        connection: (navigator.connection && navigator.connection.effectiveType) ? navigator.connection.effectiveType : "",
        cookieEnabled: navigator.cookieEnabled ? "true" : "false",
        onLine: navigator.onLine ? "true" : "false",
        referrer: document.referrer || "",
        resolution: (window.screen && window.screen.width && window.screen.height) ? `${window.screen.width}x${window.screen.height}` : ""
    };
}

async function logVisit() {
    try {
        const logVisitUrl = document.body.dataset.logVisitUrl;
        const tokenEl = document.querySelector('input[name="__RequestVerificationToken"]');
        const token = tokenEl ? tokenEl.value : '';

        const headers = {
            [HttpHeaders.CONTENT_TYPE]: 'application/json'
        };
        if (token) {
            headers[HttpHeaders.X_CSRF_TOKEN] = token;
        }

        const clientInfo = getClientInfo();
        await fetch(logVisitUrl, {
            method: HttpMethod.POST,
            headers: headers,
            body: JSON.stringify(clientInfo)
        });
    } catch (e) {
        console.debug('Visit logging failed:', e);
    }
}

function scheduleLogVisit() {
    const onIdle = () => {
        if ('requestIdleCallback' in window) {
            window.requestIdleCallback(() => logVisit(), { timeout: 4000 });
        } else {
            setTimeout(logVisit, 2500);
        }
    };

    if (document.readyState === 'complete') {
        onIdle();
    } else {
        window.addEventListener('load', onIdle, { once: true });
    }
}

scheduleLogVisit();
