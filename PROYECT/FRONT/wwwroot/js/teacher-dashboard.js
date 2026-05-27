/**
 * teacher-dashboard.js
 * Lógica del dashboard exclusivo para maestros en Dorja.
 */

'use strict';

// ── State ────────────────────────────────────────────────────────────────────
const TeacherState = {
    // Utilities for avatars
    getAvatarColor: (id) => {
        const colors = ['#6366f1', '#8b5cf6', '#ec4899', '#f43f5e', '#f97316', '#10b981', '#0ea5e9', '#3b82f6', '#14b8a6'];
        return colors[(id || 0) % colors.length];
    },
    getInitials: (nombre, apellido) => {
        return ((nombre?.[0] ?? '') + (apellido?.[0] ?? '')).toUpperCase() || 'U';
    }
};

const State = {
    teacherId:   null,
    teacherName: '',
    students:    [],         // raw list from API
    filtered:    [],         // after search filter
    progress:    {},         // { [studentId]: progressSummary }
    grades:      {},         // { [studentId]: { valor, comentario } }
    activeModal: null,       // currently open student id
};

// ── Avatar colours pool ───────────────────────────────────────────────────────
const AVATAR_COLORS = [
    '#6366f1','#8b5cf6','#ec4899','#f59e0b',
    '#10b981','#3b82f6','#ef4444','#14b8a6',
];
// Removing local initials function since it's in renderer.js

// ── Toast ────────────────────────────────────────────────----------------─────
function showToast(msg, type = 'success') {
    const container = document.getElementById('toast-container');
    if (!container) return;
    const t = document.createElement('div');
    t.className = `toast ${type}`;
    let icon = '✅';
    if (type === 'error') icon = '❌';
    if (type === 'info') icon = 'ℹ️';
    t.innerHTML = `<span>${icon}</span><span>${msg}</span>`;
    container.appendChild(t);
    setTimeout(() => {
        t.style.opacity = '0';
        t.style.transform = 'translateX(60px)';
        t.style.transition = 'all 0.3s ease';
        setTimeout(() => t.remove(), 300);
    }, 3500);
}

// ── Helpers ───────────────────────────────────────────────────────────────────
function formatDate(dateStr) {
    if (!dateStr) return '—';
    try {
        const d = new Date(dateStr);
        return d.toLocaleDateString('es-MX', { day: '2-digit', month: 'short', year: 'numeric' });
    } catch { return '—'; }
}

function isRecentlyActive(dateStr) {
    if (!dateStr) return false;
    try {
        const d = new Date(dateStr);
        const diffDays = (Date.now() - d.getTime()) / (1000 * 60 * 60 * 24);
        return diffDays <= 7;
    } catch { return false; }
}

// ── Guard: verify session is maestro ─────────────────────────────────────────
function checkAuth() {
    const userId = sessionStorage.getItem('userId');
    const rol    = sessionStorage.getItem('userRol');

    if (!userId) {
        window.location.href = 'login.html';
        return false;
    }
    if (rol && rol !== 'maestro') {
        window.location.href = 'home.html';
        return false;
    }

    State.teacherId   = parseInt(userId, 10);
    State.teacherName = sessionStorage.getItem('userName') || sessionStorage.getItem('username') || 'Maestro';
    return true;
}

// ── Init teacher UI ────────────────────────────────────────────────────────────
function initTeacherUI() {
    const nameEl   = document.getElementById('teacher-name');
    const avatarEl = document.getElementById('teacher-avatar');
    if (nameEl)   nameEl.textContent = State.teacherName;
    if (avatarEl) avatarEl.textContent = State.teacherName[0]?.toUpperCase() ?? 'M';
}

// ── Backend readiness check ───────────────────────────────────────────────────
async function waitForBackend(maxRetries = 15, intervalMs = 1500) {
    for (let i = 0; i < maxRetries; i++) {
        try {
            const result = await window.api._makeRequest('/Users');
            if (result !== undefined) return true;
        } catch (e) {
            console.log(`⏳ Backend no disponible aún (intento ${i + 1}/${maxRetries})...`);
        }
        await new Promise(r => setTimeout(r, intervalMs));
    }
    return false;
}

