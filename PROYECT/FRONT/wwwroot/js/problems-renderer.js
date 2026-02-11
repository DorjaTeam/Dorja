// js/problems-renderer.js
// Updated: 2024-12-19 - Certificate button functionality - VERSION 4
// Load achievements.js functions
if (typeof showAchievementPopup === 'undefined') {
    // If achievements.js is not loaded, define a placeholder
    window.showAchievementPopup = async function (nombre, descripcion, icono) {
        console.log('Achievement unlocked:', nombre);
    };
}

class ProblemsRenderer {
    constructor() {
        this.currentProblemaId = null;
        this.currentProblema = null;
        this.userId = null;
        this.currentTemaId = null;
        this.temas = [];
        this.problemas = [];
        this.problemasPorTema = {};
        this.progresoPorTema = {};
        this.currentNivelId = 1;
    }

    async init(userId, nivelId = null) {
        console.log('🔄 Inicializando ProblemsRenderer con userId:', userId, 'nivelId:', nivelId);
        this.userId = userId;
        this.currentNivelId = nivelId || 1;

        try {
            await window.curriculumManager.init(userId);
            console.log('✅ CurriculumManager inicializado');

            // Load user progress to determine unlocked topics
            await this.loadUserProgress();
            
            // Render topics and problems organized by syllabus
            await this.renderSidebarTemas();
        } catch (error) {
            console.error('❌ Error en init:', error);
            this.showError('Error al inicializar: ' + error.message);
        }
    }

    async loadUserProgress() {
        try {
            console.log(`🔄 Cargando progreso para usuario ${this.userId}...`);
            const progreso = await window.api.getProgresoByUserId(this.userId);
            console.log('📊 Progreso recibido del API:', progreso);
            
            // Handle different response formats
            let progresoArray = [];
            if (Array.isArray(progreso)) {
                progresoArray = progreso;
            } else if (progreso && Array.isArray(progreso.data)) {
                progresoArray = progreso.data;
            } else if (progreso && progreso.success && Array.isArray(progreso.data)) {
                progresoArray = progreso.data;
            } else if (progreso && typeof progreso === 'object' && progreso.result && Array.isArray(progreso.result)) {
                progresoArray = progreso.result;
            }
            
            console.log(`📊 Progreso procesado: ${progresoArray.length} registros encontrados`);
            
            if (progresoArray.length === 0) {
                console.log('📊 No hay progreso registrado para el usuario');
                // Don't reset progresoPorTema completely, keep existing local state
                if (!this.progresoPorTema || Object.keys(this.progresoPorTema).length === 0) {
                    this.progresoPorTema = {};
                }
                return;
            }

            // Get all problems to map problemaId to temaId
            // IMPORTANT: Use useRandom=false to get ALL problems, not just random ones
            let allProblemas = [];
            try {
                // Get problems from all topics in current level
                const temas = await window.curriculumManager.cargarTemas(this.currentNivelId);
                for (const tema of temas) {
                    const temaId = tema.id || tema.IdTemas || tema.Id || tema.idTemas;
                    
                    // Validate temaId before using it
                    if (!temaId || temaId === 'undefined' || temaId === undefined) {
                        console.warn(`⚠️ Saltando tema con ID inválido:`, tema);
                        continue;
                    }
                    
                    const numTemaId = parseInt(temaId);
                    if (isNaN(numTemaId) || numTemaId <= 0) {
                        console.warn(`⚠️ Saltando tema con ID no numérico:`, temaId);
                        continue;
                    }
                    
                    try {
                        // Use useRandom=false to get ALL problems for mapping, not just random ones
                        const problemas = await window.curriculumManager.cargarProblemas(numTemaId, false);
                        if (Array.isArray(problemas)) {
                            allProblemas = allProblemas.concat(problemas);
                            console.log(`📚 Cargados ${problemas.length} problemas del tema ${numTemaId} para mapeo`);
                        }
                    } catch (err) {
                        console.warn(`Error cargando problemas del tema ${numTemaId}:`, err);
                        // Fallback: try to get from API directly without random
                        try {
                            const result = await window.api._makeRequest('/Problemas');
                            let problemsFromAPI = [];
                            if (Array.isArray(result)) {
                                problemsFromAPI = result;
                            } else if (result && Array.isArray(result.data)) {
                                problemsFromAPI = result.data;
                            }
                            const filtered = problemsFromAPI.filter(p => {
                                const pTemaId = p.temaId || p.tema_id || p.TemaId;
                                return pTemaId === numTemaId || pTemaId === parseInt(numTemaId);
                            });
                            if (filtered.length > 0) {
                                allProblemas = allProblemas.concat(filtered);
                                console.log(`📚 Cargados ${filtered.length} problemas del tema ${numTemaId} vía fallback`);
                            }
                        } catch (apiErr) {
                            console.warn('Error en fallback API:', apiErr);
                        }
                    }
                }
            } catch (err) {
                console.warn('Error cargando problemas por tema:', err);
            }

            // Create a map of problemaId to temaId - normalize IDs to ensure consistency
            const problemaToTema = {};
            for (const problema of allProblemas) {
                const problemaId = problema.id || problema.Id || problema.IdProblema;
                const temaId = problema.temaId || problema.TemaId || problema.tema_id;
                if (problemaId && temaId) {
                    // Normalize to numbers for consistency
                    const numProblemaId = parseInt(problemaId);
                    const numTemaId = parseInt(temaId);
                    if (!isNaN(numProblemaId) && !isNaN(numTemaId) && numProblemaId > 0 && numTemaId > 0) {
                        problemaToTema[numProblemaId] = numTemaId;
                        problemaToTema[problemaId] = numTemaId; // Also store as string key for flexibility
                    }
                }
            }
            console.log(`🗺️ Mapa problema→tema creado con ${Object.keys(problemaToTema).length} entradas`);

            // Group progress by tema (topic)
            this.progresoPorTema = {};
            for (const p of progresoArray) {
                const problemaId = p.problemaId || p.ProblemaId;
                const completado = p.completado || p.Completado || false;
                
                if (completado && problemaId) {
                    // Use the map if available, otherwise fetch individual problem
                    let temaId = problemaToTema[problemaId];
                    
                    if (!temaId) {
                        // Fallback: get the problem individually
                        try {
                            const problema = await window.curriculumManager.obtenerProblema(problemaId);
                            if (problema) {
                                temaId = problema.temaId || problema.TemaId || problema.tema_id;
                                if (temaId) {
                                    problemaToTema[problemaId] = temaId; // Cache it
                                }
                            }
                        } catch (err) {
                            console.warn('Error obteniendo problema para progreso:', err);
                            continue;
                        }
                    }
                    
                    if (temaId) {
                        // Ensure temaId is consistent (always use number)
                        const numTemaId = parseInt(temaId);
                        if (!isNaN(numTemaId) && numTemaId > 0) {
                            if (!this.progresoPorTema[numTemaId]) {
                                this.progresoPorTema[numTemaId] = [];
                            }
                            // Only add if not already present (avoid duplicates)
                            if (!this.progresoPorTema[numTemaId].includes(problemaId)) {
                                this.progresoPorTema[numTemaId].push(problemaId);
                                console.log(`✅ Progreso mapeado: problema ${problemaId} → tema ${numTemaId}`);
                            }
                        } else {
                            console.warn(`⚠️ temaId inválido para problema ${problemaId}:`, temaId);
                        }
                    } else {
                        console.warn(`⚠️ No se pudo determinar temaId para problema completado ${problemaId}`);
                    }
                }
            }
            
            // Log final progress by topic
            console.log('📊 Progreso por tema final:', this.progresoPorTema);
            for (const [temaId, problemas] of Object.entries(this.progresoPorTema)) {
                console.log(`   Tema ${temaId}: ${problemas.length} problemas completados`);
            }
        } catch (error) {
            console.error('❌ Error cargando progreso:', error);
            this.progresoPorTema = {};
        }
    }

