/**
 * teacher-dashboard.js
 * Lógica del dashboard exclusivo para maestros en Dorja.
 */

'use strict';

// ── State ────────────────────────────────────────────────────────────────────
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
function avatarColor(id) { return AVATAR_COLORS[id % AVATAR_COLORS.length]; }
function initials(nombre, apellido) {
    return ((nombre?.[0] ?? '') + (apellido?.[0] ?? '')).toUpperCase() || '?';
}

// ── Toast ─────────────────────────────────────────────────────────────────────
function showToast(msg, type = 'success') {
    const container = document.getElementById('toast-container');
    if (!container) return;
    const t = document.createElement('div');
    t.className = `toast ${type}`;
    t.innerHTML = `<span>${type === 'success' ? '✅' : '❌'}</span><span>${msg}</span>`;
    container.appendChild(t);
    setTimeout(() => t.remove(), 3500);
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

// ── Load students ─────────────────────────────────────────────────────────────
async function loadStudents() {
    try {
        const tbody = document.getElementById('students-tbody');
        if (tbody) {
            tbody.innerHTML = `<tr><td colspan="7">
                <div class="empty-state"><div class="icon">⏳</div><p>Cargando estudiantes…</p></div>
            </td></tr>`;
        }

        // 1. Fetch students list
        const students = await window.api.getStudents();
        State.students = Array.isArray(students) ? students : [];
        State.filtered = [...State.students];

        // 2. Fetch teacher's grades
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

        // 3. Fetch progress summaries in parallel (limit concurrency)
        State.progress = {};
        await Promise.all(State.students.map(async s => {
            try {
                const p = await window.api.getStudentProgressSummary(s.id ?? s.Id);
                State.progress[s.id ?? s.Id] = p;
            } catch { /* ignore */ }
        }));

        // 4. Render
        renderTable();
        updateMetrics();

    } catch (err) {
        console.error('Error loading students:', err);
        const tbody = document.getElementById('students-tbody');
        if (tbody) {
            tbody.innerHTML = `<tr><td colspan="7">
                <div class="empty-state">
                    <div class="icon">⚠️</div>
                    <p>Error al cargar estudiantes. Verifica que el servidor esté activo.</p>
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
            <div class="empty-state">
                <div class="icon">👥</div>
                <p>No se encontraron estudiantes registrados.</p>
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
        const color   = avatarColor(sid);
        const inits   = initials(nombre, apPat);
        const gradeVal= grade ? grade.valor.toFixed(1) : '—';

        return `<tr onclick="openModal(${sid})" title="Ver perfil completo">
            <td>
                <div class="student-cell">
                    <div class="student-avatar" style="background:${color}">${inits}</div>
                    <div>
                        <div class="student-name">${nombre} ${apPat}</div>
                        <div class="student-username">@${username}</div>
                    </div>
                </div>
            </td>
            <td><span class="level-badge">⚡ Nivel ${nivel}</span></td>
            <td>${pts.toLocaleString('es-MX')}</td>
            <td>
                <div class="progress-bar-wrap">
                    <div class="progress-bar-bg">
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
                <span style="font-weight:700;font-size:1rem;color:${grade ? 'var(--accent2)' : 'var(--text-muted)'}">
                    ${gradeVal}
                </span>
                ${grade ? '<span style="font-size:0.72rem;color:var(--text-muted)">/10</span>' : ''}
            </td>
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
    event?.currentTarget?.classList.add('active');

    const titles = {
        dashboard: ['Dashboard', 'Resumen general de tus estudiantes'],
        students:  ['Estudiantes', 'Lista completa de todos los estudiantes'],
        grades:    ['Calificaciones', 'Calificaciones asignadas por ti'],
    };
    const [title, subtitle] = titles[section] ?? ['Dashboard', ''];
    document.getElementById('page-title').textContent    = title;
    document.getElementById('page-subtitle').textContent = subtitle;
}

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
