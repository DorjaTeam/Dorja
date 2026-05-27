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
        @import url('https://fonts.googleapis.com/css2?family=Inter:wght@400;500;600&family=Outfit:wght@700;800;900&display=swap');

        * { margin: 0; padding: 0; box-sizing: border-box; }

        body {
            font-family: 'Inter', sans-serif;
            background: #ffffff;
            display: flex;
            justify-content: center;
            align-items: center;
            padding: 40px;
            color: #1a0f3c;
        }

        .certificate-container {
            background: #ffffff;
            width: 100%;
            max-width: 1050px;
            padding: 70px 80px;
            border-radius: 24px;
            position: relative;
            box-shadow: 0 0 0 1px rgba(73, 41, 164, 0.1);
            overflow: hidden;
        }

        .certificate-container::before {
            content: '';
            position: absolute;
            top: 0; left: 0; right: 0; height: 12px;
            background: linear-gradient(90deg, #4929a4, #8a5df5, #a855f7);
        }

        /* Watermark */
        .watermark {
            position: absolute;
            top: 50%; left: 50%;
            transform: translate(-50%, -50%);
            font-size: 200px;
            color: rgba(73, 41, 164, 0.03);
            font-family: 'Outfit', sans-serif;
            font-weight: 900;
            z-index: 0;
            white-space: nowrap;
            pointer-events: none;
        }

        .content-wrapper { position: relative; z-index: 1; }

        .header {
            display: flex;
            align-items: center;
            justify-content: space-between;
            margin-bottom: 60px;
            border-bottom: 1px solid rgba(73, 41, 164, 0.1);
            padding-bottom: 30px;
        }

        .brand {
            display: flex;
            align-items: center;
            gap: 20px;
        }

        .logo-box {
            width: 64px; height: 64px;
            background: linear-gradient(135deg, #4929a4, #8a5df5);
            border-radius: 16px;
            display: flex;
            align-items: center;
            justify-content: center;
            color: white;
            font-size: 32px;
            font-weight: 900;
            font-family: 'Outfit', sans-serif;
            box-shadow: 0 10px 20px rgba(73, 41, 164, 0.2);
        }

        .institution {
            display: flex;
            flex-direction: column;
        }

        .institution-name {
            font-family: 'Outfit', sans-serif;
            font-size: 28px;
            color: #1a0f3c;
            font-weight: 900;
            letter-spacing: -0.5px;
        }

        .institution-sub {
            font-size: 14px;
            color: #64748b;
            font-weight: 500;
            text-transform: uppercase;
            letter-spacing: 1px;
        }

        .cert-id {
            font-size: 13px;
            color: #94a3b8;
            font-family: monospace;
            text-align: right;
        }

        .main-content {
            text-align: center;
        }

        .certificate-title {
            font-family: 'Outfit', sans-serif;
            font-size: 52px;
            color: #1a0f3c;
            letter-spacing: -1.5px;
            font-weight: 900;
            margin-bottom: 30px;
        }

        .cert-text {
            font-size: 18px;
            color: #64748b;
            margin-bottom: 20px;
        }

        .student-name {
            font-family: 'Outfit', sans-serif;
            font-size: 48px;
            color: #4929a4;
            font-weight: 800;
            margin: 20px 0;
            padding-bottom: 10px;
        }

        .achievement-box {
            margin: 40px auto;
            max-width: 600px;
            padding: 30px;
            background: rgba(138, 93, 245, 0.04);
            border-radius: 16px;
            border: 1px solid rgba(138, 93, 245, 0.1);
        }

        .achievement-box strong {
            display: block;
            font-family: 'Outfit', sans-serif;
            font-size: 24px;
            color: #1a0f3c;
            margin-bottom: 12px;
        }

        .achievement-box p {
            font-size: 16px;
            color: #475569;
            line-height: 1.6;
        }

        .footer {
            display: flex;
            justify-content: space-between;
            align-items: flex-end;
            margin-top: 80px;
        }

        .signature-section {
            text-align: left;
        }

        .signature-line {
            width: 240px;
            border-bottom: 2px solid #1a0f3c;
            margin-bottom: 12px;
            height: 40px;
        }

        .signature-name {
            font-family: 'Outfit', sans-serif;
            font-size: 18px;
            font-weight: 800;
            color: #1a0f3c;
        }

        .signature-role {
            font-size: 14px;
            color: #64748b;
        }

        .date-section {
            text-align: right;
        }

        .date-value {
            font-family: 'Outfit', sans-serif;
            font-size: 18px;
            font-weight: 800;
            color: #1a0f3c;
        }

        .date-label {
            font-size: 14px;
            color: #64748b;
        }
        
        .seal {
            position: absolute;
            bottom: 60px;
            right: 60px;
            width: 140px;
            height: 140px;
            border-radius: 50%;
            border: 2px dashed rgba(73, 41, 164, 0.2);
            display: flex;
            align-items: center;
            justify-content: center;
            transform: rotate(-15deg);
        }
        .seal-inner {
            width: 120px;
            height: 120px;
            background: rgba(73, 41, 164, 0.05);
            border-radius: 50%;
            display: flex;
            align-items: center;
            justify-content: center;
            text-align: center;
            font-family: 'Outfit', sans-serif;
            font-weight: 800;
            font-size: 14px;
            color: #4929a4;
            text-transform: uppercase;
            line-height: 1.2;
        }
    </style>
</head>
<body>
    <div class="certificate-container">
        <div class="watermark">DORJA</div>
        
        <div class="content-wrapper">
            <div class="header">
                <div class="brand">
                    <div class="logo-box">D</div>
                    <div class="institution">
                        <div class="institution-name">DORJA</div>
                        <div class="institution-sub">Plataforma Educativa de Programación</div>
                    </div>
                </div>
                <div class="cert-id">
                    ID: ${Math.random().toString(36).substr(2, 9).toUpperCase()}<br>
                    EMISIÓN: ${fecha}
                </div>
            </div>

            <div class="main-content">
                <div class="certificate-title">Certificado de Logro</div>
                
                <div class="cert-text">Por medio del presente se certifica que</div>
                
                <div class="student-name">${nombreCompleto}</div>
                
                <div class="cert-text">Ha completado exitosamente</div>
                
                <div class="achievement-box">
                    <strong>Su Primer Ejercicio de Programación</strong>
                    <p>Demostrando dedicación, esfuerzo y las habilidades necesarias para iniciar su camino en el desarrollo de software.</p>
                </div>
            </div>

            <div class="footer">
                <div class="signature-section">
                    <div class="signature-line"></div>
                    <div class="signature-name">Plataforma Dorja</div>
                    <div class="signature-role">Dirección Académica</div>
                </div>
                
                <div class="date-section">
                    <div class="date-value">${fecha}</div>
                    <div class="date-label">Fecha de Expedición</div>
                </div>
            </div>

            <div class="seal">
                <div class="seal-inner">Sello<br>Oficial<br>Dorja</div>
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
        const worker = html2pdf().set(options).from(tempDiv);
        
        // Descargar localmente
        await worker.save();
        
        // Obtener Base64 para guardarlo en BD
        try {
            const base64Pdf = await worker.outputPdf('datauristring');
            if (window.api && window.api.saveCertificate && data.userId && data.temaId) {
                console.log('📤 Subiendo certificado al servidor...');
                await window.api.saveCertificate(data.userId, data.temaId, base64Pdf);
                console.log('✅ Certificado guardado en base de datos');
            }
        } catch (uploadError) {
            console.error('❌ Error guardando certificado en servidor:', uploadError);
        }

        // Limpiar el elemento temporal
        setTimeout(() => {
            if (tempDiv.parentNode) {
                document.body.removeChild(tempDiv);
            }
        }, 1000);

        console.log('✅ Certificado de nivel PDF generado y descargado exitosamente');

        // Mostrar mensaje de éxito
        showSuccessMessage('¡Certificado generado exitosamente! 🎉');

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
        @import url('https://fonts.googleapis.com/css2?family=Inter:wght@400;500;600&family=Outfit:wght@700;800;900&display=swap');

        * { margin: 0; padding: 0; box-sizing: border-box; }

        body {
            font-family: 'Inter', sans-serif;
            background: #ffffff;
            display: flex;
            justify-content: center;
            align-items: center;
            padding: 40px;
            color: #1a0f3c;
        }

        .certificate-container {
            background: #ffffff;
            width: 100%;
            max-width: 1050px;
            padding: 70px 80px;
            border-radius: 24px;
            position: relative;
            box-shadow: 0 0 0 1px rgba(73, 41, 164, 0.1);
            overflow: hidden;
        }

        .certificate-container::before {
            content: '';
            position: absolute;
            top: 0; left: 0; right: 0; height: 12px;
            background: linear-gradient(90deg, #4929a4, #8a5df5, #a855f7);
        }

        /* Watermark */
        .watermark {
            position: absolute;
            top: 50%; left: 50%;
            transform: translate(-50%, -50%);
            font-size: 200px;
            color: rgba(73, 41, 164, 0.03);
            font-family: 'Outfit', sans-serif;
            font-weight: 900;
            z-index: 0;
            white-space: nowrap;
            pointer-events: none;
        }

        .content-wrapper { position: relative; z-index: 1; }

        .header {
            display: flex;
            align-items: center;
            justify-content: space-between;
            margin-bottom: 50px;
            border-bottom: 1px solid rgba(73, 41, 164, 0.1);
            padding-bottom: 20px;
        }

        .brand {
            display: flex;
            align-items: center;
            gap: 20px;
        }

        .logo-box {
            width: 64px; height: 64px;
            background: linear-gradient(135deg, #4929a4, #8a5df5);
            border-radius: 16px;
            display: flex;
            align-items: center;
            justify-content: center;
            color: white;
            font-size: 32px;
            font-weight: 900;
            font-family: 'Outfit', sans-serif;
            box-shadow: 0 10px 20px rgba(73, 41, 164, 0.2);
        }

        .institution {
            display: flex;
            flex-direction: column;
        }

        .institution-name {
            font-family: 'Outfit', sans-serif;
            font-size: 28px;
            color: #1a0f3c;
            font-weight: 900;
            letter-spacing: -0.5px;
        }

        .institution-sub {
            font-size: 14px;
            color: #64748b;
            font-weight: 500;
            text-transform: uppercase;
            letter-spacing: 1px;
        }

        .cert-id {
            font-size: 13px;
            color: #94a3b8;
            font-family: monospace;
            text-align: right;
        }

        .main-content {
            text-align: center;
        }

        .certificate-title {
            font-family: 'Outfit', sans-serif;
            font-size: 46px;
            color: #1a0f3c;
            letter-spacing: -1.5px;
            font-weight: 900;
            margin-bottom: 20px;
        }

        .cert-text {
            font-size: 18px;
            color: #64748b;
            margin-bottom: 10px;
        }

        .student-name {
            font-family: 'Outfit', sans-serif;
            font-size: 42px;
            color: #4929a4;
            font-weight: 800;
            margin: 10px 0;
            padding-bottom: 5px;
        }

        /* Grid layout for detailed info */
        .info-panels {
            display: grid;
            grid-template-columns: 1fr 1fr;
            gap: 20px;
            margin: 30px auto;
            max-width: 800px;
            text-align: left;
        }

        .info-card {
            background: rgba(138, 93, 245, 0.04);
            border-radius: 16px;
            border: 1px solid rgba(138, 93, 245, 0.1);
            padding: 24px;
        }

        .info-card h4 {
            font-family: 'Outfit', sans-serif;
            color: #1a0f3c;
            font-size: 18px;
            margin-bottom: 16px;
            font-weight: 800;
        }

        .detail-row {
            display: flex;
            justify-content: space-between;
            align-items: center;
            padding: 8px 0;
            border-bottom: 1px solid rgba(73, 41, 164, 0.05);
        }
        .detail-row:last-child { border-bottom: none; }

        .detail-label {
            font-size: 14px;
            color: #64748b;
            font-weight: 500;
        }

        .detail-value {
            font-size: 15px;
            color: #1a0f3c;
            font-weight: 700;
        }

        .achievement-box {
            margin: 20px auto;
            max-width: 600px;
            text-align: center;
        }

        .achievement-box strong {
            display: block;
            font-family: 'Outfit', sans-serif;
            font-size: 20px;
            color: #4929a4;
            margin-bottom: 8px;
        }

        .achievement-box p {
            font-size: 15px;
            color: #475569;
            line-height: 1.6;
        }

        .footer {
            display: flex;
            justify-content: space-between;
            align-items: flex-end;
            margin-top: 50px;
        }

        .signature-section {
            text-align: left;
        }

        .signature-line {
            width: 240px;
            border-bottom: 2px solid #1a0f3c;
            margin-bottom: 12px;
            height: 40px;
        }

        .signature-name {
            font-family: 'Outfit', sans-serif;
            font-size: 18px;
            font-weight: 800;
            color: #1a0f3c;
        }

        .signature-role {
            font-size: 14px;
            color: #64748b;
        }

        .date-section {
            text-align: right;
        }

        .date-value {
            font-family: 'Outfit', sans-serif;
            font-size: 18px;
            font-weight: 800;
            color: #1a0f3c;
        }

        .date-label {
            font-size: 14px;
            color: #64748b;
        }
        
        .seal {
            position: absolute;
            bottom: 60px;
            right: 60px;
            width: 140px;
            height: 140px;
            border-radius: 50%;
            border: 2px dashed rgba(73, 41, 164, 0.2);
            display: flex;
            align-items: center;
            justify-content: center;
            transform: rotate(-15deg);
        }
        .seal-inner {
            width: 120px;
            height: 120px;
            background: rgba(73, 41, 164, 0.05);
            border-radius: 50%;
            display: flex;
            align-items: center;
            justify-content: center;
            text-align: center;
            font-family: 'Outfit', sans-serif;
            font-weight: 800;
            font-size: 14px;
            color: #4929a4;
            text-transform: uppercase;
            line-height: 1.2;
        }
    </style>
</head>
<body>
    <div class="certificate-container">
        <div class="watermark">DORJA</div>
        
        <div class="content-wrapper">
            <div class="header">
                <div class="brand">
                    <div class="logo-box">D</div>
                    <div class="institution">
                        <div class="institution-name">DORJA</div>
                        <div class="institution-sub">Plataforma Educativa de Programación</div>
                    </div>
                </div>
                <div class="cert-id">
                    ID: ${Math.random().toString(36).substr(2, 9).toUpperCase()}<br>
                    EMISIÓN: ${fecha}
                </div>
            </div>

            <div class="main-content">
                <div class="certificate-title">Certificado de Nivel Completado</div>
                <div class="cert-text">Por medio del presente se certifica que</div>
                <div class="student-name">${nombreCompleto}</div>
                <div class="cert-text">Ha completado exitosamente el nivel</div>
                
                <div class="info-panels">
                    <div class="info-card">
                        <h4>Módulo Completado</h4>
                        <div class="detail-row">
                            <span class="detail-label">Tema:</span>
                            <span class="detail-value">${temaNombre}</span>
                        </div>
                        <div class="detail-row">
                            <span class="detail-label">Nivel:</span>
                            <span class="detail-value">${nivelId}</span>
                        </div>
                        <div class="detail-row">
                            <span class="detail-label">Ejercicios:</span>
                            <span class="detail-value">${problemasCompletados}/10</span>
                        </div>
                    </div>
                    
                    <div class="info-card">
                        <h4>Datos del Estudiante</h4>
                        ${nombre ? \`<div class="detail-row"><span class="detail-label">Nombre:</span><span class="detail-value">\${nombre}</span></div>\` : ''}
                        ${apellidoPaterno ? \`<div class="detail-row"><span class="detail-label">Apellido Paterno:</span><span class="detail-value">\${apellidoPaterno}</span></div>\` : ''}
                        ${apellidoMaterno ? \`<div class="detail-row"><span class="detail-label">Apellido Materno:</span><span class="detail-value">\${apellidoMaterno}</span></div>\` : ''}
                        ${email ? \`<div class="detail-row"><span class="detail-label">Email:</span><span class="detail-value">\${email}</span></div>\` : ''}
                    </div>
                </div>

                <div class="achievement-box">
                    <strong>Nivel Completado con Éxito</strong>
                    <p>Demostrando dedicación, esfuerzo y las habilidades necesarias para avanzar en su camino de aprendizaje en programación.</p>
                </div>
            </div>

            <div class="footer">
                <div class="signature-section">
                    <div class="signature-line"></div>
                    <div class="signature-name">Plataforma Dorja</div>
                    <div class="signature-role">Dirección Académica</div>
                </div>
                
                <div class="date-section">
                    <div class="date-value">${fecha}</div>
                    <div class="date-label">Fecha de Expedición</div>
                </div>
            </div>

            <div class="seal">
                <div class="seal-inner">Sello<br>Oficial<br>Dorja</div>
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

/**
 * Genera el certificado en PDF y lo retorna como string Base64 (data URI)
 * @param {Object} userData - Datos del usuario
 * @returns {Promise<string>} Base64 data URI string del PDF
 */
window.generateCertificatePDFAsBase64 = async function (userData) {
    try {
        console.log('🎓 Generando certificado PDF como Base64 para:', userData);

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
        tempDiv.style.width = '1200px';
        document.body.appendChild(tempDiv);

        // Esperar un momento para que el contenido se renderice
        await new Promise(resolve => setTimeout(resolve, 500));

        // Opciones para html2pdf
        const options = {
            margin: 0,
            image: { type: 'jpeg', quality: 0.95 },
            html2canvas: {
                scale: 1.5,
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

        // Generar el PDF como data URL string
        const base64Pdf = await html2pdf().from(tempDiv).set(options).outputPdf('datauristring');

        // Limpiar el elemento temporal
        if (tempDiv.parentNode) {
            document.body.removeChild(tempDiv);
        }

        console.log('✅ Certificado PDF Base64 generado exitosamente');
        return base64Pdf;

    } catch (error) {
        console.error('❌ Error al generar el PDF Base64:', error);
        throw error;
    }
};