// ── Load students ─────────────────────────────────────────────────────────────
async function loadStudents() {
    const tbody = document.getElementById('students-tbody');
    try {
        if (tbody) {
            tbody.innerHTML = `
                <tr>
                    <td colspan="7">
                        <div class="empty-state" style="padding: 40px 20px;">
                            <div class="icon pulse-glow" style="font-size: 2rem; color: var(--accent); margin-bottom: 16px;"><i class="fas fa-circle-notch fa-spin"></i></div>
                            <p style="font-size: 1.1rem; font-weight: 600; color: var(--text);">Sincronizando base de datos...</p>
                            <p style="font-size: 0.85rem; color: var(--text-muted); margin-top: 8px;">Cargando estudiantes y progreso en tiempo real</p>
                        </div>
                    </td>
                </tr>
                <tr><td colspan="7" style="padding: 0;"><div class="skeleton-box skeleton-text" style="height: 48px; margin: 4px;"></div></td></tr>
                <tr><td colspan="7" style="padding: 0;"><div class="skeleton-box skeleton-text" style="height: 48px; margin: 4px;"></div></td></tr>
            `;
        }

        // Wait for backend to be ready before loading data
        const backendReady = await waitForBackend();
        if (!backendReady) {
            throw new Error('El servidor no respondió después de varios intentos.');
        }

        // 1. Fetch students list
        const students = await window.api.getStudents();
        State.students = Array.isArray(students) ? students : [];
        State.filtered = [...State.students];

        // 2. Fetch teacher's grades
        try {
            const gradesArr = await window.api.getCalificacionesByMaestro(State.teacherId);
            State.grades = {};
            if (Array.isArray(gradesArr)) {
                gradesArr.forEach(g => {
                    State.grades[g.estudianteId || g.EstudianteId] = {
                        valor:      g.valor      ?? g.Valor      ?? 0,
                        comentario: g.comentario ?? g.Comentario ?? '',
                    };
                });
            }
        } catch (gradeErr) {
            console.warn('⚠ No se pudieron cargar calificaciones:', gradeErr);
            State.grades = {};
        }

        // 3. Fetch progress summaries in parallel (limit concurrency)
        State.progress = {};
        await Promise.allSettled(State.students.map(async s => {
            try {
                const p = await window.api.getStudentProgressSummary(s.id ?? s.Id);
                State.progress[s.id ?? s.Id] = p;
            } catch { /* ignore per-student errors */ }
        }));

        // 4. Render
        renderTable();
        updateMetrics();

    } catch (err) {
        console.error('Error loading students:', err);
        showToast('Error de conexión al cargar estudiantes', 'error');
        if (tbody) {
            tbody.innerHTML = `<tr><td colspan="7">
                <div class="empty-state" style="padding: 40px;">
                    <div class="icon" style="color: var(--danger); font-size: 2.5rem; margin-bottom: 16px;"><i class="fas fa-exclamation-triangle"></i></div>
                    <p style="font-size: 1.1rem; font-weight: 600; color: var(--text);">Fallo al cargar los estudiantes</p>
                    <p style="font-size: 0.85rem; color: var(--text-muted); margin-top: 8px; max-width: 400px; margin-left: auto; margin-right: auto;">
                        ${err.message || 'Verifica que el servidor esté activo y que tengas una conexión a internet estable.'}
                    </p>
                    <button class="btn btn-primary hover-lift" style="margin-top: 24px;" onclick="loadStudents()"><i class="fas fa-sync-alt" style="margin-right:8px;"></i> Reintentar conexión</button>
                </div>
            </td></tr>`;
        }
    }
}

