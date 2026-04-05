import { useEffect, useState } from 'react';
import { getCategories, getProducts } from '../api/catalogApi';
import ProductCard from '../components/ProductCard';

export default function HomePage() {
  const [categories, setCategories] = useState([]);
  const [products, setProducts] = useState([]);
  const [activeCat, setActiveCat] = useState(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);

  useEffect(() => {
    getCategories()
      .then(setCategories)
      .catch(() => {/* non-fatal */});
  }, []);

  useEffect(() => {
    setLoading(true);
    setError(null);
    getProducts(activeCat)
      .then(setProducts)
      .catch(err => setError(err.message))
      .finally(() => setLoading(false));
  }, [activeCat]);

  return (
    <div className="home-page">
      <div className="home-hero">
        <h1>Welcome to KubeCart</h1>
        <p>Browse our catalog and add items to your cart</p>
      </div>

      {categories.length > 0 && (
        <div className="category-filter">
          <button
            className={`cat-btn ${activeCat === null ? 'active' : ''}`}
            onClick={() => setActiveCat(null)}
          >
            All
          </button>
          {categories.map(cat => (
            <button
              key={cat.id}
              className={`cat-btn ${activeCat === cat.id ? 'active' : ''}`}
              onClick={() => setActiveCat(cat.id)}
            >
              {cat.name}
            </button>
          ))}
        </div>
      )}

      {loading && <div className="page-loading">Loading products…</div>}
      {error && <div className="error-banner">{error}</div>}

      {!loading && !error && (
        products.length === 0
          ? <p className="page-empty">No products found.</p>
          : (
            <div className="product-grid">
              {products.map(p => (
                <ProductCard key={p.id} product={p} />
              ))}
            </div>
          )
      )}
    </div>
  );
}