    async renderSidebarTemas() {
        try {
            // Load topics for current level
            this.temas = await window.curriculumManager.cargarTemas(this.currentNivelId);
            console.log('📚 Temas cargados:', this.temas);
            
            // Sort topics by order
            this.temas.sort((a, b) => (a.orden || a.Orden || 0) - (b.orden || b.Orden || 0));
            
            // Determine which topics are unlocked
            const temasUnlocked = this.determineUnlockedTopics();
            
            // Render topics sidebar
            const topicsList = document.getElementById('topics-list');
            if (topicsList) {
                if (this.temas.length === 0) {
                    topicsList.innerHTML = '<div class="text-gray-500 dark:text-slate-400 text-sm p-4">No hay temas disponibles</div>';
                    return;
                }

                // Load problems count for each topic
                this.temas = await Promise.all(this.temas.map(async (tema) => {
                    // Try multiple ways to get temaId
                    const temaId = tema.id || tema.IdTemas || tema.Id || tema.idTemas;
                    console.log('🔍 Extrayendo temaId del tema:', tema, '→ temaId:', temaId);
                    
                    if (!temaId || temaId === 'undefined' || temaId === undefined) {
                        console.error('❌ No se pudo extraer temaId válido del tema:', tema);
                        return { ...tema, total_problemas: 0 };
                    }
                    
                    try {
                        // Ensure temaId is a number
                        const numTemaId = parseInt(temaId);
                        if (isNaN(numTemaId) || numTemaId <= 0) {
                            console.error('❌ temaId no es un número válido:', temaId);
                            return { ...tema, total_problemas: 0 };
                        }
                        
                        const problemas = await window.curriculumManager.cargarProblemas(numTemaId);
                        return { ...tema, total_problemas: Array.isArray(problemas) ? problemas.length : 0 };
                    } catch (error) {
                        console.warn(`Error cargando problemas para tema ${temaId}:`, error);
                        return { ...tema, total_problemas: 0 };
                    }
                }));

                let html = '';
                for (let i = 0; i < this.temas.length; i++) {
                    const tema = this.temas[i];
                    // Try multiple ways to get temaId - backend uses IdTemas
                    const temaId = tema.id || tema.IdTemas || tema.Id || tema.idTemas;
                    const titulo = tema.titulo || tema.Titulo || 'Sin título';
                    const descripcion = tema.descripcion || tema.Descripcion || '';
                    
                    if (!temaId) {
                        console.error('❌ Tema sin ID válido:', tema);
                        continue;
                    }
                    
                    const isLocked = !temasUnlocked[temaId];
                    const completados = (this.progresoPorTema[temaId] || []).length;
                    const totalProblemas = tema.total_problemas || 0;
                    
                    html += `
                        <div class="tema p-3 mb-2 rounded ${isLocked ? 'locked' : ''}" 
                             data-tema-id="${temaId || ''}"
                             ${isLocked ? '' : 'style="cursor: pointer;"'}>
                            <div class="flex items-center justify-between mb-1">
                                <h4 class="font-semibold text-gray-800 dark:text-white">${titulo}</h4>
                                ${isLocked ? '<span class="text-xs text-gray-500">🔒</span>' : ''}
                            </div>
                            <p class="text-xs text-gray-600 dark:text-slate-400 mb-2">${descripcion}</p>
                            <div class="flex items-center justify-between text-xs">
                                <span class="text-gray-500 dark:text-slate-400">
                                    ${completados}/${totalProblemas} completados
                                </span>
                                ${!isLocked && completados >= 10 ? 
                                    '<span class="text-green-600 dark:text-green-400">✓ Desbloqueado</span>' : 
                                    isLocked ? 
                                    `<span class="text-gray-500">Completa 10 del tema anterior</span>` :
                                    `<span class="text-blue-600">${10 - completados} para desbloquear siguiente</span>`
                                }
                            </div>
                            ${!isLocked ? `
                                <div class="mt-2 w-full bg-gray-200 dark:bg-slate-700 rounded-full h-2">
                                    <div class="bg-indigo-600 h-2 rounded-full progress-bar" 
                                         style="width: ${Math.min((completados / 10) * 100, 100)}%"></div>
                                </div>
                            ` : ''}
                        </div>
                    `;
                }
                
                topicsList.innerHTML = html;
                
                // Add click handlers
                topicsList.querySelectorAll('.tema:not(.locked)').forEach(el => {
                    el.addEventListener('click', () => {
                        const temaIdStr = el.dataset.temaId;
                        const temaId = temaIdStr ? parseInt(temaIdStr) : null;
                        console.log('🖱️ Click en tema. temaId del dataset:', temaIdStr, '→ parseado:', temaId);
                        if (temaId && !isNaN(temaId)) {
                            this.selectTema(temaId);
                        } else {
                            console.error('❌ temaId inválido al hacer click:', temaIdStr);
                        }
                    });
                });
                
                // Update topics count
                const topicsCount = document.getElementById('topics-count');
                if (topicsCount) {
                    const unlockedCount = Object.values(temasUnlocked).filter(v => v).length;
                    topicsCount.textContent = `${unlockedCount}/${this.temas.length} temas desbloqueados`;
                }
                
                // Auto-select first unlocked topic
                const firstUnlockedTema = this.temas.find(t => {
                    const temaId = t.id || t.IdTemas || t.Id || t.idTemas;
                    return temaId && temasUnlocked[temaId];
                });
                
                if (firstUnlockedTema) {
                    const temaId = firstUnlockedTema.id || firstUnlockedTema.IdTemas || firstUnlockedTema.Id || firstUnlockedTema.idTemas;
                    console.log('🔄 Auto-seleccionando primer tema desbloqueado:', temaId, 'Tema:', firstUnlockedTema);
                    // Use setTimeout to ensure DOM is ready
                    setTimeout(() => {
                        this.selectTema(temaId);
                    }, 100);
                } else {
                    console.warn('⚠ No se encontró ningún tema desbloqueado. Temas:', this.temas);
                }
            }
        } catch (error) {
            console.error('❌ Error renderizando temas:', error);
            const topicsList = document.getElementById('topics-list');
            if (topicsList) {
                topicsList.innerHTML = '<div class="text-red-500 text-sm p-4">Error al cargar temas</div>';
            }
        }
    }

