import { useEffect, useState } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { getProduct } from '../api/catalogApi';
import { addToCart } from '../api/ordersApi';
import { useAuth } from '../contexts/AuthContext';

export default function ProductDetailPage() {
  const { id } = useParams();
  const { auth } = useAuth();
  const navigate = useNavigate();

  const [product, setProduct] = useState(null);
  const [qty, setQty] = useState(1);
  const [loading, setLoading] = useState(true);
  const [adding, setAdding] = useState(false);
  const [feedback, setFeedback] = useState(null);
  const [error, setError] = useState(null);

  useEffect(() => {
    setLoading(true);
    getProduct(id)
      .then(setProduct)
      .catch(err => setError(err.message))
      .finally(() => setLoading(false));
  }, [id]);

  async function handleAddToCart() {
    if (!auth) {
      navigate('/login');
      return;
    }
    setAdding(true);
    setFeedback(null);
    try {
      await addToCart(auth.userId, product.id, qty, auth.token);
      setFeedback({ type: 'success', msg: `${qty} item(s) added to your cart.` });
    } catch (err) {
      setFeedback({ type: 'error', msg: err.message });
    } finally {
      setAdding(false);
    }
  }

  if (loading) return <div className="page-loading">Loading product…</div>;
  if (error) return <div className="error-banner page-error">{error}</div>;
  if (!product) return null;

  const outOfStock = product.stockQuantity === 0;

  return (
    <div className="product-detail-page">
      <button className="back-link" onClick={() => navigate(-1)}>← Back</button>

      <div className="product-detail-grid">
        <div className="product-detail-img-wrap">
          {product.imageUrl
            ? <img src={product.imageUrl} alt={product.name} />
            : <div className="product-detail-img-placeholder">📦</div>
          }
        </div>

        <div className="product-detail-info">
          <h1>{product.name}</h1>
          {product.categoryName && (
            <span className="badge badge-secondary">{product.categoryName}</span>
          )}
          <p className="product-detail-price">${product.price.toFixed(2)}</p>

          {product.description && (
            <p className="product-detail-desc">{product.description}</p>
          )}

          <p className={`product-detail-stock ${outOfStock ? 'error' : 'success'}`}>
            {outOfStock
              ? '✗ Out of stock'
              : `✓ In stock (${product.stockQuantity} available)`
            }
          </p>

          {!outOfStock && (
            <div className="qty-row">
              <label>Quantity</label>
              <div className="qty-stepper">
                <button onClick={() => setQty(q => Math.max(1, q - 1))} className="qty-btn">−</button>
                <span>{qty}</span>
                <button onClick={() => setQty(q => Math.min(product.stockQuantity, q + 1))} className="qty-btn">+</button>
              </div>
            </div>
          )}

          {feedback && (
            <div className={`feedback-banner ${feedback.type}`}>{feedback.msg}</div>
          )}

          <button
            className="btn btn-primary"
            onClick={handleAddToCart}
            disabled={adding || outOfStock}
          >
            {adding ? 'Adding…' : 'Add to cart'}
          </button>
        </div>
      </div>
    </div>
  );
}
