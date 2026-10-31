(function () {
    const observer = new IntersectionObserver((entries) => {
        entries.forEach(entry => {
            if (entry.isIntersecting) {
                const sectionId = entry.target.id;
                const navLinks = document.querySelectorAll('#navbar .nav-link');
                navLinks.forEach(link => {
                    const href = link.getAttribute('href');
                    if (href === `#${sectionId}`) {
                        link.classList.add('active');
                    } else {
                        link.classList.remove('active');
                    }
                });
            }
        });
    }, {
        rootMargin: "-20% 0px -45% 0px",
        threshold: 0
    });

    const initObserver = () => {
        const sections = document.querySelectorAll("section");
        sections.forEach((section) => observer.observe(section));
    };

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', initObserver);
    } else {
        initObserver();
    }
})();
