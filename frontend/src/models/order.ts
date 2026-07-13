export interface Order {
  id: string;
  orderNumber: string;
  customerName: string;
  email: string;
  productSku: string;
  quantity: number;
  totalAmount: number;
  status: string;
  createdDate: string;
  updatedDate: string;
}

export interface CreateOrderRequest {
  customerName: string;
  email: string;
  productSku: string;
  quantity: number;
  totalAmount: number;
}

export interface UpdateOrderRequest {
  customerName: string;
  email: string;
  productSku: string;
  quantity: number;
  totalAmount: number;
  status: string;
}

export interface InventoryItem {
  id: string;
  productSku: string;
  productName: string;
  quantityAvailable: number;
  quantityReserved: number;
  createdDate: string;
  updatedDate: string;
}

export interface NotificationItem {
  id: string;
  orderId?: string;
  recipientEmail: string;
  subject: string;
  body: string;
  type: string;
  status: string;
  createdDate: string;
  sentDate?: string;
}
