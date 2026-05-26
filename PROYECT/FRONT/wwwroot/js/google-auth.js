// Google Auth Integration

const GOOGLE_CLIENT_ID = '701491874891-7tki2gf1ubvhjfvcj3si4ggfrvjc65qn.apps.googleusercontent.com'; // TODO: Reemplazar con el Client ID real

window.onload = function () {
    if (typeof google === 'undefined') {
        console.warn('Google Identity Services no pudo cargarse.');
        return;
    }

    google.accounts.id.initialize({
        client_id: GOOGLE_CLIENT_ID,
        callback: handleCredentialResponse
    });

    const buttonContainer = document.getElementById('google-login-button');
    if (buttonContainer) {
        google.accounts.id.renderButton(
            buttonContainer,
            { theme: document.body.classList.contains('dark-theme') ? 'filled_black' : 'outline', size: 'large', width: '300' }
        );
        // google.accounts.id.prompt(); // Opcional: mostrar popup de un toque
    }
};

async function handleCredentialResponse(response) {
    const errorContainer = document.getElementById('error-container');
    const errorMessage = document.getElementById('error-message');
    const submitBtn = document.getElementById('submit-btn'); // if on signup/login

    if (submitBtn) {
        submitBtn.disabled = true;
    }

    try {
        const result = await window.api.loginWithGoogle(response.credential);

        if (result && result.success) {
            if (result.requiresRole) {
                // Mostrar el modal y detener el flujo aquí
                const modal = document.getElementById('role-selection-modal');
                if (modal) {
                    modal.style.display = 'flex';
                    
                    // Configurar botones del modal
                    const maestroBtn = document.getElementById('role-maestro-btn');
                    const estudianteBtn = document.getElementById('role-estudiante-btn');
                    const loading = document.getElementById('role-selection-loading');
                    
                    const handleRoleSelection = async (role) => {
                        maestroBtn.disabled = true;
                        estudianteBtn.disabled = true;
                        loading.style.display = 'block';
                        
                        try {
                            const finalResult = await window.api.loginWithGoogle(response.credential, role);
                            if (finalResult && finalResult.success && finalResult.user) {
                                saveAndRedirect(finalResult.user);
                            } else {
                                throw new Error(finalResult?.message || 'Error al completar el registro');
                            }
                        } catch (e) {
                            alert(e.message);
                            maestroBtn.disabled = false;
                            estudianteBtn.disabled = false;
                            loading.style.display = 'none';
                        }
                    };

                    maestroBtn.onclick = () => handleRoleSelection('maestro');
                    estudianteBtn.onclick = () => handleRoleSelection('estudiante');
                    
                    return; // Importante: Salir y esperar la acción del usuario
                }
            } else if (result.user) {
                saveAndRedirect(result.user);
            }
        } else {
            throw new Error(result?.message || 'Error al autenticar con Google');
        }
    } catch (err) {
        console.error(err);
        if (errorContainer && errorMessage) {
            errorMessage.textContent = err.message;
            errorContainer.classList.add('visible');
        } else {
            alert('Error al iniciar sesión con Google: ' + err.message);
        }
    } finally {
        if (submitBtn) {
            submitBtn.disabled = false;
        }
    }
}

function saveAndRedirect(user) {
    localStorage.setItem('currentUser', JSON.stringify(user));
    sessionStorage.setItem('userId', user.id || user.Id);
    sessionStorage.setItem('username', user.username || user.Username);
    sessionStorage.setItem('userRol', user.rol || user.Rol || 'estudiante');
    if (user.nombre || user.Nombre) {
        sessionStorage.setItem('userName', user.nombre || user.Nombre);
    }

    const rol = (user.rol || user.Rol || '').toLowerCase();
    if (rol === 'maestro') {
        window.location.href = 'teacher-dashboard.html';
    } else {
        window.location.href = 'home.html';
    }
}
