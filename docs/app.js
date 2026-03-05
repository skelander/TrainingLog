const API = 'https://traininglog-rikard.fly.dev';
const FIELD_TYPES = ['Number', 'Text', 'Duration'];
const LS_TOKEN = 'tl_token';
const LS_USER  = 'tl_user';
const LS_ROLE  = 'tl_role';

let token = localStorage.getItem(LS_TOKEN);
let currentUser = localStorage.getItem(LS_USER);
let currentRole = localStorage.getItem(LS_ROLE);
let workoutTypes = [];
let editingTypeId = null;

// ── Elements ──────────────────────────────────────────────

const loginSection = document.getElementById('login-section');
const appSection = document.getElementById('app-section');
const userInfo = document.getElementById('user-info');
const typeFormSection = document.getElementById('type-form-section');
const typeFormTitle = document.getElementById('type-form-title');
const typeSubmitBtn = document.getElementById('type-submit-btn');
const typeCancelBtn = document.getElementById('type-cancel-btn');
const fieldsContainer = document.getElementById('fields-container');

// ── API helper ────────────────────────────────────────────

async function api(path, options = {}) {
  const res = await fetch(API + path, {
    ...options,
    headers: {
      'Content-Type': 'application/json',
      ...(token ? { Authorization: `Bearer ${token}` } : {}),
      ...(options.headers ?? {}),
    },
  });
  return res;
}

// ── Auth ──────────────────────────────────────────────────

document.getElementById('login-form').addEventListener('submit', async e => {
  e.preventDefault();
  const err = document.getElementById('login-error');
  const btn = e.target.querySelector('button[type="submit"]');
  err.textContent = '';
  btn.disabled = true;
  btn.textContent = 'Logging in…';

  try {
    let res;
    try {
      res = await fetch(API + '/auth/login', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          username: document.getElementById('username').value,
          password: document.getElementById('password').value,
        }),
      });
    } catch (fetchErr) {
      err.textContent = `Could not reach the server: ${fetchErr.message}`;
      return;
    }

    if (!res.ok) { err.textContent = 'Invalid credentials.'; return; }

    const data = await res.json();
    token = data.token;
    currentUser = data.user;
    currentRole = data.role;
    localStorage.setItem(LS_TOKEN, token);
    localStorage.setItem(LS_USER, currentUser);
    localStorage.setItem(LS_ROLE, currentRole);
    await showApp();
  } catch (unexpected) {
    err.textContent = `Error: ${unexpected.message}`;
  } finally {
    btn.disabled = false;
    btn.textContent = 'Log in';
  }
});

document.getElementById('logout-btn').addEventListener('click', logout);

function logout() {
  [LS_TOKEN, LS_USER, LS_ROLE].forEach(k => localStorage.removeItem(k));
  token = currentUser = currentRole = null;
  loginSection.hidden = false;
  appSection.hidden = true;
}

async function showApp() {
  loginSection.hidden = true;
  appSection.hidden = false;
  userInfo.textContent = `${currentUser} (${currentRole})`;
  typeFormSection.hidden = currentRole !== 'admin';
  const sessionsSection = document.getElementById('sessions-section');
  sessionsSection.hidden = currentRole === 'admin';
  await loadWorkoutTypes();
  if (currentRole !== 'admin') {
    setSessionDateDefault();
    populateSessionTypeSelect();
    await loadSessions();
  }
}

// ── Workout Types ─────────────────────────────────────────

async function loadWorkoutTypes() {
  const tbody = document.querySelector('#types-table tbody');
  tbody.innerHTML = '<tr class="empty-row"><td colspan="3">Loading…</td></tr>';
  let res;
  try {
    res = await api('/workout-types');
  } catch {
    tbody.innerHTML = '<tr class="empty-row"><td colspan="3">Could not reach server.</td></tr>';
    return;
  }
  if (!res.ok) {
    tbody.innerHTML = `<tr class="empty-row"><td colspan="3">Error ${res.status} loading types.</td></tr>`;
    return;
  }
  workoutTypes = await res.json();
  renderWorkoutTypes();
}

