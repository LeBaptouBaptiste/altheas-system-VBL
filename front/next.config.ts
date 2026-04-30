import type { NextConfig } from "next";

// Build connect-src dynamically from NEXT_PUBLIC_API_URL when available,
// falling back to the local dev API endpoints.
const apiUrl = process.env.NEXT_PUBLIC_API_URL;
let apiOrigin: string | null = null;
if (apiUrl) {
  try {
    apiOrigin = new URL(apiUrl).origin;
  } catch {
    apiOrigin = null;
  }
}

const connectSrcOrigins = [
  "'self'",
  apiOrigin,
  'http://localhost:5207',
  'https://localhost:5207',
]
  .filter((v, i, arr): v is string => Boolean(v) && arr.indexOf(v) === i)
  .join(' ');

const csp = [
  "default-src 'self'",
  "script-src 'self' 'unsafe-inline' 'unsafe-eval'",
  "style-src 'self' 'unsafe-inline'",
  "img-src 'self' data: blob: https:",
  "font-src 'self' data:",
  `connect-src ${connectSrcOrigins}`,
  "frame-ancestors 'none'",
].join('; ') + ';';

const nextConfig: NextConfig = {
  output: 'standalone',
  images: {
    remotePatterns: [
      {
        protocol: 'https',
        hostname: 'images.unsplash.com',
      },
    ],
  },
  async headers() {
    return [
      {
        source: '/(.*)',
        headers: [
          { key: 'X-Frame-Options', value: 'DENY' },
          { key: 'X-Content-Type-Options', value: 'nosniff' },
          { key: 'Referrer-Policy', value: 'strict-origin-when-cross-origin' },
          { key: 'Permissions-Policy', value: 'camera=(), microphone=(), geolocation=()' },
          { key: 'Content-Security-Policy', value: csp },
        ],
      },
    ];
  },
};

export default nextConfig;
