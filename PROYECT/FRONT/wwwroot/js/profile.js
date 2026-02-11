document.addEventListener('DOMContentLoaded', async () => {
    const userId = sessionStorage.getItem('userId');
    if (!userId) {
        window.location.href = 'login.html';
        return;
    }

    // ELEMENTOS DE LA UI

    const profileUsername = document.getElementById('profile-username');
    const profileEmail = document.getElementById('profile-email');
    const profilePhoto = document.getElementById('profile-photo');
    const coverPhoto = document.getElementById('cover-photo');
    const profilePhotoContainer = document.getElementById('profile-photo-container');

    // Botones principales
    const editProfileBtn = document.getElementById('edit-profile-btn');
    const changePasswordBtn = document.getElementById('change-password-btn');
    const deleteAccountBtn = document.getElementById('delete-account-btn');

    // Elementos del Modal de Editar Perfil
    const editModal = document.getElementById('edit-modal');
    const cancelEditBtn = document.getElementById('cancel-edit-btn');
    const saveEditBtn = document.getElementById('save-edit-btn');
    const editProfileForm = document.getElementById('edit-profile-form');
    const editUsernameInput = document.getElementById('edit-username');
    const editEmailInput = document.getElementById('edit-email');
    const profilePhotoInput = document.getElementById('profile-photo-input');
    const coverPhotoInput = document.getElementById('cover-photo-input'); 

    // Elementos del Modal de Cambiar Contraseña
    const passwordModal = document.getElementById('password-modal');
    
    // Elementos del Modal de Eliminar Cuenta
    const deleteModal = document.getElementById('delete-modal');

    let currentUser = null;

    // CARGA DE DATOS DEL USUARIO 
    
    async function loadAchievements() {
        try {
            const achievements = await loadUserAchievements(userId);
            const container = document.getElementById('achievements-container');
            if (container) {
                renderAchievements(achievements, container);
            }
        } catch (error) {
            console.error('Error loading achievements:', error);
            const container = document.getElementById('achievements-container');
            if (container) {
                container.innerHTML = '<p class="text-red-500">Error al cargar los logros</p>';
            }
        }
    }

    async function loadUserProfile() {
        try {
            const user = await window.api.getUserById(userId);
            console.log('User data loaded:', user); // Debug log
            console.log('ProfilePhotoPath:', user?.profilePhotoPath); // Debug log
            console.log('CoverPhotoPath:', user?.coverPhotoPath); // Debug log
            
            if (user) {
                currentUser = user;
                profileUsername.textContent = user.username;
                profileEmail.textContent = user.email;
                editUsernameInput.value = user.username;
                editEmailInput.value = user.email;

                const initial = user.username ? user.username.charAt(0).toUpperCase() : 'U';
                
                // Store initial in container for fallback
                if (profilePhotoContainer) {
                    profilePhotoContainer.dataset.initial = initial;
                }
                
                // Try to load profile photo from BLOB first, then fallback to file path
                let profilePhotoLoaded = false;
                
                // Try BLOB first (database storage)
                try {
                    const profileBlobUrl = await window.api.getImageBlob(userId, 'profile');
                    if (profileBlobUrl) {
                        console.log('Loading profile photo from BLOB'); // Debug log
                        if (profilePhoto) {
                            profilePhoto.style.display = 'block';
                            profilePhoto.onerror = () => {
                                console.error('Failed to load profile image from BLOB');
                                profilePhoto.style.display = 'none';
                                if (profilePhotoContainer) {
                                    profilePhotoContainer.innerHTML = `<span class='text-white text-4xl font-bold'>${initial}</span>`;
                                }
                                profilePhoto.onerror = null;
                            };
                            profilePhoto.src = profileBlobUrl;
                            profilePhotoLoaded = true;
                        }
                    }
                } catch (blobError) {
                    console.log('No profile photo BLOB found, trying file path'); // Debug log
                }
                
                // Fallback to file path if BLOB not found
                if (!profilePhotoLoaded && user.profilePhotoPath && user.profilePhotoPath.trim() !== '') {
                    const profileImageUrl = `http://localhost:5222${user.profilePhotoPath}`;
                    console.log('Setting profile photo URL from file path:', profileImageUrl); // Debug log
                    
                    if (profilePhoto) {
                        profilePhoto.style.display = 'block';
                        profilePhoto.onerror = () => {
                            console.error('Failed to load profile image from file path:', profileImageUrl);
                            profilePhoto.style.display = 'none';
                            if (profilePhotoContainer) {
                                profilePhotoContainer.innerHTML = `<span class='text-white text-4xl font-bold'>${initial}</span>`;
                            }
                            profilePhoto.onerror = null;
                        };
                        profilePhoto.src = profileImageUrl;
                        profilePhotoLoaded = true;
                    }
                }
                
                // If still no image, show initial
                if (!profilePhotoLoaded) {
                    console.log('No profile photo found, showing initial'); // Debug log
                    if (profilePhoto) {
                        profilePhoto.style.display = 'none';
                    }
                    if (profilePhotoContainer) {
                        profilePhotoContainer.innerHTML = `<span class='text-white text-4xl font-bold'>${initial}</span>`;
                    }
                }

                // Try to load cover photo from BLOB first, then fallback to file path
                let coverPhotoLoaded = false;
                
                // Try BLOB first (database storage)
                try {
                    const coverBlobUrl = await window.api.getImageBlob(userId, 'cover');
                    if (coverBlobUrl) {
                        console.log('Loading cover photo from BLOB'); // Debug log
                        if (coverPhoto) {
                            coverPhoto.onerror = () => {
                                console.error('Failed to load cover image from BLOB');
                                coverPhoto.style.display = 'none';
                                coverPhoto.onerror = null;
                            };
                            coverPhoto.src = coverBlobUrl;
                            coverPhotoLoaded = true;
                        }
                    }
                } catch (blobError) {
                    console.log('No cover photo BLOB found, trying file path'); // Debug log
                }
                
                // Fallback to file path if BLOB not found
                if (!coverPhotoLoaded && user.coverPhotoPath && user.coverPhotoPath.trim() !== '') {
                    const coverImageUrl = `http://localhost:5222${user.coverPhotoPath}`;
                    console.log('Setting cover photo URL from file path:', coverImageUrl); // Debug log
                    
                    if (coverPhoto) {
                        coverPhoto.onerror = () => {
                            console.error('Failed to load cover image from file path:', coverImageUrl);
                            coverPhoto.style.display = 'none';
                            coverPhoto.onerror = null;
                        };
                        coverPhoto.src = coverImageUrl;
                        coverPhotoLoaded = true;
                    }
                }
                
                // If still no image, hide cover photo
                if (!coverPhotoLoaded) {
                    console.log('No cover photo found'); // Debug log
                    if (coverPhoto) {
                        coverPhoto.style.display = 'none';
                    }
                }

            } else {
                sessionStorage.removeItem('userId');
                window.location.href = 'login.html';
            }
        } catch (error) {
            console.error('Error al cargar datos del perfil:', error);
        }
    }

    // Load user profile and achievements
    await loadUserProfile();
    await loadAchievements();

    const handleImageUpload = async (file, imageType) => {
        if (!file) return;

        // Validate file type
        const validTypes = ['image/jpeg', 'image/jpg', 'image/png', 'image/gif'];
        if (!validTypes.includes(file.type)) {
            alert('Tipo de archivo no válido. Solo se permiten imágenes (JPG, PNG, GIF).');
            return;
        }

        // Validate file size (5MB max)
        if (file.size > 5 * 1024 * 1024) {
            alert('El archivo es demasiado grande. El tamaño máximo es 5MB.');
            return;
        }

        // Show preview immediately
        const reader = new FileReader();
        reader.onload = async (e) => {
            const dataUrl = e.target.result;
            const imgElement = imageType === 'profile' ? profilePhoto : coverPhoto;
            const originalSrc = imgElement.src; // Save original src
            
            // Set preview immediately and keep it
            imgElement.src = dataUrl;
            imgElement.onerror = null; // Clear any error handlers

            try {
                // Use BLOB storage method (saves directly to database)
                const result = await window.api.saveImageAsBlob({ userId, imageType, dataUrl });
                console.log('Image BLOB upload result:', result); // Debug log
                
                if (result.success) {
                    // For BLOB storage, we keep the preview as data URL or reload from BLOB
                    // The image is now stored in the database, so we can keep the preview
                    // or fetch it from the database endpoint
                    
                    // Option 1: Keep the data URL (simpler, works immediately)
                    // imgElement.src = dataUrl; // Already set above
                    
                    // Option 2: Fetch from database BLOB endpoint (ensures sync with DB)
                    try {
                        const blobUrl = await window.api.getImageBlob(userId, imageType);
                        if (blobUrl) {
                            imgElement.src = blobUrl;
                            imgElement.onerror = () => {
                                console.error('Failed to load image from BLOB, using preview');
                                imgElement.src = dataUrl;
                            };
                        }
                    } catch (blobError) {
                        console.warn('Could not fetch image from BLOB, using preview:', blobError);
                        // Keep the data URL preview
                    }
                    
                    // Check if achievement was granted
                    if (result.achievementGranted && imageType === 'profile') {
                        await showAchievementPopup('Personalizar perfil', 'Has añadido una foto de perfil. ¡Tu perfil se ve genial!', 'fa-user-circle');
                    } else {
                        alert(`La foto de ${imageType === 'profile' ? 'perfil' : 'portada'} se ha guardado exitosamente en la base de datos.`);
                    }
                } else {
                    console.error('Image BLOB upload failed:', result.message); // Debug log
                    alert(result.message || 'No se pudo guardar la imagen.');
                    // Restore original image
                    if (originalSrc) {
                        imgElement.src = originalSrc;
                    } else {
                        await loadUserProfile();
                    }
                }
            } catch (error) {
                console.error(`Error al guardar la imagen de ${imageType}:`, error);
                alert('No se pudo guardar la imagen. Por favor, intenta de nuevo.');
                // Restore original image
                if (originalSrc) {
                    imgElement.src = originalSrc;
                } else {
                    await loadUserProfile();
                }
            }
        };
        reader.readAsDataURL(file);
    };

    profilePhotoInput.addEventListener('change', (e) => handleImageUpload(e.target.files[0], 'profile'));
    coverPhotoInput.addEventListener('change', (e) => handleImageUpload(e.target.files[0], 'cover'));

    editProfileBtn.addEventListener('click', () => {
        editModal.classList.remove('hidden');
    });

    cancelEditBtn.addEventListener('click', () => {
        editModal.classList.add('hidden');
        if (currentUser) {
            editUsernameInput.value = currentUser.username;
            editEmailInput.value = currentUser.email;
        }
    });

    saveEditBtn.addEventListener('click', async () => {
        const newUsername = editUsernameInput.value.trim();
        const newEmail = editEmailInput.value.trim();

        if (!newUsername || !newEmail) {
            alert('El nombre de usuario y el email no pueden estar vacíos.');
            return;
        }

        try {
            await window.api.updateUserProfile({ userId, username: newUsername, email: newEmail });
            alert('Perfil actualizado con éxito.');
            editModal.classList.add('hidden');
            await loadUserProfile();
        } catch (error) {
            console.error('Error al actualizar el perfil:', error);
            alert(error.message || 'Ocurrió un error al actualizar. Es posible que el nombre de usuario o email ya estén en uso.');
        }
    });


    changePasswordBtn.addEventListener('click', () => {
        alert('Funcionalidad de cambiar contraseña próximamente.');
    });

    function showDeleteModalContent(step) {
        let content = '';
        if (step === 1) {
            content = `
                <div class="bg-white dark:bg-slate-800 rounded-xl shadow-2xl max-w-md w-full">
                    <div class="p-6 border-b border-gray-200 dark:border-slate-700">
                        <h3 class="text-xl font-bold text-red-600 dark:text-red-500">Eliminar Cuenta</h3>
                    </div>
                    <div class="p-6">
                        <p class="text-gray-600 dark:text-slate-300">¿Estás absolutamente seguro? Esta acción es irreversible y todos tus datos, progreso y logros se perderán para siempre.</p>
                    </div>
                    <div class="p-4 bg-gray-50 dark:bg-slate-800/50 border-t border-gray-200 dark:border-slate-700 flex justify-end gap-4 rounded-b-xl">
                        <button id="cancel-delete-1" class="px-4 py-2 text-sm font-semibold bg-gray-200 hover:bg-gray-300 dark:bg-slate-600 dark:hover:bg-slate-500 rounded-lg">Cancelar</button>
                        <button id="confirm-delete-1" class="px-4 py-2 text-sm font-semibold bg-red-600 hover:bg-red-700 text-white rounded-lg">Entiendo, continuar</button>
                    </div>
                </div>
            `;
        } else if (step === 2) {
            content = `
                <div class="bg-white dark:bg-slate-800 rounded-xl shadow-2xl max-w-md w-full">
                    <div class="p-6 border-b border-gray-200 dark:border-slate-700">
                        <h3 class="text-xl font-bold text-red-600 dark:text-red-500">Confirmación Final</h3>
                    </div>
                    <div class="p-6 flex flex-col gap-4">
                        <p>Para confirmar, por favor, introduce tu contraseña actual:</p>
                        <input type="password" id="delete-password-input" class="w-full bg-gray-100 dark:bg-slate-700 border-transparent rounded-lg p-2" placeholder="••••••••">
                    </div>
                    <div class="p-4 bg-gray-50 dark:bg-slate-800/50 border-t border-gray-200 dark:border-slate-700 flex justify-end gap-4 rounded-b-xl">
                        <button id="cancel-delete-2" class="px-4 py-2 text-sm font-semibold bg-gray-200 hover:bg-gray-300 dark:bg-slate-600 dark:hover:bg-slate-500 rounded-lg">Cancelar</button>
                        <button id="confirm-delete-2" class="px-4 py-2 text-sm font-semibold bg-red-600 hover:bg-red-700 text-white rounded-lg">Eliminar mi cuenta permanentemente</button>
                    </div>
                </div>
            `;
        }
        deleteModal.innerHTML = content;
        deleteModal.classList.remove('hidden');
    }

    deleteAccountBtn.addEventListener('click', () => {
        showDeleteModalContent(1);

        document.getElementById('cancel-delete-1').addEventListener('click', () => deleteModal.classList.add('hidden'));
        document.getElementById('confirm-delete-1').addEventListener('click', () => showDeleteModalContent(2));
    });

    deleteModal.addEventListener('click', async (event) => {
        const targetId = event.target.id;

        if (targetId === 'cancel-delete-2') {
            deleteModal.classList.add('hidden');
        }

        if (targetId === 'confirm-delete-2') {
            const password = document.getElementById('delete-password-input').value;
            if (!password) {
                alert('Debes introducir tu contraseña para confirmar.');
                return;
            }

            try {
                const result = await window.api.deleteUserAccount({ userId, password });
                if (result.success) {
                    alert('Tu cuenta ha sido eliminada permanentemente.');
                    sessionStorage.clear();
                    window.location.href = 'login.html';
                } else {
                    alert(result.message || 'Ocurrió un error al eliminar la cuenta.');
                }
            } catch (error) {
                console.error('Error al eliminar la cuenta:', error);
                alert('Ocurrió un error grave al intentar eliminar la cuenta.');
            }
        }
    });
});