// pdf-generator.js
// Módulo para generar certificados en PDF

/**
 * Genera y descarga un certificado en PDF con los datos del usuario
 * @param {Object} userData - Datos del usuario
 * @param {string} userData.nombre - Nombre del usuario
 * @param {string} userData.apellidoPaterno - Apellido paterno
 * @param {string} userData.apellidoMaterno - Apellido materno
 */
async function generateCertificatePDF(userData) {
    try {
        console.log('🎓 Generando certificado PDF para:', userData);

        // Obtener el nombre completo del usuario
        const nombreCompleto = `${userData.nombre} ${userData.apellidoPaterno} ${userData.apellidoMaterno}`;

        // Obtener la fecha actual
        const fecha = new Date().toLocaleDateString('es-MX', {
            year: 'numeric',
            month: 'long',
            day: 'numeric'
        });

        // Generar el HTML del certificado
        const certificateHTML = getCertificateHTML(nombreCompleto, fecha);

        // Crear un elemento temporal para el certificado
        const tempDiv = document.createElement('div');
        tempDiv.innerHTML = certificateHTML;
        tempDiv.style.position = 'absolute';
        tempDiv.style.left = '-9999px';
        document.body.appendChild(tempDiv);

        // Opciones para html2pdf
        const options = {
            margin: 0,
            filename: `Certificado_${nombreCompleto.replace(/\s+/g, '_')}.pdf`,
            image: { type: 'jpeg', quality: 0.98 },
            html2canvas: {
                scale: 2,
                useCORS: true,
                letterRendering: true
            },
            jsPDF: {
                unit: 'mm',
                format: 'a4',
                orientation: 'landscape'
            }
        };

        // Generar y descargar el PDF
        await html2pdf().from(tempDiv).set(options).save();

        // Limpiar el elemento temporal
        document.body.removeChild(tempDiv);

        console.log('✅ Certificado PDF generado y descargado exitosamente');

        // Mostrar mensaje de éxito
        showSuccessMessage('¡Certificado descargado! 🎉');

    } catch (error) {
        console.error('❌ Error al generar el PDF:', error);
        alert('Error al generar el certificado. Por favor, intenta de nuevo.');
    }
}

/**
 * Genera el HTML del certificado con los datos del usuario
 * @param {string} nombreCompleto - Nombre completo del usuario
 * @param {string} fecha - Fecha de emisión del certificado
 * @returns {string} HTML del certificado
 */
