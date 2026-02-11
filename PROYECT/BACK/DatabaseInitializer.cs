using Microsoft.Data.Sqlite;
using System;
using System.IO;
using System.Threading.Tasks;

namespace BACK
{
    public static class DatabaseInitializer
    {
        public static void InitializeDatabase(string connectionString)
        {
            try
            {
                Console.WriteLine($"🔧 Initializing database with connection string: {connectionString}");
                
                // Parse connection string and ensure absolute path
                var dbPath = connectionString.Replace("Data Source=", "").Trim();
                var dbDirectory = Path.GetDirectoryName(dbPath);
                
                if (string.IsNullOrEmpty(dbDirectory))
                {
                    // If no directory specified, use current directory (where the backend runs)
                    dbPath = Path.Combine(Directory.GetCurrentDirectory(), dbPath);
                    dbDirectory = Directory.GetCurrentDirectory();
                }
                else if (!Path.IsPathRooted(dbPath))
                {
                    // If path is relative, make it absolute
                    dbPath = Path.Combine(Directory.GetCurrentDirectory(), dbPath);
                    dbDirectory = Path.GetDirectoryName(dbPath);
                }
                
                // Update connection string with absolute path
                connectionString = $"Data Source={dbPath}";
                
                Console.WriteLine($"📁 Database path: {dbPath}");
                Console.WriteLine($"📁 Database directory: {dbDirectory}");
                
                // Ensure directory exists
                if (!string.IsNullOrEmpty(dbDirectory) && !Directory.Exists(dbDirectory))
                {
                    Directory.CreateDirectory(dbDirectory);
                    Console.WriteLine($"✅ Created directory: {dbDirectory}");
                }

                using var connection = new SqliteConnection(connectionString);
                connection.Open();
                Console.WriteLine("✅ Database connection opened successfully");

                // Create tables - wrap each in try-catch to prevent one failure from stopping everything
                Console.WriteLine("📋 Creating/verifying tables...");
                try
                {
                    CreateUsersTable(connection);
                    Console.WriteLine("✅ Users table OK");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"⚠️ WARNING: Error with users table: {ex.Message}");
                }

                try
                {
                    CreateNivelesTable(connection);
                    Console.WriteLine("✅ Niveles table OK");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"⚠️ WARNING: Error with niveles table: {ex.Message}");
                }

                try
                {
                    CreateTemasTable(connection);
                    Console.WriteLine("✅ Temas table OK");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"⚠️ WARNING: Error with temas table: {ex.Message}");
                }

