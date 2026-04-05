import { useEffect, useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { useAuth } from '../contexts/AuthContext';
import { getCart, updateCartItem, removeCartItem } from '../api/ordersApi';

export default function CartDrawer({ open, onClose }) {
  const { auth } = useAuth();
  const navigate = useNavigate();
  const [cart, setCart] = useState(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState(null);

  useEffect(() => {
    if (open && auth) fetchCart();
  }, [open, auth]);

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

  async function handleQtyChange(productId, qty) {
    if (qty < 1) return handleRemove(productId);
    try {
      await updateCartItem(productId, auth.userId, qty, auth.token);
      fetchCart();
    } catch (err) {
      setError(err.message);
    }
  }

  async function handleRemove(productId) {
    try {
      await removeCartItem(productId, auth.userId, auth.token);
      fetchCart();
    } catch (err) {
      setError(err.message);
    }
  }

  const total = cart?.items?.reduce((sum, i) => sum + i.price * i.quantity, 0) ?? 0;

  return (
    <>
      {open && <div className="drawer-overlay" onClick={onClose} />}
      <aside className={`cart-drawer ${open ? 'open' : ''}`}>
        <div className="drawer-header">
          <h2>Your Cart</h2>
          <button className="drawer-close" onClick={onClose}>✕</button>
        </div>

        {!auth ? (
          <div className="drawer-empty">
            <p>Please <Link to="/login" onClick={onClose}>log in</Link> to view your cart.</p>
          </div>
        ) : loading ? (
          <div className="drawer-empty"><p>Loading…</p></div>
        ) : error ? (
          <div className="drawer-empty error"><p>{error}</p></div>
        ) : !cart?.items?.length ? (
          <div className="drawer-empty"><p>Your cart is empty.</p></div>
        ) : (
          <>
            <ul className="drawer-items">
              {cart.items.map(item => (
                <li key={item.productId} className="drawer-item">
                  <div className="drawer-item-info">
                    <span className="drawer-item-name">{item.productName}</span>
                    <span className="drawer-item-price">${(item.price * item.quantity).toFixed(2)}</span>
                  </div>
                  <div className="drawer-item-controls">
                    <button onClick={() => handleQtyChange(item.productId, item.quantity - 1)} className="qty-btn">−</button>
                    <span>{item.quantity}</span>
                    <button onClick={() => handleQtyChange(item.productId, item.quantity + 1)} className="qty-btn">+</button>
                    <button onClick={() => handleRemove(item.productId)} className="remove-btn">✕</button>
                  </div>
                </li>
              ))}
            </ul>
            <div className="drawer-footer">
              <div className="drawer-total"><strong>Total:</strong> <strong>${total.toFixed(2)}</strong></div>
              <button
                className="btn btn-primary btn-full"
                onClick={() => { onClose(); navigate('/checkout'); }}
              >
                Proceed to Checkout
              </button>
            </div>
          </>
        )}
      </aside>
    </>
  );
}
