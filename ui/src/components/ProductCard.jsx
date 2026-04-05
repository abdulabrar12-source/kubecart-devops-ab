import { useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { useAuth } from '../contexts/AuthContext';
import { addToCart } from '../api/ordersApi';

export default function ProductCard({ product, onAdded }) {
  const { auth } = useAuth();
  const navigate = useNavigate();
  const [loading, setLoading] = useState(false);
  const [feedback, setFeedback] = useState(null);

  async function handleAddToCart() {
    if (!auth) {
      navigate('/login');
      return;
    }
    setLoading(true);
    setFeedback(null);
    try {
      await addToCart(auth.userId, product.id, 1, auth.token);
      setFeedback('Added!');
      onAdded?.();
      setTimeout(() => setFeedback(null), 1500);
    } catch (err) {
      setFeedback(err.message);
    } finally {
      setLoading(false);
    }
  }

  const outOfStock = product.stockQuantity === 0;

  return (
    <div className="product-card">
      <Link to={`/products/${product.id}`} className="product-card-image-link">
        {product.imageUrl
          ? <img src={product.imageUrl} alt={product.name} className="product-card-img" />
          : <div className="product-card-img-placeholder">📦</div>
        }
      </Link>

      <div className="product-card-body">
        <Link to={`/products/${product.id}`} className="product-card-name">{product.name}</Link>
        <p className="product-card-price">${product.price.toFixed(2)}</p>
        {outOfStock && <span className="badge badge-danger">Out of stock</span>}

        {feedback && <p className={`product-card-feedback ${feedback === 'Added!' ? 'success' : 'error'}`}>{feedback}</p>}

        <button
          className="btn btn-primary btn-full"
          onClick={handleAddToCart}
          disabled={loading || outOfStock}
        >
          {loading ? 'Adding…' : outOfStock ? 'Out of stock' : 'Add to cart'}
        </button>
      </div>
    </div>
  );
}
