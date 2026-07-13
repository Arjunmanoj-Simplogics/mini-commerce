import { useCallback, useEffect, useState } from 'react';
import { orderApi } from '../services/orderService';
import type { Order } from '../models/order';

interface UseOrdersResult {
  orders: Order[];
  loading: boolean;
  error: string | null;
  refresh: () => Promise<void>;
  removeOrder: (id: string) => Promise<void>;
}

export function useOrders(scope: 'mine' | 'all' = 'mine'): UseOrdersResult {
  const [orders, setOrders] = useState<Order[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const refresh = useCallback(async () => {
    setLoading(true);
    setError(null);

    try {
      const data = scope === 'all' ? await orderApi.getAll() : await orderApi.getMine();
      setOrders(data);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load orders');
    } finally {
      setLoading(false);
    }
  }, [scope]);

  const removeOrder = useCallback(async (id: string) => {
    await orderApi.delete(id);
    setOrders((current) => current.filter((order) => order.id !== id));
  }, []);

  useEffect(() => {
    void refresh();
  }, [refresh]);

  return { orders, loading, error, refresh, removeOrder };
}
