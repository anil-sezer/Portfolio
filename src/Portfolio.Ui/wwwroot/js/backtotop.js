(function () {
    const toggleBackToTopButton = () => {
        const backToTopButton = document.querySelector('.back-to-top');
        if (backToTopButton) {
            if (window.scrollY > 100) {
                backToTopButton.classList.add('active');
            } else {
                backToTopButton.classList.remove('active');
            }
        }
    };

    window.addEventListener('load', toggleBackToTopButton);
    window.addEventListener('scroll', toggleBackToTopButton);
    document.addEventListener('DOMContentLoaded', toggleBackToTopButton);

    toggleBackToTopButton();

    document.addEventListener('click', function (e) {
        const button = e.target.closest('.back-to-top');
        if (button) {
            e.preventDefault();
            window.scrollTo({
                top: 0,
                behavior: 'smooth'
            });
        }
    });
})();