function renderWorkoutTypes() {
  const tbody = document.querySelector('#types-table tbody');
  tbody.innerHTML = '';

  if (!workoutTypes.length) {
    tbody.innerHTML = '<tr class="empty-row"><td colspan="3">No workout types yet.</td></tr>';
    return;
  }

  for (const t of workoutTypes) {
    const fieldText = t.fields
      .map(f => `${f.name} (${FIELD_TYPES[f.type] ?? f.type}${f.unit ? ', ' + f.unit : ''})`)
      .join(', ');

    const tdName = document.createElement('td');
    tdName.textContent = t.name;

    const tdFields = document.createElement('td');
    tdFields.textContent = fieldText || '—';

    const tdActions = document.createElement('td');
    tdActions.className = 'td-actions';
    if (currentRole === 'admin') {
      const editBtn = document.createElement('button');
      editBtn.className = 'small secondary';
      editBtn.dataset.action = 'edit';
      editBtn.dataset.id = t.id;
      editBtn.textContent = 'Edit';
      const deleteBtn = document.createElement('button');
      deleteBtn.className = 'small danger';
      deleteBtn.dataset.action = 'delete';
      deleteBtn.dataset.id = t.id;
      deleteBtn.textContent = 'Delete';
      tdActions.append(editBtn, deleteBtn);
    }

    const tr = document.createElement('tr');
    tr.append(tdName, tdFields, tdActions);
    tbody.appendChild(tr);
  }
}

document.querySelector('#types-table tbody').addEventListener('click', e => {
  const btn = e.target.closest('button[data-action]');
  if (!btn) return;
  const id = parseInt(btn.dataset.id);
  if (btn.dataset.action === 'edit') editType(id);
  if (btn.dataset.action === 'delete') deleteType(id);
});

// ── Type form ─────────────────────────────────────────────

document.getElementById('add-field-btn').addEventListener('click', () => addFieldRow());
document.getElementById('type-cancel-btn').addEventListener('click', resetTypeForm);
fieldsContainer.addEventListener('click', e => {
  if (e.target.closest('.remove-field-btn')) e.target.closest('.field-row').remove();
});

function addFieldRow(name = '', type = 0, unit = '') {
  const row = document.createElement('div');
  row.className = 'field-row';
  row.innerHTML = `
    <input type="text" class="field-name" placeholder="Field name" required>
    <select class="field-type">
      <option value="0">Number</option>
      <option value="1">Text</option>
      <option value="2">Duration</option>
    </select>
    <input type="text" class="field-unit" placeholder="Unit">
    <button type="button" class="small secondary remove-field-btn">✕</button>
  `;
  row.querySelector('.field-name').value = name;
  row.querySelector('.field-type').value = type;
  row.querySelector('.field-unit').value = unit ?? '';
  fieldsContainer.appendChild(row);
}

document.getElementById('type-form').addEventListener('submit', async e => {
  e.preventDefault();
  const err = document.getElementById('type-error');
  err.textContent = '';

  const name = document.getElementById('type-name').value.trim();
  const fields = [...document.querySelectorAll('.field-row')].map(row => ({
    name: row.querySelector('.field-name').value.trim(),
    type: parseInt(row.querySelector('.field-type').value),
    unit: row.querySelector('.field-unit').value.trim() || null,
  }));

  const isEdit = editingTypeId !== null;
  const path = isEdit ? `/workout-types/${editingTypeId}` : '/workout-types';
  const res = await api(path, { method: isEdit ? 'PUT' : 'POST', body: JSON.stringify({ name, fields }) });

  if (res?.ok) {
    resetTypeForm();
    await loadWorkoutTypes();
  } else {
    err.textContent = `Error ${res?.status ?? '—'}: could not save workout type.`;
  }
});

function resetTypeForm() {
  editingTypeId = null;
  document.getElementById('type-name').value = '';
  document.getElementById('type-error').textContent = '';
  fieldsContainer.innerHTML = '';
  typeFormTitle.textContent = 'New Workout Type';
  typeSubmitBtn.textContent = 'Create';
  typeCancelBtn.hidden = true;
}

function editType(id) {
  const type = workoutTypes.find(t => t.id === id);
  if (!type) return;

  editingTypeId = id;
  document.getElementById('type-name').value = type.name;
  fieldsContainer.innerHTML = '';
  type.fields.forEach(f => addFieldRow(f.name, f.type, f.unit));
  typeFormTitle.textContent = `Edit: ${type.name}`;
  typeSubmitBtn.textContent = 'Save';
  typeCancelBtn.hidden = false;
  typeFormSection.scrollIntoView({ behavior: 'smooth' });
}

async function deleteType(id) {
  const type = workoutTypes.find(t => t.id === id);
  if (!confirm(`Delete "${type?.name ?? id}"?`)) return;
  const res = await api(`/workout-types/${id}`, { method: 'DELETE' });
  if (res?.ok) {
    if (editingTypeId === id) resetTypeForm();
    await loadWorkoutTypes();
  }
}

// ── Sessions ──────────────────────────────────────────────

