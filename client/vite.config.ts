/// <reference types="vitest/config" />
import react from '@vitejs/plugin-react'
import { defineConfig } from 'vite'

// https://vite.dev/config/
export default defineConfig({
  plugins: [react()],
  resolve: {
    // Resolve these from client/node_modules even when imported by the tests in ../tests/client.
    dedupe: ['react', 'react-dom', 'react-router-dom', '@testing-library/react', '@testing-library/user-event'],
  },
  server: {
    // Lets the test runner load the tests in ../tests/client.
    fs: { allow: ['..'] },
    // Forward API calls to the backend's "http" launch profile.
    proxy: {
      '/api': 'http://localhost:5084',
    },
  },
  test: {
    // Frontend tests live in tests/client at the repository root.
    dir: '../tests/client',
    environment: 'jsdom',
  },
})
