import { createContext, useContext, useState, useCallback } from 'react';

const AuthContext = createContext(null);

export function AuthProvider({ children }) {
  const [auth, setAuth] = useState(() => {
    try {
      const stored = localStorage.getItem('kubecart_auth');
      return stored ? JSON.parse(stored) : null;
    } catch {
      return null;
    }
  });

  const login = useCallback((data) => {
    const authData = {
      userId:   data.userId ?? data.UserId,
      email:    data.email  ?? data.Email,
      fullName: data.fullName ?? data.FullName,
      token:    data.token  ?? data.Token,
      // Admin check is client-side only — no role claim in the JWT.
      isAdmin:  (data.email ?? data.Email) === 'admin@kubecart.com',
    };
    localStorage.setItem('kubecart_auth', JSON.stringify(authData));
    setAuth(authData);
  }, []);

  const logout = useCallback(() => {
    localStorage.removeItem('kubecart_auth');
    setAuth(null);
  }, []);

  return (
    <AuthContext.Provider value={{ auth, login, logout }}>
      {children}
    </AuthContext.Provider>
  );
}

export function useAuth() {
  const ctx = useContext(AuthContext);
  if (ctx === null) throw new Error('useAuth must be used inside <AuthProvider>');
  return ctx;
}
