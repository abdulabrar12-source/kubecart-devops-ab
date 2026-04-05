import { useEffect, useState } from 'react';
import { useAuth } from '../../contexts/AuthContext';
import {
  getCategories,
  getProducts,
  createProduct,
  updateProduct,
  deleteProduct,
  updateProductImage,
  createCategory,
} from '../../api/catalogApi';

const EMPTY_FORM = {
  name: '', description: '', price: '', stockQuantity: '',
  imageUrl: '', categoryId: '',
};

export default function AdminProductsPage() {
  const { auth } = useAuth();
  const [products, setProducts]   = useState([]);
  const [categories, setCategories] = useState([]);
  const [loading, setLoading]     = useState(true);
  const [error, setError]         = useState(null);
  const [formError, setFormError] = useState(null);

  const [form, setForm]           = useState(EMPTY_FORM);
  const [editId, setEditId]       = useState(null);
  const [showForm, setShowForm]   = useState(false);

  const [catName, setCatName]     = useState('');
  const [catBusy, setCatBusy]     = useState(false);
  const [catMsg, setCatMsg]       = useState(null);

  useEffect(() => { loadData(); }, []);

  async function loadData() {
    setLoading(true);
    setError(null);
    try {
      const [p, c] = await Promise.all([getProducts(), getCategories()]);
      setProducts(p);
      setCategories(c);
    } catch (err) {
      setError(err.message);
    } finally {
      setLoading(false);
    }
  }

  function startCreate() {
    setEditId(null);
    setForm(EMPTY_FORM);
    setFormError(null);
    setShowForm(true);
  }

  function startEdit(p) {
    setEditId(p.id);
    setForm({
      name:          p.name,
      description:   p.description ?? '',
      price:         p.price,
      stockQuantity: p.stockQuantity,
      imageUrl:      p.imageUrl ?? '',
      categoryId:    p.categoryId ?? '',
    });
    setFormError(null);
    setShowForm(true);
  }

  function handleChange(e) {
    setForm(prev => ({ ...prev, [e.target.name]: e.target.value }));
  }

  async function handleSave(e) {
    e.preventDefault();
    setFormError(null);
    const payload = {
      name:          form.name.trim(),
      description:   form.description.trim() || null,
      price:         parseFloat(form.price),
      stockQuantity: parseInt(form.stockQuantity, 10),
      categoryId:    form.categoryId ? parseInt(form.categoryId, 10) : null,
    };
    try {
      if (editId) {
        await updateProduct(editId, payload, auth.token);
        if (form.imageUrl.trim()) {
          await updateProductImage(editId, form.imageUrl.trim(), auth.token);
        }
      } else {
        const created = await createProduct(payload, auth.token);
        if (form.imageUrl.trim()) {
          await updateProductImage(created.id, form.imageUrl.trim(), auth.token);
        }
      }
      setShowForm(false);
      loadData();
    } catch (err) {
      setFormError(err.message);
    }
  }

  async function handleDelete(id) {
    if (!window.confirm('Delete this product?')) return;
    try {
      await deleteProduct(id, auth.token);
      loadData();
    } catch (err) {
      setError(err.message);
    }
  }

  async function handleAddCategory(e) {
    e.preventDefault();
    setCatBusy(true);
    setCatMsg(null);
    try {
      await createCategory(catName.trim(), auth.token);
      setCatName('');
      setCatMsg({ type: 'success', text: 'Category created!' });
      const c = await getCategories();
      setCategories(c);
    } catch (err) {
      setCatMsg({ type: 'error', text: err.message });
    } finally {
      setCatBusy(false);
    }
  }

  if (loading) return <div className="page-loading">Loading…</div>;

  return (
    <div className="admin-page">
      <h1>Product Management</h1>

      {error && <div className="error-banner">{error}</div>}

      {/* Category quick-add */}
      <section className="admin-section">
        <h2>Add Category</h2>
        <form onSubmit={handleAddCategory} className="inline-form">
          <input
            type="text"
            value={catName}
            onChange={e => setCatName(e.target.value)}
            placeholder="Category name"
            required
          />
          <button type="submit" className="btn btn-outline btn-sm" disabled={catBusy}>
            {catBusy ? 'Adding…' : 'Add'}
          </button>
        </form>
        {catMsg && <p className={`feedback-banner ${catMsg.type}`}>{catMsg.text}</p>}
      </section>

      {/* Product list */}
      <section className="admin-section">
        <div className="section-header">
          <h2>Products ({products.length})</h2>
          <button className="btn btn-primary btn-sm" onClick={startCreate}>+ New product</button>
        </div>

        {products.length === 0 ? (
          <p className="page-empty">No products yet.</p>
        ) : (
          <table className="admin-table">
            <thead>
              <tr>
                <th>ID</th><th>Name</th><th>Category</th><th>Price</th><th>Stock</th><th>Actions</th>
              </tr>
            </thead>
            <tbody>
              {products.map(p => (
                <tr key={p.id}>
                  <td>{p.id}</td>
                  <td>{p.name}</td>
                  <td>{p.categoryName ?? '—'}</td>
                  <td>${p.price.toFixed(2)}</td>
                  <td>{p.stockQuantity}</td>
                  <td>
                    <button className="btn btn-outline btn-xs" onClick={() => startEdit(p)}>Edit</button>{' '}
                    <button className="btn btn-danger btn-xs" onClick={() => handleDelete(p.id)}>Delete</button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </section>

      {/* Product form */}
      {showForm && (
        <div className="modal-overlay" onClick={() => setShowForm(false)}>
          <div className="modal" onClick={e => e.stopPropagation()}>
            <div className="modal-header">
              <h2>{editId ? 'Edit Product' : 'New Product'}</h2>
              <button className="drawer-close" onClick={() => setShowForm(false)}>✕</button>
            </div>

            {formError && <div className="error-banner">{formError}</div>}

            <form onSubmit={handleSave} className="auth-form">
              <label className="field">
                <span>Name *</span>
                <input name="name" value={form.name} onChange={handleChange} required />
              </label>
              <label className="field">
                <span>Description</span>
                <textarea name="description" value={form.description} onChange={handleChange} rows={3} />
              </label>
              <div className="form-row">
                <label className="field">
                  <span>Price *</span>
                  <input name="price" type="number" min="0" step="0.01" value={form.price} onChange={handleChange} required />
                </label>
                <label className="field">
                  <span>Stock *</span>
                  <input name="stockQuantity" type="number" min="0" value={form.stockQuantity} onChange={handleChange} required />
                </label>
              </div>
              <label className="field">
                <span>Category</span>
                <select name="categoryId" value={form.categoryId} onChange={handleChange}>
                  <option value="">— None —</option>
                  {categories.map(c => (
                    <option key={c.id} value={c.id}>{c.name}</option>
                  ))}
                </select>
              </label>
              <label className="field">
                <span>Image URL</span>
                <input name="imageUrl" type="url" value={form.imageUrl} onChange={handleChange} placeholder="https://…" />
              </label>
              <div className="form-actions">
                <button type="submit" className="btn btn-primary">{editId ? 'Save changes' : 'Create product'}</button>
                <button type="button" className="btn btn-outline" onClick={() => setShowForm(false)}>Cancel</button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  );
}