async function loadSessions() {
  const tbody = document.querySelector('#sessions-table tbody');
  tbody.innerHTML = '<tr class="empty-row"><td colspan="5">Loading…</td></tr>';
  let res;
  try {
    res = await api('/workouts');
  } catch {
    tbody.innerHTML = '<tr class="empty-row"><td colspan="5">Could not reach server.</td></tr>';
    return;
  }
  if (!res.ok) {
    tbody.innerHTML = `<tr class="empty-row"><td colspan="5">Error ${res.status}.</td></tr>`;
    return;
  }
  renderSessions(await res.json());
}

function renderSessions(sessions) {
  const tbody = document.querySelector('#sessions-table tbody');
  tbody.innerHTML = '';
  if (!sessions.length) {
    tbody.innerHTML = '<tr class="empty-row"><td colspan="5">No sessions yet.</td></tr>';
    return;
  }
  for (const s of sessions) {
    const valueText = s.values.map(v => `${v.fieldDefinitionName}: ${v.value}`).join(', ');

    const tdDate = document.createElement('td');
    tdDate.textContent = new Date(s.loggedAt).toLocaleString();

    const tdType = document.createElement('td');
    tdType.textContent = s.workoutTypeName;

    const tdValues = document.createElement('td');
    tdValues.textContent = valueText || '—';

    const tdNotes = document.createElement('td');
    tdNotes.textContent = s.notes || '—';

    const deleteBtn = document.createElement('button');
    deleteBtn.className = 'small danger';
    deleteBtn.dataset.action = 'delete';
    deleteBtn.dataset.id = s.id;
    deleteBtn.textContent = 'Delete';
    const tdActions = document.createElement('td');
    tdActions.className = 'td-actions';
    tdActions.appendChild(deleteBtn);

    const tr = document.createElement('tr');
    tr.append(tdDate, tdType, tdValues, tdNotes, tdActions);
    tbody.appendChild(tr);
  }
}

document.querySelector('#sessions-table tbody').addEventListener('click', async e => {
  const btn = e.target.closest('button[data-action="delete"]');
  if (!btn) return;
  if (!confirm('Delete this session?')) return;
  const res = await api(`/workouts/${parseInt(btn.dataset.id)}`, { method: 'DELETE' });
  if (res?.ok) await loadSessions();
})

function populateSessionTypeSelect() {
  const sel = document.getElementById('session-type');
  sel.innerHTML = '<option value="">— Select type —</option>';
  for (const t of workoutTypes) {
    const opt = document.createElement('option');
    opt.value = t.id;
    opt.textContent = t.name;
    sel.appendChild(opt);
  }
}

document.getElementById('session-type').addEventListener('change', function () {
  const typeId = parseInt(this.value);
  const container = document.getElementById('session-fields');
  container.innerHTML = '';
  if (!typeId) return;
  const type = workoutTypes.find(t => t.id === typeId);
  if (!type) return;
  for (const f of type.fields) {
    const label = document.createElement('label');
    label.className = 'session-field-label';
    const span = document.createElement('span');
    span.textContent = f.unit ? `${f.name} (${f.unit})` : f.name;
    const input = document.createElement('input');
    input.type = f.type === 0 ? 'number' : 'text';
    input.className = 'session-field-value';
    input.dataset.fieldId = f.id;
    input.placeholder = FIELD_TYPES[f.type] ?? 'Value';
    label.appendChild(span);
    label.appendChild(input);
    container.appendChild(label);
  }
});

document.getElementById('session-form').addEventListener('submit', async e => {
  e.preventDefault();
  const err = document.getElementById('session-error');
  err.textContent = '';

  const typeId = parseInt(document.getElementById('session-type').value);
  const loggedAt = document.getElementById('session-date').value;
  const notes = document.getElementById('session-notes').value.trim() || null;
  const values = [...document.querySelectorAll('.session-field-value')].map(inp => ({
    fieldDefinitionId: parseInt(inp.dataset.fieldId),
    value: inp.value,
  }));
  const res = await api('/workouts', {
    method: 'POST',
    body: JSON.stringify({ workoutTypeId: typeId, loggedAt, notes, values }),
  });
  if (res?.ok) {
    e.target.reset();
    document.getElementById('session-fields').innerHTML = '';
    setSessionDateDefault();
    await loadSessions();
  } else {
    err.textContent = `Error ${res?.status ?? '—'}: could not log session.`;
  }
});

function setSessionDateDefault() {
  const now = new Date();
  now.setSeconds(0, 0);
  document.getElementById('session-date').value = now.toISOString().slice(0, 16);
}

// ── Init ──────────────────────────────────────────────────

if (token) showApp();
