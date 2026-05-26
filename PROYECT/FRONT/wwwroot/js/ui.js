window.addEventListener('mousemove', (e) => {
    const root = document.documentElement;
    root.style.setProperty('--mouse-x', e.clientX + 'px');
    root.style.setProperty('--mouse-y', e.clientY + 'px');
});

// Sistema global de Toasts
window.showToast = function(message, type = 'success') {
    // Verificar si ya existe el contenedor de toasts
    let container = document.getElementById('toast-container');
    if (!container) {
        container = document.createElement('div');
        container.id = 'toast-container';
        container.style.cssText = `
            position: fixed;
            top: 24px;
            right: 24px;
            z-index: 9999;
            display: flex;
            flex-direction: column;
            gap: 12px;
        `;
        document.body.appendChild(container);
    }

    const toast = document.createElement('div');
    
    let icon = '✅';
    let borderColor = 'rgba(16, 185, 129, 0.3)';
    
    if (type === 'error') {
        icon = '❌';
        borderColor = 'rgba(239, 68, 68, 0.3)';
    } else if (type === 'warning') {
        icon = '⚠️';
        borderColor = 'rgba(245, 158, 11, 0.3)';
    } else if (type === 'info') {
        icon = 'ℹ️';
        borderColor = 'rgba(73, 41, 164, 0.4)';
    }
    
    toast.style.cssText = `
        background: rgba(18, 18, 20, 0.85);
        backdrop-filter: blur(24px);
        -webkit-backdrop-filter: blur(24px);
        border: 1px solid ${borderColor};
        color: rgba(255, 255, 255, 0.95);
        padding: 16px 20px;
        border-radius: 14px;
        font-family: 'Inter', -apple-system, sans-serif;
        font-size: 0.9rem;
        font-weight: 500;
        box-shadow: 0 10px 40px rgba(0,0,0,0.4), inset 0 1px 0 rgba(255,255,255,0.15);
        opacity: 0;
        transform: translateX(30px) scale(0.95);
        transition: opacity 0.4s cubic-bezier(0.16, 1, 0.3, 1), transform 0.4s cubic-bezier(0.16, 1, 0.3, 1);
        display: flex;
        align-items: center;
        gap: 14px;
        min-width: 280px;
        max-width: 360px;
        line-height: 1.4;
    `;

    toast.innerHTML = `
        <span style="font-size: 1.25rem; flex-shrink: 0;">${icon}</span>
        <span>${message}</span>
    `;

    container.appendChild(toast);

    // Animar entrada
    requestAnimationFrame(() => {
        toast.style.opacity = '1';
        toast.style.transform = 'translateX(0) scale(1)';
    });

    // Remover después de 4 segundos
    setTimeout(() => {
        toast.style.opacity = '0';
        toast.style.transform = 'translateX(20px) scale(0.95)';
        setTimeout(() => {
            if (toast.parentNode) {
                toast.parentNode.removeChild(toast);
            }
        }, 300);
    }, 3500);
};