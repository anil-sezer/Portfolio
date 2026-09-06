(function () {
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
            resolution: (window.screen && window.screen.width && window.screen.height) ? `${window.screen.height}x${window.screen.width}` : ""
        };
    }

    async function logVisit() {
        try {
            const clientInfo = getClientInfo();
            await fetch('/api/visits/log', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify(clientInfo)
            });
        } catch (e) {
            console.debug('Visit logging failed:', e);
        }
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', logVisit);
    } else {
        logVisit();
    }
})();
