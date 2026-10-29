(function () {
    const select = (el, all = false) => {
        el = el.trim()
        if (all) {
            return [...document.querySelectorAll(el)]
        } else {
            return document.querySelector(el)
        }
    }

    const on = (type, el, listener, all = false) => {
        let selectEl = select(el, all)
        if (selectEl) {
            if (all) {
                selectEl.forEach(e => e.addEventListener(type, listener))
            } else {
                selectEl.addEventListener(type, listener)
            }
        }
    }

    on('click', '.mobile-nav-toggle', function(e) {
        e.stopPropagation()
        const isActive = select('body').classList.toggle('mobile-nav-active')
        const toggle = select('.mobile-nav-toggle')
        if (toggle) {
            toggle.setAttribute('aria-expanded', isActive ? 'true' : 'false')
        }
    })

    // Function to close mobile nav
    const closeMobileNav = () => {
        const body = select('body')

        if (body.classList.contains('mobile-nav-active')) {
            body.classList.remove('mobile-nav-active')
            const toggle = select('.mobile-nav-toggle')
            if (toggle) {
                toggle.setAttribute('aria-expanded', 'false')
            }
        }
    }

    // Close mobile nav when clicking outside
    document.addEventListener('click', function(e) {
        const navbar = select('#navbar')
        const toggle = select('.mobile-nav-toggle')

        // Check if click is outside navbar and toggle button
        if (!navbar.contains(e.target) && !toggle.contains(e.target)) {
            closeMobileNav()
        }
    })

    // Prevent clicks inside navbar from closing the menu
    on('click', '#navbar', function(e) {
        e.stopPropagation()
    })

    // Close mobile nav when ESC key is pressed
    document.addEventListener('keydown', function(e) {
        if (e.key === 'Escape') {
            closeMobileNav()
        }
    })

    // Collapsible accordion toggle (replaces Bootstrap Collapse JS)
    on('click', '[data-bs-toggle="collapse"]', function(e) {
        const btn = e.currentTarget
        const targetSelector = btn.getAttribute('data-bs-target')
        const target = targetSelector ? select(targetSelector) : null
        if (!target || target.classList.contains('collapsing')) return

        const isShown = target.classList.contains('show')
        btn.classList.toggle('collapsed', isShown)
        btn.setAttribute('aria-expanded', isShown ? 'false' : 'true')

        let done = false
        const finish = (callback) => {
            const complete = () => {
                if (done) return
                done = true
                callback()
            }
            target.addEventListener('transitionend', complete, { once: true })
            setTimeout(complete, 400)
        }

        if (isShown) {
            target.style.height = `${target.getBoundingClientRect().height}px`
            target.offsetHeight
            target.classList.add('collapsing')
            target.classList.remove('collapse', 'show')
            target.style.height = ''
            finish(() => {
                target.classList.remove('collapsing')
                target.classList.add('collapse')
            })
        } else {
            target.classList.remove('collapse')
            target.classList.add('collapsing')
            target.style.height = `${target.scrollHeight}px`
            finish(() => {
                target.classList.remove('collapsing')
                target.classList.add('collapse', 'show')
                target.style.height = ''
            })
        }
    }, true)
})();