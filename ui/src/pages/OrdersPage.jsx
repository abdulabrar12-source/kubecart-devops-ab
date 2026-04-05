import { useEffect, useState } from 'react';
import { useLocation } from 'react-router-dom';
import { useAuth } from '../contexts/AuthContext';
import { getOrders } from '../api/ordersApi';

const STATUS_LABELS = {
  Pending:    { label: 'Pending',    css: 'badge-warning' },
  Processing: { label: 'Processing', css: 'badge-info' },
  Shipped:    { label: 'Shipped',    css: 'badge-primary' },
  Delivered:  { label: 'Delivered',  css: 'badge-success' },
  Cancelled:  { label: 'Cancelled',  css: 'badge-danger' },
};

export default function OrdersPage() {
  const { auth } = useAuth();
  const location = useLocation();
  const justOrdered = location.state?.justOrdered;

  const [orders, setOrders] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);

  useEffect(() => {
    getOrders(auth.userId, auth.token)
      .then(setOrders)
      .catch(err => setError(err.message))
      .finally(() => setLoading(false));
  }, []);

  if (loading) return <div className="page-loading">Loading orders…</div>;

  return (
    <div className="orders-page">
      <h1>My Orders</h1>

      {justOrdered && (
        <div className="success-banner">
          🎉 Order placed successfully! It will be fulfilled shortly.
        </div>
      )}

      {error && <div className="error-banner">{error}</div>}

      {orders.length === 0 ? (
        <p className="page-empty">You haven't placed any orders yet.</p>
      ) : (
        <div className="orders-list">
          {orders.map(order => {
            const meta = STATUS_LABELS[order.status] ?? { label: order.status, css: 'badge-secondary' };
            const total = order.items?.reduce((sum, i) => sum + i.price * i.quantity, 0) ?? 0;
            return (
              <div key={order.id} className="order-card">
                <div className="order-card-header">
                  <span className="order-id">Order #{order.id}</span>
                  <span className={`badge ${meta.css}`}>{meta.label}</span>
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
                </div>
              </div>
            );
          })}
        </div>
      )}
    </div>
  );
}
