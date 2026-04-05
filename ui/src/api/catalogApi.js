const BASE = '/api/catalog';

async function handleResponse(res) {
  if (!res.ok) {
    const err = await res.json().catch(() => ({}));
    throw new Error(err.error || `Request failed (${res.status})`);
  }
  return res.json();
}

function authHeaders(token) {
  return { 'Content-Type': 'application/json', Authorization: `Bearer ${token}` };
}

// ── Public ────────────────────────────────────────────────────────────────────

export async function getCategories() {
  const res = await fetch(`${BASE}/categories`);
  return handleResponse(res);
}

export async function getProducts(categoryId = null) {
  const url = categoryId
    ? `${BASE}/products?categoryId=${categoryId}`
    : `${BASE}/products`;
  const res = await fetch(url);
  return handleResponse(res);
}

export async function getProduct(id) {
  const res = await fetch(`${BASE}/products/${id}`);
  return handleResponse(res);
}

// ── Admin ─────────────────────────────────────────────────────────────────────

export async function createCategory(name, token) {
  const res = await fetch(`${BASE}/admin/categories`, {
    method:  'POST',
    headers: authHeaders(token),
    body:    JSON.stringify({ name }),
  });
  return handleResponse(res);
}

export async function createProduct(data, token) {
  const res = await fetch(`${BASE}/admin/products`, {
    method:  'POST',
    headers: authHeaders(token),
    body:    JSON.stringify(data),
  });
  return handleResponse(res);
}

export async function updateProduct(id, data, token) {
  const res = await fetch(`${BASE}/admin/products/${id}`, {
    method:  'PUT',
    headers: authHeaders(token),
    body:    JSON.stringify(data),
  });
  return handleResponse(res);
}

export async function deleteProduct(id, token) {
  const res = await fetch(`${BASE}/admin/products/${id}`, {
    method:  'DELETE',
    headers: { Authorization: `Bearer ${token}` },
  });
  if (!res.ok) {
    const err = await res.json().catch(() => ({}));
    throw new Error(err.error || 'Failed to delete product.');
  }
}

export async function updateProductImage(id, imageUrl, token) {
  const res = await fetch(`${BASE}/admin/products/${id}/images`, {
    method:  'POST',
    headers: authHeaders(token),
    body:    JSON.stringify({ imageUrl }),
  });
  return handleResponse(res);
}
