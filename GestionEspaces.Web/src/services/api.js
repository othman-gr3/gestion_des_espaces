import axios from 'axios';

const baseURL = import.meta.env.VITE_API_URL || 'http://localhost:5153/api';

const api = axios.create({
  baseURL,
  headers: {
    'Content-Type': 'application/json',
  },
});

// Request interceptor to attach bearer token
api.interceptors.request.use(
  (config) => {
    const token = localStorage.getItem('token');
    if (token) {
      config.headers.Authorization = `Bearer ${token}`;
    }
    return config;
  },
  (error) => {
    return Promise.reject(error);
  }
);

const clearSessionAndRedirect = () => {
  localStorage.removeItem('token');
  localStorage.removeItem('refreshToken');
  localStorage.removeItem('user');
  window.location.href = '/login';
};

// Access tokens are short-lived (30 min) by design — a 401 on any request is treated
// as "the access token expired" and triggers a silent refresh-then-retry, exactly once
// per request, before giving up and sending the user back to /login. Concurrent 401s
// share a single in-flight refresh instead of each firing their own.
let isRefreshing = false;
let pendingRequests = [];

const resolvePending = (newToken) => {
  pendingRequests.forEach((resolve) => resolve(newToken));
  pendingRequests = [];
};

// Response interceptor to handle token expiry / authorization errors globally
api.interceptors.response.use(
  (response) => response,
  async (error) => {
    const { response, config: originalRequest } = error;

    if (!response) {
      return Promise.reject(error);
    }

    if (response.status === 403) {
      clearSessionAndRedirect();
      return Promise.reject(error);
    }

    if (response.status === 401 && !originalRequest?._retriedAfterRefresh) {
      const refreshToken = localStorage.getItem('refreshToken');
      if (!refreshToken) {
        clearSessionAndRedirect();
        return Promise.reject(error);
      }

      originalRequest._retriedAfterRefresh = true;

      if (isRefreshing) {
        return new Promise((resolve) => {
          pendingRequests.push((newToken) => {
            originalRequest.headers.Authorization = `Bearer ${newToken}`;
            resolve(api(originalRequest));
          });
        });
      }

      isRefreshing = true;
      try {
        const refreshResponse = await axios.post(`${baseURL}/auth/refresh`, { refreshToken });
        const { token: newToken, refreshToken: newRefreshToken } = refreshResponse.data;

        localStorage.setItem('token', newToken);
        localStorage.setItem('refreshToken', newRefreshToken);

        resolvePending(newToken);
        originalRequest.headers.Authorization = `Bearer ${newToken}`;
        return api(originalRequest);
      } catch (refreshError) {
        pendingRequests = [];
        clearSessionAndRedirect();
        return Promise.reject(refreshError);
      } finally {
        isRefreshing = false;
      }
    }

    return Promise.reject(error);
  }
);

export default api;
