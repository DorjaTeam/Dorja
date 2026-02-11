// SCRIPT PARA INYECTAR EL BOTÓN DE CERTIFICADO - SE EJECUTA AUTOMÁTICAMENTE
(function injectCertificateButton() {
  console.log('🚀 INICIANDO INYECTOR DE BOTÓN DE CERTIFICADO...');
  
  function crearBoton() {
    var sidebar = document.getElementById('problems-sidebar');
    if (!sidebar) {
      return false;
    }
    
    var existing = document.getElementById('certificate-panel-injected');
    if (existing) {
      existing.style.display = 'block';
      existing.style.visibility = 'visible';
      return true;
    }
    
    var panel = document.createElement('div');
    panel.id = 'certificate-panel-injected';
    panel.style.cssText = 'display:block !important;visibility:visible !important;margin:1rem;padding:1rem;background:#f3f4f6;border:2px solid #9ca3af;border-radius:0.5rem;text-align:center;position:relative;z-index:10000;opacity:1 !important';
    panel.innerHTML = '<h4 style="color:#166534;font-weight:bold;margin-bottom:0.5rem" id="cert-title-injected">Nivel en Progreso</h4><p style="color:#15803d;margin-bottom:1rem" id="cert-msg-injected">Completa 10 problemas para desbloquear el certificado</p><button id="cert-btn-injected" disabled style="width:100%;padding:0.75rem;background:#9ca3af;color:white;border-radius:0.5rem;border:none;cursor:not-allowed;font-weight:600;opacity:0.6">📄 Descargar Certificado PDF</button>';
    sidebar.appendChild(panel);
    console.log('✅ Botón creado e inyectado');
    return true;
  }
  
  function update() {
    if (!crearBoton()) return;
    
    var panel = document.getElementById('certificate-panel-injected');
    var btn = document.getElementById('cert-btn-injected');
    var msg = document.getElementById('cert-msg-injected');
    var title = document.getElementById('cert-title-injected');
    var pc = document.getElementById('problems-count');
    
    if (!panel || !btn) return;
    
    panel.style.display = 'block';
    panel.style.visibility = 'visible';
    
    if (pc && msg && title) {
      var txt = pc.textContent || '';
      var m = txt.match(/(\d+)\/(\d+)/);
      if (m) {
        var c = parseInt(m[1]);
        var tid = (window.problemsRenderer && window.problemsRenderer.currentTemaId) || 1;
        
        if (c >= 10) {
          panel.style.background = '#dcfce7';
          panel.style.borderColor = '#22c55e';
          title.textContent = '¡Nivel Completado!';
          msg.textContent = 'Has completado ' + c + ' problemas. ¡Felicidades!';
          btn.disabled = false;
          btn.style.background = 'linear-gradient(to right,#4f46e5,#7c3aed)';
          btn.style.cursor = 'pointer';
          btn.style.opacity = '1';
          btn.onclick = function() {
            if (window.problemsRenderer && window.problemsRenderer.generarCertificadoNivel) {
              window.problemsRenderer.generarCertificadoNivel(tid);
            } else {
              alert('Función de certificado no disponible');
            }
          };
        } else {
          panel.style.background = '#f3f4f6';
          panel.style.borderColor = '#9ca3af';
          title.textContent = 'Nivel en Progreso';
          msg.textContent = 'Completa ' + (10 - c) + ' problemas más para desbloquear el certificado';
          btn.disabled = true;
          btn.style.background = '#9ca3af';
          btn.style.cursor = 'not-allowed';
          btn.style.opacity = '0.6';
        }
      }
    }
  }
  
  // Ejecutar cuando el DOM esté listo
  if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', function() {
      crearBoton();
      setInterval(update, 300);
      update();
    });
  } else {
    crearBoton();
    setInterval(update, 300);
    update();
  }
  
  // También cuando la ventana carga
  window.addEventListener('load', function() {
    crearBoton();
    update();
  });
  
  console.log('✅ Inyector de certificado configurado');
})();




