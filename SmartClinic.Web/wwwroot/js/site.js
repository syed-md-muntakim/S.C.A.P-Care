(function () {
    var btn = document.getElementById('themeToggle');
    var root = document.documentElement;

    function apply(theme) {
        root.setAttribute('data-theme', theme);
        root.setAttribute('data-bs-theme', theme);
        localStorage.setItem('theme', theme);
        if (btn) btn.innerHTML = theme === 'dark' ? '<i class="bi bi-sun"></i>' : '<i class="bi bi-moon-stars"></i>';
    }

    apply(localStorage.getItem('theme') || 'light');
    if (btn) btn.addEventListener('click', function () {
        apply(root.getAttribute('data-theme') === 'dark' ? 'light' : 'dark');
    });
})();