    determineUnlockedTopics() {
        const unlocked = {};
        
        // First topic is always unlocked
        if (this.temas.length > 0) {
            const firstTema = this.temas[0];
            const firstTemaId = firstTema.id || firstTema.IdTemas || firstTema.Id || firstTema.idTemas;
            if (firstTemaId) {
                unlocked[firstTemaId] = true;
                console.log(`✅ Tema inicial desbloqueado: ${firstTemaId} - ${firstTema.titulo || firstTema.Titulo}`);
            }
        }
        
        // Subsequent topics are unlocked if previous topic has 10+ completed problems
        for (let i = 1; i < this.temas.length; i++) {
            const prevTema = this.temas[i - 1];
            const prevTemaId = prevTema.id || prevTema.IdTemas || prevTema.Id || prevTema.idTemas;
            if (!prevTemaId) {
                console.warn(`⚠ No se pudo obtener ID del tema anterior en índice ${i - 1}`);
                continue;
            }
            
            const prevCompletados = (this.progresoPorTema[prevTemaId] || []).length;
            const prevTitulo = prevTema.titulo || prevTema.Titulo || 'Tema anterior';
            
            console.log(`📊 Tema anterior "${prevTitulo}" (ID: ${prevTemaId}): ${prevCompletados}/10 problemas completados`);
            
            if (prevCompletados >= 10) {
                const currentTema = this.temas[i];
                const currentTemaId = currentTema.id || currentTema.IdTemas || currentTema.Id || currentTema.idTemas;
                if (currentTemaId) {
                    unlocked[currentTemaId] = true;
                    const currentTitulo = currentTema.titulo || currentTema.Titulo || 'Tema actual';
                    console.log(`✅ Tema desbloqueado: ${currentTemaId} - ${currentTitulo} (${prevCompletados} problemas completados en tema anterior)`);
                }
            } else {
                const currentTema = this.temas[i];
                const currentTitulo = currentTema.titulo || currentTema.Titulo || 'Tema siguiente';
                console.log(`🔒 Tema bloqueado: ${currentTitulo} (requiere ${10 - prevCompletados} problemas más del tema anterior)`);
                break; // Stop unlocking if a topic doesn't meet requirements
            }
        }
        
        console.log('🔓 Resumen de temas desbloqueados:', Object.keys(unlocked).length, 'de', this.temas.length);
        return unlocked;
    }

    async selectTema(temaId) {
        try {
            // Validate temaId before proceeding
            if (!temaId || temaId === 'undefined' || temaId === undefined) {
                console.error('❌ Error: temaId inválido al seleccionar tema:', temaId);
                return;
            }
            
            const numTemaId = parseInt(temaId);
            if (isNaN(numTemaId) || numTemaId <= 0) {
                console.error('❌ Error: temaId no es un número válido:', temaId);
                return;
            }
            
            console.log('🔄 Seleccionando tema:', numTemaId);
            this.currentTemaId = numTemaId;
            
            // Load problems for this topic
            this.problemas = await window.curriculumManager.cargarProblemas(numTemaId);
            console.log('📝 Problemas cargados para tema', numTemaId, ':', this.problemas?.length || 0);
            
            // Ensure problemas is an array
            if (!Array.isArray(this.problemas)) {
                console.warn('⚠ Problemas no es un array:', this.problemas);
                this.problemas = [];
            }
            
            // Sort problems by order
            this.problemas.sort((a, b) => (a.orden || a.Orden || 0) - (b.orden || b.Orden || 0));
            
            // Render problems sidebar
            await this.renderProblemsList();
            
            // Update counter after loading problems
            this.updateProblemsCounter();
            
            // Highlight selected topic
            document.querySelectorAll('.tema').forEach(el => {
                el.classList.remove('active');
                if (parseInt(el.dataset.temaId) === numTemaId) {
                    el.classList.add('active');
                }
            });
        } catch (error) {
            console.error('❌ Error seleccionando tema:', error);
            const problemsList = document.getElementById('problems-list');
            if (problemsList) {
                problemsList.innerHTML = `<div class="text-red-500 text-sm p-4">Error al cargar problemas: ${error.message}</div>`;
            }
        }
    }

