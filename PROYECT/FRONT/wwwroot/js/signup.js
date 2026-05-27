document.addEventListener('DOMContentLoaded', () => {
    // ── Elements ────────────────────────────────────────────────────────────
    const signupForm     = document.getElementById('signup-form');
    const errorContainer = document.getElementById('error-container');
    const errorMessage   = document.getElementById('error-message');
    const submitBtn      = document.getElementById('submit-btn');
    const btnText        = document.getElementById('btn-text');
    const spinner        = document.getElementById('spinner');

    // Success overlay
    const overlay        = document.getElementById('success-overlay');
    const usernameMsg    = document.getElementById('success-username-msg');
    const redirectBar    = document.getElementById('redirect-bar');
    const countdownEl    = document.getElementById('countdown');

    if (!signupForm) {
        console.error('Formulario de registro no encontrado');
        return;
    }

    // ── Error helpers ────────────────────────────────────────────────────────
    const showError = (msg) => {
        errorMessage.textContent = msg;
        errorContainer.classList.add('visible');
        // Shake the form
        signupForm.classList.remove('shake');
        void signupForm.offsetWidth; // reflow
        signupForm.classList.add('shake');
        errorContainer.scrollIntoView({ behavior: 'smooth', block: 'nearest' });
    };

    const hideError = () => {
        errorContainer.classList.remove('visible');
    };

    // ── Button state helpers ─────────────────────────────────────────────────
    const setLoading = (loading) => {
        submitBtn.disabled = loading;
        spinner.classList.toggle('hidden', !loading);
        btnText.textContent = loading ? 'Creando cuenta…' : 'Crear Cuenta';
        signupForm.querySelectorAll('input').forEach(i => i.disabled = loading);
    };

    // ── Success overlay ──────────────────────────────────────────────────────
    const showSuccess = (username) => {
        usernameMsg.textContent = `¡Hola, ${username}! 🎉`;
        overlay.classList.add('visible');

        // Animate progress bar and countdown
        let seconds = 3;
        countdownEl.textContent = seconds;

        // Trigger bar animation via JS so it syncs with timer
        redirectBar.style.transition = `width ${seconds}s linear`;
        redirectBar.style.width = '100%';

        const tick = setInterval(() => {
            seconds--;
            if (countdownEl) countdownEl.textContent = seconds;
            if (seconds <= 0) {
                clearInterval(tick);
                window.location.href = 'login.html';
            }
        }, 1000);
    };

    // ── Field validation ─────────────────────────────────────────────────────
    const validate = () => {
        const fields = [
            { id: 'username',        label: 'Nombre de Usuario' },
            { id: 'nombre',          label: 'Nombre(s)' },
            { id: 'apellidoPaterno', label: 'Apellido Paterno' },
            { id: 'apellidoMaterno', label: 'Apellido Materno' },
            { id: 'email',           label: 'Email' },
            { id: 'password',        label: 'Contraseña' },
            { id: 'confirmPassword', label: 'Confirmar Contraseña' },
        ];

        for (const { id, label } of fields) {
            const el = document.getElementById(id);
            if (!el?.value.trim()) {
                showError(`El campo "${label}" no puede estar vacío.`);
                el?.focus();
                return false;
            }
        }

        const email = document.getElementById('email').value;
        if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email)) {
            showError('Por favor, ingresa un email válido.');
            document.getElementById('email').focus();
            return false;
        }

        const password        = document.getElementById('password').value;
        const confirmPassword = document.getElementById('confirmPassword').value;

        if (password.length < 6) {
            showError('La contraseña debe tener al menos 6 caracteres.');
            document.getElementById('password').focus();
            return false;
        }

        if (password !== confirmPassword) {
            showError('Las contraseñas no coinciden.');
            document.getElementById('confirmPassword').focus();
            return false;
        }

        const username = document.getElementById('username').value;
        if (!/^[a-zA-Z0-9_]+$/.test(username)) {
            showError('El nombre de usuario solo puede contener letras, números y guiones bajos (_).');
            document.getElementById('username').focus();
            return false;
        }

        return true;
    };

    // ── Submit handler ───────────────────────────────────────────────────────
    signupForm.addEventListener('submit', async (e) => {
        e.preventDefault();
        hideError();

        if (!validate()) return;

        setLoading(true);

        try {
            const selectedRole = document.getElementById('selected-role')?.value || 'estudiante';

            const formData = {
                username:        document.getElementById('username').value.trim(),
                nombre:          document.getElementById('nombre').value.trim(),
                apellidoPaterno: document.getElementById('apellidoPaterno').value.trim(),
                apellidoMaterno: document.getElementById('apellidoMaterno').value.trim(),
                email:           document.getElementById('email').value.trim(),
                password:        document.getElementById('password').value,
                rol:             selectedRole,
            };

            const result = await window.api.signup(formData);

            if (result.success) {
                // Store achievement for later display on home/dashboard
                if (result.achievementGranted) {
                    sessionStorage.setItem('pendingAchievement', JSON.stringify({
                        nombre:      'Crear cuenta',
                        descripcion: 'Has creado tu cuenta en Dorja. ¡Bienvenido!',
                        icono:       'fa-user-plus'
                    }));
                }
                showSuccess(formData.username);
            } else {
                showError(result.message || 'Error durante el registro. Inténtalo de nuevo.');
                setLoading(false);
            }
        } catch (err) {
            console.error('Error en el registro:', err);
            const msg = typeof err === 'string' ? err : err?.message ?? '';

            if (msg.includes('username') || msg.includes('usuario')) {
                showError('El nombre de usuario ya está en uso. Por favor, elige otro.');
            } else if (msg.includes('email') || msg.includes('correo') || msg.includes('Email ya')) {
                showError('El email ya está registrado. ¿Ya tienes una cuenta?');
            } else if (msg.includes('UNIQUE')) {
                showError('El nombre de usuario o email ya están registrados.');
            } else {
                showError(msg || 'Error inesperado durante el registro. Inténtalo de nuevo.');
            }

            setLoading(false);
        }
    });

    // ── Real-time inline validation ──────────────────────────────────────────
    signupForm.querySelectorAll('input').forEach(input => {
        input.addEventListener('input', () => {
            hideError();

            if (input.id === 'password' || input.id === 'confirmPassword') {
                const pw  = document.getElementById('password').value;
                const cpw = document.getElementById('confirmPassword').value;
                if (pw && cpw && pw !== cpw) {
                    showError('Las contraseñas no coinciden.');
                }
            }
        });
    });

    // ── Overlay: clicking backdrop closes and does not redirect yet ──────────
    overlay?.addEventListener('click', (e) => {
        if (e.target === overlay) {
            // Do nothing — user must click button or wait for countdown
        }
    });
});