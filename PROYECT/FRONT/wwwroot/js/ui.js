window.addEventListener('mousemove', (e) => {
    const root = document.documentElement;
    root.style.setProperty('--mouse-x', e.clientX + 'px');
    root.style.setProperty('--mouse-y', e.clientY + 'px');
});