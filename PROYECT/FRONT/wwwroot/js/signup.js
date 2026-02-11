document.addEventListener('DOMContentLoaded', () => {
    const signupForm = document.getElementById('signup-form');
    
    if (!signupForm) {
        console.error('No se encontró el formulario de registro');
        return;
    }

    // Referencias a elementos
    const errorContainer = document.getElementById('error-container');
    const errorMessageElement = document.getElementById('error-message');
    const submitButton = signupForm.querySelector('button[type="submit"]');

    // Validar que existan los elementos necesarios
    if (!errorContainer || !errorMessageElement || !submitButton) {
        console.error('Elementos esenciales del formulario no encontrados');
        return;
    }

    // Función para mostrar errores
    const displayError = (message) => {
        errorMessageElement.textContent = message;
        errorContainer.classList.remove('opacity-0');
        errorContainer.scrollIntoView({ behavior: 'smooth', block: 'center' });
    };

    // Función para ocultar errores
    const hideError = () => {
        errorContainer.classList.add('opacity-0');
    };

    // Función para gestionar estado del formulario
    const setFormState = (disabled, buttonText = null) => {
        const inputs = signupForm.querySelectorAll('input');
        inputs.forEach(input => input.disabled = disabled);
        
        submitButton.disabled = disabled;
        if (buttonText) {
            submitButton.textContent = buttonText;
        }
    };

    // Función para validar campos
    const validateFields = () => {
        const fields = [
            { id: 'username', name: 'Nombre de Usuario' },
            { id: 'nombre', name: 'Nombre(s)' },
            { id: 'apellidoPaterno', name: 'Apellido Paterno' },
            { id: 'apellidoMaterno', name: 'Apellido Materno' },
            { id: 'email', name: 'Email' },
            { id: 'password', name: 'Contraseña' },
            { id: 'confirmPassword', name: 'Confirmar Contraseña' }
        ];

        // Validar campos vacíos
        for (const field of fields) {
            const input = document.getElementById(field.id);
            if (!input?.value.trim()) {
                displayError(`El campo "${field.name}" no puede estar vacío.`);
                input?.focus();
                return false;
            }
        }

        // Validar formato de email
        const email = document.getElementById('email').value;
        const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
        if (!emailRegex.test(email)) {
            displayError('Por favor, ingresa un email válido.');
            document.getElementById('email').focus();
            return false;
        }

        // Validar contraseñas
        const password = document.getElementById('password').value;
        const confirmPassword = document.getElementById('confirmPassword').value;
        
        if (password !== confirmPassword) {
            displayError('Las contraseñas no coinciden.');
            document.getElementById('confirmPassword').focus();
            return false;
        }

        // Validar fortaleza de contraseña (opcional)
        if (password.length < 6) {
            displayError('La contraseña debe tener al menos 6 caracteres.');
            document.getElementById('password').focus();
            return false;
        }

        // Validar nombre de usuario (solo caracteres alfanuméricos)
        const username = document.getElementById('username').value;
        const usernameRegex = /^[a-zA-Z0-9_]+$/;
        if (!usernameRegex.test(username)) {
            displayError('El nombre de usuario solo puede contener letras, números y guiones bajos.');
            document.getElementById('username').focus();
            return false;
        }

        return true;
    };

    // Función para obtener datos del formulario
    const getFormData = () => {
        return {
            username: document.getElementById('username').value.trim(),
            nombre: document.getElementById('nombre').value.trim(),
            apellidoPaterno: document.getElementById('apellidoPaterno').value.trim(),
            apellidoMaterno: document.getElementById('apellidoMaterno').value.trim(),
            email: document.getElementById('email').value.trim(),
            password: document.getElementById('password').value
        };
    };

    // Manejar envío del formulario
    signupForm.addEventListener('submit', async (e) => {
        e.preventDefault();
        
        // Ocultar errores anteriores
        hideError();

        // Validar campos
        if (!validateFields()) {
            return;
        }

        // Deshabilitar formulario
        setFormState(true, 'Creando cuenta...');

        try {
            const formData = getFormData();
            const result = await window.api.signup(formData);

            if (result.success) {
                // Check if achievement was granted
                if (result.achievementGranted && result.userId) {
                    // Store achievement info to show popup after login
                    sessionStorage.setItem('pendingAchievement', JSON.stringify({
                        nombre: 'Crear cuenta',
                        descripcion: 'Has creado tu cuenta en Dorja. ¡Bienvenido!',
                        icono: 'fa-user-plus'
                    }));
                }
                alert('¡Registro exitoso! Ahora puedes iniciar sesión.');
                window.location.href = 'login.html';
            } else {
                displayError(result.message || 'Error durante el registro.');
            }
        } catch (error) {
            console.error('Error en el registro:', error);
            
            // Mensajes de error más específicos
            const errorMessage = typeof error === 'string' ? error : error.message || error;
            
            if (errorMessage.includes('username') || errorMessage.includes('usuario')) {
                displayError('El nombre de usuario ya está en uso. Por favor, elige otro.');
            } else if (errorMessage.includes('email') || errorMessage.includes('correo')) {
                displayError('El email ya está registrado. ¿Ya tienes una cuenta?');
            } else if (errorMessage.includes('UNIQUE constraint failed')) {
                displayError('El nombre de usuario o email ya están registrados.');
            } else {
                displayError(errorMessage || 'Error inesperado durante el registro.');
            }
        } finally {
            // Restaurar formulario
            setFormState(false, 'Crear Cuenta');
        }
    });

    // Limpiar errores al escribir en cualquier campo
    const formInputs = signupForm.querySelectorAll('input');
    formInputs.forEach(input => {
        input.addEventListener('input', () => {
            hideError();
            
            // Validación en tiempo real para contraseñas
            if (input.id === 'password' || input.id === 'confirmPassword') {
                const password = document.getElementById('password').value;
                const confirmPassword = document.getElementById('confirmPassword').value;
                
                if (password && confirmPassword && password !== confirmPassword) {
                    displayError('Las contraseñas no coinciden.');
                } else {
                    hideError();
                }
            }
        });
    });

    // Manejar tecla Enter
    document.addEventListener('keypress', (e) => {
        if (e.key === 'Enter' && !submitButton.disabled) {
            signupForm.dispatchEvent(new Event('submit'));
        }
    });
});