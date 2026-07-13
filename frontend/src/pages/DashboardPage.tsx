import { Alert, Box, Chip, CircularProgress, Paper, Stack, Typography } from '@mui/material';
import { useOrders } from '../hooks/useOrders';

export default function DashboardPage() {
  const { orders, loading, error } = useOrders();

  if (loading) {
    return (
      <Stack sx={{ alignItems: 'center', py: 6 }}>
        <CircularProgress />
      </Stack>
    );
  }

  if (error) {
    return <Alert severity="error">{error}</Alert>;
  }

  const totalRevenue = orders.reduce((sum, order) => sum + order.totalAmount, 0);
  const pendingCount = orders.filter((order) => order.status === 'Pending').length;

  return (
    <Stack spacing={3}>
      <Typography variant="h4" sx={{ fontWeight: 700 }}>
        Dashboard
      </Typography>

      <Box
        sx={{
          display: 'grid',
          gap: 2,
          gridTemplateColumns: { xs: '1fr', md: 'repeat(3, 1fr)' },
        }}
      >
        <Paper sx={{ p: 3 }}>
          <Typography color="text.secondary">Total Orders</Typography>
          <Typography variant="h4">{orders.length}</Typography>
        </Paper>
        <Paper sx={{ p: 3 }}>
          <Typography color="text.secondary">Pending Orders</Typography>
          <Typography variant="h4">{pendingCount}</Typography>
        </Paper>
        <Paper sx={{ p: 3 }}>
          <Typography color="text.secondary">Total Revenue</Typography>
          <Typography variant="h4">${totalRevenue.toFixed(2)}</Typography>
        </Paper>
      </Box>

      <Paper sx={{ p: 3 }}>
        <Typography variant="h6" gutterBottom>
          Recent Orders
        </Typography>
        <Stack spacing={1}>
          {orders.slice(0, 5).map((order) => (
            <Stack
              key={order.id}
              direction="row"
              sx={{ justifyContent: 'space-between', alignItems: 'center' }}
            >
              <Typography>{order.orderNumber}</Typography>
              <Chip label={order.status} size="small" />
            </Stack>
          ))}
          {orders.length === 0 && <Typography color="text.secondary">No orders yet.</Typography>}
        </Stack>
      </Paper>
    </Stack>
  );
}
