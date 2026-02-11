// Helper function to set profile image (same logic as profile.js)
async function setProfileImage(imgElement, user, userId) {
    if (!imgElement || !user) return;
    
    const initial = user.username ? user.username.charAt(0).toUpperCase() : 'U';
    const container = imgElement.parentElement;
    
    // Store initial in container for fallback
    if (container) {
        container.dataset.initial = initial;
    }
    
    let profilePhotoLoaded = false;
    
    // Try BLOB first (database storage)
    try {
        const profileBlobUrl = await window.api.getImageBlob(userId, 'profile');
        if (profileBlobUrl) {
            console.log('Loading profile photo from BLOB');
            imgElement.style.display = 'block';
            imgElement.onerror = () => {
                console.error('Failed to load profile image from BLOB');
                imgElement.style.display = 'none';
                if (container) {
                    // Remove any existing span
                    const existingSpan = container.querySelector('span');
                    if (existingSpan) existingSpan.remove();
                    container.innerHTML = `<span class='text-white text-lg font-bold'>${initial}</span>`;
                }
                imgElement.onerror = null;
            };
            imgElement.src = profileBlobUrl;
            profilePhotoLoaded = true;
        }
    } catch (blobError) {
        console.log('No profile photo BLOB found, trying file path');
    }
    
    // Fallback to file path if BLOB not found
    if (!profilePhotoLoaded && user.profilePhotoPath && user.profilePhotoPath.trim() !== '') {
        const profileImageUrl = `http://localhost:5222${user.profilePhotoPath}`;
        console.log('Setting profile photo URL from file path:', profileImageUrl);
        
        imgElement.style.display = 'block';
        imgElement.onerror = () => {
            console.error('Failed to load profile image from file path:', profileImageUrl);
            imgElement.style.display = 'none';
            if (container) {
                // Remove any existing span
                const existingSpan = container.querySelector('span');
                if (existingSpan) existingSpan.remove();
                container.innerHTML = `<span class='text-white text-lg font-bold'>${initial}</span>`;
            }
            imgElement.onerror = null;
        };
        imgElement.src = profileImageUrl;
        profilePhotoLoaded = true;
    }
    
    // If still no image, show initial
    if (!profilePhotoLoaded) {
        console.log('No profile photo found, showing initial');
        imgElement.style.display = 'none';
        if (container) {
            // Remove any existing span
            const existingSpan = container.querySelector('span');
            if (existingSpan) existingSpan.remove();
            container.innerHTML = `<span class='text-white text-lg font-bold'>${initial}</span>`;
        }
    }
    
    imgElement.alt = `Avatar de ${user.username || 'Usuario'}`;
}