function getCertificateHTML(nombreCompleto, fecha) {
    return `
<!DOCTYPE html>
<html lang="es">
<head>
    <meta charset="UTF-8">
    <style>
        * {
            margin: 0;
            padding: 0;
            box-sizing: border-box;
        }

        body {
            font-family: 'Georgia', serif;
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            display: flex;
            justify-content: center;
            align-items: center;
            padding: 20px;
        }

        .certificate-container {
            background: white;
            width: 100%;
            max-width: 900px;
            padding: 60px;
            border: 20px solid #2c3e50;
            border-image: linear-gradient(45deg, #667eea, #764ba2) 1;
            position: relative;
        }

        .certificate-border {
            border: 3px solid #f39c12;
            padding: 40px;
            position: relative;
        }

        .corner-decoration {
            position: absolute;
            width: 80px;
            height: 80px;
            border: 3px solid #f39c12;
        }

        .corner-decoration.top-left {
            top: -3px;
            left: -3px;
            border-right: none;
            border-bottom: none;
        }

        .corner-decoration.top-right {
            top: -3px;
            right: -3px;
            border-left: none;
            border-bottom: none;
        }

        .corner-decoration.bottom-left {
            bottom: -3px;
            left: -3px;
            border-right: none;
            border-top: none;
        }

        .corner-decoration.bottom-right {
            bottom: -3px;
            right: -3px;
            border-left: none;
            border-top: none;
        }

        .header {
            text-align: center;
            margin-bottom: 30px;
        }

        .logo {
            width: 100px;
            height: 100px;
            margin: 0 auto 20px;
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            border-radius: 50%;
            display: flex;
            align-items: center;
            justify-content: center;
            color: white;
            font-size: 48px;
            font-weight: bold;
        }

        .institution-name {
            font-size: 28px;
            color: #2c3e50;
            font-weight: bold;
            margin-bottom: 5px;
        }

        .institution-subtitle {
            font-size: 16px;
            color: #7f8c8d;
            margin-bottom: 10px;
        }

        .certificate-title {
            font-size: 42px;
            color: #667eea;
            text-transform: uppercase;
            letter-spacing: 4px;
            margin: 30px 0;
            font-weight: bold;
        }

        .certificate-text {
            text-align: center;
            font-size: 18px;
            color: #34495e;
            line-height: 1.8;
            margin: 30px 0;
        }

        .student-name {
            font-size: 36px;
            color: #2c3e50;
            font-weight: bold;
            margin: 30px 0;
            padding: 10px 0;
            border-bottom: 2px solid #f39c12;
            display: inline-block;
        }

        .achievement {
            margin: 40px 0;
            font-size: 18px;
            color: #34495e;
            line-height: 2;
            text-align: center;
        }

        .achievement strong {
            color: #667eea;
            font-size: 20px;
        }

        .footer {
            display: flex;
            justify-content: space-around;
            margin-top: 60px;
            padding-top: 40px;
        }

        .signature-block {
            text-align: center;
            flex: 1;
            margin: 0 20px;
        }

        .signature-line {
            border-top: 2px solid #2c3e50;
            margin-bottom: 10px;
            padding-top: 10px;
        }

        .signature-name {
            font-weight: bold;
            color: #2c3e50;
            font-size: 16px;
        }

        .signature-title {
            color: #7f8c8d;
            font-size: 14px;
        }

        .date-location {
            text-align: center;
            margin-top: 30px;
            color: #7f8c8d;
            font-size: 14px;
        }

        .seal {
            position: absolute;
            bottom: 100px;
            right: 80px;
            width: 120px;
            height: 120px;
            border: 3px solid #e74c3c;
            border-radius: 50%;
            display: flex;
            align-items: center;
            justify-content: center;
            font-size: 14px;
            color: #e74c3c;
            font-weight: bold;
            text-align: center;
            transform: rotate(-15deg);
            opacity: 0.3;
        }
    </style>
</head>
<body>
    <div class="certificate-container">
        <div class="certificate-border">
            <div class="corner-decoration top-left"></div>
            <div class="corner-decoration top-right"></div>
            <div class="corner-decoration bottom-left"></div>
            <div class="corner-decoration bottom-right"></div>

            <div class="header">
                <div class="logo">🎓</div>
                <div class="institution-name">PLATAFORMA EDUCATIVA DORJA</div>
                <div class="institution-subtitle">Aprendiendo Programación Paso a Paso</div>
            </div>

            <div style="text-align: center;">
                <div class="certificate-title">Certificado de Logro</div>
                
                <div class="certificate-text">
                    Por medio del presente se certifica que
                </div>

                <div class="student-name">${nombreCompleto}</div>

                <div class="certificate-text">
                    Ha completado exitosamente
                </div>

                <div class="achievement">
                    <strong>🏆 Su Primer Ejercicio de Programación 🏆</strong><br>
                    Demostrando dedicación, esfuerzo y las habilidades necesarias<br>
                    para iniciar su camino en el desarrollo de software.
                </div>

                <div class="certificate-text">
                    Este logro marca el inicio de un emocionante viaje de aprendizaje<br>
                    en el mundo de la programación.
                </div>
            </div>

            <div class="footer">
                <div class="signature-block">
                    <div class="signature-line">
                        <div class="signature-name">Plataforma Dorja</div>
                        <div class="signature-title">Sistema Educativo</div>
                    </div>
                </div>
            </div>

            <div class="date-location">
                ${fecha}
            </div>

            <div class="seal">
                SELLO<br>OFICIAL
            </div>
        </div>
    </div>
</body>
</html>
    `;
}

/**
 * Muestra un mensaje de éxito temporal
 * @param {string} message - Mensaje a mostrar
 */
function showSuccessMessage(message) {
    // Crear el elemento del mensaje
    const messageDiv = document.createElement('div');
    messageDiv.textContent = message;
    messageDiv.style.cssText = `
        position: fixed;
        top: 20px;
        right: 20px;
        background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
        color: white;
        padding: 15px 25px;
        border-radius: 10px;
        font-size: 16px;
        font-weight: bold;
        box-shadow: 0 4px 15px rgba(0, 0, 0, 0.3);
        z-index: 10000;
        animation: slideIn 0.5s ease-out;
    `;

    // Agregar animación
    const style = document.createElement('style');
    style.textContent = `
        @keyframes slideIn {
            from {
                transform: translateX(400px);
                opacity: 0;
            }
            to {
                transform: translateX(0);
                opacity: 1;
            }
        }
        @keyframes slideOut {
            from {
                transform: translateX(0);
                opacity: 1;
            }
            to {
                transform: translateX(400px);
                opacity: 0;
            }
        }
    `;
    document.head.appendChild(style);

    // Agregar al DOM
    document.body.appendChild(messageDiv);

    // Eliminar después de 5 segundos
    setTimeout(() => {
        messageDiv.style.animation = 'slideOut 0.5s ease-in';
        setTimeout(() => {
            document.body.removeChild(messageDiv);
        }, 500);
    }, 5000);
}

