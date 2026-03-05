const API = 'https://traininglog-rikard.fly.dev';
const FIELD_TYPES = ['Number', 'Text', 'Duration'];

let token = localStorage.getItem('tl_token');
let currentUser = localStorage.getItem('tl_user');
let currentRole = localStorage.getItem('tl_role');
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
  if (res.status === 401) { logout(); return null; }
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
  } catch {
    err.textContent = 'Could not reach the server.';
    btn.disabled = false;
    btn.textContent = 'Log in';
    return;
  } finally {
    btn.disabled = false;
    btn.textContent = 'Log in';
  }

  if (!res.ok) { err.textContent = 'Invalid credentials.'; return; }

  const data = await res.json();
  token = data.token;
  currentUser = data.user;
  currentRole = data.role;
  localStorage.setItem('tl_token', token);
  localStorage.setItem('tl_user', currentUser);
  localStorage.setItem('tl_role', currentRole);
  await showApp();
});

document.getElementById('logout-btn').addEventListener('click', logout);

function logout() {
  ['tl_token', 'tl_user', 'tl_role'].forEach(k => localStorage.removeItem(k));
  token = currentUser = currentRole = null;
  loginSection.hidden = false;
  appSection.hidden = true;
}

async function showApp() {
  loginSection.hidden = true;
  appSection.hidden = false;
  userInfo.textContent = `${currentUser} (${currentRole})`;
  typeFormSection.hidden = currentRole !== 'admin';
  await loadWorkoutTypes();
}

// ── Workout Types ─────────────────────────────────────────

async function loadWorkoutTypes() {
  const res = await api('/workout-types');
  if (!res?.ok) return;
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
    const fields = t.fields
      .map(f => `${f.name} (${FIELD_TYPES[f.type] ?? f.type}${f.unit ? ', ' + f.unit : ''})`)
      .join(', ');

    const tr = document.createElement('tr');
    tr.innerHTML = `
      <td>${t.name}</td>
      <td>${fields || '—'}</td>
      <td class="td-actions">${currentRole === 'admin' ? `
        <button class="small secondary" onclick="editType(${t.id})">Edit</button>
        <button class="small danger" onclick="deleteType(${t.id})">Delete</button>
      ` : ''}</td>
    `;
    tbody.appendChild(tr);
  }
}

// ── Type form ─────────────────────────────────────────────

document.getElementById('add-field-btn').addEventListener('click', () => addFieldRow());
document.getElementById('type-cancel-btn').addEventListener('click', resetTypeForm);

function addFieldRow(name = '', type = 0, unit = '') {
  const row = document.createElement('div');
  row.className = 'field-row';
  row.innerHTML = `
    <input type="text" class="field-name" placeholder="Field name" value="${name}" required>
    <select class="field-type">
      <option value="0"${type === 0 ? ' selected' : ''}>Number</option>
      <option value="1"${type === 1 ? ' selected' : ''}>Text</option>
      <option value="2"${type === 2 ? ' selected' : ''}>Duration</option>
    </select>
    <input type="text" class="field-unit" placeholder="Unit" value="${unit ?? ''}">
    <button type="button" class="small secondary" onclick="this.parentElement.remove()">✕</button>
  `;
  fieldsContainer.appendChild(row);
}

document.getElementById('type-form').addEventListener('submit', async e => {
  e.preventDefault();

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
  }
});

function resetTypeForm() {
  editingTypeId = null;
  document.getElementById('type-name').value = '';
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

// ── Init ──────────────────────────────────────────────────

if (token) showApp();
