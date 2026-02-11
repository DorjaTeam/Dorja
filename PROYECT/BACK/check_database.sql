-- Check users
SELECT 'USERS:' as Info;
SELECT id, username, email FROM users;

-- Check problemas
SELECT 'PROBLEMAS:' as Info;
SELECT id, titulo, tema_id FROM problemas ORDER BY id;

-- Check progreso_problema
SELECT 'PROGRESO:' as Info;
SELECT * FROM progreso_problema;
