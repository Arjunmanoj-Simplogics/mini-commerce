import { orderApiClient, inventoryApiClient, notificationApiClient } from './apiClient';
import type {
  CreateOrderRequest,
  InventoryItem,
  NotificationItem,
  Order,
  UpdateOrderRequest,
} from '../models/order';

export const orderApi = {
  getAll: async (): Promise<Order[]> => (await orderApiClient.get<Order[]>('/api/orders')).data,
  getMine: async (): Promise<Order[]> => (await orderApiClient.get<Order[]>('/api/orders/mine')).data,
  getById: async (id: string): Promise<Order> => (await orderApiClient.get<Order>(`/api/orders/${id}`)).data,
  create: async (payload: CreateOrderRequest): Promise<Order> =>
    (await orderApiClient.post<Order>('/api/orders', payload)).data,
  update: async (id: string, payload: UpdateOrderRequest): Promise<Order> =>
    (await orderApiClient.put<Order>(`/api/orders/${id}`, payload)).data,
  delete: async (id: string): Promise<void> => {
    await orderApiClient.delete(`/api/orders/${id}`);
  },
};

export const inventoryApi = {
  getAll: async (): Promise<InventoryItem[]> =>
    (await inventoryApiClient.get<InventoryItem[]>('/api/inventory')).data,
  update: async (id: string, payload: { productName: string; quantityAvailable: number }): Promise<InventoryItem> =>
    (await inventoryApiClient.put<InventoryItem>(`/api/inventory/${id}`, payload)).data,
};

export const notificationApi = {
  getAll: async (): Promise<NotificationItem[]> =>
    (await notificationApiClient.get<NotificationItem[]>('/api/notifications')).data,
};
