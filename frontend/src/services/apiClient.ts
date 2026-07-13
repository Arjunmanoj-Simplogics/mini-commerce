import axios, { type AxiosInstance } from 'axios';

const TOKEN_KEY = 'mc_auth_token';

export const getToken = () => localStorage.getItem(TOKEN_KEY);
export const setToken = (token: string | null) => {
  if (token) localStorage.setItem(TOKEN_KEY, token);
  else localStorage.removeItem(TOKEN_KEY);
};

export const orderApiClient = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL,
  headers: { 'Content-Type': 'application/json' },
});

export const inventoryApiClient = axios.create({
  baseURL: import.meta.env.VITE_INVENTORY_API_BASE_URL,
  headers: { 'Content-Type': 'application/json' },
});

export const notificationApiClient = axios.create({
  baseURL: import.meta.env.VITE_NOTIFICATION_API_BASE_URL,
  headers: { 'Content-Type': 'application/json' },
});

export const authApiClient = axios.create({
  baseURL: import.meta.env.VITE_AUTH_API_BASE_URL,
  headers: { 'Content-Type': 'application/json' },
});

export const catalogApiClient = axios.create({
  baseURL: import.meta.env.VITE_CATALOG_API_BASE_URL,
  headers: { 'Content-Type': 'application/json' },
});

export const cartApiClient = axios.create({
  baseURL: import.meta.env.VITE_CART_API_BASE_URL,
  headers: { 'Content-Type': 'application/json' },
});

export const paymentApiClient = axios.create({
  baseURL: import.meta.env.VITE_PAYMENT_API_BASE_URL,
  headers: { 'Content-Type': 'application/json' },
});

const attachAuth = (client: AxiosInstance) => {
  client.interceptors.request.use((config) => {
    const token = getToken();
    if (token) {
      config.headers.Authorization = `Bearer ${token}`;
    }
    return config;
  });
};

const attachErrorInterceptor = (client: AxiosInstance) => {
  client.interceptors.response.use(
    (response) => response,
    (error) => {
      const data = error.response?.data;
      const message =
        data?.message ??
        data?.detail ??
        data?.title ??
        error.message ??
        'An unexpected error occurred';
      return Promise.reject(new Error(typeof message === 'string' ? message : 'Request failed'));
    },
  );
};

[
  orderApiClient,
  inventoryApiClient,
  notificationApiClient,
  authApiClient,
  catalogApiClient,
  cartApiClient,
  paymentApiClient,
].forEach((client) => {
  attachAuth(client);
  attachErrorInterceptor(client);
});

export default orderApiClient;