    async renderProblemsList() {
        console.log('🚀 INICIANDO renderProblemsList()');
        const problemsList = document.getElementById('problems-list');
        const problemsTitle = document.getElementById('problems-title');
        const problemsCount = document.getElementById('problems-count');
        
        if (!problemsList) {
            console.warn('⚠ Elemento problems-list no encontrado');
            return;
        }
        
        console.log('🔄 Renderizando lista de problemas. TemaId:', this.currentTemaId, 'Problemas:', this.problemas?.length || 0);
        console.log('🔄 progresoPorTema completo:', this.progresoPorTema);
        
        if (!this.currentTemaId) {
            problemsList.innerHTML = '<div class="text-gray-500 dark:text-slate-400 text-sm p-4">Selecciona un tema para ver los problemas</div>';
            if (problemsCount) problemsCount.textContent = '0/10 completados';
            return;
        }
        
        if (!this.problemas || this.problemas.length === 0) {
            problemsList.innerHTML = '<div class="text-yellow-500 dark:text-yellow-400 text-sm p-4">No hay problemas disponibles para este tema</div>';
            if (problemsCount) {
                const completadosCount = (this.progresoPorTema[this.currentTemaId] || []).length;
                problemsCount.textContent = `${completadosCount}/10 completados`;
            }
            console.warn('⚠ No hay problemas para el tema', this.currentTemaId);
            return;
        }
        
        // Get completed problems for this topic - normalize temaId for consistent lookup
        const numTemaId = parseInt(this.currentTemaId);
        const completadosRaw = this.progresoPorTema[numTemaId] || this.progresoPorTema[this.currentTemaId] || [];
        console.log(`🎨 Renderizando lista - Tema ${numTemaId}, Completados raw:`, completadosRaw);
        
        // Normalize all completed IDs to numbers for consistent comparison
        const completados = completadosRaw.map(id => parseInt(id)).filter(id => !isNaN(id));
        console.log(`🎨 Completados normalizados:`, completados);
        
        // Count how many problems in the current list are actually completed
        let problemasCompletadosEnLista = 0;
        let html = '';
        for (let i = 0; i < this.problemas.length; i++) {
            const problema = this.problemas[i];
            const problemaId = problema.id || problema.Id;
            // Normalize problemaId to number for consistent comparison
            const numProblemaId = parseInt(problemaId);
            const titulo = problema.titulo || problema.Titulo || 'Sin título';
            // Check if this problem is completed
            const isCompleted = completados.includes(numProblemaId);
            const isLocked = problema.locked || problema.Locked;
            const isActive = this.currentProblemaId === problemaId || this.currentProblemaId === numProblemaId;
            
            if (isCompleted) {
                problemasCompletadosEnLista++;
                console.log(`  ✅ Problema ${numProblemaId} (${titulo}) marcado como completado`);
            }
            
            html += `
                <div class="problema-item p-2 mb-1 rounded ${isCompleted ? 'completed' : ''} ${isActive ? 'active' : ''} ${isLocked ? 'locked' : ''}"
                     data-problema-id="${problemaId}"
                     ${isLocked ? '' : 'style="cursor: pointer;"'}>
                    <div class="flex items-center justify-between">
                        <span class="text-sm text-gray-700 dark:text-gray-300">${i + 1}. ${titulo}</span>
                        ${isCompleted ? '<span class="text-green-600">✓</span>' : ''}
                        ${isLocked ? '<span class="text-gray-500 text-xs">🔒</span>' : ''}
                    </div>
                </div>
            `;
        }
        
        // AGREGAR BOTÓN DE CERTIFICADO SIEMPRE - BLOQUEADO SI NO HAY 10 COMPLETADOS
        const puedeDescargar = problemasCompletadosEnLista >= 10;
        // NO agregar botón aquí - se usa el botón único del HTML principal
        problemsList.innerHTML = html;
        
        // Update problems count to show X/10 (for unlock requirement)
        // Use the helper method for consistency
        this.updateProblemsCounter();
        
        // Add click handlers for problems
        problemsList.querySelectorAll('.problema-item:not(.locked)').forEach(el => {
            el.addEventListener('click', () => {
                const problemaId = parseInt(el.dataset.problemaId);
                this.cargarProblema(problemaId);
            });
        });
        
        // Update problems count
        if (problemsCount) {
            problemsCount.textContent = `${problemasCompletadosEnLista}/${this.problemas.length} completados`;
        }
        
        if (problemsTitle) {
            const tema = this.temas.find(t => (t.id || t.IdTemas || t.Id) === this.currentTemaId);
            problemsTitle.textContent = `Problemas - ${tema ? (tema.titulo || tema.Titulo) : 'Tema'}`;
        }
    }

    async cargarProblema(problemaId) {
        try {
            this.showLoading();
            
            // Ensure problemaId is a number
            problemaId = parseInt(problemaId);
            if (isNaN(problemaId) || problemaId <= 0) {
                this.showError('ID de problema inválido.');
                return;
            }
            
            const problema = await window.curriculumManager.obtenerProblema(problemaId);
            
            if (!problema) {
                this.showError('No se pudo cargar el problema. El problema no existe en la base de datos.');
                return;
            }
            
            // Ensure we have the correct problemaId from the problem object (database ID takes precedence)
            const actualProblemaId = problema.Id || problema.id || problema.IdProblema;
            if (!actualProblemaId) {
                console.error('❌ Problema sin ID válido:', problema);
                this.showError('Error: El problema no tiene un ID válido. Por favor, recarga la página.');
                return;
            }
            
            if (actualProblemaId !== problemaId) {
                console.warn(`⚠ ID mismatch: requested ${problemaId}, got ${actualProblemaId} from database. Using database ID ${actualProblemaId}.`);
            }
            
            this.currentProblema = problema;
            this.currentProblemaId = actualProblemaId;
            
            console.log('✅ Problema cargado correctamente. ID:', this.currentProblemaId);
            
            this.renderProblem(problema);
            
            // Update active problem in list
            document.querySelectorAll('.problema-item').forEach(el => {
                el.classList.remove('active');
                if (parseInt(el.dataset.problemaId) === problemaId) {
                    el.classList.add('active');
                }
            });
            
            // Update problem counter
            const index = this.problemas.findIndex(p => (p.id || p.Id) === problemaId);
            const problemCounter = document.getElementById('problem-counter');
            if (problemCounter) {
                problemCounter.textContent = `${index + 1}/${this.problemas.length}`;
            }
            
            // Update navigation buttons
            const prevBtn = document.getElementById('prev-problem-btn');
            const nextBtn = document.getElementById('next-problem-btn');
            if (prevBtn) prevBtn.disabled = index === 0;
            if (nextBtn) nextBtn.disabled = index === this.problemas.length - 1;
            
        } catch (error) {
            console.error('❌ Error cargando problema:', error);
            this.showError('Error al cargar el problema: ' + error.message);
        }
    }

    cargarProblemaAnterior() {
        if (!this.currentProblemaId || this.problemas.length === 0) return;
        
        const currentIndex = this.problemas.findIndex(p => (p.id || p.Id) === this.currentProblemaId);
        if (currentIndex > 0) {
            const prevProblema = this.problemas[currentIndex - 1];
            this.cargarProblema(prevProblema.id || prevProblema.Id);
        }
    }

    cargarProblemaSiguiente() {
        if (!this.currentProblemaId || this.problemas.length === 0) return;
        
        const currentIndex = this.problemas.findIndex(p => (p.id || p.Id) === this.currentProblemaId);
        if (currentIndex < this.problemas.length - 1) {
            const nextProblema = this.problemas[currentIndex + 1];
            this.cargarProblema(nextProblema.id || nextProblema.Id);
        }
    }

