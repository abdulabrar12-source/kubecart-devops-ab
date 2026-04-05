// Base URL is injected at build time via VITE_API_BASE_URL.
// Falls back to '' (empty string) so that relative paths work
// when a reverse proxy (nginx / K8s ingress) forwards /api → backend.
const BASE_URL = import.meta.env.VITE_API_BASE_URL ?? '';

/**
 * Fetch all todos from the API.
 * @returns {Promise<Array>} Array of TodoItem objects
 */
export async function getTodos() {
  const res = await fetch(`${BASE_URL}/api/todos`);
  if (!res.ok) throw new Error(`Failed to fetch todos (${res.status})`);
  return res.json();
}

/**
 * Update a todo's completion status.
 * @param {number} id - Todo ID
 * @param {boolean} isCompleted - New completion status
 * @returns {Promise<Object>} Updated TodoItem object
 */
export async function updateTodo(id, isCompleted) {
  const res = await fetch(`${BASE_URL}/api/todos/${id}`, {
    method: 'PUT',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ isCompleted }),
  });

  if (!res.ok) {
    const body = await res.json().catch(() => ({}));
    throw new Error(body.error ?? `Failed to update todo (${res.status})`);
  }

  return res.json();
}

/**
 * Delete a todo item.
 * @param {number} id - Todo ID
 * @returns {Promise<void>}
 */
export async function deleteTodo(id) {
  const res = await fetch(`${BASE_URL}/api/todos/${id}`, { method: 'DELETE' });
  if (!res.ok) {
    const body = await res.json().catch(() => ({}));
    throw new Error(body.error ?? `Failed to delete todo (${res.status})`);
  }
}

/**
 * Create a new todo item.
 * @param {string} title - The title for the new todo
 * @returns {Promise<Object>} Created TodoItem object
 */
export async function createTodo(title) {
  const res = await fetch(`${BASE_URL}/api/todos`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ title }),
  });

  if (!res.ok) {
    const body = await res.json().catch(() => ({}));
    throw new Error(body.error ?? `Failed to create todo (${res.status})`);
  }

  return res.json();
}
