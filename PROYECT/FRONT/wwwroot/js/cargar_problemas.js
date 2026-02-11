const curriculum = [
  {
    id: 1,
    title: "Variables en Python",
    description: "Aprende los fundamentos de las variables",
    locked: false, // El primero siempre desbloqueado
    problems: [
      {
        id: 1,
        title: "¿Qué es una variable?",
        description: "Crea una variable llamada 'nombre' y asígnale tu nombre.",
        example: "nombre = 'Ana'",
        initialCode: `# Escribe tu código aquí\n`,
        solution: "nombre = 'Ana'",
        tests: [
          { test: "typeof nombre === 'string'", message: "Debe ser una cadena de texto" }
        ]
      },
      {
        id: 2,
        title: "Tipos de variables", 
        description: "Crea tres variables: una cadena, un número y un booleano.",
        example: "texto = 'Hola', numero = 42, activo = True",
        initialCode: `# Escribe tu código aquí\n`,
        locked: true, // Se desbloquea al completar el problema 1
        requiredScore: 80 // % mínimo para desbloquear
      },
      // ... 38 problemas más del tema "Variables"
    ]
  },
  {
    id: 2, 
    title: "Declaraciones condicionales",
    description: "Aprende if, else, elif",
    locked: true, // Bloqueado inicialmente
    requiredTopic: 1, // Requiere completar tema 1
    problems: [
      // ... 40 problemas de condicionales
    ]
  },
  // ... más temas
];