/**
 * Genera y descarga un certificado en PDF para un nivel/tema completado
 * @param {Object} data - Datos del certificado
 * @param {string} data.nombreCompleto - Nombre completo del usuario
 * @param {string} data.temaNombre - Nombre del tema/nivel completado
 * @param {number} data.nivelId - ID del nivel
 * @param {number} data.problemasCompletados - Número de problemas completados
 * @param {string} data.email - Email del usuario
 * @param {string} data.nombre - Nombre del usuario
 * @param {string} data.apellidoPaterno - Apellido paterno
 * @param {string} data.apellidoMaterno - Apellido materno
 */
async function generateLevelCertificatePDF(data) {
    try {
        console.log('🎓 Generando certificado de nivel PDF para:', data);

        // Verificar que html2pdf esté disponible
        if (typeof html2pdf === 'undefined') {
            throw new Error('La librería html2pdf.js no está cargada. Por favor, recarga la página.');
        }

        // Obtener la fecha actual
        const fecha = new Date().toLocaleDateString('es-MX', {
            year: 'numeric',
            month: 'long',
            day: 'numeric'
        });

        // Generar el HTML del certificado con todos los datos
        const certificateHTML = getLevelCertificateHTML(
            data.nombreCompleto, 
            data.temaNombre, 
            fecha, 
            data.nivelId, 
            data.problemasCompletados,
            data.email || '',
            data.nombre || '',
            data.apellidoPaterno || '',
            data.apellidoMaterno || ''
        );

        // Crear un elemento temporal para el certificado
        const tempDiv = document.createElement('div');
        tempDiv.innerHTML = certificateHTML;
        tempDiv.style.position = 'absolute';
        tempDiv.style.left = '-9999px';
        tempDiv.style.width = '1200px'; // Ancho fijo para mejor renderizado
        document.body.appendChild(tempDiv);

        // Esperar un momento para que el contenido se renderice
        await new Promise(resolve => setTimeout(resolve, 500));

        // Opciones para html2pdf
        const options = {
            margin: [10, 10, 10, 10],
            filename: `Certificado_${data.temaNombre.replace(/\s+/g, '_')}_${data.nombreCompleto.replace(/\s+/g, '_')}.pdf`,
            image: { type: 'jpeg', quality: 0.98 },
            html2canvas: {
                scale: 2,
                useCORS: true,
                letterRendering: true,
                logging: false,
                windowWidth: 1200
            },
            jsPDF: {
                unit: 'mm',
                format: 'a4',
                orientation: 'landscape'
            }
        };

        // Generar y descargar el PDF
        await html2pdf().set(options).from(tempDiv).save();

        // Limpiar el elemento temporal
        setTimeout(() => {
            if (tempDiv.parentNode) {
                document.body.removeChild(tempDiv);
            }
        }, 1000);

        console.log('✅ Certificado de nivel PDF generado y descargado exitosamente');

        // Mostrar mensaje de éxito
        showSuccessMessage('¡Certificado descargado! 🎉');

    } catch (error) {
        console.error('❌ Error al generar el PDF:', error);
        alert('Error al generar el certificado: ' + error.message);
    }
}

/**
 * Genera el HTML del certificado de nivel completado
 * @param {string} nombreCompleto - Nombre completo del usuario
 * @param {string} temaNombre - Nombre del tema/nivel
 * @param {string} fecha - Fecha de emisión
 * @param {number} nivelId - ID del nivel
 * @param {number} problemasCompletados - Número de problemas completados
 * @param {string} email - Email del usuario
 * @param {string} nombre - Nombre del usuario
 * @param {string} apellidoPaterno - Apellido paterno
 * @param {string} apellidoMaterno - Apellido materno
 * @returns {string} HTML del certificado
 */
