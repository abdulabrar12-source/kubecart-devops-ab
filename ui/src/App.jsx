import { BrowserRouter, Routes, Route } from 'react-router-dom';
import { AuthProvider } from './contexts/AuthContext';
import Navbar from './components/Navbar';
import ProtectedRoute from './components/ProtectedRoute';
import HomePage from './pages/HomePage';
import ProductDetailPage from './pages/ProductDetailPage';
import CartPage from './pages/CartPage';
import CheckoutPage from './pages/CheckoutPage';
import OrdersPage from './pages/OrdersPage';
import LoginPage from './pages/LoginPage';
import RegisterPage from './pages/RegisterPage';
import AdminProductsPage from './pages/admin/ProductsPage';
import AdminOrdersPage from './pages/admin/OrdersPage';
import './App.css';

export default function App() {
  return (
    <BrowserRouter>
      <AuthProvider>
        <Navbar />
        <main className="kubecart-main">
          <Routes>
            <Route path="/" element={<HomePage />} />
            <Route path="/products/:id" element={<ProductDetailPage />} />
            <Route path="/login" element={<LoginPage />} />
            <Route path="/register" element={<RegisterPage />} />
            <Route
              path="/cart"
              element={<ProtectedRoute><CartPage /></ProtectedRoute>}
            />
            <Route
              path="/checkout"
              element={<ProtectedRoute><CheckoutPage /></ProtectedRoute>}
            />
            <Route
              path="/orders"
              element={<ProtectedRoute><OrdersPage /></ProtectedRoute>}
            />
            <Route
              path="/admin/products"
              element={<ProtectedRoute requireAdmin><AdminProductsPage /></ProtectedRoute>}
            />
            <Route
              path="/admin/orders"
              element={<ProtectedRoute requireAdmin><AdminOrdersPage /></ProtectedRoute>}
            />
          </Routes>
        </main>
      </AuthProvider>
    </BrowserRouter>
  );
}