async function initializeHomePage() {
    const userId = sessionStorage.getItem('userId');
    const usernameDisplay = document.getElementById('username-display');
    const userAvatar = document.querySelector('#user-menu-button img');
    // Try multiple selectors for hero profile image
    const heroProfileImage = document.querySelector('main section img[alt="Foto de perfil"]') || 
                             document.querySelector('main .lg\\:col-span-2 img') ||
                             document.querySelector('main section img');

    if (!userId) {
        window.location.href = 'login.html';
        return;
    }

    try {
        const user = await window.api.getUserById(userId);

        if (user) {
            if (usernameDisplay) {
                usernameDisplay.textContent = user.username;
            }
            
            // Set avatar in dropdown menu
            if (userAvatar) {
                await setProfileImage(userAvatar, user, userId);
            }
            
            // Set profile image in hero section (the large image)
            const heroImg = document.getElementById('profile-photo');
            const heroImageContainer = heroImg?.parentElement;
            
            if (heroImg) {
                const initial = user.username ? user.username.charAt(0).toUpperCase() : 'U';
                
                // Store initial in container for fallback
                if (heroImageContainer) {
                    heroImageContainer.dataset.initial = initial;
                }
                
                let profilePhotoLoaded = false;
                
                // Try BLOB first (database storage)
                try {
                    const profileBlobUrl = await window.api.getImageBlob(userId, 'profile');
                    if (profileBlobUrl) {
                        console.log('Loading hero profile photo from BLOB');
                        heroImg.style.display = 'block';
                        heroImg.onerror = () => {
                            console.error('Failed to load hero profile image from BLOB');
                            heroImg.style.display = 'none';
                            if (heroImageContainer) {
                                // Remove any existing span
                                const existingSpan = heroImageContainer.querySelector('span');
                                if (existingSpan) existingSpan.remove();
                                heroImageContainer.innerHTML = `<span class='text-white text-4xl font-bold'>${initial}</span>`;
                            }
                            heroImg.onerror = null;
                        };
                        heroImg.src = profileBlobUrl;
                        profilePhotoLoaded = true;
                    }
                } catch (blobError) {
                    console.log('No hero profile photo BLOB found, trying file path');
                }
                
                // Fallback to file path if BLOB not found
                if (!profilePhotoLoaded && user.profilePhotoPath && user.profilePhotoPath.trim() !== '') {
                    const profileImageUrl = `http://localhost:5222${user.profilePhotoPath}`;
                    console.log('Setting hero profile photo URL from file path:', profileImageUrl);
                    
                    heroImg.style.display = 'block';
                    heroImg.onerror = () => {
                        console.error('Failed to load hero profile image from file path:', profileImageUrl);
                        heroImg.style.display = 'none';
                        if (heroImageContainer) {
                            // Remove any existing span
                            const existingSpan = heroImageContainer.querySelector('span');
                            if (existingSpan) existingSpan.remove();
                            heroImageContainer.innerHTML = `<span class='text-white text-4xl font-bold'>${initial}</span>`;
                        }
                        heroImg.onerror = null;
                    };
                    heroImg.src = profileImageUrl;
                    profilePhotoLoaded = true;
                }
                
                // If still no image, show initial
                if (!profilePhotoLoaded) {
                    console.log('No hero profile photo found, showing initial');
                    heroImg.style.display = 'none';
                    if (heroImageContainer) {
                        // Remove any existing span
                        const existingSpan = heroImageContainer.querySelector('span');
                        if (existingSpan) existingSpan.remove();
                        heroImageContainer.innerHTML = `<span class='text-white text-4xl font-bold'>${initial}</span>`;
                    }
                }
            }
            
            // Load progress and lobby data
            await loadProgressData(userId, user);
            await loadUpcomingLevel(userId, user);
            await loadHomeAchievements(userId);
            // Syllabus removed from home.html, so we don't load it anymore
            
            // Check for pending achievement from signup
            const pendingAchievement = sessionStorage.getItem('pendingAchievement');
            if (pendingAchievement) {
                try {
                    const achievement = JSON.parse(pendingAchievement);
                    await showAchievementPopup(achievement.nombre, achievement.descripcion, achievement.icono);
                    sessionStorage.removeItem('pendingAchievement');
                } catch (e) {
                    console.error('Error showing pending achievement:', e);
                }
            }
        } else {
            sessionStorage.removeItem('userId');
            window.location.href = 'login.html';
        }
    } catch (error) {
        console.error("Error al obtener los datos del usuario:", error);
    }
}

async function loadProgressData(userId, user) {
    try {
        // These elements might not exist in the current HTML, so we check first
        const totalPointsEl = document.getElementById('total-points');
        const currentLevelEl = document.getElementById('current-level');
        const progressBarEl = document.getElementById('progress-bar');
        const progressTextEl = document.getElementById('progress-text');
        
        if (totalPointsEl || currentLevelEl || progressBarEl || progressTextEl) {
            const totalPoints = user.puntosTotales || 0;
            const currentLevel = user.nivelActual || 1;
            
            if (totalPointsEl) totalPointsEl.textContent = totalPoints;
            if (currentLevelEl) currentLevelEl.textContent = currentLevel;
            
            // Calculate progress percentage (simplified - you can enhance this)
            if (progressBarEl || progressTextEl) {
                const progressPercentage = Math.min((totalPoints % 1000) / 10, 100);
                if (progressBarEl) progressBarEl.style.width = `${progressPercentage}%`;
                if (progressTextEl) progressTextEl.textContent = `${Math.round(progressPercentage)}% completado`;
            }
        }
    } catch (error) {
        console.error("Error loading progress:", error);
    }
}

async function loadHomeAchievements(userId) {
    try {
        const achievements = await loadUserAchievements(userId);
        const container = document.getElementById('achievements-grid');
        if (container) {
            if (achievements.length === 0) {
                container.innerHTML = `
                    <div class="text-center py-8 text-gray-500 dark:text-gray-400 col-span-full">
                        <i class="fas fa-trophy text-4xl mb-4 opacity-50"></i>
                        <p>No has obtenido logros aún. ¡Completa acciones para desbloquearlos!</p>
                    </div>
                `;
            } else {
                // Show only the 4 most recent achievements
                const recentAchievements = achievements.slice(-4).reverse();
                renderAchievements(recentAchievements, container);
            }
        }
    } catch (error) {
        console.error('Error loading home achievements:', error);
        const container = document.getElementById('achievements-grid');
        if (container) {
            container.innerHTML = '<p class="text-red-500 col-span-full text-center">Error al cargar los logros</p>';
        }
    }
}

