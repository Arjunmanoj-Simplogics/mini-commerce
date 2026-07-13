import { Alert, CircularProgress, Stack, Typography } from '@mui/material';
import { useEffect, useMemo, useState } from 'react';
import ProductGrid from '../components/ProductGrid';
import { catalogApi, type Product } from '../services/commerceApi';
import { Chip, Box } from '@mui/material';

export default function ShopPage() {
  const [products, setProducts] = useState<Product[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [category, setCategory] = useState<string>('All');

  useEffect(() => {
    void (async () => {
      try {
        setProducts(await catalogApi.getAll());
      } catch (err) {
        setError(err instanceof Error ? err.message : 'Failed to load shop');
      } finally {
        setLoading(false);
      }
    })();
  }, []);

  const categories = useMemo(
    () => ['All', ...Array.from(new Set(products.map((p) => p.category))).sort()],
    [products],
  );

  const filtered = category === 'All' ? products : products.filter((p) => p.category === category);

  if (loading) {
    return (
      <Stack sx={{ alignItems: 'center', py: 8 }}>
        <CircularProgress />
      </Stack>
    );
  }

  return (
    <Stack spacing={3} className="animate-rise">
      <Box className="glass-panel" sx={{ borderRadius: 4, p: { xs: 2.5, md: 3.5 } }}>
        <Typography variant="h3" sx={{ mb: 1 }}>
          Shop
        </Typography>
        <Typography color="text.secondary" sx={{ mb: 2 }}>
          Browse the full MiniMart catalog — stock is reserved when you place an order.
        </Typography>
        <Stack direction="row" spacing={1} useFlexGap sx={{ flexWrap: 'wrap' }}>
          {categories.map((c) => (
            <Chip
              key={c}
              label={c}
              clickable
              color={category === c ? 'secondary' : 'default'}
              variant={category === c ? 'filled' : 'outlined'}
              onClick={() => setCategory(c)}
            />
          ))}
        </Stack>
      </Box>
      {error && <Alert severity="error">{error}</Alert>}
      <ProductGrid products={filtered} />
    </Stack>
  );
}