// ── Render table ──────────────────────────────────────────────────────────────
function renderTable() {
    const tbody = document.getElementById('students-tbody');
    if (!tbody) return;

    if (State.filtered.length === 0) {
        tbody.innerHTML = `<tr><td colspan="7">
            <div class="empty-state" style="padding: 60px 20px;">
                <div class="icon" style="font-size: 3rem; color: rgba(255,255,255,0.1); margin-bottom: 16px;"><i class="fas fa-users-slash"></i></div>
                <p style="font-size: 1.1rem; font-weight: 600; color: var(--text);">Aún no hay estudiantes asignados</p>
                <p style="font-size: 0.85rem; color: var(--text-muted); margin-top: 8px;">Cuando los estudiantes se registren aparecerán aquí automáticamente.</p>
            </div>
        </td></tr>`;
        return;
    }

    tbody.innerHTML = State.filtered.map(s => {
        const sid     = s.id ?? s.Id;
        const prog    = State.progress[sid];
        const pct     = prog?.completionPercentage ?? 0;
        const pts     = prog?.puntosTotales        ?? s.puntosTotales ?? s.PuntosTotales ?? 0;
        const nivel   = prog?.nivelActual          ?? s.nivelActual   ?? s.NivelActual   ?? 1;
        const ultima  = prog?.ultimaConexion       ?? s.ultimaConexion?? s.UltimaConexion;
        const grade   = State.grades[sid];
        const nombre  = s.nombre       ?? s.Nombre       ?? '';
        const apPat   = s.apellidoPaterno ?? s.ApellidoPaterno ?? '';
        const username= s.username     ?? s.Username     ?? '';
        const active  = isRecentlyActive(ultima);
        const color   = TeacherState.getAvatarColor(sid);
        const inits   = TeacherState.getInitials(nombre, apPat);
        const gradeVal= grade ? grade.valor.toFixed(1) : '—';
        const gradeBadgeClass = !grade ? 'badge-grade-none' : (grade.valor >= 6 ? 'badge-grade-pass' : 'badge-grade-fail');

        return `<tr onclick="openModal(${sid})" title="Ver perfil completo" style="cursor: pointer;">
            <td>
                <div class="student-cell">
                    <div class="student-avatar" style="background:${color}">${inits}</div>
                    <div>
                        <div class="student-name">${nombre} ${apPat}</div>
                        <div class="student-username">@${username}</div>
                    </div>
                </div>
            </td>
            <td><span class="badge-level">🏅 Nivel ${nivel}</span></td>
            <td>${pts.toLocaleString('es-MX')} xp</td>
            <td>
                <div class="progress-bar-wrap" style="width: 120px;">
                    <div class="progress-bar-bg" style="height: 6px;">
                        <div class="progress-bar-fill" style="width:${pct}%"></div>
                    </div>
                    <span class="progress-label">${pct}%</span>
                </div>
            </td>
            <td>
                <div>
                    <span class="status-chip ${active ? 'active' : 'inactive'}">
                        <span class="dot"></span>
                        ${active ? 'Activo' : 'Inactivo'}
                    </span>
                    <div style="font-size:0.72rem;color:var(--text-muted);margin-top:3px">${formatDate(ultima)}</div>
                </div>
            </td>
            <td>
                <span class="${gradeBadgeClass}">
                    ${gradeVal}
                </span>
            <td onclick="event.stopPropagation()">
                <button class="btn btn-ghost btn-sm" onclick="openModal(${sid})">Ver perfil</button>
            </td>
        </tr>`;
    }).join('');
}

// ── Update global metrics ─────────────────────────────────────────────────────
function updateMetrics() {
    const total = State.students.length;
    document.getElementById('metric-total').textContent = total;
    document.getElementById('metric-total-sub').textContent =
        `${total === 1 ? '1 estudiante' : `${total} estudiantes`} registrado${total === 1 ? '' : 's'}`;

    if (total === 0) {
        document.getElementById('metric-avg').textContent = '0%';
        document.getElementById('metric-pts').textContent = '0';
        document.getElementById('metric-grades').textContent = '0';
        return;
    }

    const pcts = State.students.map(s => {
        const sid = s.id ?? s.Id;
        return State.progress[sid]?.completionPercentage ?? 0;
    });
    const avgPct = pcts.reduce((a, b) => a + b, 0) / total;
    document.getElementById('metric-avg').textContent = avgPct.toFixed(1) + '%';

    const pts = State.students.map(s => {
        const sid = s.id ?? s.Id;
        return State.progress[sid]?.puntosTotales ?? s.puntosTotales ?? s.PuntosTotales ?? 0;
    });
    const avgPts = Math.round(pts.reduce((a, b) => a + b, 0) / total);
    document.getElementById('metric-pts').textContent = avgPts.toLocaleString('es-MX');

    document.getElementById('metric-grades').textContent = Object.keys(State.grades).length;
}

// ── Filter students by search ────────────────────────────────────────────────
function filterStudents(query) {
    const q = query.toLowerCase().trim();
    if (!q) {
        State.filtered = [...State.students];
    } else {
        State.filtered = State.students.filter(s => {
            const nombre    = (s.nombre    ?? s.Nombre    ?? '').toLowerCase();
            const apPat     = (s.apellidoPaterno ?? s.ApellidoPaterno ?? '').toLowerCase();
            const apMat     = (s.apellidoMaterno ?? s.ApellidoMaterno ?? '').toLowerCase();
            const username  = (s.username  ?? s.Username  ?? '').toLowerCase();
            return nombre.includes(q) || apPat.includes(q) || apMat.includes(q) || username.includes(q);
        });
    }
    renderTable();
}

