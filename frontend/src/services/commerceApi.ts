import { authApiClient, cartApiClient, catalogApiClient, paymentApiClient } from './apiClient';
import { orderApi } from './orderService';

export interface AuthResponse {
  userId: string;
  email: string;
  fullName: string;
  role: string;
  token: string;
  expiresAtUtc: string;
}

export interface Product {
  id: string;
  sku: string;
  name: string;
  description: string;
  category: string;
  imageUrl: string;
  price: number;
  isActive: boolean;
}

export interface CartItem {
  id: string;
  productSku: string;
  productName: string;
  unitPrice: number;
  quantity: number;
  lineTotal: number;
}

export interface Cart {
  id: string;
  userId: string;
  items: CartItem[];
  totalAmount: number;
}

export interface AuthUserDto {
  id: string;
  email: string;
  fullName: string;
  role: string;
}

export interface ChargeRequest {
  amount: number;
  currency: string;
  cardHolder: string;
  cardNumber: string;
  expiryMonth: number;
  expiryYear: number;
  cvv: string;
}

export interface PaymentResult {
  paymentId: string;
  status: string;
  message: string;
  chargedAmount: number;
  currency: string;
  last4: string;
  createdAtUtc: string;
}

export const authApi = {
  register: async (email: string, fullName: string, password: string) =>
    (await authApiClient.post<AuthResponse>('/api/auth/register', { email, fullName, password })).data,
  login: async (email: string, password: string) =>
    (await authApiClient.post<AuthResponse>('/api/auth/login', { email, password })).data,
  me: async () => (await authApiClient.get<AuthUserDto>('/api/auth/me')).data,
};

export const catalogApi = {
  getAll: async (): Promise<Product[]> => (await catalogApiClient.get<Product[]>('/api/catalog')).data,
};

export const cartApi = {
  get: async (): Promise<Cart> => (await cartApiClient.get<Cart>('/api/cart')).data,
  addItem: async (payload: { productSku: string; productName: string; unitPrice: number; quantity: number }) =>
    (await cartApiClient.post<Cart>('/api/cart/items', payload)).data,
  removeItem: async (itemId: string) => (await cartApiClient.delete<Cart>(`/api/cart/items/${itemId}`)).data,
  clear: async () => {
    await cartApiClient.delete('/api/cart');
  },
};

export const paymentApi = {
  charge: async (payload: ChargeRequest): Promise<PaymentResult> =>
    (await paymentApiClient.post<PaymentResult>('/api/payments/charge', payload)).data,
};

export { orderApi };
