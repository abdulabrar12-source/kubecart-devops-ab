import { useEffect, useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { useAuth } from '../contexts/AuthContext';
import { getCart, updateCartItem, removeCartItem } from '../api/ordersApi';

export default function CartPage() {
  const { auth } = useAuth();
  const navigate = useNavigate();
  const [cart, setCart] = useState(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const [busy, setBusy] = useState(null); // productId of the item being updated

  useEffect(() => { fetchCart(); }, []);

  async function fetchCart() {
    setLoading(true);
    setError(null);
    try {
      const data = await getCart(auth.userId, auth.token);
      setCart(data);
    } catch (err) {
      setError(err.message);
    } finally {
      setLoading(false);
    }
  }

  async function handleQty(productId, qty) {
    if (qty < 1) return handleRemove(productId);
    setBusy(productId);
    try {
      await updateCartItem(productId, auth.userId, qty, auth.token);
      await fetchCart();
    } catch (err) {
      setError(err.message);
    } finally {
      setBusy(null);
    }
  }

  async function handleRemove(productId) {
    setBusy(productId);
    try {
      await removeCartItem(productId, auth.userId, auth.token);
      await fetchCart();
    } catch (err) {
      setError(err.message);
    } finally {
      setBusy(null);
    }
  }

  const items = cart?.items ?? [];
  const total = items.reduce((sum, i) => sum + i.price * i.quantity, 0);

  if (loading) return <div className="page-loading">Loading cart…</div>;

  return (
    <div className="cart-page">
      <h1>Your Cart</h1>

      {error && <div className="error-banner">{error}</div>}

      {items.length === 0 ? (
        <div className="cart-empty">
          <p>Your cart is empty.</p>
          <Link to="/" className="btn btn-primary">Browse products</Link>
        </div>
      ) : (
        <div className="cart-layout">
          <ul className="cart-items">
            {items.map(item => (
              <li key={item.productId} className="cart-item">
                <div className="cart-item-info">
                  <Link to={`/products/${item.productId}`} className="cart-item-name">
                    {item.productName}
                  </Link>
                  <span className="cart-item-unit">${item.price.toFixed(2)} each</span>
                </div>

                <div className="cart-item-controls">
                  <button
                    onClick={() => handleQty(item.productId, item.quantity - 1)}
                    disabled={busy === item.productId}
                    className="qty-btn"
                  >−</button>
                  <span className="qty-value">{item.quantity}</span>
                  <button
                    onClick={() => handleQty(item.productId, item.quantity + 1)}
                    disabled={busy === item.productId}
                    className="qty-btn"
                  >+</button>
                </div>

                <span className="cart-item-subtotal">${(item.price * item.quantity).toFixed(2)}</span>

                <button
                  onClick={() => handleRemove(item.productId)}
                  disabled={busy === item.productId}
                  className="btn-remove"
                  title="Remove"
                >✕</button>
              </li>
            ))}
          </ul>

          <div className="cart-summary">
            <h2>Order Summary</h2>
            <div className="summary-row">
              <span>Items ({items.length})</span>
              <span>${total.toFixed(2)}</span>
            </div>
            <div className="summary-row total">
              <strong>Total</strong>
              <strong>${total.toFixed(2)}</strong>
            </div>
            <button
              className="btn btn-primary btn-full"
              onClick={() => navigate('/checkout')}
            >
              Proceed to Checkout
            </button>
          </div>
        </div>
      )}
    </div>
  );
}
