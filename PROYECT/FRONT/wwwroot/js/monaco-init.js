document.addEventListener('DOMContentLoaded', () => {
    require.config({ paths: { 'vs': 'https://unpkg.com/monaco-editor@latest/min/vs' } });
    window.MonacoEnvironment = {
        getWorkerUrl: function (moduleId, label) {
            if (label === 'python') return './python.worker.bundle.js';
            return 'https://unpkg.com/monaco-editor@latest/min/vs/base/worker/workerMain.js';
        }
    };
    let editor;
    let currentLanguage = 'python';
    
    const pythonDefaultCode = `# Escribe tu solución aquí
def procesar_numeros(lista):
    suma_pares = 0
    producto_impares = 1
    
    if not lista:
        return (0, 1, None)
        
    mayor = lista[0]
    
    for num in lista:
        if num % 2 == 0:
            suma_pares += num
        else:
            producto_impares *= num
        
        if num > mayor:
            mayor = num
            
    return (suma_pares, producto_impares, mayor)

# Prueba tu función
numeros = [1, 2, 3, 4, 5, 9]
resultado = procesar_numeros(numeros)
print(f"Resultado para {numeros}: {resultado}")`;

    const csharpDefaultCode = `// Escribe tu solución aquí
using System;
using System.Collections.Generic;
using System.Linq;

public class Program
{
    public static void Main()
    {
        var numeros = new List<int> { 1, 2, 3, 4, 5, 9 };
        var resultado = ProcesarNumeros(numeros);
        Console.WriteLine($"Resultado para [{string.Join(", ", numeros)}]: {resultado}");
    }
    
    public static (int sumaPares, int productoImpares, int? mayor) ProcesarNumeros(List<int> lista)
    {
        if (lista == null || lista.Count == 0)
            return (0, 1, null);
            
        int sumaPares = 0;
        int productoImpares = 1;
        int mayor = lista[0];
        
        foreach (var num in lista)
        {
            if (num % 2 == 0)
                sumaPares += num;
            else
                productoImpares *= num;
                
            if (num > mayor)
                mayor = num;
        }
        
        return (sumaPares, productoImpares, mayor);
    }
}`;

    require(["vs/editor/editor.main"], function () {
        const getCurrentTheme = () => document.documentElement.classList.contains('dark') ? 'vs-dark' : 'vs-light';
        
        editor = monaco.editor.create(document.getElementById('editor'), {
            value: pythonDefaultCode,
            language: 'python',
            theme: getCurrentTheme(),
            automaticLayout: true,
            fontSize: 14,
            minimap: { enabled: false },
            scrollBeyondLastLine: false,
            renderLineHighlight: 'all',
            lineNumbers: 'on',
            wordWrap: 'on',
            // Enable IntelliSense
            quickSuggestions: true,
            suggestOnTriggerCharacters: true,
            acceptSuggestionOnEnter: 'on',
            tabCompletion: 'on',
            wordBasedSuggestions: 'allDocuments',
            // Additional IntelliSense settings
            parameterHints: { enabled: true },
            hover: { enabled: true },
            formatOnPaste: true,
            formatOnType: true
        });

        // Asignar el editor a window para que esté disponible globalmente
        window.monacoEditor = editor;
        window.currentLanguage = currentLanguage;

        const outputContent = document.getElementById('output-content');
        const runBtn = document.getElementById('run-btn');
        const verifyBtn = document.getElementById('verify-btn');
        const languageSelector = document.getElementById('language-selector');

        // Language selector handler
        languageSelector.addEventListener('change', (e) => {
            const newLanguage = e.target.value;
            const currentValue = editor.getValue();
            
            // Save current code if user wants to switch
            if (currentLanguage === 'python' && currentValue !== pythonDefaultCode) {
                // Could save to localStorage here
            }
            
            currentLanguage = newLanguage;
            window.currentLanguage = newLanguage;
            const monacoLanguage = newLanguage === 'csharp' ? 'csharp' : 'python';
            const defaultCode = newLanguage === 'csharp' ? csharpDefaultCode : pythonDefaultCode;
            
            monaco.editor.setModelLanguage(editor.getModel(), monacoLanguage);
            editor.setValue(defaultCode);
        });

        // El botón run-btn será manejado por problems-renderer.js
        // Solo configuramos el editor aquí
        
        const observer = new MutationObserver(() => { monaco.editor.setTheme(getCurrentTheme()); });
        observer.observe(document.documentElement, { attributes: true, attributeFilter: ['class'] });
    });
});