function getLevelCertificateHTML(nombreCompleto, temaNombre, fecha, nivelId, problemasCompletados, email, nombre, apellidoPaterno, apellidoMaterno) {
    return `
<!DOCTYPE html>
<html lang="es">
<head>
    <meta charset="UTF-8">
    <style>
        * {
            margin: 0;
            padding: 0;
            box-sizing: border-box;
        }

        body {
            font-family: 'Georgia', serif;
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            display: flex;
            justify-content: center;
            align-items: center;
            padding: 20px;
        }

        .certificate-container {
            background: white;
            width: 100%;
            max-width: 900px;
            padding: 60px;
            border: 20px solid #2c3e50;
            border-image: linear-gradient(45deg, #667eea, #764ba2) 1;
            position: relative;
        }

        .certificate-border {
            border: 3px solid #f39c12;
            padding: 40px;
            position: relative;
        }

        .corner-decoration {
            position: absolute;
            width: 80px;
            height: 80px;
            border: 3px solid #f39c12;
        }

        .corner-decoration.top-left {
            top: -3px;
            left: -3px;
            border-right: none;
            border-bottom: none;
        }

        .corner-decoration.top-right {
            top: -3px;
            right: -3px;
            border-left: none;
            border-bottom: none;
        }

        .corner-decoration.bottom-left {
            bottom: -3px;
            left: -3px;
            border-right: none;
            border-top: none;
        }

        .corner-decoration.bottom-right {
            bottom: -3px;
            right: -3px;
            border-left: none;
            border-top: none;
        }

        .header {
            text-align: center;
            margin-bottom: 30px;
        }

        .logo {
            width: 100px;
            height: 100px;
            margin: 0 auto 20px;
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            border-radius: 50%;
            display: flex;
            align-items: center;
            justify-content: center;
            color: white;
            font-size: 48px;
            font-weight: bold;
        }

        .institution-name {
            font-size: 28px;
            color: #2c3e50;
            font-weight: bold;
            margin-bottom: 5px;
        }

        .institution-subtitle {
            font-size: 16px;
            color: #7f8c8d;
            margin-bottom: 10px;
        }

        .certificate-title {
            font-size: 42px;
            color: #667eea;
            text-transform: uppercase;
            letter-spacing: 4px;
            margin: 30px 0;
            font-weight: bold;
        }

        .certificate-text {
            text-align: center;
            font-size: 18px;
            color: #34495e;
            line-height: 1.8;
            margin: 30px 0;
        }

        .student-name {
            font-size: 36px;
            color: #2c3e50;
            font-weight: bold;
            margin: 30px 0;
            padding: 10px 0;
            border-bottom: 2px solid #f39c12;
            display: inline-block;
        }

        .achievement {
            margin: 40px 0;
            font-size: 18px;
            color: #34495e;
            line-height: 2;
            text-align: center;
        }

        .achievement strong {
            color: #667eea;
            font-size: 20px;
        }

        .level-info {
            background: linear-gradient(135deg, #f0f4ff 0%, #e0e7ff 100%);
            padding: 25px;
            border-radius: 10px;
            margin: 30px 0;
            text-align: center;
            border: 2px solid #667eea;
        }

        .level-info h3 {
            font-size: 26px;
            color: #667eea;
            margin-bottom: 20px;
            font-weight: bold;
        }

        .info-grid {
            display: flex;
            justify-content: space-around;
            gap: 20px;
            margin-top: 15px;
        }

        .info-item {
            display: flex;
            flex-direction: column;
            align-items: center;
            gap: 5px;
        }

        .info-label {
            font-size: 14px;
            color: #7f8c8d;
            font-weight: 600;
        }

        .info-value {
            font-size: 20px;
            color: #2c3e50;
            font-weight: bold;
        }

        .user-info {
            background: linear-gradient(135deg, #fff5f5 0%, #ffe0e0 100%);
            padding: 25px;
            border-radius: 10px;
            margin: 30px 0;
            border: 2px solid #f39c12;
        }

        .user-info h4 {
            font-size: 22px;
            color: #e74c3c;
            margin-bottom: 15px;
            text-align: center;
            font-weight: bold;
        }

        .user-details {
            display: flex;
            flex-direction: column;
            gap: 10px;
        }

        .detail-row {
            display: flex;
            justify-content: space-between;
            align-items: center;
            padding: 8px 0;
            border-bottom: 1px solid #f39c12;
        }

        .detail-row:last-child {
            border-bottom: none;
        }

        .detail-label {
            font-size: 15px;
            color: #7f8c8d;
            font-weight: 600;
        }

        .detail-value {
            font-size: 16px;
            color: #2c3e50;
            font-weight: bold;
        }

        .footer {
            display: flex;
            justify-content: space-around;
            margin-top: 60px;
            padding-top: 40px;
        }

        .signature-block {
            text-align: center;
            flex: 1;
            margin: 0 20px;
        }

        .signature-line {
            border-top: 2px solid #2c3e50;
            margin-bottom: 10px;
            padding-top: 10px;
        }

        .signature-name {
            font-weight: bold;
            color: #2c3e50;
            font-size: 16px;
        }

        .signature-title {
            color: #7f8c8d;
            font-size: 14px;
        }

        .date-location {
            text-align: center;
            margin-top: 30px;
            color: #7f8c8d;
            font-size: 14px;
        }

        .seal {
            position: absolute;
            bottom: 100px;
            right: 80px;
            width: 120px;
            height: 120px;
            border: 3px solid #e74c3c;
            border-radius: 50%;
            display: flex;
            align-items: center;
            justify-content: center;
            font-size: 14px;
            color: #e74c3c;
            font-weight: bold;
            text-align: center;
            transform: rotate(-15deg);
            opacity: 0.3;
        }
    </style>
</head>
<body>
    <div class="certificate-container">
        <div class="certificate-border">
            <div class="corner-decoration top-left"></div>
            <div class="corner-decoration top-right"></div>
            <div class="corner-decoration bottom-left"></div>
            <div class="corner-decoration bottom-right"></div>

            <div class="header">
                <div class="logo">🎓</div>
                <div class="institution-name">DORJA</div>
                <div class="institution-subtitle">Plataforma Educativa de Programación</div>
            </div>

            <div style="text-align: center;">
                <div class="certificate-title">Certificado de Nivel Completado</div>
                
                <div class="certificate-text">
                    Por medio del presente se certifica que
                </div>

                <div class="student-name">${nombreCompleto}</div>

                <div class="certificate-text">
                    Ha completado exitosamente el nivel
                </div>

                <div class="level-info">
                    <h3>${temaNombre}</h3>
                    <div class="info-grid">
                        <div class="info-item">
                            <span class="info-label">Categoría/Nivel:</span>
                            <span class="info-value">Nivel ${nivelId}</span>
                        </div>
                        <div class="info-item">
                            <span class="info-label">Ejercicios Completados:</span>
                            <span class="info-value">${problemasCompletados}/10</span>
                        </div>
                    </div>
                </div>

                <div class="user-info">
                    <h4>Datos del Estudiante</h4>
                    <div class="user-details">
                        ${nombre ? `<div class="detail-row"><span class="detail-label">Nombre:</span><span class="detail-value">${nombre}</span></div>` : ''}
                        ${apellidoPaterno ? `<div class="detail-row"><span class="detail-label">Apellido Paterno:</span><span class="detail-value">${apellidoPaterno}</span></div>` : ''}
                        ${apellidoMaterno ? `<div class="detail-row"><span class="detail-label">Apellido Materno:</span><span class="detail-value">${apellidoMaterno}</span></div>` : ''}
                        ${email ? `<div class="detail-row"><span class="detail-label">Email:</span><span class="detail-value">${email}</span></div>` : ''}
                    </div>
                </div>

                <div class="achievement">
                    <strong>🏆 Nivel Completado con Éxito 🏆</strong><br>
                    Demostrando dedicación, esfuerzo y las habilidades necesarias<br>
                    para avanzar en su camino de aprendizaje en programación.
                </div>

                <div class="certificate-text">
                    Este logro representa un paso importante en su desarrollo<br>
                    como programador en la plataforma Dorja.
                </div>
            </div>

            <div class="footer">
                <div class="signature-block">
                    <div class="signature-line">
                        <div class="signature-name">Plataforma Dorja</div>
                        <div class="signature-title">Sistema Educativo</div>
                    </div>
                </div>
            </div>

            <div class="date-location">
                ${fecha}
            </div>

            <div class="seal">
                SELLO<br>OFICIAL
            </div>
        </div>
    </div>
</body>
</html>
    `;
}

// Exportar las funciones para uso global
window.generateCertificatePDF = generateCertificatePDF;
window.generateLevelCertificatePDF = generateLevelCertificatePDF;
