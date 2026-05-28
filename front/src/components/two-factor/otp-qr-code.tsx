'use client';

import QRCodeImport from 'react-qr-code';

// react-qr-code ships types that confuse React 19's stricter JSX element
// inference (the default export is typed as `typeof import('react-qr-code')`
// rather than a component). Cast once here so the rest of the file keeps a
// clean JSX call-site.
const QRCode = QRCodeImport as unknown as React.FC<{
  value: string;
  size?: number;
  level?: 'L' | 'M' | 'Q' | 'H';
  bgColor?: string;
  fgColor?: string;
}>;

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
