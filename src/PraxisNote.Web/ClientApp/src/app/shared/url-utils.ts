/**
 * Normalizes and validates a URL against an allowed protocol list.
 * - Trims whitespace
 * - Auto-prepends https:// if no protocol present
 * - Returns null for invalid URLs or disallowed protocols
 */
export function normalizeUrl(
  input: string,
  allowedProtocols: string[] = ['http:', 'https:', 'mailto:'],
): string | null {
  const trimmed = input.trim();
  if (!trimmed) return null;

  try {
    const hasProtocol = /^[a-zA-Z][a-zA-Z0-9+.-]*:/.test(trimmed);
    const candidate = hasProtocol ? trimmed : `https://${trimmed}`;
    const url = new URL(candidate);

    if (!allowedProtocols.includes(url.protocol)) {
      return null;
    }

    return url.toString();
  } catch {
    return null;
  }
}

/** Normalizes a URL for links (allows http, https, mailto) */
export function normalizeLinkUrl(input: string): string | null {
  return normalizeUrl(input, ['http:', 'https:', 'mailto:']);
}

/** Normalizes a URL for images (allows http, https only) */
export function normalizeImageUrl(input: string): string | null {
  return normalizeUrl(input, ['http:', 'https:']);
}
