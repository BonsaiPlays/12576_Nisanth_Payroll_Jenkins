import React, { createContext, useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import { jwtDecode } from "jwt-decode";
import { isTokenExpired } from "../utils/auth";

export const AuthContext = createContext();

export function AuthProvider({ children }) {
  const [user, setUser] = useState(() => {
    const role = localStorage.getItem("role");
    const token = localStorage.getItem("jwt");
    const fullName = localStorage.getItem("fullName");
    return role ? { role, token, fullName } : null;
  });

  useEffect(() => {
    const token = user?.token;
    if (!token || isTokenExpired(token)) {
      if (user !== null) logout(); // only logout if user is still set
    } else {
      try {
        const { exp } = jwtDecode(token);
        const timeout = exp * 1000 - Date.now();
        const timer = setTimeout(() => logout(), timeout);
        return () => clearTimeout(timer);
      } catch {
        if (user !== null) logout();
      }
    }
  }, [user?.token]);

  const login = (data) => {
    const userData = {
      token: data.token,
      role: data.role,
      fullName: data.fullName,
    };

    localStorage.setItem("jwt", userData.token);
    localStorage.setItem("role", userData.role);
    localStorage.setItem("fullName", userData.fullName);

    setUser(userData); // react state update triggers Navbar re-render
  };

  const navigate = useNavigate();

  const logout = () => {
    if (user === null) return;
    localStorage.clear();
    setUser(null);
    navigate("/login");
  };

  return (
    <AuthContext.Provider value={{ user, login, logout }}>
      {children}
    </AuthContext.Provider>
  );
}
