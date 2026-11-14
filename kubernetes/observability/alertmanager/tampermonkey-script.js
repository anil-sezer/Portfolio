// ==UserScript==
// @name         Alertmanager Dark Mode
// @namespace    https://alertmanager.anilsezer.net/
// @version      1.0.0
// @description  Enforces dark mode on Prometheus Alertmanager
// @match        https://alertmanager.anilsezer.net/*
// @run-at       document-start
// @grant        none
// ==/UserScript==

(function () {
    'use strict';

    const style = document.createElement('style');
    style.id = 'am-force-dark-mode';
    style.textContent = `
        html {
            background-color: #ffffff !important;
            filter: invert(0.91) hue-rotate(180deg) !important;
            min-height: 100vh !important;
        }

        /* Re-invert raster media so images/videos keep their original colors */
        img,
        video,
        picture,
        canvas {
            filter: invert(1) hue-rotate(180deg) !important;
        }
    `;

    (document.head || document.documentElement).appendChild(style);
})();