import {
  Alert,
  Box,
  Button,
  CardMedia,
  Chip,
  Grid,
  Snackbar,
  Stack,
  Typography,
} from '@mui/material';
import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useAuth } from '../hooks/useAuth';
import { cartApi, type Product } from '../services/commerceApi';

interface Props {
  products: Product[];
}

export default function ProductGrid({ products }: Props) {
  const { isAuthenticated } = useAuth();
  const navigate = useNavigate();
  const [message, setMessage] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);

  const addToCart = async (product: Product) => {
    if (!isAuthenticated) {
      navigate('/login');
      return;
    }
    try {
      await cartApi.addItem({
        productSku: product.sku,
        productName: product.name,
        unitPrice: product.price,
        quantity: 1,
      });
      setMessage(`${product.name} added to bag`);
      setError(null);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Could not add to cart');
    }
  };

  if (products.length === 0) {
    return <Typography color="text.secondary">No products yet.</Typography>;
  }

  return (
    <>
      <Grid container spacing={2.5}>
        {products.map((product, index) => (
          <Grid key={product.id} size={{ xs: 12, sm: 6, md: 3 }}>
            <Box
              className="glass-panel"
              sx={{
                borderRadius: 3,
                overflow: 'hidden',
                height: '100%',
                display: 'flex',
                flexDirection: 'column',
                transition: 'transform 0.25s ease, box-shadow 0.25s ease',
                animation: `rise-in 0.6s ease ${index * 0.05}s both`,
                '&:hover': {
                  transform: 'translateY(-4px)',
                  boxShadow: '0 22px 50px rgba(16,38,28,0.16)',
                },
              }}
            >
              <CardMedia
                component="img"
                image={product.imageUrl || 'https://images.unsplash.com/photo-1526170375885-4d8ecf77b99f?w=800&q=80'}
                alt={product.name}
                sx={{ height: 180, objectFit: 'cover' }}
              />
              <Stack spacing={1} sx={{ p: 2, flex: 1 }}>
                <Chip label={product.category} size="small" sx={{ alignSelf: 'flex-start', bgcolor: 'rgba(27,67,50,0.08)' }} />
                <Typography variant="h6" sx={{ lineHeight: 1.25 }}>
                  {product.name}
                </Typography>
                <Typography variant="body2" color="text.secondary" sx={{ flex: 1 }}>
                  {product.description}
                </Typography>
                <Stack direction="row" sx={{ justifyContent: 'space-between', alignItems: 'center', pt: 1 }}>
                  <Typography sx={{ fontWeight: 700, fontFamily: '"Sora", sans-serif' }}>
                    ${product.price.toFixed(2)}
                  </Typography>
                  <Button size="small" variant="contained" color="secondary" onClick={() => void addToCart(product)}>
                    Add
                  </Button>
                </Stack>
              </Stack>
            </Box>
          </Grid>
        ))}
      </Grid>
      <Snackbar open={Boolean(message)} autoHideDuration={2500} onClose={() => setMessage(null)} message={message} />
      {error && (
        <Alert severity="error" sx={{ mt: 2 }} onClose={() => setError(null)}>
          {error}
        </Alert>
      )}
    </>
  );
}
