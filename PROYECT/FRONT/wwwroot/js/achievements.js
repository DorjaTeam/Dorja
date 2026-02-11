// achievements.js - Funciones para manejar logros y popups

/**
 * Muestra un popup cuando se obtiene un logro
 * @param {string} nombre - Nombre del logro
 * @param {string} descripcion - Descripción del logro
 * @param {string} icono - Clase del icono (FontAwesome)
 */
async function showAchievementPopup(nombre, descripcion, icono = 'fa-trophy') {
    // Remove any existing popup
    const existingPopup = document.getElementById('achievement-popup');
    if (existingPopup) {
        existingPopup.remove();
    }

    // Create popup element
    const popup = document.createElement('div');
    popup.id = 'achievement-popup';
    popup.className = 'fixed inset-0 z-50 flex items-center justify-center p-4 pointer-events-none';
    popup.innerHTML = `
        <div class="bg-white dark:bg-slate-800 rounded-2xl shadow-2xl border-2 border-yellow-400 dark:border-yellow-500 p-8 max-w-md w-full transform transition-all duration-500 pointer-events-auto animate-bounce-in">
            <div class="text-center">
                <div class="mb-4 flex justify-center">
                    <div class="w-20 h-20 rounded-full bg-gradient-to-br from-yellow-400 to-orange-500 flex items-center justify-center shadow-lg animate-pulse">
                        <i class="fas ${icono} text-white text-3xl"></i>
                    </div>
                </div>
                <h3 class="text-2xl font-bold text-gray-900 dark:text-white mb-2">¡Logro Desbloqueado!</h3>
                <p class="text-lg font-semibold text-indigo-600 dark:text-indigo-400 mb-3">${nombre}</p>
                <p class="text-sm text-gray-600 dark:text-gray-300 mb-6">${descripcion}</p>
                <button onclick="closeAchievementPopup()" class="px-6 py-2 bg-indigo-600 hover:bg-indigo-700 text-white font-semibold rounded-lg transition-colors">
                    ¡Genial!
                </button>
            </div>
        </div>
    `;

    // Add to body
    document.body.appendChild(popup);

    // Auto-close after 5 seconds
    setTimeout(() => {
        closeAchievementPopup();
    }, 5000);
}

/**
 * Cierra el popup de logro
 */
function closeAchievementPopup() {
    const popup = document.getElementById('achievement-popup');
    if (popup) {
        popup.style.opacity = '0';
        popup.style.transform = 'scale(0.8)';
        setTimeout(() => {
            popup.remove();
        }, 300);
    }
}

/**
 * Carga y muestra los logros de un usuario
 * @param {number} userId - ID del usuario
 * @returns {Promise<Array>} Array de logros del usuario
 */
async function loadUserAchievements(userId) {
    try {
        const userLogros = await window.api.getLogrosByUserId(userId);
        const allLogros = await window.api.getAllLogros();
        
        // Create a map of logro IDs to logro details
        const logrosMap = {};
        if (Array.isArray(allLogros)) {
            allLogros.forEach(logro => {
                logrosMap[logro.id || logro.Id] = logro;
            });
        }
        
        // Combine user logros with logro details
        const achievements = [];
        if (Array.isArray(userLogros)) {
            userLogros.forEach(userLogro => {
                const logroId = userLogro.id_Logro || userLogro.Id_Logro;
                const logro = logrosMap[logroId];
                if (logro) {
                    achievements.push({
                        id: logro.id || logro.Id,
                        nombre: logro.nombre || logro.Nombre,
                        descripcion: logro.descripcion || logro.Descripcion,
                        icono: logro.iconoPhoto || logro.IconoPhoto || 'fa-trophy',
                        fechaObtencion: userLogro.fecha_Obtencion || userLogro.Fecha_Obtencion
                    });
                }
            });
        }
        
        return achievements;
    } catch (error) {
        console.error('Error loading achievements:', error);
        return [];
    }
}

/**
 * Renderiza los logros en un contenedor
 * @param {Array} achievements - Array de logros
 * @param {HTMLElement} container - Contenedor donde renderizar
 */
function renderAchievements(achievements, container) {
    if (!container) return;
    
    if (!achievements || achievements.length === 0) {
        container.innerHTML = `
            <div class="text-center py-8 text-gray-500 dark:text-gray-400">
                <i class="fas fa-trophy text-4xl mb-4 opacity-50"></i>
                <p>No has obtenido logros aún. ¡Completa acciones para desbloquearlos!</p>
            </div>
        `;
        return;
    }
    
    container.innerHTML = achievements.map(achievement => {
        const fecha = achievement.fechaObtencion ? new Date(achievement.fechaObtencion).toLocaleDateString('es-ES') : '';
        return `
            <div class="group relative p-5 sm:p-6 rounded-xl bg-gradient-to-br from-indigo-50 via-purple-50 to-indigo-50 dark:from-indigo-900/30 dark:via-purple-900/20 dark:to-indigo-900/30 border border-indigo-200/50 dark:border-indigo-800/50 shadow-md hover:shadow-lg transition-all duration-300 cursor-pointer overflow-hidden">
                <div class="absolute top-0 right-0 w-20 h-20 bg-indigo-200/20 dark:bg-indigo-800/10 rounded-full -mr-10 -mt-10 blur-2xl group-hover:bg-indigo-300/30 transition-colors"></div>
                <div class="relative z-10">
                    <div class="mb-4 sm:mb-5 flex items-center justify-between">
                        <div>
                            <i class="fas ${achievement.icono} text-white text-2xl"></i>
                        </div>
                        <div class="w-1.5 h-1.5 rounded-full bg-indigo-400 opacity-80"></div>
                    </div>
                    <div class="font-bold text-gray-900 dark:text-white text-sm sm:text-base mb-1.5">${achievement.nombre}</div>
                    <div class="text-xs text-indigo-600 dark:text-indigo-400 font-medium">${fecha ? `Obtenido ${fecha}` : 'Obtenido'}</div>
                </div>
            </div>
        `;
    }).join('');
}