    renderProblem(problema) {
        try {
            const problemaId = problema.id || problema.Id || problema.IdProblema;
            const titulo = problema.titulo || problema.Titulo || 'Sin título';
            const descripcion = problema.descripcion || problema.Descripcion || '';
            const ejemplo = problema.ejemplo || problema.Ejemplo || '';
            const dificultad = problema.dificultad || problema.Dificultad || 'Media';
            const puntos = problema.puntos_otorgados || problema.PuntosOtorgados || 0;
            const codigoInicial = problema.codigo_inicial || problema.CodigoInicial || '# Escribe tu código aquí\n';

            // Update UI
            const problemTitleEl = document.getElementById('problem-title');
            const problemDescEl = document.getElementById('problem-description');
            const completedBadge = document.getElementById('completed-badge');

            if (problemTitleEl) {
                problemTitleEl.textContent = titulo;
            }
            
            // Show/hide completed badge
            if (completedBadge) {
                const completados = this.progresoPorTema[this.currentTemaId] || [];
                const isCompleted = completados.includes(problemaId);
                if (isCompleted) {
                    completedBadge.classList.remove('hidden');
                } else {
                    completedBadge.classList.add('hidden');
                }
            }

            if (problemDescEl) {
                problemDescEl.innerHTML = `
                    <div class="space-y-4">
                        <p class="text-gray-700 dark:text-gray-300">${descripcion}</p>
                        ${ejemplo ? `
                            <div class="mt-4 p-3 bg-blue-50 dark:bg-slate-700 rounded-lg">
                                <strong class="text-blue-800 dark:text-blue-300">Ejemplo:</strong>
                                <code class="block mt-2 text-sm bg-gray-100 dark:bg-slate-800 px-2 py-1 rounded">${ejemplo}</code>
                            </div>
                        ` : ''}
                        <div class="flex gap-4 text-sm text-gray-600 dark:text-slate-400">
                            <span><strong>Dificultad:</strong> ${dificultad}</span>
                            <span><strong>Puntos:</strong> ${puntos}</span>
                        </div>
                    </div>
                `;
            }

            // Load code in editor
            if (window.monacoEditor) {
                window.monacoEditor.setValue(codigoInicial);
                console.log('✅ Código inicial cargado en editor');
            } else {
                console.log('⚠ Editor Monaco no está disponible');
            }

            // Enable buttons
            const runBtn = document.getElementById('run-btn');
            const verifyBtn = document.getElementById('verify-btn');
            if (runBtn) runBtn.disabled = false;
            if (verifyBtn) verifyBtn.disabled = false;

            // Hide loading, show problem
            this.hideLoading();
            this.showProblem();

            console.log('✅ Problema renderizado correctamente');
        } catch (error) {
            console.error('❌ Error renderizando problema:', error);
            this.showError('Error al mostrar el problema: ' + error.message);
        }
    }


    showLoading() {
        const problemContainer = document.getElementById('problem-container') ||
            document.querySelector('.problem-container') ||
            document.querySelector('[id*="problem"]');

        if (problemContainer) {
            const loadingHtml = `
                <div class="flex items-center justify-center h-64">
                    <div class="text-center">
                        <div class="animate-spin rounded-full h-12 w-12 border-b-2 border-indigo-600 mx-auto mb-4"></div>
                        <p class="text-gray-600 dark:text-slate-400">Cargando problema...</p>
                    </div>
                </div>
            `;
            const problemDescEl = document.getElementById('problem-description');
            if (problemDescEl) {
                problemDescEl.innerHTML = loadingHtml;
            }
        }
    }

    hideLoading() {
        // Loading is hidden when problem is rendered
    }

    updateProblemCompletedUI(problemaId, isCompleted) {
        // Normalize problemaId to number for consistent matching
        const numProblemaId = parseInt(problemaId);
        console.log(`🎨 Actualizando UI visual para problema ${numProblemaId}, completado: ${isCompleted}`);
        
        // Try multiple selector strategies to find the element
        let problemaItem = document.querySelector(`.problema-item[data-problema-id="${numProblemaId}"]`);
        if (!problemaItem) {
            problemaItem = document.querySelector(`.problema-item[data-problema-id="${problemaId}"]`);
        }
        if (!problemaItem) {
            // Try finding by comparing all items
            const allItems = document.querySelectorAll('.problema-item');
            for (const item of allItems) {
                const itemId = parseInt(item.getAttribute('data-problema-id'));
                if (itemId === numProblemaId) {
                    problemaItem = item;
                    break;
                }
            }
        }
        
        if (problemaItem) {
            console.log(`✅ Elemento encontrado para problema ${numProblemaId}`);
            
            if (isCompleted) {
                // Force add completed class for green styling
                problemaItem.classList.add('completed');
                
                // Also add inline styles as backup to ensure green color is visible
                problemaItem.style.backgroundColor = '#dcfce7';
                problemaItem.style.borderLeft = '3px solid #16a34a';
                
                // Find the flex container
                let flexContainer = problemaItem.querySelector('.flex.items-center.justify-between');
                
                if (flexContainer) {
                    // Remove any existing checkmark first
                    const existingChecks = flexContainer.querySelectorAll('.text-green-600, span.text-green-600');
                    existingChecks.forEach(check => {
                        const text = check.textContent || check.innerText || '';
                        if (text.trim() === '✓' || text.includes('✓')) {
                            check.remove();
                        }
                    });
                    
                    // Add checkmark
                    const checkmark = document.createElement('span');
                    checkmark.className = 'text-green-600 font-bold ml-2';
                    checkmark.textContent = '✓';
                    checkmark.style.fontSize = '1.2em';
                    flexContainer.appendChild(checkmark);
                    
                    console.log(`✅ Checkmark agregado al problema ${numProblemaId}`);
                } else {
                    console.warn(`⚠️ No se encontró flex container para problema ${numProblemaId}`);
                }
                
                console.log(`✅ Problema ${numProblemaId} marcado visualmente como completado - Clase 'completed' agregada + estilos inline aplicados`);
            } else {
                problemaItem.classList.remove('completed');
                problemaItem.style.backgroundColor = '';
                problemaItem.style.borderLeft = '';
                const checkmarks = problemaItem.querySelectorAll('.text-green-600');
                checkmarks.forEach(check => {
                    if (check.textContent === '✓' || check.textContent.trim() === '✓') {
                        check.remove();
                    }
                });
            }
        } else {
            console.warn(`⚠️ No se encontró elemento DOM para problema ${numProblemaId}`);
            // Debug: list all problema items
            const allItems = document.querySelectorAll('.problema-item');
            console.log(`🔍 Total de elementos problema-item encontrados: ${allItems.length}`);
            allItems.forEach((item, index) => {
                const itemId = item.getAttribute('data-problema-id');
                console.log(`   Item ${index}: data-problema-id="${itemId}"`);
            });
        }
        
        // Also update the completed badge in the problem view
        const completedBadge = document.getElementById('completed-badge');
        if (completedBadge) {
            if (isCompleted) {
                completedBadge.classList.remove('hidden');
                console.log(`✅ Badge 'Completado' mostrado`);
            } else {
                completedBadge.classList.add('hidden');
            }
        }
    }

