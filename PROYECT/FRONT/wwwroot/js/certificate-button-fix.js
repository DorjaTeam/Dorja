// Script para agregar botón de certificado
// Este archivo se puede cargar independientemente para agregar el botón

(function() {
  console.log('🔧 Script de certificado cargado');
  
  // Esperar a que el DOM esté completamente cargado
  function init() {
    console.log('🔧 Inicializando script de certificado');
    
    // Esperar un poco más para que problemsRenderer esté listo
    setTimeout(agregarBotonCertificado, 1000);
    setTimeout(agregarBotonCertificado, 3000);
    setTimeout(agregarBotonCertificado, 5000);
    
    // Configurar observers
    setupObservers();
    
    // Ejecutar cada 2 segundos
    setInterval(agregarBotonCertificado, 2000);
  }
  
  function setupObservers() {
    // Observer para el contador
    const problemsCount = document.getElementById('problems-count');
    if (problemsCount) {
      const obs = new MutationObserver(() => {
        setTimeout(agregarBotonCertificado, 500);
      });
      obs.observe(problemsCount, {
        childList: true,
        characterData: true,
        subtree: true
      });
      console.log('✅ Observer de contador configurado');
    }

    // También observar la lista de problemas
    const problemsList = document.getElementById('problems-list');
    if (problemsList) {
      const obs2 = new MutationObserver(() => {
        setTimeout(agregarBotonCertificado, 500);
      });
      obs2.observe(problemsList, {
        childList: true,
        subtree: true
      });
      console.log('✅ Observer de lista configurado');
    }
  }
  
  function agregarBotonCertificado() {
    try {
      const problemsCount = document.getElementById('problems-count');
      const problemsList = document.getElementById('problems-list');
      
      if (!problemsCount || !problemsList) {
        return;
      }

      const countText = problemsCount.textContent || '';
      const match = countText.match(/(\d+)\/(\d+)/);
      
      if (match) {
        const completados = parseInt(match[1]);
        
        if (completados >= 10) {
          // Obtener temaId
          let temaId = null;
          if (window.problemsRenderer && window.problemsRenderer.currentTemaId) {
            temaId = window.problemsRenderer.currentTemaId;
          } else {
            const temaActivo = document.querySelector('.tema.active');
            if (temaActivo) {
              temaId = temaActivo.dataset.temaId;
            }
          }

          if (!temaId) {
            console.log('⚠️ No se pudo obtener temaId');
            return;
          }
          
          const numTemaId = parseInt(temaId);
          if (isNaN(numTemaId)) return;

          // Verificar si ya existe
          if (document.getElementById(`certificate-btn-${numTemaId}`)) {
            return;
          }

          console.log(`🎯 AGREGANDO BOTÓN - Tema ${numTemaId}, Completados: ${completados}`);

          const panel = document.createElement('div');
          panel.id = `certificate-panel-${numTemaId}`;
          panel.style.cssText = 'margin-top: 1rem; padding: 1rem; background: linear-gradient(to right, #dcfce7, #d1fae5); border: 2px solid #22c55e; border-radius: 0.5rem; display: block !important;';
          
          panel.innerHTML = `
            <div style="text-align: center;">
              <div style="font-size: 2rem; margin-bottom: 0.75rem;">🎉</div>
              <h4 style="font-weight: bold; color: #166534; margin-bottom: 0.5rem; font-size: 1.125rem;">¡Nivel Completado!</h4>
              <p style="color: #15803d; margin-bottom: 1rem; font-size: 0.875rem;">Has completado ${completados} problemas. ¡Felicidades!</p>
              <button id="certificate-btn-${numTemaId}" 
                      style="width: 100%; padding: 0.75rem; background: linear-gradient(to right, #4f46e5, #7c3aed); color: white; font-weight: 600; border-radius: 0.5rem; border: none; cursor: pointer; display: flex; align-items: center; justify-content: center; gap: 0.5rem; font-size: 1rem;">
                📄 Descargar Certificado PDF
              </button>
            </div>
          `;

          problemsList.appendChild(panel);

          const btn = document.getElementById(`certificate-btn-${numTemaId}`);
          if (btn) {
            btn.onclick = function() {
              console.log(`Click en certificado tema ${numTemaId}`);
              if (window.problemsRenderer && window.problemsRenderer.generarCertificadoNivel) {
                window.problemsRenderer.generarCertificadoNivel(numTemaId);
              } else {
                alert('Generando certificado...');
              }
            };
            console.log(`✅ Botón agregado para tema ${numTemaId}`);
          }
        }
      }
    } catch (e) {
      console.error('Error agregando botón:', e);
    }
  }

  // Esperar a que problemsRenderer esté disponible
  function waitForProblemsRenderer() {
    if (window.problemsRenderer) {
      console.log('✅ problemsRenderer encontrado, inicializando...');
      init();
    } else {
      console.log('⏳ Esperando problemsRenderer...');
      setTimeout(waitForProblemsRenderer, 500);
    }
  }
  
  // Inicializar cuando el DOM esté listo
  if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', () => {
      setTimeout(waitForProblemsRenderer, 1000);
    });
  } else {
    // DOM ya está listo
    setTimeout(waitForProblemsRenderer, 1000);
  }
  
  // También inicializar después de delays adicionales por si acaso
  setTimeout(waitForProblemsRenderer, 3000);
  setTimeout(waitForProblemsRenderer, 5000);
})();

