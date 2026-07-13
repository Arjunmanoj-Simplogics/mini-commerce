import {
  Alert,
  Box,
  Button,
  Chip,
  CircularProgress,
  Stack,
  Typography,
} from '@mui/material';
import { Link as RouterLink, useParams } from 'react-router-dom';
import { useOrder } from '../hooks/useOrder';

export default function OrderDetailsPage() {
  const { id } = useParams<{ id: string }>();
  const { order, loading, error } = useOrder(id);

  if (loading) {
    return (
      <Stack sx={{ alignItems: 'center', py: 8 }}>
        <CircularProgress />
      </Stack>
    );
  }

  if (error || !order) {
    return (
      <Stack spacing={2} className="animate-rise">
        <Alert severity="error">{error ?? 'Order not found'}</Alert>
        <Button component={RouterLink} to="/orders" variant="outlined">
          Back to orders
        </Button>
      </Stack>
    );
  }

  return (
    <Stack spacing={3} className="animate-rise" sx={{ maxWidth: 640 }}>
      <Stack direction="row" sx={{ justifyContent: 'space-between', alignItems: 'center' }}>
        <Typography variant="h3">Order</Typography>
        <Button component={RouterLink} to="/orders" variant="outlined">
          Back
        </Button>
      </Stack>

      <Box className="glass-panel" sx={{ borderRadius: 4, p: 3 }}>
        <Stack spacing={2}>
          <Stack direction="row" sx={{ justifyContent: 'space-between', alignItems: 'center' }}>
            <Typography variant="h5">{order.orderNumber}</Typography>
            <Chip label={order.status} color="secondary" />
          </Stack>
          <Typography>
            <strong>Customer:</strong> {order.customerName}
          </Typography>
          <Typography>
            <strong>Email:</strong> {order.email}
          </Typography>
          <Typography>
            <strong>SKU:</strong> {order.productSku}
          </Typography>
          <Typography>
            <strong>Quantity:</strong> {order.quantity}
          </Typography>
          <Typography>
            <strong>Total:</strong> ${order.totalAmount.toFixed(2)}
          </Typography>
          <Typography color="text.secondary">
            Placed {new Date(order.createdDate).toLocaleString()}
          </Typography>
        </Stack>
      </Box>
    </Stack>
  );
}