async function loadUpcomingLevel(userId, user) {
    try {
        const upcomingLevelDiv = document.getElementById('upcoming-level');
        if (!upcomingLevelDiv) return; // Element doesn't exist in current HTML
        
        const niveles = await window.api.getAllNiveles();
        const currentLevel = user.nivelActual || 1;
        
        // Find next level
        const sortedNiveles = Array.isArray(niveles) ? niveles.sort((a, b) => (a.orden || 0) - (b.orden || 0)) : [];
        const nextLevel = sortedNiveles.find(n => (n.orden || n.IdNiveles || 0) > currentLevel) || sortedNiveles[0];
        
        if (nextLevel) {
            upcomingLevelDiv.innerHTML = `
                <div class="text-center">
                    <div class="text-4xl font-bold text-indigo-600 dark:text-indigo-400 mb-2">${nextLevel.NombreNivel || `Nivel ${nextLevel.orden || nextLevel.IdNiveles || 'N/A'}`}</div>
                    <p class="text-gray-600 dark:text-gray-400 mb-4">${nextLevel.DescripcionNivel || 'Sin descripción'}</p>
                    <div class="flex items-center justify-center gap-2 text-sm text-gray-500 dark:text-gray-400">
                        <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 8v13m0-13V6a2 2 0 112 2h-2zm0 0V5.5A2.5 2.5 0 109.5 8H12zm-7 4h14M5 12a2 2 0 110-4h14a2 2 0 110 4M5 12v7a2 2 0 002 2h10a2 2 0 002-2v-7"></path>
                        </svg>
                        <span>${nextLevel.puntosRequeridos || 0} puntos requeridos</span>
                    </div>
                </div>
            `;
        } else {
            upcomingLevelDiv.innerHTML = `
                <div class="text-center py-8">
                    <p class="text-gray-500 dark:text-gray-400">¡Felicidades! Has completado todos los niveles.</p>
                </div>
            `;
        }
    } catch (error) {
        console.error("Error loading upcoming level:", error);
        const upcomingLevelDiv = document.getElementById('upcoming-level');
        if (upcomingLevelDiv) {
            upcomingLevelDiv.innerHTML = `
                <div class="text-center py-8">
                    <p class="text-red-500">Error al cargar el próximo nivel</p>
                </div>
            `;
        }
    }
}

async function loadSyllabus() {
    try {
        const syllabusContent = document.getElementById('syllabus-content');
        if (!syllabusContent) return; // Element doesn't exist
        
        const niveles = await window.api.getAllNiveles();
        const temas = await window.api.getAllTemas();
        
        const sortedNiveles = Array.isArray(niveles) ? niveles.sort((a, b) => (a.orden || 0) - (b.orden || 0)) : [];
        const sortedTemas = Array.isArray(temas) ? temas.sort((a, b) => (a.orden || 0) - (b.orden || 0)) : [];
        
        if (sortedNiveles.length === 0) {
            syllabusContent.innerHTML = '<p class="text-gray-500 dark:text-gray-400 text-center py-8">No hay niveles disponibles</p>';
            return;
        }
        
        let html = '';
        for (const nivel of sortedNiveles) {
            const nivelTemas = sortedTemas.filter(t => t.IdNivel === nivel.IdNiveles || t.IdNivel === nivel.id);
            html += `
                <div class="border-l-4 border-indigo-600 dark:border-indigo-400 pl-4 mb-6">
                    <h4 class="text-xl font-bold text-gray-900 dark:text-white mb-2">
                        ${nivel.NombreNivel || `Nivel ${nivel.orden || nivel.IdNiveles || 'N/A'}`}
                    </h4>
                    <p class="text-gray-600 dark:text-gray-400 mb-3">${nivel.DescripcionNivel || 'Sin descripción'}</p>
                    ${nivelTemas.length > 0 ? `
                        <div class="ml-4 space-y-2">
                            ${nivelTemas.map(tema => `
                                <div class="flex items-center gap-2 text-gray-700 dark:text-gray-300">
                                    <svg class="w-4 h-4 text-indigo-600 dark:text-indigo-400" fill="currentColor" viewBox="0 0 20 20">
                                        <path fill-rule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zm3.707-9.293a1 1 0 00-1.414-1.414L9 10.586 7.707 9.293a1 1 0 00-1.414 1.414l2 2a1 1 0 001.414 0l4-4z" clip-rule="evenodd"></path>
                                    </svg>
                                    <span>${tema.Titulo || 'Tema sin título'}</span>
                                </div>
                            `).join('')}
                        </div>
                    ` : '<p class="text-gray-500 dark:text-gray-400 text-sm ml-4">No hay temas disponibles</p>'}
                </div>
            `;
        }
        
        syllabusContent.innerHTML = html;
    } catch (error) {
        console.error("Error loading syllabus:", error);
        const syllabusContent = document.getElementById('syllabus-content');
        if (syllabusContent) {
            syllabusContent.innerHTML = `
                <p class="text-red-500 text-center py-8">Error al cargar el sílabo</p>
            `;
        }
    }
}

// Initialize everything when DOM is ready
window.addEventListener('DOMContentLoaded', () => {
    initializeHomePage();
});