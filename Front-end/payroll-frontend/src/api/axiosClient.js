import axios from "axios";
import { normalizeKeys } from "../utils/normalizeKeys";
import { isTokenExpired } from "../utils/auth";

const api = axios.create({
  baseURL: "http://localhost:5111/api",
  headers: { "Content-Type": "application/json" },
});

api.defaults.headers.post["Content-Type"] = "application/json";

api.interceptors.request.use((config) => {
  if (
  config.url.includes("/auth/login") ||
  config.url.includes("/auth/forgot-password")
) {
  return config;
}

  const token = localStorage.getItem("jwt");
  if (!token || isTokenExpired(token)) {
    localStorage.clear();
    if (window.location.pathname !== "/login") {
      window.location.href = "/login";
    }
    return Promise.reject("Token expired");
  }

  config.headers.Authorization = `Bearer ${token}`;

  const fullName = localStorage.getItem("fullName");
  if (fullName) config.headers["X-User"] = fullName;

  return config;
});

api.interceptors.response.use(
  (response) => {
    const { responseType } = response.config || {};
    if (responseType === "blob" || responseType === "arraybuffer") {
      return response;
    }

    if (response.data && typeof response.data === "object") {
      response.data = normalizeKeys(response.data);
    }

    return response;
  },
  (error) => {
    // Only force logout redirect if not login request
    const originalRequest = error.config?.url || "";
    if (
      (error.response?.status === 401 || error.response?.status === 403) &&
      !originalRequest.includes("/auth/login")
    ) {
      localStorage.clear();
      window.location.href = "/login";
    }
    return Promise.reject(error);
  }
);

export default api;
