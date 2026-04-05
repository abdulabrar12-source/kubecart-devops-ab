const BASE = '/api/orders';

async function handleResponse(res) {
  if (!res.ok) {
    const err = await res.json().catch(() => ({}));
    throw new Error(err.error || err.title || `Request failed (${res.status})`);
  }
  // 204 No Content has no body
  if (res.status === 204) return null;
  return res.json();
}

function authHeaders(token) {
  return { 'Content-Type': 'application/json', Authorization: `Bearer ${token}` };
}

// ── Cart ──────────────────────────────────────────────────────────────────────

export async function getCart(userId, token) {
  const res = await fetch(`${BASE}/cart?userId=${userId}`, {
    headers: { Authorization: `Bearer ${token}` },
  });
  const data = await handleResponse(res);
  // API returns a flat array; normalise to { items: [...] } with price alias
  const raw = Array.isArray(data) ? data : (data?.items ?? []);
  return {
    items: raw.map(i => ({ ...i, price: i.price ?? i.unitPrice })),
  };
}

export async function addToCart(userId, productId, quantity, token) {
  const res = await fetch(`${BASE}/cart/items`, {
    method:  'POST',
    headers: authHeaders(token),
    body:    JSON.stringify({ userId, productId, quantity }),
  });
  return handleResponse(res);
}

export async function updateCartItem(productId, userId, quantity, token) {
  const res = await fetch(`${BASE}/cart/items/${productId}?userId=${userId}`, {
    method:  'PUT',
    headers: authHeaders(token),
    body:    JSON.stringify({ quantity }),
  });
  return handleResponse(res);
}

export async function removeCartItem(productId, userId, token) {
  const res = await fetch(`${BASE}/cart/items/${productId}?userId=${userId}`, {
    method:  'DELETE',
    headers: { Authorization: `Bearer ${token}` },
  });
  return handleResponse(res);
}

// ── Checkout ──────────────────────────────────────────────────────────────────

export async function checkout(userId, token) {
  const res = await fetch(`${BASE}/checkout`, {
    method:  'POST',
    headers: authHeaders(token),
    body:    JSON.stringify({ userId }),
  });
  return handleResponse(res);
}

// ── Orders ────────────────────────────────────────────────────────────────────

export async function getOrders(userId, token) {
  const res = await fetch(`${BASE}?userId=${userId}`, {
    headers: { Authorization: `Bearer ${token}` },
  });
  const data = await handleResponse(res);
  const raw = Array.isArray(data) ? data : [];
  // Normalise unitPrice → price on every order item
  return raw.map(order => ({
    ...order,
    items: (order.items ?? []).map(i => ({ ...i, price: i.price ?? i.unitPrice ?? i.UnitPrice })),
  }));
}

export async function getOrder(id, token) {
  const res = await fetch(`${BASE}/${id}`, {
    headers: { Authorization: `Bearer ${token}` },
  });
  return handleResponse(res);
}

// ── Admin ─────────────────────────────────────────────────────────────────────

export async function updateOrderStatus(orderId, status, token) {
  const res = await fetch(`${BASE}/admin/${orderId}/status`, {
    method:  'PUT',
    headers: authHeaders(token),
    body:    JSON.stringify({ status }),
  });
  return handleResponse(res);
}
