import {
  Alert,
  Box,
  Button,
  CircularProgress,
  Stack,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  TextField,
  Typography,
} from '@mui/material';
import { useEffect, useState } from 'react';
import type { InventoryItem } from '../../models/order';
import { inventoryApi } from '../../services/orderService';

export default function AdminInventoryPage() {
  const [items, setItems] = useState<InventoryItem[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [message, setMessage] = useState<string | null>(null);
  const [drafts, setDrafts] = useState<Record<string, string>>({});
  const [savingId, setSavingId] = useState<string | null>(null);

  useEffect(() => {
    void (async () => {
      try {
        const data = await inventoryApi.getAll();
        setItems(data);
        setDrafts(Object.fromEntries(data.map((i) => [i.id, String(i.quantityAvailable)])));
      } catch (err) {
        setError(err instanceof Error ? err.message : 'Failed to load inventory');
      } finally {
        setLoading(false);
      }
    })();
  }, []);

  const save = async (item: InventoryItem) => {
    const qty = Number(drafts[item.id]);
    if (!Number.isInteger(qty) || qty < 0) {
      setError('Quantity must be a non-negative whole number.');
      return;
    }
    setSavingId(item.id);
    setError(null);
    setMessage(null);
    try {
      const updated = await inventoryApi.update(item.id, {
        productName: item.productName,
        quantityAvailable: qty,
      });
      setItems((current) => current.map((row) => (row.id === item.id ? updated : row)));
      setMessage(`Updated ${item.productSku}`);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Update failed');
    } finally {
      setSavingId(null);
    }
  };

  if (loading) {
    return (
      <Stack sx={{ alignItems: 'center', py: 8 }}>
        <CircularProgress />
      </Stack>
    );
  }

  return (
    <Stack spacing={3} className="animate-rise">
      <Typography variant="h3">Admin · Inventory</Typography>
      <Typography color="text.secondary">Adjust available stock. Changes apply immediately for new orders.</Typography>
      {error && <Alert severity="error">{error}</Alert>}
      {message && <Alert severity="success">{message}</Alert>}
      <Box className="glass-panel" sx={{ borderRadius: 4, overflow: 'hidden' }}>
        <TableContainer>
          <Table>
            <TableHead>
              <TableRow>
                <TableCell>SKU</TableCell>
                <TableCell>Product</TableCell>
                <TableCell>Reserved</TableCell>
                <TableCell width={160}>Available</TableCell>
                <TableCell align="right">Save</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {items.map((item) => (
                <TableRow key={item.id}>
                  <TableCell>{item.productSku}</TableCell>
                  <TableCell>{item.productName}</TableCell>
                  <TableCell>{item.quantityReserved}</TableCell>
                  <TableCell>
                    <TextField
                      size="small"
                      type="number"
                      value={drafts[item.id] ?? ''}
                      onChange={(e) => setDrafts((d) => ({ ...d, [item.id]: e.target.value }))}
                      slotProps={{ htmlInput: { min: 0 } }}
                    />
                  </TableCell>
                  <TableCell align="right">
                    <Button
                      variant="contained"
                      color="secondary"
                      size="small"
                      disabled={savingId === item.id}
                      onClick={() => void save(item)}
                    >
                      {savingId === item.id ? 'Saving…' : 'Update'}
                    </Button>
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </TableContainer>
      </Box>
    </Stack>
  );
}
