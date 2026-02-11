using DorjaModelado;
using Microsoft.Data.Sqlite;
using Dapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DorjaData.Repositories
{
    public class ProblemaRepository : IProblemaRepository
    {
        private readonly SQLiteConfiguration _connectionString;

        public ProblemaRepository(SQLiteConfiguration connectionString)
        {
            _connectionString = connectionString;
        }

        protected SqliteConnection dbConnection()
        {
            return new SqliteConnection(_connectionString.ConnectionString);
        }

        // Obtener todos los problemas
        public async Task<IEnumerable<Problema>> GetAllProblemas()
        {
            var db = dbConnection();
            var sql = @"SELECT id as Id, tema_id as TemaId, titulo as Titulo, descripcion as Descripcion, 
                       ejemplo as Ejemplo, dificultad as Dificultad, codigo_inicial as CodigoInicial, 
                       solucion as Solucion, orden as Orden, locked as Locked, puntos_otorgados as PuntosOtorgados 
                       FROM problemas";
            return await db.QueryAsync<Problema>(sql);
        }

        // Obtener detalles de un problema por id
        public async Task<Problema> GetDetails(int id)
        {
            var db = dbConnection();
            var sql = @"SELECT id as Id, tema_id as TemaId, titulo as Titulo, descripcion as Descripcion, 
                       ejemplo as Ejemplo, dificultad as Dificultad, codigo_inicial as CodigoInicial, 
                       solucion as Solucion, orden as Orden, locked as Locked, puntos_otorgados as PuntosOtorgados 
                       FROM problemas WHERE id = @Id";
            return await db.QueryFirstOrDefaultAsync<Problema>(sql, new { Id = id });
        }

        // Obtener problemas por tema
        public async Task<IEnumerable<Problema>> GetProblemasByTema(int temaId)
        {
            var db = dbConnection();
            var sql = @"SELECT id as Id, tema_id as TemaId, titulo as Titulo, descripcion as Descripcion, 
                       ejemplo as Ejemplo, dificultad as Dificultad, codigo_inicial as CodigoInicial, 
                       solucion as Solucion, orden as Orden, locked as Locked, puntos_otorgados as PuntosOtorgados 
                       FROM problemas WHERE tema_id = @TemaId ORDER BY orden";
            return await db.QueryAsync<Problema>(sql, new { TemaId = temaId });
        }

        // Obtener problemas aleatorios por tema (para evitar que usuarios compartan soluciones)
        // Si userId se proporciona, prioriza problemas no completados, pero si no hay suficientes,
        // incluye problemas completados para llegar al count solicitado
        public async Task<IEnumerable<Problema>> GetProblemasRandomByTema(int temaId, int count = 10, int? userId = null)
        {
            var db = dbConnection();
            
            // Si userId está proporcionado, primero intentar obtener problemas no completados
            if (userId.HasValue)
            {
                // Primero, obtener problemas no completados
                var sqlNotCompleted = @"SELECT p.id as Id, p.tema_id as TemaId, p.titulo as Titulo, p.descripcion as Descripcion, 
                       p.ejemplo as Ejemplo, p.dificultad as Dificultad, p.codigo_inicial as CodigoInicial, 
                       p.solucion as Solucion, p.orden as Orden, p.locked as Locked, p.puntos_otorgados as PuntosOtorgados 
                       FROM problemas p
                       WHERE p.tema_id = @TemaId 
                       AND p.id NOT IN (
                           SELECT problema_id FROM progreso_problema 
                           WHERE user_id = @UserId AND completado = 1
                       )
                       AND p.locked = 0
                       ORDER BY RANDOM()
                       LIMIT @Count";
                
                var notCompleted = (await db.QueryAsync<Problema>(sqlNotCompleted, new { TemaId = temaId, UserId = userId.Value, Count = count })).ToList();
                
                Console.WriteLine($"📊 Tema {temaId}: {notCompleted.Count} problemas no completados encontrados (solicitados: {count})");
                
                // Si tenemos suficientes problemas no completados, retornar solo esos
                if (notCompleted.Count >= count)
                {
                    Console.WriteLine($"✅ Devolviendo {notCompleted.Count} problemas no completados");
                    return notCompleted;
                }
                
                // Si no hay suficientes, obtener algunos problemas completados para llegar al count
                var needed = count - notCompleted.Count;
                Console.WriteLine($"📊 Necesitamos {needed} problemas más. Buscando problemas completados...");
                var sqlCompleted = @"SELECT p.id as Id, p.tema_id as TemaId, p.titulo as Titulo, p.descripcion as Descripcion, 
                       p.ejemplo as Ejemplo, p.dificultad as Dificultad, p.codigo_inicial as CodigoInicial, 
                       p.solucion as Solucion, p.orden as Orden, p.locked as Locked, p.puntos_otorgados as PuntosOtorgados 
                       FROM problemas p
                       WHERE p.tema_id = @TemaId 
                       AND p.id IN (
                           SELECT problema_id FROM progreso_problema 
                           WHERE user_id = @UserId AND completado = 1
                       )
                       AND p.locked = 0
                       ORDER BY RANDOM()
                       LIMIT @Needed";
                
                var completed = (await db.QueryAsync<Problema>(sqlCompleted, new { TemaId = temaId, UserId = userId.Value, Needed = needed })).ToList();
                
                Console.WriteLine($"📊 Tema {temaId}: {completed.Count} problemas completados encontrados (necesitados: {needed})");
                
                // Combinar y mezclar usando un random
                var allProblems = notCompleted.Concat(completed).ToList();
                Console.WriteLine($"📊 Tema {temaId}: Total combinado: {allProblems.Count} problemas (solicitados: {count})");
                
                // Si aún no tenemos suficientes, obtener TODOS los problemas del tema (sin filtrar por completado)
                if (allProblems.Count < count)
                {
                    Console.WriteLine($"⚠️ Solo hay {allProblems.Count} problemas disponibles para tema {temaId}, solicitados: {count}");
                    Console.WriteLine($"📊 Obteniendo TODOS los problemas del tema (sin filtrar por completado)...");
                    
                    var sqlAll = @"SELECT id as Id, tema_id as TemaId, titulo as Titulo, descripcion as Descripcion, 
                           ejemplo as Ejemplo, dificultad as Dificultad, codigo_inicial as CodigoInicial, 
                           solucion as Solucion, orden as Orden, locked as Locked, puntos_otorgados as PuntosOtorgados 
                           FROM problemas 
                           WHERE tema_id = @TemaId AND locked = 0
                           ORDER BY RANDOM()
                           LIMIT @Count";
                    
                    var allFromTema = (await db.QueryAsync<Problema>(sqlAll, new { TemaId = temaId, Count = count })).ToList();
                    Console.WriteLine($"📊 Tema {temaId}: {allFromTema.Count} problemas totales disponibles (sin filtrar por completado)");
                    
                    if (allFromTema.Count > allProblems.Count)
                    {
                        // Usar los problemas totales si hay más disponibles
                        return allFromTema;
                    }
                }
                
                var random = new Random();
                var shuffled = allProblems.OrderBy(x => random.Next()).Take(count).ToList();
                Console.WriteLine($"✅ Devolviendo {shuffled.Count} problemas (mezclados)");
                return shuffled;
            }
            else
            {
                // Si no hay userId, simplemente obtener problemas aleatorios no bloqueados
                var sql = @"SELECT id as Id, tema_id as TemaId, titulo as Titulo, descripcion as Descripcion, 
                       ejemplo as Ejemplo, dificultad as Dificultad, codigo_inicial as CodigoInicial, 
                       solucion as Solucion, orden as Orden, locked as Locked, puntos_otorgados as PuntosOtorgados 
                       FROM problemas 
                       WHERE tema_id = @TemaId AND locked = 0
                       ORDER BY RANDOM()
                       LIMIT @Count";
                return await db.QueryAsync<Problema>(sql, new { TemaId = temaId, Count = count });
            }
        }

        // Obtener problemas por nivel (a través de temas)
        public async Task<IEnumerable<Problema>> GetProblemasByNivel(int nivelId)
        {
            var db = dbConnection();
            var sql = @"SELECT p.id as Id, p.tema_id as TemaId, p.titulo as Titulo, p.descripcion as Descripcion, 
                       p.ejemplo as Ejemplo, p.dificultad as Dificultad, p.codigo_inicial as CodigoInicial, 
                       p.solucion as Solucion, p.orden as Orden, p.locked as Locked, p.puntos_otorgados as PuntosOtorgados 
                       FROM problemas p
                       INNER JOIN temas t ON p.tema_id = t.id
                       WHERE t.nivel_id = @NivelId
                       ORDER BY t.orden, p.orden";
            return await db.QueryAsync<Problema>(sql, new { NivelId = nivelId });
        }

        // Insertar un nuevo problema
        public async Task<bool> InsertProblemas(Problema problema)
        {
            var db = dbConnection();
            var sql = @"INSERT INTO problemas 
                        (tema_id, titulo, descripcion, ejemplo, dificultad, codigo_inicial, solucion, orden, locked, puntos_otorgados) 
                        VALUES 
                        (@TemaId, @Titulo, @Descripcion, @Ejemplo, @Dificultad, @CodigoInicial, @Solucion, @Orden, @Locked, @PuntosOtorgados)";
            var result = await db.ExecuteAsync(sql, problema);
            return result > 0;
        }

        // Actualizar un problema existente
        public async Task<bool> UpdateProblemas(Problema problema)
        {
            var db = dbConnection();
            var sql = @"UPDATE problemas SET
                            tema_id = @TemaId,
                            titulo = @Titulo,
                            descripcion = @Descripcion,
                            ejemplo = @Ejemplo,
                            dificultad = @Dificultad,
                            codigo_inicial = @CodigoInicial,
                            solucion = @Solucion,
                            orden = @Orden,
                            locked = @Locked,
                            puntos_otorgados = @PuntosOtorgados
                        WHERE id = @Id";
            var result = await db.ExecuteAsync(sql, problema);
            return result > 0;
        }

        // Eliminar un problema
        public async Task<bool> DeleteProblemas(Problema problema)
        {
            var db = dbConnection();
            var sql = "DELETE FROM problemas WHERE id = @Id";
            var result = await db.ExecuteAsync(sql, problema);
            return result > 0;
        }
    }
}
