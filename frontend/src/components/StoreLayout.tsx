import {
  AppBar,
  Badge,
  Box,
  Button,
  Container,
  IconButton,
  Stack,
  Toolbar,
  Typography,
  useMediaQuery,
  useTheme,
} from '@mui/material';
import ShoppingCartOutlinedIcon from '@mui/icons-material/ShoppingCartOutlined';
import StorefrontOutlinedIcon from '@mui/icons-material/StorefrontOutlined';
import Inventory2OutlinedIcon from '@mui/icons-material/Inventory2Outlined';
import ReceiptLongOutlinedIcon from '@mui/icons-material/ReceiptLongOutlined';
import { useEffect, useState } from 'react';
import { Link as RouterLink, Outlet, useLocation } from 'react-router-dom';
import { useAuth } from '../hooks/useAuth';
import { cartApi } from '../services/commerceApi';

export default function StoreLayout() {
  const theme = useTheme();
  const isMobile = useMediaQuery(theme.breakpoints.down('md'));
  const location = useLocation();
  const { isAuthenticated, user, logout, ready } = useAuth();
  const [cartCount, setCartCount] = useState(0);
  const isAdmin = user?.role === 'Admin';
  const isHome = location.pathname === '/';

  useEffect(() => {
    if (!ready || !isAuthenticated) {
      setCartCount(0);
      return;
    }
    void cartApi
      .get()
      .then((cart) => setCartCount(cart.items.reduce((sum, i) => sum + i.quantity, 0)))
      .catch(() => setCartCount(0));
  }, [ready, isAuthenticated, location.pathname]);

  return (
    <Box sx={{ minHeight: '100vh', display: 'flex', flexDirection: 'column' }}>
      <AppBar
        position="sticky"
        elevation={0}
        sx={{
          mt: { xs: 1, md: 2 },
          mx: { xs: 1, md: 3 },
          width: { xs: 'auto', md: 'calc(100% - 48px)' },
          borderRadius: 3,
          bgcolor: 'rgba(255,255,255,0.55)',
          color: 'text.primary',
          border: '1px solid rgba(255,255,255,0.7)',
          backdropFilter: 'blur(18px)',
          boxShadow: '0 12px 40px rgba(16,38,28,0.08)',
        }}
      >
        <Toolbar sx={{ gap: 1, flexWrap: 'wrap', py: 1 }}>
          <Stack direction="row" spacing={1} sx={{ alignItems: 'center', mr: 'auto' }} component={RouterLink} to="/">
            <StorefrontOutlinedIcon sx={{ color: 'secondary.main' }} />
            <Typography
              variant="h6"
              sx={{
                fontFamily: '"Sora", sans-serif',
                fontWeight: 700,
                letterSpacing: '-0.03em',
                fontSize: { xs: '1.1rem', md: '1.35rem' },
              }}
            >
              MiniMart
            </Typography>
          </Stack>

          {!isMobile && (
            <Stack direction="row" spacing={1}>
              <Button component={RouterLink} to="/shop" color="inherit">
                Shop
              </Button>
              {isAuthenticated && (
                <Button component={RouterLink} to="/orders" color="inherit">
                  My orders
                </Button>
              )}
              {isAdmin && (
                <>
                  <Button component={RouterLink} to="/admin/inventory" color="inherit" startIcon={<Inventory2OutlinedIcon />}>
                    Inventory
                  </Button>
                  <Button component={RouterLink} to="/admin/orders" color="inherit" startIcon={<ReceiptLongOutlinedIcon />}>
                    All orders
                  </Button>
                </>
              )}
            </Stack>
          )}

          <IconButton component={RouterLink} to="/cart" color="inherit" aria-label="Cart">
            <Badge badgeContent={cartCount} color="secondary">
              <ShoppingCartOutlinedIcon />
            </Badge>
          </IconButton>

          {isAuthenticated ? (
            <Stack direction="row" spacing={1} sx={{ alignItems: 'center' }}>
              {!isMobile && (
                <Typography variant="body2" color="text.secondary">
                  {user?.fullName}
                </Typography>
              )}
              <Button color="inherit" onClick={logout}>
                Sign out
              </Button>
            </Stack>
          ) : (
            <Button component={RouterLink} to="/login" variant="contained" color="secondary" size="small">
              Sign in
            </Button>
          )}
        </Toolbar>
      </AppBar>

      <Box component="main" sx={{ flex: 1, pb: 6 }}>
        <Container maxWidth={isHome ? false : 'lg'} disableGutters={isHome} sx={{ px: isHome ? 0 : { xs: 2, md: 3 }, pt: isHome ? 0 : 3 }}>
          <Outlet />
        </Container>
      </Box>

      <Box
        component="footer"
        className="glass-panel"
        sx={{ mx: { xs: 1, md: 3 }, mb: 2, borderRadius: 3, px: 3, py: 2 }}
      >
        <Typography variant="body2" color="text.secondary">
          MiniMart — mock payments, live inventory, microservices under glass.
        </Typography>
      </Box>
    </Box>
  );
}
