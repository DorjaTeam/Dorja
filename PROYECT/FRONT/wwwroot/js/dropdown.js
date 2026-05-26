document.addEventListener('DOMContentLoaded', () => {
    const userMenuButton = document.getElementById('user-menu-button');
    const userMenu = document.getElementById('user-menu');
    const logoutButton = document.getElementById('logout-button');

    // Detect which system is in use:
    // - design system uses class="header-dropdown" toggled with .open
    // - old Tailwind pages use class="hidden" toggled by adding/removing 'hidden'
    const usesDesignSystem = userMenu && userMenu.classList.contains('header-dropdown');

    const openMenu = () => {
        if (!userMenu) return;
        if (usesDesignSystem) {
            userMenu.classList.add('open');
        } else {
            userMenu.classList.remove('hidden');
        }
    };

    const closeMenu = () => {
        if (!userMenu) return;
        if (usesDesignSystem) {
            userMenu.classList.remove('open');
        } else {
            userMenu.classList.add('hidden');
        }
    };

    const isMenuOpen = () => {
        if (!userMenu) return false;
        if (usesDesignSystem) {
            return userMenu.classList.contains('open');
        } else {
            return !userMenu.classList.contains('hidden');
        }
    };

    const toggleMenu = (menuElement, usesDesign) => {
        if (!menuElement) return;
        const isOpen = usesDesign ? menuElement.classList.contains('open') : !menuElement.classList.contains('hidden');
        if (isOpen) {
            usesDesign ? menuElement.classList.remove('open') : menuElement.classList.add('hidden');
        } else {
            usesDesign ? menuElement.classList.add('open') : menuElement.classList.remove('hidden');
        }
    };

    const closeAll = () => {
        const uMenu = document.getElementById('user-menu');
        if (uMenu) {
            uMenu.classList.remove('open');
            uMenu.classList.add('hidden');
        }
        const nMenu = document.getElementById('notifications-menu');
        if (nMenu) {
            nMenu.classList.remove('open');
            nMenu.classList.add('hidden');
        }
    };

    if (userMenuButton) {
        userMenuButton.addEventListener('click', (event) => {
            event.stopPropagation();
            const nMenu = document.getElementById('notifications-menu');
            if (nMenu) {
                nMenu.classList.remove('open');
                nMenu.classList.add('hidden');
            }
            toggleMenu(userMenu, usesDesignSystem);
        });
    }

    const notifBtn = document.getElementById('notifications-button');
    if (notifBtn) {
        notifBtn.addEventListener('click', (event) => {
            event.stopPropagation();
            const nMenu = document.getElementById('notifications-menu');
            if (userMenu) {
                userMenu.classList.remove('open');
                userMenu.classList.add('hidden');
            }
            const nUsesDesign = nMenu && nMenu.classList.contains('header-dropdown');
            toggleMenu(nMenu, nUsesDesign);
        });
    }

    window.addEventListener('click', (event) => {
        const nMenu = document.getElementById('notifications-menu');
        if (userMenuButton && userMenuButton.contains(event.target)) return;
        if (userMenu && userMenu.contains(event.target)) return;
        if (notifBtn && notifBtn.contains(event.target)) return;
        if (nMenu && nMenu.contains(event.target)) return;
        
        closeAll();
    });

    // Support both ID formats for logout
    const logoutBtns = document.querySelectorAll('#logout-button, #logout-menu-btn, .logout-btn');
    logoutBtns.forEach(btn => {
        btn.addEventListener('click', (event) => {
            event.preventDefault();
            // Clear session
            sessionStorage.clear();
            localStorage.removeItem('theme');
            window.location.href = 'login.html';
        });
    });
});