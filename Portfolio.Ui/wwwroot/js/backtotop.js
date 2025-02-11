(function () {
    const backToTopButton = document.querySelector('.back-to-top');

    if (backToTopButton) {
        const toggleBackToTopButton = () => {
            if (window.scrollY > 100) {
                backToTopButton.classList.add('active');
            } else {
                backToTopButton.classList.remove('active');
            }
        };

        window.addEventListener('load', toggleBackToTopButton);
        window.addEventListener('scroll', toggleBackToTopButton);
    }
})();