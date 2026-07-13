import { useCallback, useEffect, useState } from 'react';
import { orderApi } from '../services/orderService';
import type { Order } from '../models/order';

interface UseOrderResult {
  order: Order | null;
  loading: boolean;
  error: string | null;
  refresh: () => Promise<void>;
}

export function useOrder(id: string | undefined): UseOrderResult {
  const [order, setOrder] = useState<Order | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const refresh = useCallback(async () => {
    if (!id) {
      setLoading(false);
      return;
    }

    setLoading(true);
    setError(null);

    try {
      const data = await orderApi.getById(id);
      setOrder(data);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load order');
    } finally {
      setLoading(false);
    }
  }, [id]);

  useEffect(() => {
    void refresh();
  }, [refresh]);

  return { order, loading, error, refresh };
}