    updateProblemsCounter() {
        const problemsCount = document.getElementById('problems-count');
        if (!problemsCount) {
            console.warn('⚠️ Elemento problems-count no encontrado en el DOM');
            return;
        }
        
        if (!this.currentTemaId) {
            console.warn('⚠️ currentTemaId no está definido, no se puede actualizar el contador');
            problemsCount.textContent = '0/10 completados';
            return;
        }
        
        // Normalize temaId to ensure consistent lookup
        const numTemaId = parseInt(this.currentTemaId);
        if (isNaN(numTemaId) || numTemaId <= 0) {
            console.error(`❌ currentTemaId no es válido: ${this.currentTemaId}`);
            problemsCount.textContent = '0/10 completados';
            return;
        }
        
        // Try both numeric and string keys
        const completados = this.progresoPorTema[numTemaId] || this.progresoPorTema[this.currentTemaId] || [];
        const completadosCount = completados.length;
        
        console.log(`📊 Actualizando contador UI: ${completadosCount}/10 problemas completados del tema ${numTemaId}`);
        
        if (completadosCount >= 10) {
            problemsCount.textContent = `✓ ${completadosCount}/10 completados - ¡Tema completado!`;
            problemsCount.classList.add('text-green-600', 'font-bold', 'dark:text-green-400');
            problemsCount.classList.remove('text-gray-600', 'dark:text-gray-400');
            
            // NO agregar botón aquí - se usa el botón único del HTML principal
        } else {
            problemsCount.textContent = `${completadosCount}/10 completados`;
            problemsCount.classList.remove('text-green-600', 'font-bold', 'dark:text-green-400');
            problemsCount.classList.add('text-gray-600', 'dark:text-gray-400');
        }
    }

    showProblem() {
        const problemContainer = document.getElementById('problem-container') ||
            document.querySelector('.problem-container');
        if (problemContainer) {
            problemContainer.style.display = 'block';
        }
    }

    showError(message) {
        const problemDescEl = document.getElementById('problem-description');
        if (problemDescEl) {
            problemDescEl.innerHTML = `
                <div class="p-4 bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800 rounded-lg">
                    <p class="text-red-800 dark:text-red-300">${message}</p>
                    <button onclick="window.problemsRenderer.loadRandomProblem()" 
                            class="mt-4 px-4 py-2 bg-indigo-600 text-white rounded hover:bg-indigo-700">
                        Intentar de nuevo
                    </button>
                </div>
            `;
        }
    }

