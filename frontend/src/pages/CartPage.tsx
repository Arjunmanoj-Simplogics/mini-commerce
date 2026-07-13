import { Alert, Box, Button, CircularProgress, Divider, Stack, Typography } from '@mui/material';
import { useEffect, useState } from 'react';
import { Link as RouterLink, useNavigate } from 'react-router-dom';
import { useAuth } from '../hooks/useAuth';
import { cartApi, type Cart } from '../services/commerceApi';

export default function CartPage() {
  const { isAuthenticated, ready } = useAuth();
  const navigate = useNavigate();
  const [cart, setCart] = useState<Cart | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!ready) return;
    if (!isAuthenticated) {
      navigate('/login');
      return;
    }
    void (async () => {
      try {
        setCart(await cartApi.get());
      } catch (err) {
        setError(err instanceof Error ? err.message : 'Failed to load cart');
      } finally {
        setLoading(false);
      }
    })();
  }, [isAuthenticated, navigate, ready]);

  if (!ready || loading) {
    return (
      <Stack sx={{ alignItems: 'center', py: 8 }}>
        <CircularProgress />
      </Stack>
    );
  }

  return (
    <Stack spacing={3} className="animate-rise" sx={{ maxWidth: 800, mx: 'auto' }}>
      <Typography variant="h3">Your bag</Typography>
      {error && <Alert severity="error">{error}</Alert>}
      <Box className="glass-panel" sx={{ borderRadius: 4, p: 3 }}>
        <Stack spacing={2}>
          {cart?.items.map((item) => (
            <Stack key={item.id} direction={{ xs: 'column', sm: 'row' }} sx={{ justifyContent: 'space-between', gap: 1 }}>
              <Box>
                <Typography sx={{ fontWeight: 600 }}>{item.productName}</Typography>
                <Typography variant="body2" color="text.secondary">
                  {item.productSku} · qty {item.quantity}
                </Typography>
              </Box>
              <Stack direction="row" spacing={2} sx={{ alignItems: 'center' }}>
                <Typography sx={{ fontWeight: 700 }}>${item.lineTotal.toFixed(2)}</Typography>
                <Button
                  color="error"
                  onClick={() =>
                    void cartApi
                      .removeItem(item.id)
                      .then(setCart)
                      .catch((err: Error) => setError(err.message))
                  }
                >
                  Remove
                </Button>
              </Stack>
            </Stack>
          ))}
          {(!cart || cart.items.length === 0) && (
            <Typography color="text.secondary">Your bag is empty. Head to the shop to fill it.</Typography>
          )}
          {cart && cart.items.length > 0 && (
            <>
              <Divider />
              <Stack direction="row" sx={{ justifyContent: 'space-between' }}>
                <Typography variant="h6">Total</Typography>
                <Typography variant="h6">${cart.totalAmount.toFixed(2)}</Typography>
              </Stack>
              <Button variant="contained" color="secondary" size="large" onClick={() => navigate('/checkout')}>
                Proceed to payment
              </Button>
            </>
          )}
          <Button component={RouterLink} to="/shop">
            Continue shopping
          </Button>
        </Stack>
      </Box>
    </Stack>
  );
}