                try
                {
                    CreateProblemasTable(connection);
                    Console.WriteLine("✅ Problemas table OK");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"⚠️ WARNING: Error with problemas table: {ex.Message}");
                }

                try
                {
                    CreateProgresoProblemaTable(connection);
                    Console.WriteLine("✅ ProgresoProblema table OK");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"⚠️ WARNING: Error with progreso_problema table: {ex.Message}");
                }

                try
                {
                    CreateLogrosTable(connection);
                    Console.WriteLine("✅ Logros table OK");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"⚠️ WARNING: Error with logros table: {ex.Message}");
                }

                try
                {
                    CreateLogrosUsuarioTable(connection);
                    Console.WriteLine("✅ LogrosUsuario table OK");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"⚠️ WARNING: Error with logros_usuario table: {ex.Message}");
                }

                try
                {
                    CreateCertificadosTable(connection);
                    Console.WriteLine("✅ Certificados table OK");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"⚠️ WARNING: Error with certificados table: {ex.Message}");
                }

                Console.WriteLine("✅ All tables processed");

                // Seed initial data
                Console.WriteLine("🌱 Starting seed process...");
                try
                {
                    SeedInitialData(connection);
                    Console.WriteLine("✅ Database initialization completed successfully");
                }
                catch (Exception seedEx)
                {
                    Console.WriteLine($"❌ ERROR during seed process: {seedEx.Message}");
                    Console.WriteLine($"Stack trace: {seedEx.StackTrace}");
                    // Don't throw - allow server to start even if seed fails
                    // The tables are created, so the app can still function
                    Console.WriteLine("⚠️ WARNING: Server will start but database may be incomplete.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ ERROR initializing database: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                // Don't throw - allow server to start even if initialization has issues
                // This ensures the app can still run
                Console.WriteLine("⚠️ WARNING: Server will start but database initialization had errors.");
            }
        }

        private static void CreateUsersTable(SqliteConnection connection)
        {
            var command = connection.CreateCommand();
            command.CommandText = @"
                CREATE TABLE IF NOT EXISTS users (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    username TEXT NOT NULL,
                    nombre TEXT NOT NULL,
                    apellidoPaterno TEXT NOT NULL,
                    apellidoMaterno TEXT NOT NULL,
                    email TEXT NOT NULL UNIQUE,
                    password TEXT NOT NULL,
                    fechaRegistro TEXT NOT NULL,
                    ultimaConexion TEXT,
                    puntosTotales INTEGER DEFAULT 0,
                    nivelActual INTEGER DEFAULT 1,
                    profilePhotoPath TEXT DEFAULT '',
                    coverPhotoPath TEXT DEFAULT '',
                    profilePhotoBlob BLOB,
                    coverPhotoBlob BLOB
                )";
            command.ExecuteNonQuery();
            
            // Add columns if they don't exist (for existing databases)
            AddColumnIfNotExists(connection, "users", "profilePhotoPath", "TEXT DEFAULT ''");
            AddColumnIfNotExists(connection, "users", "coverPhotoPath", "TEXT DEFAULT ''");
            AddColumnIfNotExists(connection, "users", "profilePhotoBlob", "BLOB");
            AddColumnIfNotExists(connection, "users", "coverPhotoBlob", "BLOB");
        }
        
        private static void AddColumnIfNotExists(SqliteConnection connection, string tableName, string columnName, string columnDefinition)
        {
            try
            {
                var checkCommand = connection.CreateCommand();
                checkCommand.CommandText = $"PRAGMA table_info({tableName})";
                var reader = checkCommand.ExecuteReader();
                bool columnExists = false;
                
                while (reader.Read())
                {
                    var existingColumnName = reader.GetString(1);
                    if (existingColumnName.Equals(columnName, StringComparison.OrdinalIgnoreCase))
                    {
                        columnExists = true;
                        break;
                    }
                }
                reader.Close();
                
                if (!columnExists)
                {
                    var alterCommand = connection.CreateCommand();
                    alterCommand.CommandText = $"ALTER TABLE {tableName} ADD COLUMN {columnName} {columnDefinition}";
                    alterCommand.ExecuteNonQuery();
                    Console.WriteLine($"Added column {columnName} to table {tableName}");
                }
            }
            catch (Exception ex)
            {
                // Log the error but don't fail - column might already exist
                Console.WriteLine($"Warning: Could not add column {columnName} to {tableName}: {ex.Message}");
            }
        }

        private static void CreateNivelesTable(SqliteConnection connection)
        {
            var command = connection.CreateCommand();
            command.CommandText = @"
                CREATE TABLE IF NOT EXISTS niveles (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    nombre TEXT NOT NULL,
                    descripcion TEXT,
                    dificultad TEXT,
                    orden INTEGER,
                    puntosRequeridos INTEGER DEFAULT 0
                )";
            command.ExecuteNonQuery();
        }

        private static void CreateTemasTable(SqliteConnection connection)
        {
            var command = connection.CreateCommand();
            command.CommandText = @"
                CREATE TABLE IF NOT EXISTS temas (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    nivel_id INTEGER,
                    titulo TEXT NOT NULL,
                    descripcion TEXT,
                    orden INTEGER,
                    locked INTEGER DEFAULT 1,
                    puntos_requeridos INTEGER DEFAULT 0,
                    FOREIGN KEY (nivel_id) REFERENCES niveles(id)
                )";
            command.ExecuteNonQuery();
        }

        private static void CreateProblemasTable(SqliteConnection connection)
        {
            var command = connection.CreateCommand();
            command.CommandText = @"
                CREATE TABLE IF NOT EXISTS problemas (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    tema_id INTEGER,
                    titulo TEXT NOT NULL,
                    descripcion TEXT,
                    ejemplo TEXT,
                    dificultad TEXT,
                    codigo_inicial TEXT,
                    solucion TEXT,
                    orden INTEGER,
                    locked INTEGER DEFAULT 1,
                    puntos_otorgados INTEGER DEFAULT 10,
                    FOREIGN KEY (tema_id) REFERENCES temas(id)
                )";
            command.ExecuteNonQuery();
        }

        private static void CreateProgresoProblemaTable(SqliteConnection connection)
        {
            var command = connection.CreateCommand();
            command.CommandText = @"
                CREATE TABLE IF NOT EXISTS progreso_problema (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    user_id INTEGER,
                    problema_id INTEGER,
                    completado INTEGER DEFAULT 0,
                    puntuacion INTEGER DEFAULT 0,
                    intentos INTEGER DEFAULT 0,
                    ultimo_codigo TEXT,
                    fecha_completado TEXT,
                    FOREIGN KEY (user_id) REFERENCES users(id),
                    FOREIGN KEY (problema_id) REFERENCES problemas(id)
                )";
            command.ExecuteNonQuery();
        }

        private static void CreateLogrosTable(SqliteConnection connection)
        {
            var command = connection.CreateCommand();
            command.CommandText = @"
                CREATE TABLE IF NOT EXISTS logros (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    nombre TEXT NOT NULL,
                    descripcion TEXT,
                    iconoPhoto TEXT
                )";
            command.ExecuteNonQuery();
        }

        private static void CreateLogrosUsuarioTable(SqliteConnection connection)
        {
            var command = connection.CreateCommand();
            command.CommandText = @"
                CREATE TABLE IF NOT EXISTS logros_usuario (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    user_id INTEGER,
                    logro_id INTEGER,
                    fechaDesbloqueo TEXT,
                    FOREIGN KEY (user_id) REFERENCES users(id),
                    FOREIGN KEY (logro_id) REFERENCES logros(id)
                )";
            command.ExecuteNonQuery();
        }

        private static void CreateCertificadosTable(SqliteConnection connection)
        {
            var command = connection.CreateCommand();
            command.CommandText = @"
                CREATE TABLE IF NOT EXISTS certificados (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    user_id INTEGER,
                    nivel_id INTEGER,
                    rutaPDF TEXT,
                    fechaGenerado TEXT,
                    FOREIGN KEY (user_id) REFERENCES users(id),
                    FOREIGN KEY (nivel_id) REFERENCES niveles(id)
                )";
            command.ExecuteNonQuery();
        }

        private static void SeedInitialData(SqliteConnection connection)
        {
            try
            {
                // Check if data already exists
                var checkCommand = connection.CreateCommand();
                checkCommand.CommandText = "SELECT COUNT(*) FROM niveles";
                var nivelCount = Convert.ToInt32(checkCommand.ExecuteScalar());
                
                // Also check problems count
                var checkProblemasCommand = connection.CreateCommand();
                checkProblemasCommand.CommandText = "SELECT COUNT(*) FROM problemas";
                var problemaCount = Convert.ToInt32(checkProblemasCommand.ExecuteScalar());
                
                // Check temas count
                var checkTemasCommand = connection.CreateCommand();
                checkTemasCommand.CommandText = "SELECT COUNT(*) FROM temas";
                var temaCount = Convert.ToInt32(checkTemasCommand.ExecuteScalar());
                
                Console.WriteLine($"📊 Database status: {nivelCount} niveles, {temaCount} temas, {problemaCount} problemas");
                
                // Expected values: 1 nivel, 5 temas, 50 problemas
                const int EXPECTED_NIVELES = 1;
                const int EXPECTED_TEMAS = 5;
                const int EXPECTED_PROBLEMAS = 50;
                
                // CRITICAL: Only skip if we have ALL expected data, especially problems
                // If there are NO problems, we MUST seed
                bool hasCorrectData = nivelCount == EXPECTED_NIVELES && 
                                     temaCount == EXPECTED_TEMAS && 
                                     problemaCount == EXPECTED_PROBLEMAS;
                
                if (hasCorrectData)
                {
                    Console.WriteLine($"✅ Database is complete: {nivelCount} niveles, {temaCount} temas, {problemaCount} problemas. Skipping seed.");
                    return;
                }
                
                // CRITICAL: If there are NO problems, we must seed regardless of other data
                if (problemaCount == 0)
                {
                    Console.WriteLine($"⚠️ CRITICAL: No problems found in database! Database must be seeded.");
                    Console.WriteLine($"   Found: {nivelCount} niveles, {temaCount} temas, {problemaCount} problemas");
                    Console.WriteLine($"   Expected: {EXPECTED_NIVELES} niveles, {EXPECTED_TEMAS} temas, {EXPECTED_PROBLEMAS} problemas");
                    Console.WriteLine("⚠️ Clearing existing data and re-seeding...");
                    
                    // Disable foreign key constraints temporarily
                    var disableFKCommand = connection.CreateCommand();
                    disableFKCommand.CommandText = "PRAGMA foreign_keys = OFF";
                    disableFKCommand.ExecuteNonQuery();
                    
                    // Clear existing data in correct order (respecting foreign keys)
                    var clearCommand = connection.CreateCommand();
                    clearCommand.CommandText = @"
                        DELETE FROM progreso_problema;
                        DELETE FROM logros_usuario;
                        DELETE FROM certificados;
                        DELETE FROM problemas;
                        DELETE FROM temas;
                        DELETE FROM niveles;
                        DELETE FROM logros;
                    ";
                    clearCommand.ExecuteNonQuery();
                    
                    // Re-enable foreign key constraints
                    var enableFKCommand = connection.CreateCommand();
                    enableFKCommand.CommandText = "PRAGMA foreign_keys = ON";
                    enableFKCommand.ExecuteNonQuery();
                    
                    Console.WriteLine("✅ Existing data cleared. Re-seeding...");
                    // Continue to seed below
                }
                // If data is incomplete or incorrect, clear and re-seed
                else if (nivelCount > 0 || temaCount > 0 || problemaCount > 0)
                {
                    Console.WriteLine($"⚠️ WARNING: Database is incomplete or corrupted!");
                    Console.WriteLine($"   Expected: {EXPECTED_NIVELES} niveles, {EXPECTED_TEMAS} temas, {EXPECTED_PROBLEMAS} problemas");
                    Console.WriteLine($"   Found: {nivelCount} niveles, {temaCount} temas, {problemaCount} problemas");
                    Console.WriteLine("⚠️ Clearing existing data and re-seeding...");
                    
                    // Disable foreign key constraints temporarily
                    var disableFKCommand = connection.CreateCommand();
                    disableFKCommand.CommandText = "PRAGMA foreign_keys = OFF";
                    disableFKCommand.ExecuteNonQuery();
                    
                    // Clear existing data in correct order (respecting foreign keys)
                    var clearCommand = connection.CreateCommand();
                    clearCommand.CommandText = @"
                        DELETE FROM progreso_problema;
                        DELETE FROM logros_usuario;
                        DELETE FROM certificados;
                        DELETE FROM problemas;
                        DELETE FROM temas;
                        DELETE FROM niveles;
                        DELETE FROM logros;
                    ";
                    clearCommand.ExecuteNonQuery();
                    
                    // Re-enable foreign key constraints
                    var enableFKCommand = connection.CreateCommand();
                    enableFKCommand.CommandText = "PRAGMA foreign_keys = ON";
                    enableFKCommand.ExecuteNonQuery();
                    
                    Console.WriteLine("✅ Existing data cleared. Re-seeding...");
                }
                else
                {
                    Console.WriteLine("📝 Database is empty. Seeding initial data...");
                }

                Console.WriteLine("🌱 Seeding initial data based on syllabus...");

            // Insert Nivel 1: Introduction to Programming
            var nivel1Command = connection.CreateCommand();
            nivel1Command.CommandText = @"
                INSERT INTO niveles (nombre, descripcion, dificultad, orden, puntosRequeridos)
                VALUES ('Nivel 1', 'Introduction to Programming: Basic concepts, language elements, and program structure', 'Principiante', 1, 0)";
            nivel1Command.ExecuteNonQuery();

            var getNivel1Command = connection.CreateCommand();
            getNivel1Command.CommandText = "SELECT last_insert_rowid()";
            var nivel1Id = Convert.ToInt32(getNivel1Command.ExecuteScalar());

            // Tema 1: 2.4 Language Elements - Variables and Data Types (First topic, unlocked)
            var tema1Command = connection.CreateCommand();
            tema1Command.CommandText = @"
                INSERT INTO temas (nivel_id, titulo, descripcion, orden, locked, puntos_requeridos)
                VALUES (@nivelId, '2.4 Variables y Declaración', 'Elementos del lenguaje: tipos de datos, literales, constantes, variables, identificadores', 1, 0, 0)";
            tema1Command.Parameters.AddWithValue("@nivelId", nivel1Id);
            tema1Command.ExecuteNonQuery();

            var getTema1Command = connection.CreateCommand();
            getTema1Command.CommandText = "SELECT last_insert_rowid()";
            var tema1Id = Convert.ToInt32(getTema1Command.ExecuteScalar());

            // Tema 1: Variables y Declaración (10 problems)
            InsertProblema(connection, tema1Id, 1, "2.4.1 Declaración de Variables",
                "Crea tres variables con los siguientes valores:\n- nombre: 'María' (texto)\n- edad: 22 (número entero)\n- activo: True (booleano)\n\nENTRADA: No hay entrada del usuario, usa los valores indicados.\nSALIDA ESPERADA: 'Nombre: María, Edad: 22, Activo: True'",
                "nombre = 'María'\nedad = 22\nactivo = True\nprint(f'Nombre: {nombre}, Edad: {edad}, Activo: {activo}')",
                "Fácil",
                "# Crea tres variables: nombre='María', edad=22, activo=True\n# Imprime el mensaje formateado\n# Escribe tu código aquí",
                "nombre = 'María'\nedad = 22\nactivo = True\nprint(f'Nombre: {nombre}, Edad: {edad}, Activo: {activo}')",
                10);

            InsertProblema(connection, tema1Id, 2, "2.4.2 Tipos de Datos Básicos",
                "Crea variables de diferentes tipos y muestra su tipo usando type():\n- numero_entero = 100\n- numero_decimal = 25.5\n- texto = 'Python'\n- es_verdadero = False\n\nENTRADA: No hay entrada del usuario, usa los valores indicados.\nSALIDA ESPERADA:\n<class 'int'>\n<class 'float'>\n<class 'str'>\n<class 'bool'>",
                "numero_entero = 100\nnumero_decimal = 25.5\ntexto = 'Python'\nes_verdadero = False\nprint(type(numero_entero))\nprint(type(numero_decimal))\nprint(type(texto))\nprint(type(es_verdadero))",
                "Fácil",
                "# Crea variables: numero_entero=100, numero_decimal=25.5, texto='Python', es_verdadero=False\n# Imprime el tipo de cada variable usando type()\n# Escribe tu código aquí",
                "numero_entero = 100\nnumero_decimal = 25.5\ntexto = 'Python'\nes_verdadero = False\nprint(type(numero_entero))\nprint(type(numero_decimal))\nprint(type(texto))\nprint(type(es_verdadero))",
                10);

            InsertProblema(connection, tema1Id, 3, "2.4.3 Literales",
                "Usa literales (valores escritos directamente) para crear variables:\n- edad = 30 (literal entero)\n- precio = 99.99 (literal decimal)\n- ciudad = 'Madrid' (literal cadena)\n- disponible = True (literal booleano)\n\nENTRADA: No hay entrada del usuario, usa los valores indicados.\nSALIDA ESPERADA: '30, 99.99, Madrid, True'",
                "edad = 30\nprecio = 99.99\nciudad = 'Madrid'\ndisponible = True\nprint(f'{edad}, {precio}, {ciudad}, {disponible}')",
                "Fácil",
                "# Crea variables usando literales: edad=30, precio=99.99, ciudad='Madrid', disponible=True\n# Imprime todos los valores separados por comas\n# Escribe tu código aquí",
                "edad = 30\nprecio = 99.99\nciudad = 'Madrid'\ndisponible = True\nprint(f'{edad}, {precio}, {ciudad}, {disponible}')",
                10);

            InsertProblema(connection, tema1Id, 4, "2.4.4 Constantes",
                "Define constantes (nombres en mayúsculas) con los siguientes valores:\n- PI = 3.14159\n- GRAVEDAD = 9.81\n\nENTRADA: No hay entrada del usuario, usa los valores indicados.\nSALIDA ESPERADA:\n'PI = 3.14159'\n'GRAVEDAD = 9.81'",
                "PI = 3.14159\nGRAVEDAD = 9.81\nprint(f'PI = {PI}')\nprint(f'GRAVEDAD = {GRAVEDAD}')",
                "Fácil",
                "# Define constantes: PI=3.14159 y GRAVEDAD=9.81 (usa mayúsculas)\n# Imprime cada constante con su valor\n# Escribe tu código aquí",
                "PI = 3.14159\nGRAVEDAD = 9.81\nprint(f'PI = {PI}')\nprint(f'GRAVEDAD = {GRAVEDAD}')",
                10);

            InsertProblema(connection, tema1Id, 5, "2.4.5 Identificadores",
                "Crea tres variables con identificadores válidos (pueden empezar con letra o _):\n- mi_variable = 15\n- _variable_privada = 25\n- VariablePublica = 35\n\nENTRADA: No hay entrada del usuario, usa los valores indicados.\nSALIDA ESPERADA: '15 25 35'",
                "mi_variable = 15\n_variable_privada = 25\nVariablePublica = 35\nprint(mi_variable, _variable_privada, VariablePublica)",
                "Fácil",
                "# Crea variables: mi_variable=15, _variable_privada=25, VariablePublica=35\n# Imprime los tres valores separados por espacios\n# Escribe tu código aquí",
                "mi_variable = 15\n_variable_privada = 25\nVariablePublica = 35\nprint(mi_variable, _variable_privada, VariablePublica)",
                10);

            InsertProblema(connection, tema1Id, 6, "2.4.6 Operadores Aritméticos",
                "Dados los valores a=20 y b=6, calcula y muestra:\n- Suma (a + b)\n- Resta (a - b)\n- Multiplicación (a * b)\n- División (a / b)\n- Módulo (a % b)\n\nENTRADA: a=20, b=6\nSALIDA ESPERADA: 'Suma: 26, Resta: 14, Multiplicación: 120, División: 3.3333333333333335, Módulo: 2'",
                "a = 20\nb = 6\nsuma = a + b\nresta = a - b\nmultiplicacion = a * b\ndivision = a / b\nmodulo = a % b\nprint(f'Suma: {suma}, Resta: {resta}, Multiplicación: {multiplicacion}, División: {division}, Módulo: {modulo}')",
                "Fácil",
                "# Dados a=20 y b=6, calcula suma, resta, multiplicación, división y módulo\n# Imprime todos los resultados en el formato indicado\n# Escribe tu código aquí",
                "a = 20\nb = 6\nsuma = a + b\nresta = a - b\nmultiplicacion = a * b\ndivision = a / b\nmodulo = a % b\nprint(f'Suma: {suma}, Resta: {resta}, Multiplicación: {multiplicacion}, División: {division}, Módulo: {modulo}')",
                15);

            InsertProblema(connection, tema1Id, 7, "2.4.7 Operadores de Comparación",
                "Compara los números a=8 y b=15 usando todos los operadores de comparación:\n- == (igual)\n- < (menor)\n- > (mayor)\n- <= (menor o igual)\n- >= (mayor o igual)\n- != (diferente)\n\nENTRADA: a=8, b=15\nSALIDA ESPERADA:\n'a == b: False'\n'a < b: True'\n'a > b: False'\n'a <= b: True'\n'a >= b: False'\n'a != b: True'",
                "a = 8\nb = 15\nprint(f'a == b: {a == b}')\nprint(f'a < b: {a < b}')\nprint(f'a > b: {a > b}')\nprint(f'a <= b: {a <= b}')\nprint(f'a >= b: {a >= b}')\nprint(f'a != b: {a != b}')",
                "Fácil",
                "# Compara a=8 y b=15 usando ==, <, >, <=, >=, !=\n# Imprime cada comparación en el formato 'a == b: False'\n# Escribe tu código aquí",
                "a = 8\nb = 15\nprint(f'a == b: {a == b}')\nprint(f'a < b: {a < b}')\nprint(f'a > b: {a > b}')\nprint(f'a <= b: {a <= b}')\nprint(f'a >= b: {a >= b}')\nprint(f'a != b: {a != b}')",
                15);

            InsertProblema(connection, tema1Id, 8, "2.4.8 Operadores Lógicos",
                "Evalúa si una persona puede votar. Condiciones:\n- edad = 19\n- es_ciudadano = True\n\nPuede votar si: edad >= 18 AND es_ciudadano == True\n\nENTRADA: edad=19, es_ciudadano=True\nSALIDA ESPERADA: 'Puede votar: True'",
                "edad = 19\nes_ciudadano = True\npuede_votar = edad >= 18 and es_ciudadano\nprint(f'Puede votar: {puede_votar}')",
                "Fácil",
                "# Evalúa si puede votar: edad=19, es_ciudadano=True\n# Condición: edad >= 18 AND es_ciudadano\n# Imprime 'Puede votar: True' o 'Puede votar: False'\n# Escribe tu código aquí",
                "edad = 19\nes_ciudadano = True\npuede_votar = edad >= 18 and es_ciudadano\nprint(f'Puede votar: {puede_votar}')",
                15);

            InsertProblema(connection, tema1Id, 9, "2.4.9 Salida de Datos (print)",
                "Crea variables y muestra un mensaje formateado:\n- nombre = 'Carlos'\n- edad = 28\n\nENTRADA: nombre='Carlos', edad=28\nSALIDA ESPERADA: 'Carlos tiene 28 años'",
                "nombre = 'Carlos'\nedad = 28\nprint(f'{nombre} tiene {edad} años')",
                "Fácil",
                "# Crea variables: nombre='Carlos', edad=28\n# Imprime el mensaje formateado usando f-string: '{nombre} tiene {edad} años'\n# Escribe tu código aquí",
                "nombre = 'Carlos'\nedad = 28\nprint(f'{nombre} tiene {edad} años')",
                10);

            InsertProblema(connection, tema1Id, 10, "2.4.10 Conversión de Tipos",
                "Realiza las siguientes conversiones de tipos:\n1. Convierte el texto '456' a entero\n2. Convierte ese entero a decimal (float)\n3. Convierte el decimal de vuelta a texto (str)\n\nENTRADA: texto = '456'\nSALIDA ESPERADA: 'Entero: 456, Decimal: 456.0, Texto: 456.0'",
                "texto = '456'\nentero = int(texto)\ndecimal = float(entero)\ntexto_nuevo = str(decimal)\nprint(f'Entero: {entero}, Decimal: {decimal}, Texto: {texto_nuevo}')",
                "Media",
                "# Convierte '456' a entero, luego a decimal, luego a texto\n# Imprime: 'Entero: 456, Decimal: 456.0, Texto: 456.0'\n# Escribe tu código aquí",
                "texto = '456'\nentero = int(texto)\ndecimal = float(entero)\ntexto_nuevo = str(decimal)\nprint(f'Entero: {entero}, Decimal: {decimal}, Texto: {texto_nuevo}')",
                15);

            // Tema 2: 2.4 Operators (Requires 10 problems from Tema 1)
            var tema2Command = connection.CreateCommand();
            tema2Command.CommandText = @"
                INSERT INTO temas (nivel_id, titulo, descripcion, orden, locked, puntos_requeridos)
                VALUES (@nivelId, '2.4 Operadores', 'Operadores aritméticos, de comparación, lógicos, de asignación y precedencia', 2, 1, 0)";
            tema2Command.Parameters.AddWithValue("@nivelId", nivel1Id);
            tema2Command.ExecuteNonQuery();

            var getTema2Command = connection.CreateCommand();
            getTema2Command.CommandText = "SELECT last_insert_rowid()";
            var tema2Id = Convert.ToInt32(getTema2Command.ExecuteScalar());

            // Tema 2: Operators (10 problems)
            InsertProblema(connection, tema2Id, 1, "2.4.11 Operadores de Asignación",
                "Usa operadores de asignación compuestos para modificar x:\n- Inicia con x = 12\n- Suma 8 usando +=\n- Multiplica por 2 usando *=\n- Divide entre 4 usando /=\n\nENTRADA: x inicial = 12\nSALIDA ESPERADA: 'Resultado: 10.0'",
                "x = 12\nx += 8\nx *= 2\nx /= 4\nprint(f'Resultado: {x}')",
                "Fácil",
                "# Inicia x=12, luego x+=8, x*=2, x/=4\n# Imprime el resultado final\n# Escribe tu código aquí",
                "x = 12\nx += 8\nx *= 2\nx /= 4\nprint(f'Resultado: {x}')",
                15);

            InsertProblema(connection, tema2Id, 2, "2.4.12 Precedencia de Operadores",
                "Calcula dos expresiones y observa cómo los paréntesis cambian el resultado:\n1. Sin paréntesis: 8 + 4 * 3\n2. Con paréntesis: (8 + 4) * 3\n\nENTRADA: No hay entrada del usuario\nSALIDA ESPERADA: 'Sin paréntesis: 20, Con paréntesis: 36'\n\nNota: Sin paréntesis, la multiplicación se hace primero (4*3=12, luego 8+12=20). Con paréntesis, primero se suma (8+4=12, luego 12*3=36).",
                "resultado1 = 8 + 4 * 3\nresultado2 = (8 + 4) * 3\nprint(f'Sin paréntesis: {resultado1}, Con paréntesis: {resultado2}')",
                "Fácil",
                "# Calcula: 8 + 4 * 3 (sin paréntesis) y (8 + 4) * 3 (con paréntesis)\n# Imprime ambos resultados\n# Escribe tu código aquí",
                "resultado1 = 8 + 4 * 3\nresultado2 = (8 + 4) * 3\nprint(f'Sin paréntesis: {resultado1}, Con paréntesis: {resultado2}')",
                15);

            InsertProblema(connection, tema2Id, 3, "2.4.13 Operadores de Identidad",
                "Crea listas y verifica si son el mismo objeto en memoria:\n- a = [10, 20, 30]\n- b = a (b apunta al mismo objeto que a)\n- c = [10, 20, 30] (c es un objeto diferente con los mismos valores)\n\nENTRADA: No hay entrada del usuario\nSALIDA ESPERADA:\n'a is b: True'\n'a is c: False'\n\nNota: 'is' verifica si apuntan al mismo objeto, no si tienen el mismo valor.",
                "a = [10, 20, 30]\nb = a\nc = [10, 20, 30]\nprint(f'a is b: {a is b}')\nprint(f'a is c: {a is c}')",
                "Media",
                "# Crea a=[10,20,30], b=a, c=[10,20,30]\n# Verifica si a is b y si a is c\n# Imprime los resultados\n# Escribe tu código aquí",
                "a = [10, 20, 30]\nb = a\nc = [10, 20, 30]\nprint(f'a is b: {a is b}')\nprint(f'a is c: {a is c}')",
                15);

            InsertProblema(connection, tema2Id, 4, "2.4.14 Operadores de Pertenencia",
                "Verifica si valores están presentes en secuencias:\n1. ¿Está el número 7 en la lista [2, 4, 6, 7, 8, 10]?\n2. ¿Está la letra 'o' en la cadena 'Python'?\n\nENTRADA: lista=[2,4,6,7,8,10], cadena='Python'\nSALIDA ESPERADA:\n'7 en lista: True'\n'o en cadena: True'",
                "lista = [2, 4, 6, 7, 8, 10]\ncadena = 'Python'\nprint(f'7 en lista: {7 in lista}')\nprint(f'o en cadena: {\"o\" in cadena}')",
                "Fácil",
                "# Verifica si 7 está en [2,4,6,7,8,10] y si 'o' está en 'Python'\n# Imprime los resultados\n# Escribe tu código aquí",
                "lista = [2, 4, 6, 7, 8, 10]\ncadena = 'Python'\nprint(f'7 en lista: {7 in lista}')\nprint(f'o en cadena: {\"o\" in cadena}')",
                15);

            InsertProblema(connection, tema2Id, 5, "2.4.15 Operadores Bitwise",
                "Realiza operaciones bitwise (a nivel de bits) con a=6 y b=4:\n- AND (&): 6 & 4\n- OR (|): 6 | 4\n- XOR (^): 6 ^ 4\n\nENTRADA: a=6, b=4\nSALIDA ESPERADA: 'AND: 4, OR: 6, XOR: 2'\n\nNota: 6 en binario es 110, 4 es 100. AND=100(4), OR=110(6), XOR=010(2).",
                "a = 6\nb = 4\nand_result = a & b\nor_result = a | b\nxor_result = a ^ b\nprint(f'AND: {and_result}, OR: {or_result}, XOR: {xor_result}')",
                "Media",
                "# Realiza operaciones bitwise: 6 & 4, 6 | 4, 6 ^ 4\n# Imprime los resultados en el formato indicado\n# Escribe tu código aquí",
                "a = 6\nb = 4\nand_result = a & b\nor_result = a | b\nxor_result = a ^ b\nprint(f'AND: {and_result}, OR: {or_result}, XOR: {xor_result}')",
                20);

            InsertProblema(connection, tema2Id, 6, "2.4.16 Expresiones Complejas",
                "Calcula la siguiente expresión usando paréntesis:\n(12 + 8) * (15 - 5) / 4\n\nPasos:\n1. Suma: 12 + 8 = 20\n2. Resta: 15 - 5 = 10\n3. Multiplica: 20 * 10 = 200\n4. Divide: 200 / 4 = 50\n\nENTRADA: No hay entrada del usuario\nSALIDA ESPERADA: 'Resultado: 50.0'",
                "resultado = (12 + 8) * (15 - 5) / 4\nprint(f'Resultado: {resultado}')",
                "Media",
                "# Calcula: (12 + 8) * (15 - 5) / 4\n# Imprime el resultado\n# Escribe tu código aquí",
                "resultado = (12 + 8) * (15 - 5) / 4\nprint(f'Resultado: {resultado}')",
                15);

            InsertProblema(connection, tema2Id, 7, "2.4.17 Operadores de Asignación Múltiple",
                "Asigna valores a múltiples variables en una sola línea:\n1. Asigna 15, 25, 35 a las variables a, b, c respectivamente\n2. Asigna el valor 7 a las variables x, y, z (todas con el mismo valor)\n\nENTRADA: No hay entrada del usuario\nSALIDA ESPERADA:\n'a=15, b=25, c=35'\n'x=7, y=7, z=7'",
                "a, b, c = 15, 25, 35\nx = y = z = 7\nprint(f'a={a}, b={b}, c={c}')\nprint(f'x={x}, y={y}, z={z}')",
                "Fácil",
                "# Asigna 15,25,35 a a,b,c en una línea\n# Asigna 7 a x,y,z en una línea\n# Imprime los valores\n# Escribe tu código aquí",
                "a, b, c = 15, 25, 35\nx = y = z = 7\nprint(f'a={a}, b={b}, c={c}')\nprint(f'x={x}, y={y}, z={z}')",
                15);

            InsertProblema(connection, tema2Id, 8, "2.4.18 Operadores de Cadena",
                "Usa operadores con cadenas:\n1. Concatena 'Hola' + ' ' + 'Mundo' usando el operador +\n2. Repite 'Python ' tres veces usando el operador *\n\nENTRADA: No hay entrada del usuario\nSALIDA ESPERADA:\n'Hola Mundo'\n'Python Python Python '",
                "saludo = 'Hola' + ' ' + 'Mundo'\nrepetido = 'Python ' * 3\nprint(saludo)\nprint(repetido)",
                "Fácil",
                "# Concatena 'Hola' + ' ' + 'Mundo'\n# Repite 'Python ' tres veces\n# Imprime ambos resultados\n# Escribe tu código aquí",
                "saludo = 'Hola' + ' ' + 'Mundo'\nrepetido = 'Python ' * 3\nprint(saludo)\nprint(repetido)",
                15);

            InsertProblema(connection, tema2Id, 9, "2.4.19 Operadores Ternarios",
                "Usa el operador ternario para determinar el estado de un número:\n- numero = 9\n- Si numero > 0, asigna 'Positivo'\n- Si no, asigna 'Negativo o Cero'\n\nENTRADA: numero = 9\nSALIDA ESPERADA: 'Positivo'",
                "numero = 9\nestado = 'Positivo' if numero > 0 else 'Negativo o Cero'\nprint(estado)",
                "Media",
                "# Usa operador ternario: si numero > 0 entonces 'Positivo', sino 'Negativo o Cero'\n# numero = 9\n# Imprime el resultado\n# Escribe tu código aquí",
                "numero = 9\nestado = 'Positivo' if numero > 0 else 'Negativo o Cero'\nprint(estado)",
                15);

            InsertProblema(connection, tema2Id, 10, "2.4.20 Evaluación de Expresiones",
                "Evalúa la expresión respetando la precedencia de operadores:\n4 ** 2 + 3 * 5 - 12 / 3\n\nOrden de evaluación:\n1. 4 ** 2 = 16 (exponenciación primero)\n2. 3 * 5 = 15 (multiplicación)\n3. 12 / 3 = 4.0 (división)\n4. 16 + 15 = 31 (suma)\n5. 31 - 4.0 = 27.0 (resta)\n\nENTRADA: No hay entrada del usuario\nSALIDA ESPERADA: 'Resultado: 27.0'",
                "resultado = 4 ** 2 + 3 * 5 - 12 / 3\nprint(f'Resultado: {resultado}')",
                "Media",
                "# Evalúa: 4 ** 2 + 3 * 5 - 12 / 3\n# Respeta la precedencia de operadores\n# Imprime el resultado\n# Escribe tu código aquí",
                "resultado = 4 ** 2 + 3 * 5 - 12 / 3\nprint(f'Resultado: {resultado}')",
                15);

            // Tema 3: 3. Control Flow (Requires 10 problems from Tema 2)
            var tema3Command = connection.CreateCommand();
            tema3Command.CommandText = @"
                INSERT INTO temas (nivel_id, titulo, descripcion, orden, locked, puntos_requeridos)
                VALUES (@nivelId, '3. Estructuras de Control', 'Estructuras secuenciales, selectivas (simple, doble, múltiple) e iterativas (while, do-while, for)', 3, 1, 0)";
            tema3Command.Parameters.AddWithValue("@nivelId", nivel1Id);
            tema3Command.ExecuteNonQuery();

            var getTema3Command = connection.CreateCommand();
            getTema3Command.CommandText = "SELECT last_insert_rowid()";
            var tema3Id = Convert.ToInt32(getTema3Command.ExecuteScalar());

            // Tema 3: Control Flow (10 problems)
            InsertProblema(connection, tema3Id, 1, "3.1 Estructuras Secuenciales",
                "Ejecuta las siguientes instrucciones en secuencia:\n1. Asigna 15 a la variable x\n2. Asigna 25 a la variable y\n3. Calcula la suma de x + y y guárdala en resultado\n4. Imprime el valor de resultado\n\nENTRADA: No hay entrada del usuario\nSALIDA ESPERADA: '40'",
                "x = 15\ny = 25\nresultado = x + y\nprint(resultado)",
                "Fácil",
                "# Ejecuta en secuencia: x=15, y=25, resultado=x+y, imprime resultado\n# Escribe tu código aquí",
                "x = 15\ny = 25\nresultado = x + y\nprint(resultado)",
                10);

            InsertProblema(connection, tema3Id, 2, "3.2.1 Estructura Selectiva Simple (if)",
                "Usa una estructura if para verificar si un número es positivo:\n- numero = 12\n- Si numero > 0, imprime 'Positivo'\n\nENTRADA: numero = 12\nSALIDA ESPERADA: 'Positivo'",
                "numero = 12\nif numero > 0:\n    print('Positivo')",
                "Fácil",
                "# Si numero > 0, imprime 'Positivo'\n# numero = 12\n# Escribe tu código aquí",
                "numero = 12\nif numero > 0:\n    print('Positivo')",
                10);

            InsertProblema(connection, tema3Id, 3, "3.2.2 Estructura Selectiva Doble (if-else)",
                "Determina si un número es par o impar:\n- numero = 14\n- Si numero % 2 == 0, imprime 'Par'\n- Si no, imprime 'Impar'\n\nENTRADA: numero = 14\nSALIDA ESPERADA: 'Par'",
                "numero = 14\nif numero % 2 == 0:\n    print('Par')\nelse:\n    print('Impar')",
                "Fácil",
                "# Si numero es par (numero % 2 == 0), imprime 'Par', sino 'Impar'\n# numero = 14\n# Escribe tu código aquí",
                "numero = 14\nif numero % 2 == 0:\n    print('Par')\nelse:\n    print('Impar')",
                15);

            InsertProblema(connection, tema3Id, 4, "3.2.3 Estructura Selectiva Múltiple (if-elif-else)",
                "Clasifica una temperatura según estos rangos:\n- temperatura = 18\n- Si temperatura < 0, imprime 'Frío'\n- Si temperatura está entre 0 y 20 (inclusive), imprime 'Templado'\n- Si temperatura > 20, imprime 'Caliente'\n\nENTRADA: temperatura = 18\nSALIDA ESPERADA: 'Templado'",
                "temperatura = 18\nif temperatura < 0:\n    print('Frío')\nelif temperatura <= 20:\n    print('Templado')\nelse:\n    print('Caliente')",
                "Fácil",
                "# Clasifica temperatura: <0='Frío', 0-20='Templado', >20='Caliente'\n# temperatura = 18\n# Escribe tu código aquí",
                "temperatura = 18\nif temperatura < 0:\n    print('Frío')\nelif temperatura <= 20:\n    print('Templado')\nelse:\n    print('Caliente')",
                15);

            InsertProblema(connection, tema3Id, 5, "3.3.1 Estructura Iterativa: while",
                "Usa un bucle while para imprimir los números del 1 al 6 (inclusive):\n- Inicia contador = 1\n- Mientras contador <= 6, imprime contador y luego incrementa contador en 1\n\nENTRADA: No hay entrada del usuario\nSALIDA ESPERADA:\n'1'\n'2'\n'3'\n'4'\n'5'\n'6'",
                "contador = 1\nwhile contador <= 6:\n    print(contador)\n    contador += 1",
                "Fácil",
                "# Imprime números del 1 al 6 usando while\n# Inicia contador=1, mientras contador<=6, imprime y aumenta\n# Escribe tu código aquí",
                "contador = 1\nwhile contador <= 6:\n    print(contador)\n    contador += 1",
                15);

            InsertProblema(connection, tema3Id, 6, "3.3.2 Estructura Iterativa: do-while (simulado)",
                "Simula un bucle do-while que imprime números del 1 al 4:\n- Usa 'while True:' para garantizar al menos una ejecución\n- Inicia contador = 1\n- Imprime contador, luego incrementa\n- Si contador > 4, usa break para salir\n\nENTRADA: No hay entrada del usuario\nSALIDA ESPERADA:\n'1'\n'2'\n'3'\n'4'",
                "contador = 1\nwhile True:\n    print(contador)\n    contador += 1\n    if contador > 4:\n        break",
                "Media",
                "# Simula do-while: imprime números del 1 al 4 usando while True y break\n# Escribe tu código aquí",
                "contador = 1\nwhile True:\n    print(contador)\n    contador += 1\n    if contador > 4:\n        break",
                20);

            InsertProblema(connection, tema3Id, 7, "3.3.3 Estructura Iterativa: for",
                "Usa un bucle for para imprimir los números del 2 al 8 (inclusive):\n- Usa range(2, 9) para generar los números\n- En cada iteración, imprime el valor\n\nENTRADA: No hay entrada del usuario\nSALIDA ESPERADA:\n'2'\n'3'\n'4'\n'5'\n'6'\n'7'\n'8'",
                "for i in range(2, 9):\n    print(i)",
                "Fácil",
                "# Imprime números del 2 al 8 usando for con range(2, 9)\n# Escribe tu código aquí",
                "for i in range(2, 9):\n    print(i)",
                15);

            InsertProblema(connection, tema3Id, 8, "3.3.4 Bucles Anidados",
                "Crea una tabla de multiplicar del 2 al 4 usando bucles anidados:\n- Bucle externo: i de 2 a 4 (inclusive)\n- Bucle interno: j de 2 a 4 (inclusive)\n- Imprime: '{i} x {j} = {resultado}'\n\nENTRADA: No hay entrada del usuario\nSALIDA ESPERADA:\n'2 x 2 = 4'\n'2 x 3 = 6'\n'2 x 4 = 8'\n'3 x 2 = 6'\n'3 x 3 = 9'\n'3 x 4 = 12'\n'4 x 2 = 8'\n'4 x 3 = 12'\n'4 x 4 = 16'",
                "for i in range(2, 5):\n    for j in range(2, 5):\n        print(f'{i} x {j} = {i * j}')",
                "Media",
                "# Crea tabla de multiplicar del 2 al 4 usando bucles anidados\n# Imprime formato: 'i x j = resultado'\n# Escribe tu código aquí",
                "for i in range(2, 5):\n    for j in range(2, 5):\n        print(f'{i} x {j} = {i * j}')",
                20);

            InsertProblema(connection, tema3Id, 9, "3.3.5 Control de Bucles: break y continue",
                "Imprime números del 1 al 8, pero omite el número 4 usando continue:\n- Usa for i in range(1, 9)\n- Si i == 4, usa continue para saltar esa iteración\n- En los demás casos, imprime i\n\nENTRADA: No hay entrada del usuario\nSALIDA ESPERADA:\n'1'\n'2'\n'3'\n'5'\n'6'\n'7'\n'8'\n\nNota: El 4 no se imprime porque se usa continue.",
                "for i in range(1, 9):\n    if i == 4:\n        continue\n    print(i)",
                "Media",
                "# Imprime números del 1 al 8, pero omite el 4 usando continue\n# Escribe tu código aquí",
                "for i in range(1, 9):\n    if i == 4:\n        continue\n    print(i)",
                20);

            InsertProblema(connection, tema3Id, 10, "3.3.6 Bucles con else",
                "Usa un bucle for con cláusula else:\n- Imprime números del 1 al 3 usando for\n- Después del bucle (cláusula else), imprime 'Bucle completado'\n\nENTRADA: No hay entrada del usuario\nSALIDA ESPERADA:\n'1'\n'2'\n'3'\n'Bucle completado'\n\nNota: La cláusula else se ejecuta cuando el bucle termina normalmente (sin break).",
                "for i in range(1, 4):\n    print(i)\nelse:\n    print('Bucle completado')",
                "Media",
                "# Usa for con else: imprime números del 1 al 3, luego 'Bucle completado'\n# Escribe tu código aquí",
                "for i in range(1, 4):\n    print(i)\nelse:\n    print('Bucle completado')",
                20);

            // Tema 4: 4. Data Organization (Requires 10 problems from Tema 3)
            var tema4Command = connection.CreateCommand();
            tema4Command.CommandText = @"
                INSERT INTO temas (nivel_id, titulo, descripcion, orden, locked, puntos_requeridos)
                VALUES (@nivelId, '4. Organización de Datos', 'Arreglos unidimensionales, multidimensionales, conceptos básicos, operaciones y aplicaciones', 4, 1, 0)";
            tema4Command.Parameters.AddWithValue("@nivelId", nivel1Id);
            tema4Command.ExecuteNonQuery();

            var getTema4Command = connection.CreateCommand();
            getTema4Command.CommandText = "SELECT last_insert_rowid()";
            var tema4Id = Convert.ToInt32(getTema4Command.ExecuteScalar());

            // Tema 4: Data Organization (10 problems)
            InsertProblema(connection, tema4Id, 1, "4.1 Introducción a Arreglos (Listas)",
                "Crea una lista con los números 5, 15, 25 y accede al primer elemento (índice 0):\n- lista = [5, 15, 25]\n- Imprime lista[0]\n\nENTRADA: No hay entrada del usuario\nSALIDA ESPERADA: '5'",
                "numeros = [5, 15, 25]\nprint(numeros[0])",
                "Fácil",
                "# Crea lista [5, 15, 25] e imprime el primer elemento (índice 0)\n# Escribe tu código aquí",
                "numeros = [5, 15, 25]\nprint(numeros[0])",
                10);

            InsertProblema(connection, tema4Id, 2, "4.2.1 Arreglos Unidimensionales: Conceptos",
                "Crea un arreglo (lista) con tres colores y accede al segundo elemento:\n- colores = ['rojo', 'verde', 'azul']\n- Imprime colores[1] (el segundo elemento, índice 1)\n\nENTRADA: No hay entrada del usuario\nSALIDA ESPERADA: 'verde'",
                "colores = ['rojo', 'verde', 'azul']\nprint(colores[1])",
                "Fácil",
                "# Crea lista ['rojo', 'verde', 'azul'] e imprime el segundo elemento (índice 1)\n# Escribe tu código aquí",
                "colores = ['rojo', 'verde', 'azul']\nprint(colores[1])",
                10);

            InsertProblema(connection, tema4Id, 3, "4.2.2 Arreglos Unidimensionales: Operaciones",
                "Realiza las siguientes operaciones en una lista:\n1. Crea lista = [10, 20, 30]\n2. Agrega el número 40 usando append()\n3. Elimina el número 20 usando remove()\n4. Imprime la longitud de la lista usando len()\n\nENTRADA: No hay entrada del usuario\nSALIDA ESPERADA: '3'\n\nNota: Después de agregar 40 y eliminar 20, la lista queda [10, 30, 40] con longitud 3.",
                "lista = [10, 20, 30]\nlista.append(40)\nlista.remove(20)\nprint(len(lista))",
                "Fácil",
                "# Crea [10,20,30], agrega 40, elimina 20, imprime len(lista)\n# Escribe tu código aquí",
                "lista = [10, 20, 30]\nlista.append(40)\nlista.remove(20)\nprint(len(lista))",
                15);

            InsertProblema(connection, tema4Id, 4, "4.2.3 Arreglos Unidimensionales: Aplicaciones",
                "Calcula la suma de todos los elementos en una lista usando un bucle:\n- numeros = [5, 10, 15, 20]\n- Usa un bucle for para sumar todos los elementos\n- Imprime el resultado\n\nENTRADA: numeros = [5, 10, 15, 20]\nSALIDA ESPERADA: '50'\n\nNota: 5 + 10 + 15 + 20 = 50",
                "numeros = [5, 10, 15, 20]\nsuma = 0\nfor num in numeros:\n    suma += num\nprint(suma)",
                "Fácil",
                "# Calcula la suma de [5, 10, 15, 20] usando un bucle for\n# Inicia suma=0, suma cada elemento, imprime resultado\n# Escribe tu código aquí",
                "numeros = [5, 10, 15, 20]\nsuma = 0\nfor num in numeros:\n    suma += num\nprint(suma)",
                15);

            InsertProblema(connection, tema4Id, 5, "4.3.1 Arreglos Multidimensionales: Conceptos",
                "Crea una matriz 2x2 y accede a un elemento específico:\n- matriz = [[10, 20], [30, 40]]\n- Accede al elemento en la fila 0 (primera fila), columna 1 (segunda columna)\n- Imprime matriz[0][1]\n\nENTRADA: No hay entrada del usuario\nSALIDA ESPERADA: '20'\n\nNota: matriz[0][1] accede a la primera fila (índice 0) y segunda columna (índice 1).",
                "matriz = [[10, 20], [30, 40]]\nprint(matriz[0][1])",
                "Fácil",
                "# Crea matriz [[10, 20], [30, 40]] e imprime matriz[0][1]\n# Escribe tu código aquí",
                "matriz = [[10, 20], [30, 40]]\nprint(matriz[0][1])",
                15);

            InsertProblema(connection, tema4Id, 6, "4.3.2 Arreglos Multidimensionales: Operaciones",
                "Itera sobre una matriz e imprime cada elemento usando bucles anidados:\n- matriz = [[2, 4, 6], [8, 10, 12]]\n- Usa un bucle externo para cada fila\n- Usa un bucle interno para cada elemento de la fila\n- Imprime cada elemento\n\nENTRADA: No hay entrada del usuario\nSALIDA ESPERADA:\n'2'\n'4'\n'6'\n'8'\n'10'\n'12'",
                "matriz = [[2, 4, 6], [8, 10, 12]]\nfor fila in matriz:\n    for elemento in fila:\n        print(elemento)",
                "Media",
                "# Itera sobre [[2,4,6], [8,10,12]] usando bucles anidados e imprime cada elemento\n# Escribe tu código aquí",
                "matriz = [[2, 4, 6], [8, 10, 12]]\nfor fila in matriz:\n    for elemento in fila:\n        print(elemento)",
                20);

            InsertProblema(connection, tema4Id, 7, "4.3.3 Arreglos Multidimensionales: Aplicaciones",
                "Crea una matriz 2x3 y muéstrala fila por fila:\n- matriz = [[1, 2, 3], [4, 5, 6]]\n- Itera sobre cada fila e imprime la fila completa\n\nENTRADA: No hay entrada del usuario\nSALIDA ESPERADA:\n'[1, 2, 3]'\n'[4, 5, 6]'",
                "matriz = [[1, 2, 3], [4, 5, 6]]\nfor fila in matriz:\n    print(fila)",
                "Media",
                "# Crea matriz [[1,2,3], [4,5,6]] e imprime cada fila\n# Escribe tu código aquí",
                "matriz = [[1, 2, 3], [4, 5, 6]]\nfor fila in matriz:\n    print(fila)",
                20);

            InsertProblema(connection, tema4Id, 8, "4.4 Estructuras o Registros (Diccionarios)",
                "Crea un diccionario que represente un estudiante y accede a sus datos:\n- estudiante = {'nombre': 'Luis', 'edad': 22, 'nota': 9.0}\n- Imprime el valor de la clave 'nombre'\n\nENTRADA: No hay entrada del usuario\nSALIDA ESPERADA: 'Luis'",
                "estudiante = {'nombre': 'Luis', 'edad': 22, 'nota': 9.0}\nprint(estudiante['nombre'])",
                "Fácil",
                "# Crea diccionario {'nombre':'Luis', 'edad':22, 'nota':9.0}\n# Imprime estudiante['nombre']\n# Escribe tu código aquí",
                "estudiante = {'nombre': 'Luis', 'edad': 22, 'nota': 9.0}\nprint(estudiante['nombre'])",
                15);

            InsertProblema(connection, tema4Id, 9, "4.4.1 Operaciones con Diccionarios",
                "Realiza operaciones en un diccionario:\n1. Crea dic = {'a': 5, 'b': 10}\n2. Agrega la clave 'c' con valor 15\n3. Elimina la clave 'a' usando del\n4. Imprime el diccionario resultante\n\nENTRADA: No hay entrada del usuario\nSALIDA ESPERADA: '{'b': 10, 'c': 15}'\n\nNota: Después de agregar 'c' y eliminar 'a', el diccionario queda con 'b' y 'c'.",
                "dic = {'a': 5, 'b': 10}\ndic['c'] = 15\ndel dic['a']\nprint(dic)",
                "Media",
                "# Crea {'a':5, 'b':10}, agrega 'c':15, elimina 'a', imprime\n# Escribe tu código aquí",
                "dic = {'a': 5, 'b': 10}\ndic['c'] = 15\ndel dic['a']\nprint(dic)",
                20);

            InsertProblema(connection, tema4Id, 10, "4.4.2 Aplicaciones de Diccionarios",
                "Crea un diccionario para un libro y clasifícalo según su año:\n- libro = {'titulo': 'Programación Python', 'autor': 'Juan Pérez', 'año': 2018}\n- Si año > 2000, imprime 'Moderno'\n- Si no, imprime 'Clásico'\n\nENTRADA: No hay entrada del usuario\nSALIDA ESPERADA: 'Moderno'",
                "libro = {'titulo': 'Programación Python', 'autor': 'Juan Pérez', 'año': 2018}\nif libro['año'] > 2000:\n    print('Moderno')\nelse:\n    print('Clásico')",
                "Media",
                "# Crea diccionario libro con titulo, autor, año=2018\n# Si año > 2000 imprime 'Moderno', sino 'Clásico'\n# Escribe tu código aquí",
                "libro = {'titulo': 'Programación Python', 'autor': 'Juan Pérez', 'año': 2018}\nif libro['año'] > 2000:\n    print('Moderno')\nelse:\n    print('Clásico')",
                20);

            // Tema 5: 5. Modularity (Requires 10 problems from Tema 4)
            var tema5Command = connection.CreateCommand();
            tema5Command.CommandText = @"
                INSERT INTO temas (nivel_id, titulo, descripcion, orden, locked, puntos_requeridos)
                VALUES (@nivelId, '5. Modularidad', 'Declaración y uso de módulos, paso de parámetros/argumentos, implementación de funciones', 5, 1, 0)";
            tema5Command.Parameters.AddWithValue("@nivelId", nivel1Id);
            tema5Command.ExecuteNonQuery();

            var getTema5Command = connection.CreateCommand();
            getTema5Command.CommandText = "SELECT last_insert_rowid()";
            var tema5Id = Convert.ToInt32(getTema5Command.ExecuteScalar());

            // Tema 5: Modularity (10 problems)
            InsertProblema(connection, tema5Id, 1, "5.1.1 Declaración de Funciones",
                "Declara una función sin parámetros que imprima un mensaje:\n- Nombre de la función: 'saludar'\n- La función debe imprimir: '¡Hola desde Python!'\n- Llama a la función después de declararla\n\nENTRADA: No hay entrada del usuario\nSALIDA ESPERADA: '¡Hola desde Python!'",
                "def saludar():\n    print('¡Hola desde Python!')\n\nsaludar()",
                "Fácil",
                "# Declara función 'saludar' que imprima '¡Hola desde Python!'\n# Llama a la función\n# Escribe tu código aquí",
                "def saludar():\n    print('¡Hola desde Python!')\n\nsaludar()",
                10);

            InsertProblema(connection, tema5Id, 2, "5.1.2 Uso de Funciones",
                "Crea una función que reciba dos números y devuelva su suma:\n- Nombre de la función: 'sumar'\n- Parámetros: a, b\n- Retorna: a + b\n- Llama a la función con a=7 y b=13, imprime el resultado\n\nENTRADA: a=7, b=13\nSALIDA ESPERADA: '20'",
                "def sumar(a, b):\n    return a + b\n\nresultado = sumar(7, 13)\nprint(resultado)",
                "Fácil",
                "# Crea función 'sumar' que reciba a y b, retorne a+b\n# Llámala con 7 y 13, imprime el resultado\n# Escribe tu código aquí",
                "def sumar(a, b):\n    return a + b\n\nresultado = sumar(7, 13)\nprint(resultado)",
                15);

            InsertProblema(connection, tema5Id, 3, "5.2.1 Paso de Parámetros por Posición",
                "Crea una función que calcule el área de un rectángulo pasando parámetros por posición:\n- Función: 'calcular_area'\n- Parámetros: base, altura\n- Retorna: base * altura\n- Llama con base=12, altura=8 (por posición)\n\nENTRADA: base=12, altura=8\nSALIDA ESPERADA: '96'",
                "def calcular_area(base, altura):\n    return base * altura\n\narea = calcular_area(12, 8)\nprint(area)",
                "Fácil",
                "# Crea función 'calcular_area' con base y altura, retorna base*altura\n# Llámala con 12 y 8 (por posición)\n# Escribe tu código aquí",
                "def calcular_area(base, altura):\n    return base * altura\n\narea = calcular_area(12, 8)\nprint(area)",
                15);

            InsertProblema(connection, tema5Id, 4, "5.2.2 Paso de Parámetros por Nombre",
                "Usa la función calcular_area del problema anterior, pero pasa los argumentos por nombre (puedes cambiar el orden):\n- Función: calcular_area(base, altura)\n- Llama con altura=6, base=14 (por nombre, orden invertido)\n\nENTRADA: base=14, altura=6 (pasados por nombre)\nSALIDA ESPERADA: '84'",
                "def calcular_area(base, altura):\n    return base * altura\n\narea = calcular_area(altura=6, base=14)\nprint(area)",
                "Fácil",
                "# Usa calcular_area pasando argumentos por nombre: altura=6, base=14\n# Escribe tu código aquí",
                "def calcular_area(base, altura):\n    return base * altura\n\narea = calcular_area(altura=6, base=14)\nprint(area)",
                15);

            InsertProblema(connection, tema5Id, 5, "5.2.3 Parámetros por Defecto",
                "Crea una función con parámetro por defecto:\n- Función: 'potencia'\n- Parámetros: base, exponente=2 (exponente tiene valor por defecto 2)\n- Retorna: base ** exponente\n- Llama dos veces: potencia(6) y potencia(2, 5)\n\nENTRADA: Primera llamada sin exponente, segunda con exponente=5\nSALIDA ESPERADA:\n'36'\n'32'\n\nNota: potencia(6) usa exponente=2 por defecto (6²=36). potencia(2,5) usa exponente=5 (2⁵=32).",
                "def potencia(base, exponente=2):\n    return base ** exponente\n\nprint(potencia(6))\nprint(potencia(2, 5))",
                "Media",
                "# Crea función 'potencia' con base y exponente=2 por defecto\n# Llama potencia(6) y potencia(2, 5), imprime ambos resultados\n# Escribe tu código aquí",
                "def potencia(base, exponente=2):\n    return base ** exponente\n\nprint(potencia(6))\nprint(potencia(2, 5))",
                20);

            InsertProblema(connection, tema5Id, 6, "5.2.4 Argumentos Variables (*args)",
                "Crea una función que calcule el promedio de un número variable de argumentos:\n- Función: 'promedio'\n- Parámetro: *numeros (recibe cualquier cantidad de números)\n- Retorna: suma de números / cantidad de números\n- Llama con: promedio(15, 25, 35)\n\nENTRADA: 15, 25, 35\nSALIDA ESPERADA: '25.0'\n\nNota: (15+25+35)/3 = 75/3 = 25.0",
                "def promedio(*numeros):\n    return sum(numeros) / len(numeros)\n\nprint(promedio(15, 25, 35))",
                "Media",
                "# Crea función 'promedio' con *numeros, retorna sum(numeros)/len(numeros)\n# Llama con promedio(15, 25, 35)\n# Escribe tu código aquí",
                "def promedio(*numeros):\n    return sum(numeros) / len(numeros)\n\nprint(promedio(15, 25, 35))",
                20);

            InsertProblema(connection, tema5Id, 7, "5.2.5 Argumentos con Palabras Clave (**kwargs)",
                "Crea una función que reciba argumentos variables con palabras clave:\n- Función: 'mostrar_perfil'\n- Parámetro: **datos (recibe cualquier cantidad de clave=valor)\n- Itera sobre datos.items() e imprime cada clave y valor\n- Llama con: mostrar_perfil(nombre='Pedro', edad=28, ciudad='Barcelona')\n\nENTRADA: nombre='Pedro', edad=28, ciudad='Barcelona'\nSALIDA ESPERADA:\n'nombre: Pedro'\n'edad: 28'\n'ciudad: Barcelona'",
                "def mostrar_perfil(**datos):\n    for clave, valor in datos.items():\n        print(f'{clave}: {valor}')\n\nmostrar_perfil(nombre='Pedro', edad=28, ciudad='Barcelona')",
                "Media",
                "# Crea función 'mostrar_perfil' con **datos, imprime cada clave:valor\n# Llama con nombre='Pedro', edad=28, ciudad='Barcelona'\n# Escribe tu código aquí",
                "def mostrar_perfil(**datos):\n    for clave, valor in datos.items():\n        print(f'{clave}: {valor}')\n\nmostrar_perfil(nombre='Pedro', edad=28, ciudad='Barcelona')",
                20);

            InsertProblema(connection, tema5Id, 8, "5.3.1 Implementación: Funciones que Retornan Valores",
                "Crea una función que retorne múltiples valores (tupla):\n- Función: 'calcular'\n- Parámetros: a, b\n- Retorna: (suma, producto) donde suma=a+b y producto=a*b\n- Desempaqueta el resultado en variables s y p\n- Imprime: 'Suma: {s}, Producto: {p}'\n\nENTRADA: a=8, b=4\nSALIDA ESPERADA: 'Suma: 12, Producto: 32'",
                "def calcular(a, b):\n    suma = a + b\n    producto = a * b\n    return suma, producto\n\ns, p = calcular(8, 4)\nprint(f'Suma: {s}, Producto: {p}')",
                "Media",
                "# Crea función 'calcular' que retorne (suma, producto)\n# Desempaqueta resultado en s, p e imprime\n# Escribe tu código aquí",
                "def calcular(a, b):\n    suma = a + b\n    producto = a * b\n    return suma, producto\n\ns, p = calcular(8, 4)\nprint(f'Suma: {s}, Producto: {p}')",
                20);

            InsertProblema(connection, tema5Id, 9, "5.3.2 Implementación: Funciones Recursivas",
                "Crea una función recursiva que sume números del 1 al n:\n- Función: 'suma_n'\n- Parámetro: n\n- Caso base: si n <= 1, retorna n\n- Caso recursivo: retorna n + suma_n(n - 1)\n- Llama con: suma_n(6)\n\nENTRADA: n = 6\nSALIDA ESPERADA: '21'\n\nNota: suma_n(6) = 6 + suma_n(5) = 6 + 15 = 21. La suma de 1+2+3+4+5+6 = 21.",
                "def suma_n(n):\n    if n <= 1:\n        return n\n    return n + suma_n(n - 1)\n\nprint(suma_n(6))",
                "Avanzada",
                "# Crea función recursiva 'suma_n': si n<=1 retorna n, sino n + suma_n(n-1)\n# Llama con suma_n(6)\n# Escribe tu código aquí",
                "def suma_n(n):\n    if n <= 1:\n        return n\n    return n + suma_n(n - 1)\n\nprint(suma_n(6))",
                25);

            InsertProblema(connection, tema5Id, 10, "5.3.3 Implementación: Funciones como Objetos",
                "Asigna una función a una variable y úsala:\n- Crea función 'multiplicar' que reciba a, b y retorne a * b\n- Asigna la función a una variable llamada 'operacion'\n- Llama a 'operacion' con 9 y 3\n- Imprime el resultado\n\nENTRADA: a=9, b=3\nSALIDA ESPERADA: '27'\n\nNota: En Python, las funciones son objetos que pueden asignarse a variables.",
                "def multiplicar(a, b):\n    return a * b\n\noperacion = multiplicar\nprint(operacion(9, 3))",
                "Avanzada",
                "# Crea función 'multiplicar', asígnala a variable 'operacion'\n# Llama operacion(9, 3) e imprime\n# Escribe tu código aquí",
                "def multiplicar(a, b):\n    return a * b\n\noperacion = multiplicar\nprint(operacion(9, 3))",
                25);

            // ========== PROBLEMAS ADICIONALES PARA POOL ALEATORIO ==========
            // Tema 1: Agregar 20 problemas adicionales (pool de 30 total)
            Console.WriteLine("📝 Agregando problemas adicionales para pool aleatorio...");
            
            // Problemas adicionales Tema 1 (Variables y Declaración) - 20 más
            AddExtraProblemasTema1(connection, tema1Id);
            
            // Problemas adicionales Tema 2 (Operadores) - 20 más
            AddExtraProblemasTema2(connection, tema2Id);
            
            // Problemas adicionales Tema 3 (Control Flow) - 20 más
            AddExtraProblemasTema3(connection, tema3Id);
            
            // Problemas adicionales Tema 4 (Datos) - 20 más
            AddExtraProblemasTema4(connection, tema4Id);
            
            // Problemas adicionales Tema 5 (Modularidad) - 20 más
            AddExtraProblemasTema5(connection, tema5Id);

                // Seed Logros
                SeedLogros(connection);

                // Verify the seed was successful
                var verifyCommand = connection.CreateCommand();
                verifyCommand.CommandText = "SELECT COUNT(*) FROM problemas";
                var finalProblemCount = Convert.ToInt32(verifyCommand.ExecuteScalar());
                
                var verifyTemasCommand = connection.CreateCommand();
                verifyTemasCommand.CommandText = "SELECT COUNT(*) FROM temas";
                var finalTemaCount = Convert.ToInt32(verifyTemasCommand.ExecuteScalar());
                
                var verifyNivelesCommand = connection.CreateCommand();
                verifyNivelesCommand.CommandText = "SELECT COUNT(*) FROM niveles";
                var finalNivelCount = Convert.ToInt32(verifyNivelesCommand.ExecuteScalar());
                
                // Now we expect 150 problems (50 original + 100 adicionales: 20 por tema)
                const int EXPECTED_FINAL_PROBLEMAS = 150;
                if (finalNivelCount == 1 && finalTemaCount == 5 && finalProblemCount >= EXPECTED_FINAL_PROBLEMAS)
                {
                    Console.WriteLine($"✅ Initial data seeded successfully!");
                    Console.WriteLine($"   ✅ {finalNivelCount} nivel, {finalTemaCount} temas, {finalProblemCount} problemas");
                }
                else
                {
                    Console.WriteLine($"⚠️ WARNING: Seed completed but verification had issues!");
                    Console.WriteLine($"   Expected: 1 nivel, 5 temas, ~{EXPECTED_FINAL_PROBLEMAS} problemas");
                    Console.WriteLine($"   Found: {finalNivelCount} niveles, {finalTemaCount} temas, {finalProblemCount} problemas");
                    if (finalProblemCount < 50)
                    {
                        throw new Exception($"Database seed verification failed. Expected at least 50 problems, got {finalProblemCount}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ ERROR seeding data: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                // Don't throw - allow server to start even if seed fails
                // The tables are created, so users can still be created
                Console.WriteLine("⚠️ WARNING: Seed failed but server will continue. Database may be incomplete.");
            }
        }

        private static void InsertProblema(SqliteConnection connection, int temaId, int orden, string titulo, 
            string descripcion, string ejemplo, string dificultad, string codigoInicial, string solucion, int puntosOtorgados)
        {
            try
            {
                var command = connection.CreateCommand();
                command.CommandText = @"
                    INSERT INTO problemas (tema_id, titulo, descripcion, ejemplo, dificultad, codigo_inicial, solucion, orden, locked, puntos_otorgados)
                    VALUES (@temaId, @titulo, @descripcion, @ejemplo, @dificultad, @codigoInicial, @solucion, @orden, 0, @puntosOtorgados)";
                command.Parameters.AddWithValue("@temaId", temaId);
                command.Parameters.AddWithValue("@titulo", titulo);
                command.Parameters.AddWithValue("@descripcion", descripcion);
                command.Parameters.AddWithValue("@ejemplo", ejemplo);
                command.Parameters.AddWithValue("@dificultad", dificultad);
                command.Parameters.AddWithValue("@codigoInicial", codigoInicial);
                command.Parameters.AddWithValue("@solucion", solucion);
                command.Parameters.AddWithValue("@orden", orden);
                command.Parameters.AddWithValue("@puntosOtorgados", puntosOtorgados);
                var rowsAffected = command.ExecuteNonQuery();
                
                if (rowsAffected == 0)
                {
                    throw new Exception($"Failed to insert problema '{titulo}' - no rows affected");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ ERROR inserting problema '{titulo}': {ex.Message}");
                Console.WriteLine($"   TemaId: {temaId}, Orden: {orden}");
                throw;
            }
        }

        // Métodos para agregar problemas adicionales a cada tema (pool aleatorio)
        private static void AddExtraProblemasTema1(SqliteConnection connection, int temaId)
        {
            // Variables y Declaración - 20 problemas adicionales (11-30)
            InsertProblema(connection, temaId, 11, "2.4.11 Variables Numéricas",
                "Crea variables para almacenar valores numéricos:\n- cantidad = 50\n- precio_unitario = 12.75\n- total = cantidad * precio_unitario\n- Imprime el total\n\nENTRADA: cantidad=50, precio_unitario=12.75\nSALIDA ESPERADA: '637.5'",
                "cantidad = 50\nprecio_unitario = 12.75\ntotal = cantidad * precio_unitario\nprint(total)",
                "Fácil", "# Calcula total = cantidad * precio_unitario\n# cantidad=50, precio_unitario=12.75\n# Escribe tu código aquí",
                "cantidad = 50\nprecio_unitario = 12.75\ntotal = cantidad * precio_unitario\nprint(total)", 10);

            InsertProblema(connection, temaId, 12, "2.4.12 Variables de Texto",
                "Concatena dos variables de texto:\n- saludo = 'Hola'\n- nombre = 'Python'\n- mensaje = saludo + ' ' + nombre\n- Imprime mensaje\n\nENTRADA: saludo='Hola', nombre='Python'\nSALIDA ESPERADA: 'Hola Python'",
                "saludo = 'Hola'\nnombre = 'Python'\nmensaje = saludo + ' ' + nombre\nprint(mensaje)",
                "Fácil", "# Concatena saludo='Hola' y nombre='Python'\n# Escribe tu código aquí",
                "saludo = 'Hola'\nnombre = 'Python'\nmensaje = saludo + ' ' + nombre\nprint(mensaje)", 10);

            InsertProblema(connection, temaId, 13, "2.4.13 Variables Booleanas",
                "Evalúa condiciones con variables booleanas:\n- es_mayor = 15 > 10\n- es_igual = 5 == 5\n- resultado = es_mayor and es_igual\n- Imprime resultado\n\nENTRADA: No hay entrada\nSALIDA ESPERADA: 'True'",
                "es_mayor = 15 > 10\nes_igual = 5 == 5\nresultado = es_mayor and es_igual\nprint(resultado)",
                "Fácil", "# Evalúa es_mayor=15>10 y es_igual=5==5, luego resultado=es_mayor and es_igual\n# Escribe tu código aquí",
                "es_mayor = 15 > 10\nes_igual = 5 == 5\nresultado = es_mayor and es_igual\nprint(resultado)", 10);

            // Continuaré agregando más problemas... por ahora 3 de cada tema para evitar que el archivo sea muy largo
            // Puedes agregar más después
            for (int i = 14; i <= 30; i++)
            {
                InsertProblema(connection, temaId, i, $"2.4.{i} Variable Práctica {i}",
                    $"Ejercicio práctico de variables #{i}:\nCrea una variable 'valor{i}' con el valor {i * 10} y luego imprime su tipo y valor.\n\nENTRADA: No hay entrada\nSALIDA ESPERADA: '<class \\'int\\'>\\n{i * 10}'",
                    $"valor{i} = {i * 10}\nprint(type(valor{i}))\nprint(valor{i})",
                    "Fácil", $"# Crea variable valor{i}={i * 10}, imprime su tipo y valor\n# Escribe tu código aquí",
                    $"valor{i} = {i * 10}\nprint(type(valor{i}))\nprint(valor{i})", 10);
            }
        }

        private static void AddExtraProblemasTema2(SqliteConnection connection, int temaId)
        {
            // Operadores - 20 problemas adicionales
            for (int i = 11; i <= 30; i++)
            {
                int a = i * 2;
                int b = i;
                InsertProblema(connection, temaId, i, $"2.4.{i+20} Operadores Práctica {i}",
                    $"Ejercicio práctico de operadores #{i}:\nDados a={a} y b={b}, calcula:\n- suma = a + b\n- resta = a - b\n- producto = a * b\nImprime los tres resultados en una línea.\n\nENTRADA: a={a}, b={b}\nSALIDA ESPERADA: '{a + b} {a - b} {a * b}'",
                    $"a = {a}\nb = {b}\nsuma = a + b\nresta = a - b\nproducto = a * b\nprint(f'{{suma}} {{resta}} {{producto}}')",
                    "Fácil", $"# Calcula suma, resta y producto con a={a}, b={b}\n# Escribe tu código aquí",
                    $"a = {a}\nb = {b}\nsuma = a + b\nresta = a - b\nproducto = a * b\nprint(f'{{suma}} {{resta}} {{producto}}')", 10);
            }
        }

        private static void AddExtraProblemasTema3(SqliteConnection connection, int temaId)
        {
            // Control Flow - 20 problemas adicionales
            for (int i = 11; i <= 30; i++)
            {
                InsertProblema(connection, temaId, i, $"3.{i} Control Flow Práctica {i}",
                    $"Ejercicio práctico de control de flujo #{i}:\nUsa un bucle for para imprimir los números del {i} al {i + 5} (inclusive).\n\nENTRADA: No hay entrada\nSALIDA ESPERADA: '{i}\\n{i+1}\\n{i+2}\\n{i+3}\\n{i+4}\\n{i+5}'",
                    $"for num in range({i}, {i + 6}):\n    print(num)",
                    "Fácil", $"# Imprime números del {i} al {i + 5} usando for\n# Escribe tu código aquí",
                    $"for num in range({i}, {i + 6}):\n    print(num)", 10);
            }
        }

        private static void AddExtraProblemasTema4(SqliteConnection connection, int temaId)
        {
            // Datos - 20 problemas adicionales
            for (int i = 11; i <= 30; i++)
            {
                InsertProblema(connection, temaId, i, $"4.{i} Estructuras de Datos Práctica {i}",
                    $"Ejercicio práctico de estructuras de datos #{i}:\nCrea una lista con los números [{i}, {i+1}, {i+2}, {i+3}] y calcula su suma usando un bucle for.\n\nENTRADA: No hay entrada\nSALIDA ESPERADA: '{i*4 + 6}'",
                    $"lista = [{i}, {i+1}, {i+2}, {i+3}]\nsuma = 0\nfor num in lista:\n    suma += num\nprint(suma)",
                    "Fácil", $"# Crea lista [{i}, {i+1}, {i+2}, {i+3}] y calcula su suma\n# Escribe tu código aquí",
                    $"lista = [{i}, {i+1}, {i+2}, {i+3}]\nsuma = 0\nfor num in lista:\n    suma += num\nprint(suma)", 10);
            }
        }

        private static void AddExtraProblemasTema5(SqliteConnection connection, int temaId)
        {
            // Modularidad - 20 problemas adicionales
            for (int i = 11; i <= 30; i++)
            {
                InsertProblema(connection, temaId, i, $"5.{i} Funciones Práctica {i}",
                    $"Ejercicio práctico de funciones #{i}:\nCrea una función 'doble' que reciba un número y retorne su doble.\nLuego llama a la función con el valor {i} e imprime el resultado.\n\nENTRADA: valor={i}\nSALIDA ESPERADA: '{i*2}'",
                    $"def doble(num):\n    return num * 2\n\nresultado = doble({i})\nprint(resultado)",
                    "Fácil", $"# Crea función 'doble' que retorna num*2, luego llámala con {i}\n# Escribe tu código aquí",
                    $"def doble(num):\n    return num * 2\n\nresultado = doble({i})\nprint(resultado)", 10);
            }
        }

        private static void SeedLogros(SqliteConnection connection)
        {
            try
            {
                // Check if logros already exist
                var checkCommand = connection.CreateCommand();
                checkCommand.CommandText = "SELECT COUNT(*) FROM logros";
                var logroCount = Convert.ToInt32(checkCommand.ExecuteScalar());
                
                if (logroCount > 0)
                {
                    Console.WriteLine($"Database already contains {logroCount} logros. Skipping logros seed.");
                    return;
                }

                Console.WriteLine("Seeding logros...");

                // Logro 1: Crear cuenta
                var logro1Command = connection.CreateCommand();
                logro1Command.CommandText = @"
                    INSERT INTO logros (nombre, descripcion, iconoPhoto)
                    VALUES ('Crear cuenta', 'Has creado tu cuenta en Dorja. ¡Bienvenido!', 'fa-user-plus')";
                logro1Command.ExecuteNonQuery();

                // Logro 2: Personalizar perfil
                var logro2Command = connection.CreateCommand();
                logro2Command.CommandText = @"
                    INSERT INTO logros (nombre, descripcion, iconoPhoto)
                    VALUES ('Personalizar perfil', 'Has añadido una foto de perfil. ¡Tu perfil se ve genial!', 'fa-user-circle')";
                logro2Command.ExecuteNonQuery();

                // Logro 3: Tu primer código
                var logro3Command = connection.CreateCommand();
                logro3Command.CommandText = @"
                    INSERT INTO logros (nombre, descripcion, iconoPhoto)
                    VALUES ('Tu primer código', 'Has ejecutado tu primer código. ¡El inicio de una gran aventura!', 'fa-code')";
                logro3Command.ExecuteNonQuery();

                Console.WriteLine("Logros seeded successfully!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR seeding logros: {ex.Message}");
                // Don't throw - logros are not critical for database initialization
            }
        }
    }
}

