import { useState } from 'react';
import { useAuth } from '../../contexts/AuthContext';
import { getOrders, updateOrderStatus } from '../../api/ordersApi';

const STATUSES = ['Pending', 'Processing', 'Shipped', 'Delivered', 'Cancelled'];

const STATUS_CSS = {
  Pending:    'badge-warning',
  Processing: 'badge-info',
  Shipped:    'badge-primary',
  Delivered:  'badge-success',
  Cancelled:  'badge-danger',
};

export default function AdminOrdersPage() {
  const { auth } = useAuth();
  const [userId, setUserId] = useState('');
  const [orders, setOrders] = useState([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState(null);
  const [updating, setUpdating] = useState(null);

  async function handleSearch(e) {
    e.preventDefault();
    if (!userId.trim()) return;
    setLoading(true);
    setError(null);
    setOrders([]);
    try {
      const data = await getOrders(userId.trim(), auth.token);
      setOrders(data);
    } catch (err) {
      setError(err.message);
    } finally {
      setLoading(false);
    }
  }

  async function handleStatusChange(orderId, status) {
    setUpdating(orderId);
    try {
      await updateOrderStatus(orderId, status, auth.token);
      setOrders(prev => prev.map(o =>
        o.id === orderId ? { ...o, status } : o
      ));
    } catch (err) {
      setError(err.message);
    } finally {
      setUpdating(null);
    }
  }

  return (
    <div className="admin-page">
      <h1>Order Management</h1>

      <section className="admin-section">
        <h2>Search orders by user ID</h2>
        <form onSubmit={handleSearch} className="inline-form">
          <input
            type="text"
            value={userId}
            onChange={e => setUserId(e.target.value)}
            placeholder="User UUID"
            required
          />
          <button type="submit" className="btn btn-primary btn-sm" disabled={loading}>
            {loading ? 'Searching…' : 'Search'}
          </button>
        </form>
        <p className="field-hint">Enter the user's UUID (from the identity service) to view their orders.</p>
      </section>

      {error && <div className="error-banner">{error}</div>}

      {!loading && orders.length > 0 && (
        <section className="admin-section">
          <h2>Orders for {userId}</h2>
          <div className="orders-list">
            {orders.map(order => {
              const total = order.items?.reduce((sum, i) => sum + i.price * i.quantity, 0) ?? 0;
              return (
                <div key={order.id} className="order-card">
                  <div className="order-card-header">
                    <span className="order-id">Order #{order.id}</span>
                    <span className={`badge ${STATUS_CSS[order.status] ?? 'badge-secondary'}`}>
                      {order.status}
                    </span>
                    <span className="order-date">
                      {new Date(order.createdAt).toLocaleDateString()}
                    </span>
                  </div>

                  <ul className="order-items">
                    {order.items?.map(item => (
                      <li key={item.productId} className="order-item-row">
                        <span>{item.productName}</span>
                        <span>× {item.quantity}</span>
                        <span>${(item.price * item.quantity).toFixed(2)}</span>
                      </li>
                    ))}
                  </ul>

                  <div className="order-card-footer">
                    <strong>Total: ${total.toFixed(2)}</strong>
                    <div className="status-update">
                      <label>Update status:</label>
                      <select
                        value={order.status}
                        onChange={e => handleStatusChange(order.id, e.target.value)}
                        disabled={updating === order.id}
                      >
                        {STATUSES.map(s => (
                          <option key={s} value={s}>{s}</option>
                        ))}
                      </select>
                      {updating === order.id && <span className="updating-hint">Saving…</span>}
                    </div>
                  </div>
                </div>
              );
            })}
          </div>
        </section>
      )}

      {!loading && orders.length === 0 && userId && !error && (
        <p className="page-empty">No orders found for this user.</p>
      )}
    </div>
  );
}
