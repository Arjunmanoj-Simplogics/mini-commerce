import {
  Alert,
  Button,
  Paper,
  Stack,
  TextField,
  Typography,
} from '@mui/material';
import { type FormEvent, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { orderApi } from '../services/orderService';

export default function CreateOrderPage() {
  const navigate = useNavigate();
  const [customerName, setCustomerName] = useState('');
  const [email, setEmail] = useState('');
  const [productSku, setProductSku] = useState('SKU-PHONE-01');
  const [quantity, setQuantity] = useState('1');
  const [totalAmount, setTotalAmount] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);

  const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setError(null);
    setSubmitting(true);

    try {
      const created = await orderApi.create({
        customerName,
        email,
        productSku,
        quantity: Number(quantity),
        totalAmount: Number(totalAmount),
      });
      navigate(`/orders/${created.id}`);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to create order');
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <Stack spacing={3}>
      <Typography variant="h4" sx={{ fontWeight: 700 }}>
        Create Order
      </Typography>

      <Paper sx={{ p: 3, maxWidth: 560 }}>
        {error && (
          <Alert severity="error" sx={{ mb: 2 }}>
            {error}
          </Alert>
        )}

        <Stack component="form" spacing={2} onSubmit={handleSubmit}>
          <TextField label="Customer Name" value={customerName} onChange={(e) => setCustomerName(e.target.value)} required fullWidth />
          <TextField label="Email" type="email" value={email} onChange={(e) => setEmail(e.target.value)} required fullWidth />
          <TextField
            label="Product SKU"
            value={productSku}
            onChange={(e) => setProductSku(e.target.value)}
            helperText="Use seeded SKUs: SKU-LAPTOP-01, SKU-PHONE-01, SKU-HEADSET-01"
            required
            fullWidth
          />
          <TextField
            label="Quantity"
            type="number"
            slotProps={{ htmlInput: { min: 1, step: 1 } }}
            value={quantity}
            onChange={(e) => setQuantity(e.target.value)}
            required
            fullWidth
          />
          <TextField
            label="Total Amount"
            type="number"
            slotProps={{ htmlInput: { min: 0.01, step: 0.01 } }}
            value={totalAmount}
            onChange={(e) => setTotalAmount(e.target.value)}
            required
            fullWidth
          />
          <Button type="submit" variant="contained" disabled={submitting}>
            {submitting ? 'Creating...' : 'Create Order'}
          </Button>
        </Stack>
      </Paper>
    </Stack>
  );
}
