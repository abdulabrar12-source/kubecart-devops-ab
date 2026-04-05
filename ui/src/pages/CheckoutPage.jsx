import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useAuth } from '../contexts/AuthContext';
import { getCart, checkout as apiCheckout } from '../api/ordersApi';

export default function CheckoutPage() {
  const { auth } = useAuth();
  const navigate = useNavigate();

  const [cart, setCart] = useState(null);
  const [loading, setLoading] = useState(true);
  const [placing, setPlacing] = useState(false);
  const [error, setError] = useState(null);

  useEffect(() => {
    getCart(auth.userId, auth.token)
      .then(setCart)
      .catch(err => setError(err.message))
      .finally(() => setLoading(false));
  }, []);

  async function handlePlaceOrder() {
    setPlacing(true);
    setError(null);
    try {
      await apiCheckout(auth.userId, auth.token);
      navigate('/orders', { state: { justOrdered: true } });
    } catch (err) {
      setError(err.message);
      setPlacing(false);
    }
  }

  const items = cart?.items ?? [];
  const total = items.reduce((sum, i) => sum + i.price * i.quantity, 0);

  if (loading) return <div className="page-loading">Loading cart…</div>;

  if (items.length === 0) {
    return (
      <div className="checkout-page">
        <h1>Checkout</h1>
        <div className="cart-empty">
          <p>Your cart is empty — nothing to check out.</p>
          <button className="btn btn-primary" onClick={() => navigate('/')}>
            Browse products
          </button>
        </div>
      </div>
    );
  }

  return (
    <div className="checkout-page">
      <h1>Checkout</h1>

      {error && <div className="error-banner">{error}</div>}

      <div className="checkout-layout">
        <div className="checkout-items">
          <h2>Order Items</h2>
          <ul className="checkout-list">
            {items.map(item => (
              <li key={item.productId} className="checkout-row">
                <span className="checkout-row-name">{item.productName}</span>
                <span className="checkout-row-qty">× {item.quantity}</span>
                <span className="checkout-row-price">${(item.price * item.quantity).toFixed(2)}</span>
              </li>
            ))}
          </ul>
        </div>

        <div className="cart-summary">
          <h2>Summary</h2>
          <div className="summary-row">
            <span>Items ({items.length})</span>
            <span>${total.toFixed(2)}</span>
          </div>
          <div className="summary-row total">
            <strong>Total</strong>
            <strong>${total.toFixed(2)}</strong>
          </div>

          <p className="checkout-note">
            Shipping to <strong>{auth.fullName}</strong> — demo store, no real payment.
          </p>

          <button
            className="btn btn-primary btn-full"
            onClick={handlePlaceOrder}
            disabled={placing}
          >
            {placing ? 'Placing order…' : 'Place order'}
          </button>
          <button
            className="btn btn-outline btn-full"
            onClick={() => navigate('/cart')}
            disabled={placing}
          >
            Back to cart
          </button>
        </div>
      </div>
    </div>
  );
}
