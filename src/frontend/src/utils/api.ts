/**
 * Extracts a user-friendly error message from a failed fetch response.
 */
export async function readApiError(res: Response, fallback = 'Request failed'): Promise<string> {
  try {
    const data = await res.json();
    if (typeof data === 'string') return data;
    if (data?.detail) return data.detail;
    if (data?.title && data?.detail) return `${data.title}: ${data.detail}`;
    if (data?.title) return data.title;
  } catch {
    // ignored — fall back below
  }
  return `${fallback} (${res.status})`;
}

/**
 * Normalizes any thrown error into a string for UI display.
 */
export function normalizeError(err: unknown, fallback = 'Something went wrong'): string {
  if (err instanceof Error && err.message) return err.message;
  if (typeof err === 'string' && err.trim()) return err;
  return fallback;
}
