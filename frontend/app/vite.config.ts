import { defineConfig, loadEnv } from "vite";
import react from "@vitejs/plugin-react";

// https://vite.dev/config/
export default defineConfig(({ mode }) => {
  const env = loadEnv(mode, process.cwd(), "");
  return {
    plugins: [react()],
    server: {
      host: true,
      watch: {
        usePolling: true,
      },
      proxy: {
        "/api": {
          // In Docker: use service name 'task-api', locally: use localhost
          // VITE_API_URL can be set to override (e.g., for local dev outside Docker)
          target: env.VITE_API_URL || (process.env.DOCKER_ENV ? "http://task-api:8080" : "http://localhost:8080"),
          changeOrigin: true,
          rewrite: (path) => path, // Don't rewrite, pass as-is
        },
      },
    },
  };
});
