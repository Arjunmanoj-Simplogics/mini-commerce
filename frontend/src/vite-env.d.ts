/// <reference types="vite/client" />

interface ImportMetaEnv {
  readonly VITE_API_BASE_URL: string;
  readonly VITE_INVENTORY_API_BASE_URL: string;
  readonly VITE_NOTIFICATION_API_BASE_URL: string;
  readonly VITE_AUTH_API_BASE_URL: string;
  readonly VITE_CATALOG_API_BASE_URL: string;
  readonly VITE_CART_API_BASE_URL: string;
}

interface ImportMeta {
  readonly env: ImportMetaEnv;
}
