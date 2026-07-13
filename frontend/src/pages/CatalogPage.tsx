import { Alert, Button, CircularProgress, Paper, Stack, Typography } from '@mui/material';
import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useAuth } from '../hooks/useAuth';
import { cartApi, catalogApi, type Product } from '../services/commerceApi';

export default function CatalogPage() {
  const { isAuthenticated } = useAuth();
  const navigate = useNavigate();
  const [products, setProducts] = useState<Product[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [message, setMessage] = useState<string | null>(null);

  useEffect(() => {
    void (async () => {
      try {
        setProducts(await catalogApi.getAll());
      } catch (err) {
        setError(err instanceof Error ? err.message : 'Failed to load catalog');
      } finally {
        setLoading(false);
      }
    })();
  }, []);

  const addToCart = async (product: Product) => {
    if (!isAuthenticated) {
      navigate('/login');
      return;
    }
    setMessage(null);
    try {
      await cartApi.addItem({
        productSku: product.sku,
        productName: product.name,
        unitPrice: product.price,
        quantity: 1,
      });
      setMessage(`${product.name} added to cart`);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to add to cart');
    }
  };

  if (loading) {
    return (
      <Stack sx={{ alignItems: 'center', py: 6 }}>
        <CircularProgress />
      </Stack>
    );
  }

  return (
    <Stack spacing={3}>
      <Typography variant="h4" sx={{ fontWeight: 700 }}>
        Catalog
      </Typography>
      {error && <Alert severity="error">{error}</Alert>}
      {message && <Alert severity="success">{message}</Alert>}
      <Stack spacing={2}>
        {products.map((product) => (
          <Paper key={product.id} sx={{ p: 2, display: 'flex', justifyContent: 'space-between', gap: 2, flexWrap: 'wrap' }}>
            <Stack>
              <Typography variant="h6">{product.name}</Typography>
              <Typography color="text.secondary">{product.sku}</Typography>
              <Typography>{product.description}</Typography>
              <Typography sx={{ fontWeight: 700 }}>${product.price.toFixed(2)}</Typography>
            </Stack>
            <Button variant="contained" onClick={() => void addToCart(product)}>
              Add to cart
            </Button>
          </Paper>
        ))}
      </Stack>
    </Stack>
  );
}
