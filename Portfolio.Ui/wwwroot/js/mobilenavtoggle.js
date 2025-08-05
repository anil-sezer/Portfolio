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
        select('body').classList.toggle('mobile-nav-active')
        this.classList.toggle('bi-list')
        this.classList.toggle('bi-x')
    })

    // Function to close mobile nav
    const closeMobileNav = () => {
        const body = select('body')
        const toggle = select('.mobile-nav-toggle')

        if (body.classList.contains('mobile-nav-active')) {
            body.classList.remove('mobile-nav-active')
            toggle.classList.remove('bi-x')
            toggle.classList.add('bi-list')
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
})();