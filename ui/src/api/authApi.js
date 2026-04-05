const BASE = '/api/auth';

async function handleResponse(res) {
  if (!res.ok) {
    const err = await res.json().catch(() => ({}));
    throw new Error(err.error || `Request failed (${res.status})`);
  }
  return res.json();
}

export async function register(email, password, fullName) {
  const res = await fetch(`${BASE}/register`, {
    method:  'POST',
    headers: { 'Content-Type': 'application/json' },
    body:    JSON.stringify({ email, password, fullName }),
  });
  return handleResponse(res);
}

export async function login(email, password) {
  const res = await fetch(`${BASE}/login`, {
    method:  'POST',
    headers: { 'Content-Type': 'application/json' },
    body:    JSON.stringify({ email, password }),
  });
  if (res.status === 401) throw new Error('Invalid email or password.');
  return handleResponse(res);
}

export async function getMe(token) {
  const res = await fetch(`${BASE}/me`, {
    headers: { Authorization: `Bearer ${token}` },
  });
  if (res.status === 401) throw new Error('Session expired. Please log in again.');
  return handleResponse(res);
}
