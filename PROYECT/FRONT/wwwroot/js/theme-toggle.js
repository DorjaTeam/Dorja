document.addEventListener('DOMContentLoaded', () => {
    const themeToggle = document.getElementById('theme-toggle');
    const applyTheme = (isDark) => {
        document.documentElement.classList.toggle('dark', isDark);
        if (themeToggle) {
            themeToggle.checked = isDark;
        }
        localStorage.setItem('theme', isDark ? 'dark' : 'light');
    };
    const savedThemeIsDark = localStorage.getItem('theme') === 'dark';
    const systemPrefersDark = window.matchMedia('(prefers-color-scheme: dark)').matches;
    const shouldBeDark = localStorage.getItem('theme') ? savedThemeIsDark : systemPrefersDark;
    applyTheme(shouldBeDark);
    if (themeToggle) {
        themeToggle.addEventListener('change', (e) => {
            applyTheme(e.target.checked);
        });
    }
});