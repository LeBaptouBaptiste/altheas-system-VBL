import { api } from '@/lib/api';
import type {
  AuthResponse,
  LoginResponse,
  RegenerateRecoveryCodesResponse,
  RegisterResponse,
  StepUpPurpose,
  StepUpResponse,
  TwoFactorEnableResponse,
  TwoFactorSetupResult,
  TwoFactorStatus,
  UserDto,
} from '@/lib/api-types';

export const authService = {
  // ── Core ─────────────────────────────────────────────
  /**
   * Branches on the response:
   *   - outcome === 'Authenticated'           -> response.auth has the tokens
   *   - outcome === 'TwoFactorRequired'       -> POST /auth/2fa/verify with response.challengeToken
   *   - outcome === 'TwoFactorSetupRequired'  -> admin enrollment via /2fa/setup + /enable
   *                                               using response.setupToken as bearerToken
   */
  login: (email: string, password: string) =>
    api.post<LoginResponse>('/auth/login', { email, password }),

  /**
   * Phase 2: returns the created user but NO tokens. The user must confirm
   * their email (link mailed to them) before they can log in.
   */
  register: (name: string, email: string, password: string, confirmPassword: string) =>
    api.post<RegisterResponse>('/auth/register', { name, email, password, confirmPassword }),

  getMe: () =>
    api.get<UserDto>('/auth/me'),

  /**
   * Consumes the token from the confirmation link. Returns 200 on success;
   * on failure the API echoes a `reason` field:
   *   - "invalid_token"   → token doesn't match any row
   *   - "token_consumed"  → link already clicked — direct the user to /login
   *   - "token_expired"   → > 24 h since registration — offer /resend
   */
  confirmEmail: (token: string) =>
    api.post<void>('/auth/confirm-email', { token }),

  /**
   * Re-issues a fresh confirmation link. Always 200, regardless of whether
   * the email exists (anti-enumeration). Throttled at 1 mail / 5 min per
   * user server-side.
   */
  resendConfirmation: (email: string) =>
    api.post<void>('/auth/resend-confirmation', { email }),

  forgotPassword: (email: string) =>
    api.post<void>('/auth/forgot-password', { email }),

  resetPassword: (token: string, newPassword: string, confirmPassword: string) =>
    api.post<void>('/auth/reset-password', { token, newPassword, confirmPassword }),

  // ── 2FA login flow ───────────────────────────────────
  /**
   * Final step after a `TwoFactorRequired` login. The challenge token does
   * NOT live in localStorage; it's passed in the body.
   */
  verifyTwoFactorChallenge: (challengeToken: string, code: string) =>
    api.post<AuthResponse>('/auth/2fa/verify', { challengeToken, code }),

  // ── 2FA management ───────────────────────────────────
  getTwoFactorStatus: () =>
    api.get<TwoFactorStatus>('/auth/2fa/status'),

  /**
   * Returns { secret, otpAuthUri }. Pass `setupToken` for the admin forced
   * setup flow (the user has no access token yet); omit it for a regular
   * user voluntarily turning 2FA on.
   */
  setupTwoFactor: (setupToken?: string) =>
    api.post<TwoFactorSetupResult>(
      '/auth/2fa/setup',
      undefined,
      setupToken ? { bearerToken: setupToken } : undefined,
    ),

  /**
   * Confirms the TOTP code, activates 2FA server-side, and returns the
   * recovery codes (shown ONCE) plus a fresh AuthResponse with amr=mfa.
   * For an admin going through forced setup, this is the moment they
   * receive their first usable access token.
   */
  enableTwoFactor: (code: string, setupToken?: string) =>
    api.post<TwoFactorEnableResponse>(
      '/auth/2fa/enable',
      { code },
      setupToken ? { bearerToken: setupToken } : undefined,
    ),

  /**
   * Sensitive op — requires a fresh action step-up token (60s, single-use).
   * Caller obtains it via `stepUp({ purpose: 'Action', code })`, then calls
   * this with `stepUpToken`.
   */
  disableTwoFactor: (stepUpToken: string) =>
    api.post<void>('/auth/2fa/disable', undefined, { stepUpToken }),

  regenerateRecoveryCodes: (stepUpToken: string) =>
    api.post<RegenerateRecoveryCodesResponse>(
      '/auth/2fa/recovery-codes/regenerate',
      undefined,
      { stepUpToken },
    ),

  // ── Step-up ──────────────────────────────────────────
  /**
   * Requests a step-up token. Caller must already be authenticated with a
   * regular access token. Provide either:
   *   - `code` (TOTP 6 digits or recovery xxxx-xxxx-xxxx-xxxx)
   *   - `password` (only valid for purpose='Action' on accounts without 2FA)
   */
  stepUp: (purpose: StepUpPurpose, params: { code?: string; password?: string }) =>
    api.post<StepUpResponse>('/auth/step-up', { purpose, ...params }),
};
