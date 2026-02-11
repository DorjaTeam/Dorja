// js/curriculum-manager.js
class CurriculumManager {
    constructor() {
        this.currentUser = null;
        this.api = window.api;
        this.checkAPI();
    }

    checkAPI() {
        if (!this.api) {
            console.warn('⚠ API no está disponible. Usando datos de prueba.');
            this.useMockData = true;
        } else {
            console.log('✅ API disponible');
            this.useMockData = false;
        }
    }

    async init(userId) {
        this.currentUser = userId;
        console.log('✅ CurriculumManager iniciado para usuario:', userId);
    }

    async cargarTemas(nivelId = null) {
        try {
            console.log('🔄 Solicitando temas...', nivelId ? `para nivel ${nivelId}` : '');

            if (this.useMockData) {
                console.log('📚 Usando datos de prueba para temas');
                return await this.getMockTemas(nivelId);
            }

            const temas = await this.api.cargarTemas(this.currentUser, nivelId);
            console.log('📚 Temas recibidos:', temas);
            
            // Log estructura del primer tema para debug
            if (temas && temas.length > 0) {
                console.log('🔍 Estructura del primer tema:', temas[0]);
                console.log('🔍 Claves del primer tema:', Object.keys(temas[0]));
                console.log('🔍 id:', temas[0].id, 'IdTemas:', temas[0].IdTemas, 'Id:', temas[0].Id);
            }

            // Asegurar que los temas estén ordenados por orden
            return temas.sort((a, b) => (a.orden || a.Orden || 0) - (b.orden || b.Orden || 0));
        } catch (error) {
            console.error('❌ Error cargando temas:', error);
            console.log('📚 Usando datos de prueba debido al error');
            return await this.getMockTemas(nivelId);
        }
    }

    async cargarProblemas(temaId, useRandom = true) {
        try {
            // Validate temaId
            if (!temaId || temaId === 'undefined' || temaId === undefined) {
                console.error('❌ Error: temaId inválido en cargarProblemas:', temaId);
                throw new Error('temaId inválido');
            }
            
            const numTemaId = parseInt(temaId);
            if (isNaN(numTemaId) || numTemaId <= 0) {
                console.error('❌ Error: temaId no es un número válido:', temaId);
                throw new Error('temaId no es un número válido');
            }
            
            console.log('🔄 Solicitando problemas para tema:', numTemaId, useRandom ? '(aleatorios)' : '(todos)');

            if (this.useMockData) {
                console.log('📝 Usando datos de prueba para problemas');
                return await this.getMockProblemas(temaId);
            }

            // Use random problems by default to prevent sharing solutions between users
            const problemas = await this.api.cargarProblemas(this.currentUser, numTemaId, useRandom, 10);
            console.log('📝 Problemas recibidos:', problemas);
            
            // Ensure problemas is an array and has valid IDs
            if (!Array.isArray(problemas)) {
                console.error('❌ Problemas no es un array:', problemas);
                throw new Error('Formato de respuesta inválido: se esperaba un array de problemas');
            }
            
            // If we got random problems, shuffle them again client-side for extra randomness
            let problemasValidos;
            if (useRandom && problemas.length > 0) {
                const shuffled = [...problemas].sort(() => Math.random() - 0.5);
                console.log('🔄 Problemas aleatorizados:', shuffled.length);
                // Validate that each problem has a valid ID - be more lenient with property names
                problemasValidos = shuffled.filter(p => {
                    if (!p) {
                        console.warn('⚠ Problema nulo o undefined:', p);
                        return false;
                    }
                    // Try multiple ways to get the ID
                    const id = p.Id || p.id || p.IdProblema || p.idProblema || p.Id_Problema;
                    const idNum = parseInt(id);
                    if (!id || isNaN(idNum) || idNum <= 0) {
                        console.warn('⚠ Problema sin ID válido. Objeto completo:', p, 'ID intentado:', id);
                        return false;
                    }
                    return true;
                });
            } else {
                // Validate that each problem has a valid ID - be more lenient
                problemasValidos = problemas.filter(p => {
                    if (!p) {
                        console.warn('⚠ Problema nulo o undefined:', p);
                        return false;
                    }
                    // Try multiple ways to get the ID
                    const id = p.Id || p.id || p.IdProblema || p.idProblema || p.Id_Problema;
                    const idNum = parseInt(id);
                    if (!id || isNaN(idNum) || idNum <= 0) {
                        console.warn('⚠ Problema sin ID válido. Objeto completo:', p, 'ID intentado:', id);
                        return false;
                    }
                    return true;
                });
            }
            
            console.log(`📊 Problemas recibidos: ${problemas.length}, Problemas válidos después de validación: ${problemasValidos.length}`);
            
            if (problemasValidos.length === 0 && problemas.length > 0) {
                console.error('❌ Ningún problema tiene un ID válido de', problemas.length, 'problemas recibidos');
                console.error('❌ Primeros problemas recibidos para inspección:', problemas.slice(0, 3));
                // Don't throw error, return empty array instead
                return [];
            }
            
            if (problemasValidos.length < problemas.length) {
                console.warn(`⚠ Se filtraron ${problemas.length - problemasValidos.length} problemas por falta de ID válido`);
            }
            
            console.log(`✅ ${problemasValidos.length} problemas válidos cargados para tema ${numTemaId}`);
            return problemasValidos;
        } catch (error) {
            console.error('❌ Error cargando problemas:', error);
            // Don't fall back to mock data - throw the error
            throw error;
        }
    }

