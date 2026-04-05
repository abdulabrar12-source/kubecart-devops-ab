import { useState } from 'react';

/**
 * Form for adding a new todo.
 * Props:
 *   onAdd(title: string) – async callback; should throw on error
 */
export default function AddTodo({ onAdd }) {
  const [title, setTitle]     = useState('');
  const [loading, setLoading] = useState(false);
  const [error, setError]     = useState(null);

  const handleSubmit = async (e) => {
    e.preventDefault();
    const trimmed = title.trim();
    if (!trimmed) return;

    setLoading(true);
    setError(null);

    try {
      await onAdd(trimmed);
      setTitle('');
    } catch (err) {
      setError(err.message);
    } finally {
      setLoading(false);
    }
  };

  return (
    <form className="add-todo-form" onSubmit={handleSubmit} noValidate>
      <div className="add-todo-row">
        <input
          type="text"
          className="add-todo-input"
          value={title}
          onChange={(e) => setTitle(e.target.value)}
          placeholder="What needs to be done?"
          disabled={loading}
          maxLength={200}
          aria-label="New todo title"
        />
        <button
          type="submit"
          className="add-todo-btn"
          disabled={loading || !title.trim()}
        >
          {loading ? '…' : '+ Add'}
        </button>
      </div>

      {error && <p className="add-todo-error">⚠️ {error}</p>}
    </form>
  );
}
