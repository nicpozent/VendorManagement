import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";

// The SPA calls the API under /api; in dev we proxy that to the .NET backend.
export default defineConfig({
  plugins: [react()],
  server: {
    port: 5173,
    proxy: {
      "/api": {
        target: process.env.VITE_API_PROXY ?? "http://localhost:5080",
        changeOrigin: true,
        rewrite: (p) => p.replace(/^\/api/, ""),
      },
    },
  },
});