    async obtenerProblema(problemaId) {
        try {
            console.log('🔄 Solicitando problema:', problemaId);

            if (this.useMockData) {
                console.log('📄 Usando datos de prueba para problema');
                return await this.getMockProblema(problemaId);
            }

            const problema = await this.api.obtenerProblema(problemaId);
            console.log('📄 Problema recibido:', problema);
            
            if (!problema) {
                throw new Error(`Problema con ID ${problemaId} no encontrado en la base de datos`);
            }
            
            // Ensure we have a valid ID from the database
            if (!problema.Id && !problema.id) {
                console.error('❌ Problema recibido sin ID válido:', problema);
                throw new Error(`Problema recibido sin ID válido`);
            }
            
            return problema;
        } catch (error) {
            console.error('❌ Error obteniendo problema:', error);
            // Don't fall back to mock data - throw the error so the UI can handle it
            throw error;
        }
    }

    async verificarSolucion(codigoUsuario, problemaId) {
        try {
            console.log('🔄 Verificando solución para problema:', problemaId, 'usuario:', this.currentUser);

            if (this.useMockData) {
                console.log('✅ Usando verificación simulada');
                return { correcto: true, mensaje: "¡Correcto! (simulado)" };
            }

            if (!window.api || !window.api.verificarSolucion) {
                console.error('❌ API.verificarSolucion no está disponible');
                return { correcto: false, mensaje: "Error: API de verificación no disponible" };
            }

            const resultado = await window.api.verificarSolucion(this.currentUser, problemaId, codigoUsuario);
            console.log('✅ Resultado verificación recibido:', resultado);

            // Handle different response formats from API
            let finalResult;
            if (resultado && typeof resultado === 'object') {
                // If resultado has a data property, use it
                if (resultado.data) {
                    finalResult = resultado.data;
                } 
                // If resultado has success property, check it
                else if (resultado.success !== undefined) {
                    finalResult = {
                        correcto: resultado.success,
                        mensaje: resultado.message || resultado.mensaje || (resultado.success ? "¡Correcto!" : "Incorrecto"),
                        ...resultado
                    };
                }
                // Otherwise use resultado directly
                else {
                    finalResult = resultado;
                }
            } else {
                finalResult = { correcto: false, mensaje: "Formato de respuesta inválido" };
            }

            // Ensure we have the correct format
            if (finalResult.correcto === undefined && finalResult.IsCorrect !== undefined) {
                finalResult.correcto = finalResult.IsCorrect;
            }
            if (!finalResult.mensaje && finalResult.message) {
                finalResult.mensaje = finalResult.message;
            }

            console.log('✅ Resultado final procesado:', finalResult);

            // Check if solution is correct to trigger certificate check
            if (finalResult.correcto || finalResult.IsCorrect) {
                this.checkFirstExerciseCompletion();
            }

            return finalResult;
        } catch (error) {
            console.error('❌ Error verificando solución:', error);
            return { 
                correcto: false, 
                mensaje: error.message || "Error al verificar la solución. Verifica tu conexión y vuelve a intentar." 
            };
        }
    }

    async checkFirstExerciseCompletion() {
        try {
            // Verificar que el API esté disponible
            if (!window.api || !window.api.getProgresoByUserId) {
                console.log('⚠ API de progreso no disponible, saltando generación de PDF');
                return;
            }

            // Obtener el progreso del usuario
            const progreso = await window.api.getProgresoByUserId(this.currentUser);

            // Validar que progreso sea un array
            if (!Array.isArray(progreso)) {
                console.log('⚠ Progreso no es un array válido, saltando generación de PDF');
                return;
            }

            // Contar cuántos ejercicios ha completado
            const ejerciciosCompletados = progreso.filter(p => p.completado || p.Completado).length;

            console.log('📊 Ejercicios completados:', ejerciciosCompletados);

            // Si es el primer ejercicio completado, generar el PDF
            if (ejerciciosCompletados === 1) {
                console.log('🎉 ¡Primer ejercicio completado! Generando certificado...');

                // Obtener los datos del usuario
                const userData = await window.api.getUserById(this.currentUser);

                if (userData && typeof window.generateCertificatePDF === 'function') {
                    // Pequeño delay para que el usuario vea el mensaje de éxito primero
                    setTimeout(() => {
                        window.generateCertificatePDF(userData);
                    }, 1500);
                } else {
                    console.warn('⚠ No se pudo generar el certificado: función no disponible o usuario no encontrado');
                }
            }
        } catch (error) {
            console.error('❌ Error al verificar primer ejercicio:', error);
            // No lanzar el error para no interrumpir el flujo normal
        }
    }