    async verificarSolucion() {
        if (!this.currentProblemaId || !this.userId) {
            const outputContent = document.getElementById('output-content');
            if (outputContent) {
                outputContent.textContent = "Por favor, espera a que se cargue el problema.";
                outputContent.classList.add('text-red-600');
            }
            return;
        }

        const outputContent = document.getElementById('output-content');
        if (outputContent) {
            outputContent.textContent = "Verificando solución...";
            outputContent.classList.remove('text-green-600', 'text-red-600');
        }

        const verifyBtn = document.getElementById('verify-btn');
        if (verifyBtn) {
            verifyBtn.disabled = true;
        }

        try {
            const codigo = window.monacoEditor?.getValue() || '';
            if (!codigo.trim()) {
                if (outputContent) {
                    outputContent.textContent = "Por favor, escribe algún código antes de verificar.";
                    outputContent.classList.add('text-red-600');
                }
                if (verifyBtn) verifyBtn.disabled = false;
                return;
            }

            // Ensure problemaId is valid
            const problemaIdToValidate = parseInt(this.currentProblemaId);
            if (isNaN(problemaIdToValidate) || problemaIdToValidate <= 0) {
                if (outputContent) {
                    outputContent.textContent = "Error: ID de problema inválido. Por favor, recarga la página y selecciona un problema.";
                    outputContent.classList.add('text-red-600');
                }
                if (verifyBtn) verifyBtn.disabled = false;
                return;
            }
            
            console.log('🔄 Verificando solución:', { userId: this.userId, problemaId: problemaIdToValidate, codigoLength: codigo.length });
            
            const resultado = await window.curriculumManager.verificarSolucion(codigo, problemaIdToValidate);
            
            console.log('✅ Resultado verificación recibido:', resultado);

            if (outputContent) {
                // Handle different response formats
                const mensaje = resultado.mensaje || resultado.message || resultado.Mensaje || "Resultado desconocido";
                const isCorrect = resultado.correcto || resultado.IsCorrect || resultado.correct || false;

                outputContent.textContent = mensaje;

                if (isCorrect) {
                    outputContent.classList.add('text-green-600');
                    outputContent.classList.remove('text-red-600');

                    // Show success message with points
                    const puntos = resultado.puntosOtorgados || resultado.PuntosOtorgados || resultado.puntos_otorgados || 0;
                    if (puntos > 0) {
                        outputContent.textContent += ` (+${puntos} puntos)`;
                    }

                    // CRITICAL: Update local state FIRST before any UI updates
                    // This ensures that when we re-render, the state is already correct
                    if (this.currentTemaId) {
                        const numTemaId = parseInt(this.currentTemaId);
                        const numProblemaId = parseInt(problemaIdToValidate);
                        
                        if (!isNaN(numTemaId) && numTemaId > 0 && !isNaN(numProblemaId) && numProblemaId > 0) {
                            if (!this.progresoPorTema[numTemaId]) {
                                this.progresoPorTema[numTemaId] = [];
                            }
                            
                            // Ensure problem is in completed list (use numeric ID for consistency)
                            if (!this.progresoPorTema[numTemaId].includes(numProblemaId)) {
                                this.progresoPorTema[numTemaId].push(numProblemaId);
                                console.log(`✅ Estado actualizado - Problema ${numProblemaId} agregado a completados del tema ${numTemaId}`);
                                console.log(`📊 Estado actual del progreso para tema ${numTemaId}:`, this.progresoPorTema[numTemaId]);
                            } else {
                                console.log(`ℹ️ Problema ${numProblemaId} ya estaba en completados del tema ${numTemaId}`);
                            }
                        } else {
                            console.error(`❌ IDs inválidos - TemaId: ${this.currentTemaId}, ProblemaId: ${problemaIdToValidate}`);
                        }
                    } else {
                        console.error(`❌ currentTemaId no está definido al intentar guardar progreso`);
                    }
                    
                    // CRITICAL: Re-render the problems list immediately with updated state
                    // This ensures the HTML is regenerated with the correct completed state
                    console.log('🔄 Re-renderizando lista de problemas con estado actualizado...');
                    
                    // Update counter first
                    this.updateProblemsCounter();
                    
                    // Then re-render the entire list - this will include the completed problem in the state
                    this.renderProblemsList().then(() => {
                        const problemaIdForUI = parseInt(problemaIdToValidate);
                        console.log(`✅ Lista re-renderizada. Verificando problema ${problemaIdForUI}...`);
                        
                        // After re-render, verify and force visual update if needed
                        setTimeout(() => {
                            const verifyItem = document.querySelector(`.problema-item[data-problema-id="${problemaIdForUI}"]`);
                            if (verifyItem) {
                                // Force completed class and styles
                                verifyItem.classList.add('completed');
                                verifyItem.style.backgroundColor = '#dcfce7';
                                verifyItem.style.borderLeft = '3px solid #16a34a';
                                
                                // Ensure checkmark is present
                                let flexContainer = verifyItem.querySelector('.flex.items-center.justify-between');
                                if (flexContainer) {
                                    // Remove existing checkmarks
                                    const existingChecks = flexContainer.querySelectorAll('.text-green-600');
                                    existingChecks.forEach(check => check.remove());
                                    
                                    // Add checkmark
                                    const checkmark = document.createElement('span');
                                    checkmark.className = 'text-green-600 font-bold ml-2';
                                    checkmark.textContent = '✓';
                                    flexContainer.appendChild(checkmark);
                                }
                                
                                console.log(`✅ Problema ${problemaIdForUI} marcado visualmente como completado (forzado)`);
                            } else {
                                console.error(`❌ No se encontró elemento DOM para problema ${problemaIdForUI}`);
                                // Debug: list all items
                                const allItems = document.querySelectorAll('.problema-item');
                                console.log(`🔍 Total de elementos problema-item: ${allItems.length}`);
                                allItems.forEach((item, idx) => {
                                    console.log(`   ${idx}: data-problema-id="${item.getAttribute('data-problema-id')}"`);
                                });
                            }
                        }, 100);
                    });

                    // Reload progress and update UI after successful verification
                    // Use a longer delay to ensure backend has saved the progress
                    setTimeout(async () => {
                        console.log('🔄 Recargando progreso desde el servidor después de completar problema...');
                        if (this.currentTemaId) {
                            const numTemaId = parseInt(this.currentTemaId);
                            console.log(`📊 Estado ANTES de recargar - Tema ${numTemaId}:`, this.progresoPorTema[numTemaId] || []);
                        }
                        
                        try {
                            // Reload progress from server to get accurate count
                            await this.loadUserProgress();
                            
                            console.log('📊 Progreso recargado. progresoPorTema completo:', this.progresoPorTema);
                            if (this.currentTemaId) {
                                const numTemaId = parseInt(this.currentTemaId);
                                const completados = this.progresoPorTema[numTemaId] || [];
                                console.log(`📊 Estado DESPUÉS de recargar - Tema ${numTemaId}:`, completados);
                                console.log(`📊 Cantidad de problemas completados para tema ${numTemaId}:`, completados.length);
                                
                                // If the problem we just completed is not in the list, add it manually
                                if (!completados.includes(problemaIdToValidate)) {
                                    console.log(`⚠️ Problema ${problemaIdToValidate} no encontrado en progreso recargado, agregándolo manualmente...`);
                                    if (!this.progresoPorTema[numTemaId]) {
                                        this.progresoPorTema[numTemaId] = [];
                                    }
                                    this.progresoPorTema[numTemaId].push(problemaIdToValidate);
                                    console.log(`✅ Problema ${problemaIdToValidate} agregado manualmente. Nuevo estado:`, this.progresoPorTema[numTemaId]);
                                }
                            }
                            
                            // Update counter with fresh data
                            console.log('🔄 Actualizando contador con datos frescos del servidor...');
                            this.updateProblemsCounter();
                        } catch (error) {
                            console.error('❌ Error recargando progreso:', error);
                            // Even if reload fails, update counter with local state
                            this.updateProblemsCounter();
                        }
                        
                        // Re-render sidebar to show unlock status
                        await this.renderSidebarTemas();
                        
                        // Check if next topic should be unlocked (roadmap logic)
                        const temasUnlocked = this.determineUnlockedTopics();
                        console.log('🔓 Temas desbloqueados después de verificación:', temasUnlocked);
                        
                        if (this.currentTemaId) {
                            await this.renderProblemsList();
                            // After re-rendering, re-apply visual state to ensure completed problems are marked
                            const numTemaId = parseInt(this.currentTemaId);
                            const completados = this.progresoPorTema[numTemaId] || [];
                            completados.forEach(probId => {
                                this.updateProblemCompletedUI(probId, true);
                            });
                            console.log(`✅ Estado visual re-aplicado después de recargar progreso`);
                            
                            // Reload current problem to update completion status
                            if (this.currentProblemaId) {
                                await this.cargarProblema(this.currentProblemaId);
                            }
                        }
                        
                        // Show unlock notification if next topic was unlocked (Duolingo-style)
                        if (this.currentTemaId) {
                            const currentTemaIndex = this.temas.findIndex(t => {
                                const temaId = t.id || t.IdTemas || t.Id || t.idTemas;
                                return temaId === this.currentTemaId;
                            });
                            
                            if (currentTemaIndex >= 0 && currentTemaIndex < this.temas.length - 1) {
                                const nextTema = this.temas[currentTemaIndex + 1];
                                const nextTemaId = nextTema.id || nextTema.IdTemas || nextTema.Id || nextTema.idTemas;
                                if (nextTemaId && temasUnlocked[nextTemaId]) {
                                    const prevCompletados = (this.progresoPorTema[this.currentTemaId] || []).length;
                                    if (prevCompletados >= 10) {
                                        console.log('🎉 ¡Tema siguiente desbloqueado!', nextTema.titulo || nextTema.Titulo);
                                        // Show notification to user (Duolingo-style celebration)
                                        if (outputContent) {
                                            const unlockMsg = `\n\n🎉 ¡Felicidades! Has desbloqueado el siguiente tema: "${nextTema.titulo || nextTema.Titulo}"`;
                                            outputContent.textContent += unlockMsg;
                                            // Optionally show a more prominent notification
                                            setTimeout(() => {
                                                alert(`🎉 ¡Excelente trabajo! Has desbloqueado: "${nextTema.titulo || nextTema.Titulo}"\n\nPuedes continuar con el siguiente tema en el roadmap.`);
                                            }, 500);
                                        }
                                    }
                                }
                            }
                        }
                        
                        if (verifyBtn) {
                            verifyBtn.disabled = false;
                        }
                    }, 1500);
                } else {
                    outputContent.classList.add('text-red-600');
                    outputContent.classList.remove('text-green-600');
                    if (verifyBtn) {
                        verifyBtn.disabled = false;
                    }
                }
            } else {
                if (verifyBtn) {
                    verifyBtn.disabled = false;
                }
            }
        } catch (error) {
            console.error('❌ Error verificando solución:', error);
            if (outputContent) {
                let errorMsg = error.message || 'Error desconocido al verificar la solución.';
                
                // If error mentions problemaId not found, provide more helpful message
                if (errorMsg.includes('problema') && errorMsg.includes('no existe')) {
                    errorMsg += '\n\n💡 Sugerencia: El problema puede no existir en la base de datos. ';
                    errorMsg += 'Por favor, recarga la página y selecciona un problema de la lista.';
                    errorMsg += '\n\nSi el problema persiste, verifica que la base de datos esté correctamente inicializada.';
                }
                
                outputContent.textContent = `Error: ${errorMsg}`;
                outputContent.classList.add('text-red-600');
                outputContent.classList.remove('text-green-600');
                
                // Log diagnostic info
                console.error('🔍 Diagnostic info:', {
                    currentProblemaId: this.currentProblemaId,
                    problemaIdToValidate: problemaIdToValidate,
                    userId: this.userId
                });
                
                // Try to get problem count for debugging
                try {
                    const problemCount = await window.api.getProblemCount();
                    console.log('📊 Problemas en la base de datos:', problemCount);
                } catch (e) {
                    console.error('No se pudo obtener el conteo de problemas:', e);
                }
            }
            if (verifyBtn) {
                verifyBtn.disabled = false;
            }
        }
    }

