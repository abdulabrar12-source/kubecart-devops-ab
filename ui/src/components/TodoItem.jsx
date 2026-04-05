import { useState } from 'react';

/**
 * Renders a single todo row.
 * Props:
 *   todo     – { id, title, isCompleted, createdAtUtc }
 *   onToggle – async (id, isCompleted) => void
 */
export default function TodoItem({ todo, onToggle, onDelete }) {
  const [busy, setBusy]       = useState(false);
  const [deleting, setDeleting] = useState(false);

  const date = new Date(todo.createdAtUtc).toLocaleDateString('en-US', {
    month: 'short',
    day: 'numeric',
    year: 'numeric',
  });

  const handleChange = async (e) => {
    setBusy(true);
    try {
      await onToggle(todo.id, e.target.checked);
    } finally {
      setBusy(false);
    }
  };

  const handleDelete = async () => {
    setDeleting(true);
    try {
      await onDelete(todo.id);
    } finally {
      setDeleting(false);
    }
  };

  return (
    <li className={`todo-item${todo.isCompleted ? ' completed' : ''}${deleting ? ' deleting' : ''}`}>
      <input
        type="checkbox"
        className="todo-checkbox"
        checked={todo.isCompleted}
        onChange={handleChange}
        disabled={busy || deleting}
        aria-label={`Mark "${todo.title}" as ${todo.isCompleted ? 'incomplete' : 'complete'}`}
      />

      <div className="todo-content">
        <span className="todo-title">{todo.title}</span>
        <span className="todo-date">Added {date}</span>
      </div>

      <span className={`todo-badge ${todo.isCompleted ? 'badge-done' : 'badge-pending'}`}>
        {todo.isCompleted ? 'Done' : 'Pending'}
      </span>

      <button
        className="todo-delete-btn"
        onClick={handleDelete}
        disabled={deleting}
        aria-label={`Delete "${todo.title}"`}
        title="Delete"
      >
        {deleting ? '…' : '×'}
      </button>
    </li>
  );
}
