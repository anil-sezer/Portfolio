import { HttpMethod, HttpStatus, HttpHeaders } from './constants.js';

function initContactSection() {
    initClock();
    initCopyButtons();
    initSubjectChips();
    initCharCounter();
    
    const form = document.getElementById('email-form');
    if (form) {
        initFormSubmission(form);
    }
    
    initMap();
}

    // 1. Live Istanbul Clock (Europe/Istanbul - UTC+3)
    function initClock() {
        const hourEl = document.getElementById('istanbul-hour');
        const minEl = document.getElementById('istanbul-minute');
        if (!hourEl || !minEl) return;

        function updateClock() {
            try {
                const now = new Date();
                const parts = new Intl.DateTimeFormat('en-GB', {
                    timeZone: 'Europe/Istanbul',
                    hour: '2-digit',
                    minute: '2-digit',
                    hour12: false
                }).formatToParts(now);

                const hour = parts.find(p => p.type === 'hour')?.value || '--';
                const minute = parts.find(p => p.type === 'minute')?.value || '--';

                hourEl.textContent = hour;
                minEl.textContent = minute;
            } catch (e) {
                // Fallback if timezone not supported
                hourEl.textContent = '--';
                minEl.textContent = '--';
            }
        }

        updateClock();
        setInterval(updateClock, 10000);
    }

    // 2. 1-Click Copy-to-Clipboard Buttons
    function initCopyButtons() {
        const copyButtons = document.querySelectorAll('.copy-btn');
        const toastEl = document.getElementById('copyToast');
        let toastInstance = null;

        if (toastEl && window.bootstrap && window.bootstrap.Toast) {
            toastInstance = new window.bootstrap.Toast(toastEl, { delay: 2500 });
        }

        copyButtons.forEach((btn) => {
            btn.addEventListener('click', async (e) => {
                e.preventDefault();
                const textToCopy = btn.getAttribute('data-copy');
                if (!textToCopy) return;

                try {
                    await navigator.clipboard.writeText(textToCopy);

                    // Visual feedback on button
                    const icon = btn.querySelector('i');
                    if (icon) {
                        const originalClass = icon.className;
                        icon.className = 'bi bi-check2 text-success';
                        setTimeout(() => {
                            icon.className = originalClass;
                        }, 2000);
                    }

                    // Show toast if available
                    if (toastInstance) {
                        toastInstance.show();
                    }
                } catch (err) {
                    console.error('Failed to copy text: ', err);
                }
            });
        });
    }

    // 3. Subject Suggestion Chips
    function initSubjectChips() {
        const chips = document.querySelectorAll('.chip-btn');
        const subjectInput = document.getElementById('subject');
        if (!chips.length || !subjectInput) return;

        chips.forEach((chip) => {
            chip.addEventListener('click', () => {
                const subject = chip.getAttribute('data-subject');
                if (!subject) return;

                subjectInput.value = subject;
                clearFieldError('subject');

                chips.forEach((c) => c.classList.remove('active'));
                chip.classList.add('active');

                // Trigger change event and focus message input
                subjectInput.dispatchEvent(new Event('input', { bubbles: true }));
                const messageInput = document.getElementById('message');
                if (messageInput) {
                    messageInput.focus();
                }
            });
        });

        // Remove chip active state when user manually edits subject
        subjectInput.addEventListener('input', () => {
            chips.forEach((c) => {
                if (c.getAttribute('data-subject') !== subjectInput.value.trim()) {
                    c.classList.remove('active');
                }
            });
        });
    }

    // 4. Character Counter for Message Textarea
    function initCharCounter() {
        const messageInput = document.getElementById('message');
        const counterEl = document.getElementById('char-counter');
        if (!messageInput || !counterEl) return;

        const minChars = 15;

        function updateCounter() {
            const count = messageInput.value.trim().length;
            counterEl.textContent = `${count} / ${minChars} min`;
            if (count >= minChars) {
                counterEl.classList.add('valid');
            } else {
                counterEl.classList.remove('valid');
            }
        }

        messageInput.addEventListener('input', updateCounter);
        updateCounter();
    }

    // 5. Form Submission and Validation
    function initFormSubmission(form) {
        const sendUrl = form.dataset.sendUrl;
        const submitBtn = document.getElementById('submit-btn');
        const btnText = document.getElementById('btn-text');
        const btnSpinner = document.getElementById('btn-spinner');
        const btnLoadingText = document.getElementById('btn-loading-text');
        const alertBox = document.getElementById('form-alert');
        const alertIcon = document.getElementById('form-alert-icon');
        const alertMsg = document.getElementById('form-alert-message');

        const fields = [
            { id: 'name', min: 3, isEmail: false },
            { id: 'email', min: null, isEmail: true },
            { id: 'subject', min: 2, isEmail: false },
            { id: 'message', min: 15, isEmail: false }
        ];

        // Real-time error clearance on input
        fields.forEach(({ id }) => {
            const el = document.getElementById(id);
            if (el) {
                el.addEventListener('input', () => clearFieldError(id));
            }
        });

        form.addEventListener('submit', async (e) => {
            e.preventDefault();
            hideAlert();

            let isValid = true;
            for (const { id, min, isEmail } of fields) {
                const input = document.getElementById(id);
                if (!input) continue;

                const val = input.value.trim();
                let error = '';

                if (!val) {
                    error = 'This field is required.';
                } else if (isEmail) {
                    const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
                    if (!emailRegex.test(val)) {
                        error = 'Please enter a valid email address.';
                    }
                } else if (min && val.length < min) {
                    error = `Must be at least ${min} characters.`;
                }

                if (error) {
                    showFieldError(id, error);
                    isValid = false;
                } else {
                    clearFieldError(id);
                }
            }

            if (!isValid) {
                const firstInvalid = form.querySelector('.is-invalid');
                if (firstInvalid) firstInvalid.focus();
                return;
            }

            // Set loading state
            setSubmitting(true);

            const tokenEl = form.querySelector('input[name="__RequestVerificationToken"]') || document.querySelector('input[name="__RequestVerificationToken"]');
            const token = tokenEl ? tokenEl.value : '';

            const payload = {
                name: document.getElementById('name').value.trim(),
                email: document.getElementById('email').value.trim(),
                subject: document.getElementById('subject').value.trim(),
                message: document.getElementById('message').value.trim()
            };

            try {
                const headers = {
                    [HttpHeaders.CONTENT_TYPE]: 'application/json'
                };
                if (token) {
                    headers[HttpHeaders.X_CSRF_TOKEN] = token;
                }

                const response = await fetch(sendUrl, {
                    method: HttpMethod.POST,
                    headers: headers,
                    body: JSON.stringify(payload)
                });

                if (response.status === HttpStatus.TOO_MANY_REQUESTS) {
                    showAlert('danger', 'Too many requests. Please wait a moment before trying again.');
                    return;
                }

                if (!response.ok) {
                    showAlert('danger', 'Unable to send message. Please try again later.');
                    return;
                }

                const result = await response.json();

                if (result.success) {
                    form.reset();
                    clearAllErrors(fields);
                    document.querySelectorAll('.chip-btn').forEach((c) => c.classList.remove('active'));
                    const counterEl = document.getElementById('char-counter');
                    if (counterEl) {
                        counterEl.textContent = '0 / 15 min';
                        counterEl.classList.remove('valid');
                    }
                    showAlert('success', result.message || 'Thank you! Your message has been sent successfully.');
                } else {
                    showAlert('danger', result.message || 'Unable to send message. Please try again later.');
                }
            } catch (error) {
                showAlert('danger', 'An unexpected network error occurred. Please try again later.');
            } finally {
                setSubmitting(false);
            }
        });

        function setSubmitting(isSubmitting) {
            if (!submitBtn) return;
            submitBtn.disabled = isSubmitting;
            if (btnSpinner) btnSpinner.classList.toggle('d-none', !isSubmitting);
            if (btnLoadingText) btnLoadingText.classList.toggle('d-none', !isSubmitting);
            if (btnText) btnText.classList.toggle('d-none', isSubmitting);
        }

        function showAlert(type, message) {
            if (!alertBox || !alertIcon || !alertMsg) return;

            alertBox.className = `form-alert form-alert-${type}`;
            alertIcon.className = type === 'success' ? 'bi bi-check-circle-fill me-2' : 'bi bi-exclamation-triangle-fill me-2';
            alertMsg.textContent = message;
            alertBox.classList.remove('d-none');
            alertBox.scrollIntoView({ behavior: 'smooth', block: 'nearest' });
        }

        function hideAlert() {
            if (alertBox) {
                alertBox.classList.add('d-none');
            }
        }
    }

    function showFieldError(fieldId, message) {
        const input = document.getElementById(fieldId);
        const errorEl = document.getElementById(`${fieldId}-error`);
        if (input) input.classList.add('is-invalid');
        if (errorEl) {
            errorEl.textContent = message;
            errorEl.classList.add('show');
        }
    }

    function clearFieldError(fieldId) {
        const input = document.getElementById(fieldId);
        const errorEl = document.getElementById(`${fieldId}-error`);
        if (input) input.classList.remove('is-invalid');
        if (errorEl) {
            errorEl.textContent = '';
            errorEl.classList.remove('show');
        }
    }

    function clearAllErrors(fields) {
        fields.forEach(({ id }) => clearFieldError(id));
    }

    // 6. NASA VIIRS Earth at Night 2012 (Istanbul, Turkey - Continental Zoom)
    function initMap(retryCount = 0) {
        const mapEl = document.getElementById('contact-map');
        if (!mapEl) return;

        if (typeof L === 'undefined') {
            if (retryCount < 25) {
                setTimeout(() => initMap(retryCount + 1), 100);
            }
            return;
        }

        if (mapEl._leaflet_id) {
            return;
        }

        const istanbulCoords = [41.0082, 28.9784];

        function getResponsiveZoom() {
            const width = window.innerWidth;
            if (width < 576) return 3;
            if (width < 992) return 3;
            return 4;
        }

        const map = L.map('contact-map', {
            center: istanbulCoords,
            zoom: getResponsiveZoom(),
            minZoom: 2,
            maxZoom: 8, // Enforce maximum zoom level for NASA GIBS Level 8 tiles
            zoomControl: false,
            attributionControl: false,
            scrollWheelZoom: false
        });

        // Compact zoom controls at bottom right
        L.control.zoom({ position: 'bottomright' }).addTo(map);

        // NASA GIBS VIIRS Earth at Night 2012 Tile Layer (balanced across a,b,c subdomains)
        L.tileLayer('https://gibs-{s}.earthdata.nasa.gov/wmts/epsg3857/best/VIIRS_CityLights_2012/default/2012-12-01/GoogleMapsCompatible_Level8/{z}/{y}/{x}.jpg', {
            subdomains: 'abc',
            minZoom: 1,
            maxZoom: 8,
            maxNativeZoom: 8,
            tileSize: 256,
            attribution: 'Imagery &copy; NASA GIBS'
        }).addTo(map);

        // Astronomy Pulsing Star (Supernova) Marker on Istanbul
        const starIcon = L.divIcon({
            className: 'custom-leaflet-pin',
            html: `
                <div class="marker-celestial-star">
                    <div class="star-glow"></div>
                    <div class="star-crosshair">✦</div>
                </div>
            `,
            iconSize: [30, 30],
            iconAnchor: [15, 15]
        });

        L.marker(istanbulCoords, { icon: starIcon })
            .addTo(map)
            .bindPopup('<strong>Istanbul, Turkey</strong><br><small>GMT+3 &middot; NASA Earth at Night</small>');

        // Invalidate size once initial layout settles
        setTimeout(() => {
            map.invalidateSize();
        }, 250);

        // Responsive resize observer for container adjustments
        if (window.ResizeObserver) {
            const ro = new ResizeObserver(() => {
                map.invalidateSize();
            });
            ro.observe(mapEl);
        }

        // Handle window / orientation resize
        let resizeTimer;
        window.addEventListener('resize', () => {
            clearTimeout(resizeTimer);
            resizeTimer = setTimeout(() => {
                map.invalidateSize();
                map.setView(istanbulCoords, getResponsiveZoom(), { animate: false });
            }, 150);
        });
    }

    // Initialize when DOM is ready or after Blazor enhanced navigation
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', initContactSection);
    } else {
        initContactSection();
    }

    document.addEventListener('blazor:enhancedload', initContactSection);