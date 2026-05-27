document.addEventListener('DOMContentLoaded', () => {
    const loginForm = document.getElementById('login-form');
    
    // Referencias al contenedor de error
    const errorContainer = document.getElementById('error-container');
    const errorMessageElement = document.getElementById('error-message');

    // Función para mostrar errores
    const displayError = (message) => {
        if (errorMessageElement && errorContainer) {
            errorMessageElement.textContent = message;
            errorContainer.classList.remove('hidden');
        } else {
            // Fallback si no existe el contenedor de errores
            alert(message);
        }
    };

    // Función para ocultar errores
    const hideError = () => {
        if (errorContainer) {
            errorContainer.classList.add('hidden');
        }
    };

    // Función para deshabilitar/habilitar formulario
    const setFormState = (disabled, buttonText = null) => {
        const inputs = loginForm?.querySelectorAll('input');
        const submitButton = loginForm?.querySelector('button[type="submit"]');
        
        if (inputs) {
            inputs.forEach(input => input.disabled = disabled);
        }
        
        if (submitButton) {
            submitButton.disabled = disabled;
            if (buttonText) {
                submitButton.textContent = buttonText;
            }
        }
    };

    // Manejo del formulario de login
    if (loginForm) {
        loginForm.addEventListener('submit', async (e) => {
            e.preventDefault();
            
            const usernameInput = document.getElementById('username');
            const passwordInput = document.getElementById('password');
            
            // Ocultar errores anteriores
            hideError();

            // --- VALIDACIÓN MANUAL ---
            if (!usernameInput?.value.trim()) {
                displayError('Por favor, ingresa tu nombre de usuario.');
                usernameInput?.focus();
                return; 
            }
            
            if (!passwordInput?.value.trim()) {
                displayError('Por favor, ingresa tu contraseña.');
                passwordInput?.focus();
                return;
            }

            // --- LÓGICA DE ENVÍO ---
            setFormState(true, 'Procesando...');

            try {
                // Check if api is loaded
                if (!window.api || !window.api.login) {
                    throw new Error('API no está disponible. Por favor, recarga la página.');
                }

                const result = await window.api.login({
                    username: usernameInput.value,
                    password: passwordInput.value
                });

                if (result.success) {
                    // Guardar información del usuario en sessionStorage
                    if (result.user) {
                        sessionStorage.setItem('userId', result.user.id);
                        sessionStorage.setItem('username', result.user.username);
                        sessionStorage.setItem('userRol', result.user.rol || 'estudiante');
                        if (result.user.nombre) {
                            sessionStorage.setItem('userName', result.user.nombre);
                        }
                    }
                    // Redirect based on role
                    const rol = result.user?.rol || 'estudiante';
                    if (rol === 'maestro') {
                        window.location.href = 'teacher-dashboard.html';
                    } else {
                        window.location.href = 'home.html';
                    }
                } else {
                    displayError(result.message || 'Nombre de usuario o contraseña incorrectos.');
                }
            } catch (error) {
                console.error("Error en el login:", error);
                displayError('Error inesperado. Inténtalo de nuevo.');
            } finally {
                setFormState(false, 'Entrar');
            }
        });

        // Limpiar errores cuando el usuario empiece a escribir
        const formInputs = loginForm.querySelectorAll('input');
        formInputs.forEach(input => {
            input.addEventListener('input', () => {
                hideError();
            });
        });
    }

    // Manejo de la tecla Enter para enviar el formulario
    document.addEventListener('keypress', (e) => {
        if (e.key === 'Enter' && loginForm) {
            const submitButton = loginForm.querySelector('button[type="submit"]');
            if (submitButton && !submitButton.disabled) {
                loginForm.dispatchEvent(new Event('submit'));
            }
        }
    });

    // Modal de Recuperación de Contraseña
    const forgotLink = document.querySelector('.forgot-link');
    const forgotModal = document.getElementById('forgot-password-modal');
    const closeForgotModal = document.getElementById('close-forgot-modal');
    const forgotForm = document.getElementById('forgot-password-form');
    const forgotSpinner = document.getElementById('forgot-spinner');
    const btnSubmitForgot = document.getElementById('btn-submit-forgot');

    if (forgotLink && forgotModal) {
        forgotLink.addEventListener('click', (e) => {
            e.preventDefault();
            forgotModal.style.display = 'flex';
        });
    }

    if (closeForgotModal) {
        closeForgotModal.addEventListener('click', () => {
            forgotModal.style.display = 'none';
        });
    }

    if (forgotForm) {
        forgotForm.addEventListener('submit', async (e) => {
            e.preventDefault();
            const email = document.getElementById('forgot-email').value;

            // Mostrar spinner
            btnSubmitForgot.disabled = true;
            forgotSpinner.style.display = 'inline-block';

            try {
                const result = await window.api.forgotPassword(email);
                if (result.success) {
                    if (window.showToast) window.showToast('Se ha enviado una contraseña temporal a tu correo.', 'success');
                    forgotModal.style.display = 'none';
                    forgotForm.reset();
                } else {
                    if (window.showToast) window.showToast(result.message || 'Error al solicitar contraseña.', 'error');
                }
            } catch (error) {
                if (window.showToast) window.showToast('Hubo un problema de conexión.', 'error');
            } finally {
                btnSubmitForgot.disabled = false;
                forgotSpinner.style.display = 'none';
            }
        });
    }
});