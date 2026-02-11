// Ensure window.api is only defined once
if (typeof window.api === 'undefined') {
// Check if we're in Electron or web browser
const isElectron = typeof window !== 'undefined' && (window.electronAPI || (window.process && window.process.type));

window.api = {
    baseUrl: 'http://localhost:5222/api',

    // Helper method for making API calls
    async _makeRequest(endpoint, options = {}) {
        try {
            // Ensure we're using HTTP, not HTTPS
            let url = `${this.baseUrl}${endpoint}`;
            // Force HTTP protocol if somehow HTTPS got in there
            if (url.startsWith('https://')) {
                url = url.replace('https://', 'http://');
            }
            // Ensure it starts with http://
            if (!url.startsWith('http://') && !url.startsWith('https://')) {
                url = `http://${url}`;
            }

            console.log('Making API request to:', url);

            const response = await fetch(url, {
                method: options.method || 'GET',
                headers: {
                    "Content-Type": "application/json",
                    "Accept": "application/json",
                    ...options.headers
                },
                body: options.body ? JSON.stringify(options.body) : undefined
            });

            const result = await response.json().catch(() => null);

            if (!response.ok) {
                throw new Error(result?.message || `Error ${response.status}: ${response.statusText}`);
            }

            // Handle different response types
            // If result is directly an array, return it as data but also keep it accessible
            if (Array.isArray(result)) {
                return { success: true, data: result, result: result };
            } else if (result && typeof result === 'object') {
                // If result is an object, spread it and also put it in data if not already there
                return { success: true, data: result.data || result, ...result };
            } else {
                return { success: true, data: result, result: result };
            }
        } catch (error) {
            console.error(`Error en API ${endpoint}:`, error);
            let errorMessage = error.message || 'Error de conexión con el servidor';
            
            // Provide more helpful error messages
            if (error.message && error.message.includes('SSL') || error.message.includes('HTTPS')) {
                errorMessage = 'Error: El servidor está configurado para HTTP, no HTTPS. Verifica que el backend esté ejecutándose en http://localhost:5222';
            } else if (error.message && error.message.includes('Failed to fetch')) {
                errorMessage = 'Error: No se pudo conectar con el servidor. Asegúrate de que el backend esté ejecutándose en http://localhost:5222';
            }
            
            return {
                success: false,
                message: errorMessage
            };
        }
    },

    signup: async (data) => {
        return await window.api._makeRequest('/Users/signup', {
            method: 'POST',
            body: data
        });
    },

    login: async (data) => {
        // Backend now supports both username and email
        const loginData = {
            username: data.username,
            password: data.password
        };
        const result = await window.api._makeRequest('/Users/login', {
            method: 'POST',
            body: loginData
        });
        
        if (result.success && result.user) {
            return {
                success: true,
                user: result.user
            };
        }
        return result;
    },

    getUserById: async (userId) => {
        const result = await window.api._makeRequest(`/Users/${userId}`);
        const user = result.data || result;
        console.log('getUserById response:', user); // Debug log
        console.log('ProfilePhotoPath in response:', user?.profilePhotoPath); // Debug log
        console.log('CoverPhotoPath in response:', user?.coverPhotoPath); // Debug log
        return user;
    },

    updateUserProfile: async (data) => {
        // First get the current user to merge data and preserve photo paths
        const currentUser = await window.api.getUserById(data.userId);
        if (!currentUser) {
            return { success: false, message: 'Usuario no encontrado' };
        }

        const updateData = {
            id: data.userId,
            username: data.username || currentUser.username,
            email: data.email || currentUser.email,
            nombre: currentUser.nombre || '',
            apellidoPaterno: currentUser.apellidoPaterno || '',
            apellidoMaterno: currentUser.apellidoMaterno || '',
            password: currentUser.password || '', // Preserve password
            fechaRegistro: currentUser.fechaRegistro || new Date().toISOString(),
            ultimaConexion: currentUser.ultimaConexion || null,
            puntosTotales: currentUser.puntosTotales || 0,
            nivelActual: currentUser.nivelActual || 1,
            // CRITICAL: Preserve photo paths from database, not from stale currentUser object
            profilePhotoPath: currentUser.profilePhotoPath || '',
            coverPhotoPath: currentUser.coverPhotoPath || ''
        };

        return await window.api._makeRequest('/Users', {
            method: 'PUT',
            body: updateData
        });
    },

    getUserStats: async (userId) => {
        const result = await window.api._makeRequest(`/Users/${userId}/stats`);
        return result;
    },


    deleteUserAccount: async (data) => {
        // First verify password by attempting login
        const user = await window.api.getUserById(data.userId);
        if (!user) {
            return { success: false, message: 'Usuario no encontrado' };
        }

        // Verify password
        const loginResult = await window.api.login({
            username: user.email, // Use email for login verification
            password: data.password
        });

        if (!loginResult.success) {
            return { success: false, message: 'Contraseña incorrecta' };
        }

        // If password is correct, delete the account
        const result = await window.api._makeRequest(`/Users?id=${data.userId}`, {
            method: 'DELETE'
        });

        return result.success 
            ? { success: true, message: 'Cuenta eliminada exitosamente' }
            : { success: false, message: 'Error al eliminar la cuenta' };
    },

    saveImage: async (data) => {
        try {
            // Convert data URL to blob
            const response = await fetch(data.dataUrl);
            const blob = await response.blob();
            
            // Create FormData
            const formData = new FormData();
            formData.append('file', blob, `image.${blob.type.split('/')[1]}`);
            formData.append('imageType', data.imageType);

            // Use window.api.baseUrl instead of this.baseUrl
            const baseUrl = window.api.baseUrl || 'http://localhost:5222/api';
            const uploadUrl = `${baseUrl}/Users/${data.userId}/upload-image`;
            
            console.log('Uploading image to:', uploadUrl); // Debug log

            const uploadResponse = await fetch(uploadUrl, {
                method: 'POST',
                body: formData
            });

            const result = await uploadResponse.json().catch(() => null);

            if (!uploadResponse.ok) {
                throw new Error(result?.message || `Error ${uploadResponse.status}: ${uploadResponse.statusText}`);
            }

            console.log('Image upload successful:', result); // Debug log
            return { success: true, message: result.message || 'Imagen guardada exitosamente', path: result.path };
        } catch (error) {
            console.error('Error al guardar la imagen:', error);
            return { success: false, message: error.message || 'Error al guardar la imagen' };
        }
    },

    // Save image as BLOB in database
    saveImageAsBlob: async (data) => {
        try {
            // Convert data URL to blob
            const response = await fetch(data.dataUrl);
            const blob = await response.blob();
            
            // Create FormData
            const formData = new FormData();
            formData.append('file', blob, `image.${blob.type.split('/')[1]}`);
            formData.append('imageType', data.imageType);

            const baseUrl = window.api.baseUrl || 'http://localhost:5222/api';
            const uploadUrl = `${baseUrl}/Users/${data.userId}/upload-image-blob`;
            
            console.log('Uploading image as BLOB to:', uploadUrl);

            const uploadResponse = await fetch(uploadUrl, {
                method: 'POST',
                body: formData
            });

            const result = await uploadResponse.json().catch(() => null);

            if (!uploadResponse.ok) {
                throw new Error(result?.message || `Error ${uploadResponse.status}: ${uploadResponse.statusText}`);
            }

            console.log('Image BLOB upload successful:', result);
            return { success: true, message: result.message || 'Imagen guardada exitosamente en la base de datos', storageType: 'blob' };
        } catch (error) {
            console.error('Error al guardar la imagen como BLOB:', error);
            return { success: false, message: error.message || 'Error al guardar la imagen' };
        }
    },

    // Get image from BLOB in database
    getImageBlob: async (userId, imageType) => {
        try {
            const baseUrl = window.api.baseUrl || 'http://localhost:5222/api';
            const imageUrl = `${baseUrl}/Users/${userId}/image-blob?imageType=${imageType}`;
            
            const response = await fetch(imageUrl);
            
            if (!response.ok) {
                if (response.status === 404) {
                    return null; // Image not found
                }
                throw new Error(`Error ${response.status}: ${response.statusText}`);
            }

            // Return the blob URL
            const blob = await response.blob();
            return URL.createObjectURL(blob);
        } catch (error) {
            console.error('Error al recuperar la imagen desde BLOB:', error);
            return null;
        }
    },

    executePython: async (code) => {
        // Legacy method - redirects to executeCode
        return await window.api.executeCode(code, 'python');
    },

    executeCode: async (code, language = 'python', userId = null) => {
        const body = { code: code, language: language };
        if (userId) {
            body.userId = userId;
        }
        const result = await window.api._makeRequest('/Python/execute', {
            method: 'POST',
            body: body
        });
        
        return {
            success: result.success || false,
            output: result.output || result.message || 'Error desconocido',
            achievementGranted: result.achievementGranted || false
        };
    },

    // Curriculum-related APIs
    cargarTemas: async (userId, nivelId = null) => {
        const result = await window.api._makeRequest('/Temas');
        let temas = [];
        
        // The API returns data directly, not wrapped in a data property
        if (Array.isArray(result)) {
            temas = result;
        } else {
            temas = result.data || result || [];
        }
        
        // Filtrar por nivel si se especifica
        if (nivelId !== null && nivelId !== undefined) {
            temas = temas.filter(t => 
                t.IdNivel === nivelId || 
                t.idNivel === nivelId || 
                t.nivel_id === nivelId ||
                t.nivelId === nivelId
            );
        }
        
        return temas;
    },

    cargarProblemas: async (userId, temaId, useRandom = true, count = 10) => {
        try {
            // Validate temaId
            if (!temaId || temaId === 'undefined' || temaId === undefined) {
                console.error('❌ API: temaId inválido:', temaId);
                throw new Error('temaId inválido');
            }
            
            const numTemaId = parseInt(temaId);
            if (isNaN(numTemaId) || numTemaId <= 0) {
                console.error('❌ API: temaId no es un número válido:', temaId);
                throw new Error('temaId no es un número válido');
            }
            
            console.log('🔄 API: Cargando problemas para temaId:', numTemaId, useRandom ? '(aleatorios)' : '(todos)');
            
            // Use random endpoint if available and useRandom is true
            if (useRandom && numTemaId) {
                try {
                    const randomUrl = `/Problemas/tema/${numTemaId}/random?count=${count}${userId ? `&userId=${userId}` : ''}`;
                    const result = await window.api._makeRequest(randomUrl);
                    console.log('📦 API: Resultado problemas aleatorios:', result);
                    
                    let problems = [];
                    // Handle the wrapped response from _makeRequest - check multiple formats
                    console.log('🔍 API: Formato del resultado recibido:', typeof result, 'Es array?:', Array.isArray(result), 'Tiene data?:', result?.data ? 'Sí' : 'No');
                    
                    if (Array.isArray(result)) {
                        // Direct array response
                        problems = result;
                        console.log('✅ API: Resultado es un array directo:', problems.length);
                    } else if (result && Array.isArray(result.data)) {
                        // Wrapped in data property
                        problems = result.data;
                        console.log('✅ API: Resultado en propiedad data:', problems.length);
                    } else if (result && result.success && Array.isArray(result.data)) {
                        // Wrapped in success.data
                        problems = result.data;
                        console.log('✅ API: Resultado en success.data:', problems.length);
                    } else if (result && typeof result === 'object') {
                        // Try to find any array property
                        for (const key in result) {
                            if (Array.isArray(result[key])) {
                                problems = result[key];
                                console.log(`✅ API: Resultado encontrado en propiedad ${key}:`, problems.length);
                                break;
                            }
                        }
                    }
                    
                    console.log('📦 API: Problemas extraídos del resultado:', problems.length, 'problemas');
                    if (problems.length === 0) {
                        console.warn('⚠️ API: Estructura del resultado completo:', JSON.stringify(result).substring(0, 500));
                    }
                    
                    // If we got problems (even if less than requested), return them
                    if (problems.length > 0) {
                        console.log(`✅ API: Problemas aleatorios obtenidos: ${problems.length} de ${count} solicitados`);
                        // If we got fewer than requested, log a warning but return what we have
                        if (problems.length < count) {
                            console.warn(`⚠️ API: Se recibieron menos problemas de los solicitados (${problems.length} < ${count})`);
                        }
                        return problems;
                    } else {
                        console.warn('⚠️ API: No se pudieron extraer problemas del resultado:', result);
                        // Fallback to regular endpoint if random returns empty
                        console.warn('⚠️ API: No se obtuvieron problemas aleatorios, usando endpoint regular');
                    }
                } catch (randomError) {
                    console.warn('⚠️ API: Error obteniendo problemas aleatorios, usando endpoint regular:', randomError);
                }
            }
            
            // Fallback: Get all problems and filter by temaId
            const result = await window.api._makeRequest('/Problemas');
            console.log('📦 API: Resultado crudo:', result);
            
            let problems = [];
            
            // Handle different response formats
            if (Array.isArray(result)) {
                problems = result;
            } else if (result && Array.isArray(result.data)) {
                problems = result.data;
            } else if (result && result.success && Array.isArray(result.data)) {
                problems = result.data;
            } else if (result && result.data && typeof result.data === 'object' && !Array.isArray(result.data)) {
                // Sometimes the API wraps it differently
                problems = [result.data];
            }
            
            console.log('📝 API: Problemas extraídos:', problems.length);
            
            // Filter by temaId if the backend doesn't have a specific endpoint
            if (problems.length > 0 && numTemaId) {
                const filtered = problems.filter(p => {
                    const pTemaId = p.temaId || p.tema_id || p.TemaId;
                    return pTemaId === numTemaId || pTemaId === parseInt(numTemaId);
                });
                console.log('✅ API: Problemas filtrados para temaId', numTemaId, ':', filtered.length);
                return filtered;
            }
            
            console.warn('⚠ API: No se encontraron problemas o temaId no válido');
            return problems;
        } catch (error) {
            console.error('❌ API: Error cargando problemas:', error);
            return [];
        }
    },

    obtenerProblema: async (problemaId) => {
        try {
            console.log('🔄 API: Obteniendo problema con ID:', problemaId);
            const result = await window.api._makeRequest(`/Problemas/${problemaId}`);
            console.log('📦 API: Resultado crudo del problema:', result);
            
            // Handle different response formats
            let problema = null;
            if (result && typeof result === 'object') {
                if (result.data) {
                    problema = result.data;
                } else if (result.success !== false) {
                    problema = result;
                }
            }
            
            if (!problema) {
                console.error('❌ API: Problema no encontrado o formato inválido');
                return null;
            }
            
            // Validate that the problem has a valid ID
            const problemaIdFromResponse = problema.Id || problema.id;
            if (!problemaIdFromResponse || problemaIdFromResponse !== problemaId) {
                console.warn(`⚠ API: ID mismatch - requested ${problemaId}, got ${problemaIdFromResponse}`);
            }
            
            console.log('✅ API: Problema obtenido correctamente. ID:', problemaIdFromResponse);
            return problema;
        } catch (error) {
            console.error('❌ API: Error obteniendo problema:', error);
            return null;
        }
    },

    verificarSolucion: async (userId, problemaId, codigo, language = 'python') => {
        try {
            console.log('🔄 API: Verificando solución para userId:', userId, 'problemaId:', problemaId);
            const result = await window.api._makeRequest('/Exercise/validate', {
                method: 'POST',
                body: {
                    UserId: userId,
                    ProblemaId: problemaId,
                    Codigo: codigo,
                    Language: language
                }
            });
            
            console.log('✅ API: Resultado de validación recibido:', result);
            
            // Extract the actual data from the wrapped response
            let validationResult;
            if (result && result.data) {
                validationResult = result.data;
            } else if (result && typeof result === 'object') {
                validationResult = result;
            } else {
                validationResult = { correcto: false, mensaje: 'Error desconocido en la respuesta' };
            }
            
            return validationResult;
        } catch (error) {
            console.error('❌ API: Error verificando solución:', error);
            return {
                correcto: false,
                mensaje: error.message || 'Error al verificar la solución'
            };
        }
    },

    marcarProblemaCompletado: async (userId, problemaId, codigo) => {
        // This would need a backend endpoint for marking problems as completed
        // For now, return a placeholder
        return {
            success: false,
            message: 'Marcar problema como completado requiere implementación en el backend'
        };
    },

    // Progress and curriculum APIs
    getAllNiveles: async () => {
        const result = await window.api._makeRequest('/Niveles');
        return result.data || result;
    },

    getAllTemas: async () => {
        const result = await window.api._makeRequest('/Temas');
        return result.data || result;
    },

    getProgresoByUserId: async (userId) => {
        const result = await window.api._makeRequest(`/ProgresoProblemas?userId=${userId}`);
        return result.data || result;
    },

    // Achievements/Logros APIs
    getAllLogros: async () => {
        const result = await window.api._makeRequest('/Logros');
        return result.data || result;
    },

    getLogrosByUserId: async (userId) => {
        const result = await window.api._makeRequest(`/Logros_Usuario/user/${userId}`);
        return result.data || result;
    },

    getLogroByNombre: async (nombre) => {
        const result = await window.api._makeRequest(`/Logros/by-name/${encodeURIComponent(nombre)}`);
        return result.data || result;
    },

    grantLogro: async (userId, logroId) => {
        const result = await window.api._makeRequest('/Logros_Usuario/grant', {
            method: 'POST',
            body: { UserId: userId, LogroId: logroId }
        });
        return result;
    },

    // Exercise APIs
    getRandomProblem: async (userId) => {
        const result = await window.api._makeRequest(`/Exercise/random/${userId}`);
        return result.data || result;
    },

    validateSolution: async (userId, problemaId, codigo, language = 'python') => {
        const result = await window.api._makeRequest('/Exercise/validate', {
            method: 'POST',
            body: {
                UserId: userId,
                ProblemaId: problemaId,
                Codigo: codigo,
                Language: language
            }
        });
        return result.data || result;
    },
    
    getProblemCount: async () => {
        const result = await window.api._makeRequest('/Problemas/count');
        return result.data || result;
    }
};
} // End of window.api definition check