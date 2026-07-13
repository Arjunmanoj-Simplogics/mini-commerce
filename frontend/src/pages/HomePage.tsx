import { Box, Button, Container, Stack, Typography } from '@mui/material';
import { Link as RouterLink } from 'react-router-dom';
import ProductGrid from '../components/ProductGrid';
import { useEffect, useState } from 'react';
import { catalogApi, type Product } from '../services/commerceApi';

export default function HomePage() {
  const [products, setProducts] = useState<Product[]>([]);

  useEffect(() => {
    void catalogApi.getAll().then(setProducts).catch(() => setProducts([]));
  }, []);

  const featured = products.slice(0, 4);

  return (
    <Box>
      <Box
        sx={{
          position: 'relative',
          minHeight: { xs: '78vh', md: '88vh' },
          display: 'flex',
          alignItems: 'flex-end',
          overflow: 'hidden',
          backgroundImage:
            'linear-gradient(120deg, rgba(16,38,28,0.72) 0%, rgba(16,38,28,0.35) 45%, rgba(16,38,28,0.15) 100%), url(https://images.unsplash.com/photo-1441986300917-64674bd600d8?w=1600&q=80)',
          backgroundSize: 'cover',
          backgroundPosition: 'center',
        }}
      >
        <Container maxWidth="lg" sx={{ pb: { xs: 6, md: 10 }, pt: 12 }}>
          <Stack spacing={2} className="animate-rise" sx={{ maxWidth: 640, color: '#fff' }}>
            <Typography
              component="h1"
              sx={{
                fontFamily: '"Sora", sans-serif',
                fontWeight: 700,
                letterSpacing: '-0.04em',
                fontSize: { xs: '2.8rem', md: '4.4rem' },
                lineHeight: 0.95,
              }}
            >
              MiniMart
            </Typography>
            <Typography variant="h5" sx={{ fontWeight: 500, opacity: 0.92, maxWidth: 480 }}>
              A glass storefront for everyday tech — browse, bag, and pay with a mock gateway.
            </Typography>
            <Stack direction="row" spacing={1.5} className="animate-rise-delay">
              <Button component={RouterLink} to="/shop" variant="contained" color="secondary" size="large">
                Start shopping
              </Button>
              <Button
                component={RouterLink}
                to="/cart"
                size="large"
                sx={{ color: '#fff', borderColor: 'rgba(255,255,255,0.5)' }}
                variant="outlined"
              >
                View cart
              </Button>
            </Stack>
          </Stack>
        </Container>
      </Box>

      <Container maxWidth="lg" sx={{ mt: { xs: -4, md: -6 }, px: { xs: 2, md: 3 }, position: 'relative', zIndex: 1 }}>
        <Box className="glass-panel animate-rise" sx={{ borderRadius: 4, p: { xs: 2.5, md: 4 } }}>
          <Stack spacing={1} sx={{ mb: 3 }}>
            <Typography variant="h4">Featured picks</Typography>
            <Typography color="text.secondary">Fresh stock across audio, wearables, and work gear.</Typography>
          </Stack>
          <ProductGrid products={featured} />
        </Box>
      </Container>
    </Box>
  );
}