    async ejecutarCodigo() {
        if (!this.currentProblemaId) {
            const outputContent = document.getElementById('output-content');
            if (outputContent) {
                outputContent.textContent = "Por favor, espera a que se cargue el problema.";
            }
            return;
        }

        const outputContent = document.getElementById('output-content');
        if (outputContent) {
            outputContent.textContent = "Ejecutando código...";
            outputContent.classList.remove('text-green-600', 'text-red-600');
        }

        try {
            const codigo = window.monacoEditor?.getValue() || '';
            const language = window.currentLanguage || document.getElementById('language-selector')?.value || 'python';
            const userId = this.userId || (sessionStorage.getItem('userId') ? parseInt(sessionStorage.getItem('userId')) : null);

            if (!codigo.trim()) {
                if (outputContent) {
                    outputContent.textContent = "Por favor, escribe algún código antes de ejecutar.";
                }
                return;
            }

            const resultado = await window.api.executeCode(codigo, language, userId);

            if (outputContent) {
                if (resultado.success) {
                    outputContent.textContent = resultado.output || 'Código ejecutado correctamente.';
                    outputContent.classList.add('text-green-600');
                    outputContent.classList.remove('text-red-600');

                    // Show achievement popup if granted
                    if (resultado.achievementGranted) {
                        await showAchievementPopup('Tu primer código', 'Has ejecutado tu primer código. ¡El inicio de una gran aventura!', 'fa-code');
                    }
                } else {
                    outputContent.textContent = resultado.output || resultado.message || 'Error al ejecutar el código.';
                    outputContent.classList.add('text-red-600');
                    outputContent.classList.remove('text-green-600');
                }
            }
        } catch (error) {
            console.error('Error ejecutando código:', error);
            if (outputContent) {
                outputContent.textContent = `Error: ${error.message || 'Error desconocido al ejecutar el código.'}`;
                outputContent.classList.add('text-red-600');
                outputContent.classList.remove('text-green-600');
            }
        }
    }

    async generarCertificadoNivel(temaId) {
        try {
            console.log('🎓 Generando certificado para tema:', temaId);
            
            // Verificar que html2pdf esté disponible - esperar un poco si no está
            if (typeof html2pdf === 'undefined') {
                console.log('⏳ html2pdf no está disponible, esperando...');
                // Esperar hasta 3 segundos para que se cargue
                for (let i = 0; i < 30; i++) {
                    await new Promise(resolve => setTimeout(resolve, 100));
                    if (typeof html2pdf !== 'undefined') {
                        console.log('✅ html2pdf cargado después de esperar');
                        break;
                    }
                }
                
                if (typeof html2pdf === 'undefined') {
                    alert('Error: La librería de PDF no está cargada. Por favor, recarga la página.');
                    return;
                }
            }

            // Verificar que la función de generación esté disponible
            if (typeof window.generateLevelCertificatePDF === 'undefined') {
                alert('Error: La función de generación de PDF no está disponible. Por favor, recarga la página.');
                return;
            }
            
            // Get user information
            const user = await window.api.getUserById(this.userId);
            if (!user) {
                alert('No se pudo obtener la información del usuario. Por favor, intenta de nuevo.');
                return;
            }

            // Get tema information
            const tema = this.temas.find(t => {
                const tId = t.id || t.IdTemas || t.Id || t.idTemas;
                return parseInt(tId) === parseInt(temaId);
            });

            if (!tema) {
                alert('No se pudo obtener la información del nivel. Por favor, intenta de nuevo.');
                return;
            }

            // Get completed problems count
            const numTemaId = parseInt(temaId);
            const completados = this.progresoPorTema[numTemaId] || this.progresoPorTema[temaId] || [];
            const problemasCompletados = completados.length;

            // Build full name
            const nombre = user.nombre || user.Nombre || '';
            const apellidoPaterno = user.apellidoPaterno || user.ApellidoPaterno || '';
            const apellidoMaterno = user.apellidoMaterno || user.ApellidoMaterno || '';
            const nombreCompleto = `${nombre} ${apellidoPaterno} ${apellidoMaterno}`.trim() || user.username || 'Usuario';

            // Get tema name
            const temaNombre = tema.titulo || tema.Titulo || 'Nivel';
            
            // Get nivel ID
            const nivelId = this.currentNivelId || 1;

            // Get user email
            const email = user.email || user.Email || '';

            // Preparar datos para el certificado
            const certificateData = {
                nombreCompleto: nombreCompleto,
                temaNombre: temaNombre,
                nivelId: nivelId,
                problemasCompletados: problemasCompletados,
                email: email,
                nombre: nombre,
                apellidoPaterno: apellidoPaterno,
                apellidoMaterno: apellidoMaterno
            };

            // Generar el PDF usando la función de pdf-generator.js
            await window.generateLevelCertificatePDF(certificateData);

        } catch (error) {
            console.error('❌ Error generando certificado:', error);
            alert('Error al generar el certificado. Por favor, intenta de nuevo.');
        }
    }
}

// Create global instance
window.problemsRenderer = new ProblemsRenderer();