// ── Modal ─────────────────────────────────────────────────────────────────────
async function openModal(studentId) {
    State.activeModal = studentId;
    const s     = State.students.find(x => (x.id ?? x.Id) === studentId);
    const prog  = State.progress[studentId];
    const grade = State.grades[studentId];
    if (!s) return;

    const nombre   = `${s.nombre ?? s.Nombre ?? ''} ${s.apellidoPaterno ?? s.ApellidoPaterno ?? ''}`.trim();
    const username = s.username ?? s.Username ?? '';
    const nivel    = prog?.nivelActual          ?? s.nivelActual   ?? 1;
    const puntos   = prog?.puntosTotales        ?? s.puntosTotales ?? 0;
    const completados = prog?.completedCount    ?? 0;
    const pct      = prog?.completionPercentage ?? 0;
    const streak   = prog?.streak               ?? 0;

    document.getElementById('modal-student-name').textContent    = nombre || 'Estudiante';
    document.getElementById('modal-student-username').textContent = `@${username}`;
    document.getElementById('modal-nivel').textContent            = nivel;
    document.getElementById('modal-puntos').textContent           = puntos.toLocaleString('es-MX');
    document.getElementById('modal-completados').textContent      = completados;
    document.getElementById('modal-racha').textContent            = `${streak}🔥`;
    document.getElementById('modal-progress-bar').style.width     = `${pct}%`;
    document.getElementById('modal-progress-pct').textContent     = `${pct}%`;

    // Render detailed topics progress
    const topicsContainer = document.getElementById('modal-topics-progress');
    if (topicsContainer) {
        if (prog && prog.topicsProgress && prog.topicsProgress.length > 0) {
            topicsContainer.innerHTML = prog.topicsProgress.map(t => `
                <div style="margin-bottom: 8px;">
                    <div style="display: flex; justify-content: space-between; font-size: 0.8rem; margin-bottom: 2px;">
                        <span>Tema ${t.temaId}</span>
                        <span>${t.completados}/${t.total} (${t.porcentaje.toFixed(1)}%)</span>
                    </div>
                    <div class="progress-bar-bg" style="height:6px; background-color: var(--glass-border);">
                        <div class="progress-bar-fill" style="width:${t.porcentaje}%; background-color: var(--primary);"></div>
                    </div>
                </div>
            `).join('');
        } else {
            topicsContainer.innerHTML = `<div style="font-size: 0.8rem; color: var(--text-muted);">Sin datos detallados.</div>`;
        }
    }

    // Pre-fill grade fields
    document.getElementById('modal-grade-value').value   = grade ? grade.valor   : '';
    document.getElementById('modal-grade-comment').value = grade ? grade.comentario : '';

    // Open modal
    const overlay = document.getElementById('student-modal');
    if (overlay) overlay.classList.add('open');
}

function closeModal(event) {
    if (event.target === document.getElementById('student-modal')) {
        closeModalDirect();
    }
}

function closeModalDirect() {
    const overlay = document.getElementById('student-modal');
    if (overlay) overlay.classList.remove('open');
    State.activeModal = null;
}

// ── Save grade ────────────────────────────────────────────────────────────────
async function saveGrade() {
    const studentId = State.activeModal;
    if (!studentId) return;

    const valorRaw  = document.getElementById('modal-grade-value').value;
    const comentario = document.getElementById('modal-grade-comment').value.trim();

    if (valorRaw === '' || valorRaw === null) {
        showToast('Por favor ingresa una calificación.', 'error');
        return;
    }

    const valor = parseFloat(valorRaw);
    if (isNaN(valor) || valor < 0 || valor > 10) {
        showToast('La calificación debe ser un número entre 0 y 10.', 'error');
        return;
    }

    const btn = document.getElementById('modal-save-btn');
    if (btn) { btn.disabled = true; btn.textContent = 'Guardando…'; }

    try {
        const result = await window.api.saveCalificacion({
            maestroId:    State.teacherId,
            estudianteId: studentId,
            valor:        valor,
            comentario:   comentario,
        });

        if (result && result.success !== false) {
            // Update local state
            State.grades[studentId] = { valor, comentario };
            renderTable();
            updateMetrics();
            closeModalDirect();
            showToast('Calificación guardada correctamente ✓', 'success');
        } else {
            showToast(result?.message || 'Error al guardar la calificación.', 'error');
        }
    } catch (err) {
        console.error('Save grade error:', err);
        showToast('Error de conexión al guardar la calificación.', 'error');
    } finally {
        if (btn) { btn.disabled = false; btn.innerHTML = '💾 Guardar Calificación'; }
    }
}

