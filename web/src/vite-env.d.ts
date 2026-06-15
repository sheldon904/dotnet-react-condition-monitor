/// <reference types="vite/client" />

// Declare the custom env var so import.meta.env.VITE_API_BASE is type-safe.
interface ImportMetaEnv {
  readonly VITE_API_BASE?: string;
}

interface ImportMeta {
  readonly env: ImportMetaEnv;
}
