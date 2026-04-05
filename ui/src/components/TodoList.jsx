import TodoItem from './TodoItem';

/**
 * Renders the full list of todos, or an empty-state message.
 * Props:
 *   todos    – TodoItem[]
 *   onToggle – async (id, isCompleted) => void
 *   onDelete – async (id) => void
 */
export default function TodoList({ todos, onToggle, onDelete }) {
  if (todos.length === 0) {
    return (
      <div className="empty-state">
        <p>🎉 No todos yet — add one above!</p>
      </div>
    );
  }

  return (
    <ul className="todo-list">
      {todos.map((todo) => (
        <TodoItem key={todo.id} todo={todo} onToggle={onToggle} onDelete={onDelete} />
      ))}
    </ul>
  );
}