// ── Sidebar navigation ────────────────────────────────────────────────────────
function showSection(section) {
    document.querySelectorAll('.nav-item').forEach(el => el.classList.remove('active'));
    
    // Set active link visually
    const btn = document.getElementById(`nav-${section}`) || event?.currentTarget;
    if (btn) btn.classList.add('active');

    const titles = {
        dashboard: ['Dashboard', 'Resumen general de tus estudiantes'],
        students:  ['Estudiantes', 'Lista completa de todos los estudiantes'],
        grades:    ['Calificaciones', 'Calificaciones asignadas por ti'],
        messages:  ['Mensajes', 'Mensajes académicos y certificados de tus estudiantes']
    };
    const [title, subtitle] = titles[section] ?? ['Dashboard', ''];
    document.getElementById('page-title').textContent    = title;
    document.getElementById('page-subtitle').textContent = subtitle;

    const dashContainer = document.getElementById('dashboard-container');
    const msgContainer = document.getElementById('messages-container');

    if (section === 'messages') {
        if (dashContainer) dashContainer.style.display = 'none';
        if (msgContainer) msgContainer.style.display = 'block';
        loadMessages();
    } else {
        if (dashContainer) dashContainer.style.display = 'block';
        if (msgContainer) msgContainer.style.display = 'none';
    }
}

// ── Load messages received by teacher ────────────────────────────────────────
async function loadMessages() {
    const tbody = document.getElementById('messages-tbody');
    if (!tbody) return;

    tbody.innerHTML = `<tr><td colspan="5">
        <div class="empty-state"><div class="icon">⏳</div><p>Cargando mensajes del servidor…</p></div>
    </td></tr>`;

    try {
        const messages = await window.api.getTeacherMessages(State.teacherId);
        if (!messages || messages.length === 0) {
            tbody.innerHTML = `<tr><td colspan="5">
                <div class="empty-state">
                    <div class="icon">📧</div>
                    <p>No has recibido ningún mensaje académico todavía.</p>
                </div>
            </td></tr>`;
            return;
        }

        tbody.innerHTML = messages.map(m => {
            const studentName = m.nombreEstudiante || m.NombreEstudiante || 'Estudiante';
            const email = m.correoEstudiante || m.CorreoEstudiante || '';
            const subject = m.asunto || m.Asunto || '';
            const messageText = m.mensaje || m.Mensaje || '';
            const dateStr = formatDate(m.fechaEnvio || m.FechaEnvio);
            const hasPdf = m.tienePdf || m.TienePdf;
            
            // Render download button if there is a PDF
            let pdfBtn = '—';
            if (hasPdf && m.pdfBase64) {
                pdfBtn = `<button class="btn btn-primary btn-sm" onclick="downloadBase64Pdf('${m.pdfBase64}', 'Certificado_${studentName.replace(/ /g, "_")}.pdf')">
                    📥 Descargar
                </button>`;
            }

            return `<tr>
                <td>
                    <div style="font-weight: 600; color: var(--text);">${studentName}</div>
                    <div style="font-size: 0.75rem; color: var(--text-muted);">${email}</div>
                </td>
                <td style="font-weight: 600; color: var(--accent2);">${subject}</td>
                <td style="white-space: pre-wrap; font-size: 0.85rem; line-height: 1.4; color: var(--text);">${messageText}</td>
                <td style="font-size: 0.8rem; color: var(--text-muted);">${dateStr}</td>
                <td>${pdfBtn}</td>
            </tr>`;
        }).join('');

    } catch (err) {
        console.error('Error loading messages:', err);
        tbody.innerHTML = `<tr><td colspan="5">
            <div class="empty-state">
                <div class="icon">⚠️</div>
                <p>Error al cargar mensajes del servidor.</p>
            </div>
        </td></tr>`;
    }
}

// ── Download base64 PDF ──────────────────────────────────────────────────────
window.downloadBase64Pdf = function(base64Data, filename) {
    try {
        const link = document.createElement('a');
        link.href = base64Data;
        link.download = filename;
        document.body.appendChild(link);
        link.click();
        document.body.removeChild(link);
        showToast('Descarga de certificado iniciada ✓', 'success');
    } catch (err) {
        console.error('Download base64 PDF error:', err);
        showToast('No se pudo descargar el certificado.', 'error');
    }
};

// ── Logout ────────────────────────────────────────────────────────────────────
function logout() {
    sessionStorage.clear();
    window.location.href = 'login.html';
}

// ── Keyboard: Escape closes modal ────────────────────────────────────────────
document.addEventListener('keydown', e => {
    if (e.key === 'Escape') closeModalDirect();
});

// ── Boot ──────────────────────────────────────────────────────────────────────
document.addEventListener('DOMContentLoaded', async () => {
    if (!checkAuth()) return;
    initTeacherUI();

    // Init API if needed
    if (window.api?.init) {
        try { await window.api.init(); } catch { /* ignore */ }
    }

    await loadStudents();
});
