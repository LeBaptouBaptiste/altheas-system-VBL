'use client';

import QRCode from 'react-qr-code';

/**
 * Renders an `otpauth://` URI as a QR code that any authenticator app
 * (Google Authenticator, Authy, 1Password, Bitwarden…) can scan.
 *
 * The white background + 16-px padding give the QR enough quiet zone to
 * stay scannable on dark themes and on phones with imperfect autofocus.
 * Text fallback (manual secret entry) is shown by the parent component
 * for users without a camera.
 */
interface OtpQrCodeProps {
  /** otpauth://totp/... URI returned by /api/auth/2fa/setup */
  uri: string;
  /** Pixel size of the rendered SVG. Default 200 — large enough to scan, small enough to fit in a card. */
  size?: number;
}

export function OtpQrCode({ uri, size = 200 }: OtpQrCodeProps) {
  return (
    <div
      className="bg-white rounded-md p-4 inline-block"
      role="img"
      aria-label="2FA setup QR code"
    >
      <QRCode value={uri} size={size} level="M" />
    </div>
  );
}