    async getRandomProblem(userId) {
        try {
            console.log('🔄 Obteniendo problema aleatorio para usuario:', userId);

            if (this.useMockData) {
                const problemas = await this.getMockProblemas(1);
                if (problemas.length > 0) {
                    return problemas[0];
                }
                return null;
            }

            const problema = await window.api.getRandomProblem(userId);
            console.log('✅ Problema aleatorio obtenido:', problema);
            return problema;
        } catch (error) {
            console.error('❌ Error obteniendo problema aleatorio:', error);
            return null;
        }
    }

    // Datos de prueba para desarrollo
    async getMockTemas(nivelId = null) {
        const todosLosTemas = [
            {
                id: 1,
                IdTemas: 1,
                titulo: "Variables en Python",
                descripcion: "Aprende los fundamentos de las variables",
                orden: 1,
                IdNivel: 1,
                locked: 0,
                total_problemas: 3,
                problemas_completados: 0
            },
            {
                id: 2,
                IdTemas: 2,
                titulo: "Condicionales",
                descripcion: "Estructuras if, else, elif",
                orden: 2,
                IdNivel: 1,
                locked: 1,
                total_problemas: 0,
                problemas_completados: 0
            },
            {
                id: 3,
                IdTemas: 3,
                titulo: "Bucles",
                descripcion: "For y while loops",
                orden: 3,
                IdNivel: 1,
                locked: 1,
                total_problemas: 0,
                problemas_completados: 0
            },
            {
                id: 4,
                IdTemas: 4,
                titulo: "Funciones Básicas",
                descripcion: "Aprende a crear y usar funciones",
                orden: 1,
                IdNivel: 2,
                locked: 1,
                total_problemas: 0,
                problemas_completados: 0
            },
            {
                id: 5,
                IdTemas: 5,
                titulo: "Listas y Diccionarios",
                descripcion: "Estructuras de datos básicas",
                orden: 2,
                IdNivel: 2,
                locked: 1,
                total_problemas: 0,
                problemas_completados: 0
            },
            {
                id: 6,
                IdTemas: 6,
                titulo: "Programación Orientada a Objetos",
                descripcion: "Clases y objetos",
                orden: 1,
                IdNivel: 3,
                locked: 1,
                total_problemas: 0,
                problemas_completados: 0
            }
        ];

        // Filtrar por nivel si se especifica
        if (nivelId !== null && nivelId !== undefined) {
            return todosLosTemas.filter(t =>
                (t.IdNivel === nivelId || t.idNivel === nivelId || t.nivel_id === nivelId || t.nivelId === nivelId)
            );
        }

        return todosLosTemas;
    }

    async getMockProblemas(temaId) {
        if (temaId === 1) {
            return [
                {
                    id: 1,
                    tema_id: 1,
                    titulo: "Declaración de variables",
                    descripcion: "Crea una variable llamada 'nombre' y asígnale tu nombre",
                    ejemplo: "nombre = 'Ana'",
                    dificultad: "Fácil",
                    codigo_inicial: "# Escribe tu código aquí\n",
                    solucion: "nombre = 'Ana'",
                    orden: 1,
                    locked: 0,
                    puntos_otorgados: 10,
                    resuelto: false,
                    puntuacion: 0,
                    ultimo_codigo: null
                },
                {
                    id: 2,
                    tema_id: 1,
                    titulo: "Múltiples variables",
                    descripcion: "Crea tres variables: nombre (texto), edad (número) y activo (booleano)",
                    ejemplo: "nombre = 'Juan', edad = 25, activo = True",
                    dificultad: "Fácil",
                    codigo_inicial: "# Escribe tu código aquí\n",
                    solucion: "nombre = 'Juan'\nedad = 25\nactivo = True",
                    orden: 2,
                    locked: 1,
                    puntos_otorgados: 15,
                    resuelto: false,
                    puntuacion: 0,
                    ultimo_codigo: null
                },
                {
                    id: 3,
                    tema_id: 1,
                    titulo: "Operaciones con variables",
                    descripcion: "Crea dos variables numéricas y calcula su suma, resta y multiplicación",
                    ejemplo: "a = 10, b = 5 → suma = 15, resta = 5, multiplicacion = 50",
                    dificultad: "Medio",
                    codigo_inicial: "# Escribe tu código aquí\n",
                    solucion: "a = 10\nb = 5\nsuma = a + b\nresta = a - b\nmultiplicacion = a * b",
                    orden: 3,
                    locked: 1,
                    puntos_otorgados: 20,
                    resuelto: false,
                    puntuacion: 0,
                    ultimo_codigo: null
                }
            ];
        }
        return [];
    }

    async getMockProblema(problemaId) {
        const problemas = await this.getMockProblemas(1);
        return problemas.find(p => p.id === problemaId) || null;
    }
}

// Solo crear la instancia si no existe
if (!window.curriculumManager) {
    window.curriculumManager = new CurriculumManager();
}