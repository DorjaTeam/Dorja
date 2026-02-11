# Guía de Manejo de Imágenes de Perfil

Este proyecto soporta **dos métodos** para guardar y cargar imágenes de perfil:

## 📋 Métodos Disponibles

### 1. **Método BLOB (Base de Datos)** ⭐ **RECOMENDADO PARA TU CASO**
**Guardado directamente en la base de datos como BLOB**

#### Ventajas:
- ✅ **Todo en un solo lugar**: Las imágenes están en la base de datos junto con los datos del usuario
- ✅ **Más simple**: No necesitas gestionar archivos en el sistema de archivos
- ✅ **Portabilidad**: Al hacer backup de la BD, tienes todo incluido
- ✅ **Sin problemas de rutas**: No hay que preocuparse por rutas de archivos

#### Desventajas:
- ⚠️ **Tamaño de BD**: La base de datos crece con cada imagen
- ⚠️ **Rendimiento**: Para muchas imágenes grandes, puede ser más lento
- ⚠️ **Límites SQLite**: SQLite tiene un límite práctico de ~140GB por base de datos

#### Endpoints:
- **Subir**: `POST /api/Users/{userId}/upload-image-blob`
- **Obtener**: `GET /api/Users/{userId}/image-blob?imageType=profile|cover`

#### Uso en Frontend:
```javascript
// Guardar imagen como BLOB
await window.api.saveImageAsBlob({ userId, imageType: 'profile', dataUrl });

// Obtener imagen desde BLOB
const imageUrl = await window.api.getImageBlob(userId, 'profile');
```

---

### 2. **Método Archivos (Sistema de Archivos + Ruta en BD)**
**Guardado en sistema de archivos, ruta guardada en la base de datos**

#### Ventajas:
- ✅ **Mejor rendimiento**: Las imágenes se sirven directamente como archivos estáticos
- ✅ **BD más liviana**: Solo se guarda la ruta, no la imagen completa
- ✅ **Escalable**: Fácil de mover a CDN o almacenamiento en la nube
- ✅ **Sin límites de tamaño**: El sistema de archivos no tiene límites prácticos

#### Desventajas:
- ⚠️ **Gestión de archivos**: Necesitas asegurarte de que los archivos existan
- ⚠️ **Backups**: Necesitas hacer backup de BD + archivos
- ⚠️ **Rutas**: Debes manejar rutas relativas/absolutas correctamente

#### Endpoints:
- **Subir**: `POST /api/Users/{userId}/upload-image`
- **Obtener**: Directamente desde la URL del archivo (ej: `http://localhost:5222/uploads/users/1/profile_xxx.jpg`)

#### Uso en Frontend:
```javascript
// Guardar imagen en sistema de archivos
await window.api.saveImage({ userId, imageType: 'profile', dataUrl });
```

---

## 🎯 Implementación Actual

**Por defecto, el sistema usa el método BLOB** (guardado en base de datos), que es lo que solicitaste.

El código está configurado para:
1. **Intentar cargar desde BLOB primero** cuando se muestra el perfil
2. **Hacer fallback a archivos** si no encuentra BLOB (para compatibilidad con datos antiguos)
3. **Guardar nuevas imágenes como BLOB** en la base de datos

---

## 📊 Estructura de la Base de Datos

La tabla `users` ahora incluye:

```sql
profilePhotoPath TEXT DEFAULT ''     -- Ruta del archivo (método archivos)
coverPhotoPath TEXT DEFAULT ''       -- Ruta del archivo (método archivos)
profilePhotoBlob BLOB                -- Imagen como BLOB (método BLOB)
coverPhotoBlob BLOB                  -- Imagen como BLOB (método BLOB)
```

---

## 🔄 Migración entre Métodos

Si quieres migrar imágenes del método de archivos al método BLOB:

1. Lee la imagen desde la ruta del archivo
2. Convierte a bytes
3. Guarda usando `UpdatePhotoBlob()`

---

## 💡 Recomendación

Para tu caso de uso (fotos de perfil):
- **Usa BLOB** si tienes pocos usuarios y quieres simplicidad
- **Usa Archivos** si esperas muchos usuarios o imágenes grandes

**El sistema actual está configurado para usar BLOB por defecto**, que es lo que pediste.

