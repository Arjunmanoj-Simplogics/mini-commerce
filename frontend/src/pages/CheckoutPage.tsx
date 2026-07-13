import {
  Alert,
  Box,
  Button,
  CircularProgress,
  Stack,
  TextField,
  Typography,
} from '@mui/material';
import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useAuth } from '../hooks/useAuth';
import { cartApi, orderApi, paymentApi, type Cart } from '../services/commerceApi';

export default function CheckoutPage() {
  const { isAuthenticated, user, ready } = useAuth();
  const navigate = useNavigate();
  const [cart, setCart] = useState<Cart | null>(null);
  const [loading, setLoading] = useState(true);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [cardHolder, setCardHolder] = useState('');
  const [cardNumber, setCardNumber] = useState('4111111111111111');
  const [expiry, setExpiry] = useState('12/30');
  const [cvv, setCvv] = useState('123');

  useEffect(() => {
    if (!ready) return;
    if (!isAuthenticated) {
      navigate('/login');
      return;
    }
    void (async () => {
      try {
        const c = await cartApi.get();
        setCart(c);
        if (user?.fullName) setCardHolder(user.fullName);
        if (c.items.length === 0) navigate('/cart');
      } catch (err) {
        setError(err instanceof Error ? err.message : 'Failed to load checkout');
      } finally {
        setLoading(false);
      }
    })();
  }, [isAuthenticated, navigate, ready, user?.fullName]);

  const pay = async () => {
    if (!cart || !user) return;
    setBusy(true);
    setError(null);
    try {
      const [monthRaw, yearRaw] = expiry.split('/');
      const expiryMonth = Number(monthRaw);
      const expiryYear = Number(yearRaw?.length === 2 ? `20${yearRaw}` : yearRaw);

      await paymentApi.charge({
        amount: cart.totalAmount,
        currency: 'USD',
        cardHolder,
        cardNumber,
        expiryMonth,
        expiryYear,
        cvv,
      });

      let lastOrderId: string | null = null;
      for (const item of cart.items) {
        const order = await orderApi.create({
          customerName: user.fullName,
          email: user.email,
          productSku: item.productSku,
          quantity: item.quantity,
          totalAmount: item.lineTotal,
        });
        lastOrderId = order.id;
      }

      await cartApi.clear();
      navigate(lastOrderId ? `/orders/${lastOrderId}` : '/orders');
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Payment failed');
    } finally {
      setBusy(false);
    }
  };

  if (!ready || loading) {
    return (
      <Stack sx={{ alignItems: 'center', py: 8 }}>
        <CircularProgress />
      </Stack>
    );
  }

  return (
    <Stack spacing={3} className="animate-rise" sx={{ maxWidth: 560, mx: 'auto' }}>
      <Typography variant="h3">Mock payment</Typography>
      <Typography color="text.secondary">
        Use any card number. Cards ending in <strong>0000</strong> are declined on purpose.
      </Typography>
      {error && <Alert severity="error">{error}</Alert>}
      <Box className="glass-panel" sx={{ borderRadius: 4, p: 3 }}>
        <Stack spacing={2}>
          <Typography sx={{ fontWeight: 700 }}>
            Charge ${cart?.totalAmount.toFixed(2) ?? '0.00'}
          </Typography>
          <TextField label="Name on card" value={cardHolder} onChange={(e) => setCardHolder(e.target.value)} fullWidth />
          <TextField label="Card number" value={cardNumber} onChange={(e) => setCardNumber(e.target.value)} fullWidth />
          <Stack direction="row" spacing={2}>
            <TextField label="MM/YY" value={expiry} onChange={(e) => setExpiry(e.target.value)} fullWidth />
            <TextField label="CVV" value={cvv} onChange={(e) => setCvv(e.target.value)} fullWidth />
          </Stack>
          <Button variant="contained" color="secondary" size="large" disabled={busy} onClick={() => void pay()}>
            {busy ? 'Processing…' : 'Pay & place order'}
          </Button>
        </Stack>
      </Box>
    </Stack>
  );
}
