// HTTP client based on the seg_fault frontend.
// Empty base URL = same origin; in development the Vite proxy forwards /api.
const BASE_URL = import.meta.env.VITE_API_BASE_URL ?? ''

// Identifies which call failed so errors can be mapped to a user message
// by context (see services/errorMessages.ts), never by the backend text.
export type ApiEndpoint =
  | 'products.search'
  | 'cash.current'
  | 'cash.open'
  | 'cash.close'
  | 'sales.preview'
  | 'sales.register'

export class ApiError extends Error {
  constructor(
    public readonly endpoint: ApiEndpoint,
    public readonly status: number,
    public readonly code?: string,
  ) {
    // Technical message for logs only; the UI never shows it.
    super(`${endpoint} failed with HTTP ${status}${code ? ` (${code})` : ''}`)
    this.name = 'ApiError'
  }
}

export class NetworkError extends Error {
  constructor(public readonly endpoint: ApiEndpoint) {
    super(`${endpoint} could not reach the server`)
    this.name = 'NetworkError'
  }
}

async function readErrorCode(res: Response): Promise<string | undefined> {
  try {
    const data = (await res.json()) as { code?: unknown }
    return typeof data?.code === 'string' ? data.code : undefined
  } catch {
    // Body is empty or not JSON.
    return undefined
  }
}

async function request<T>(endpoint: ApiEndpoint, path: string, init?: RequestInit): Promise<T> {
  let res: Response
  try {
    res = await fetch(`${BASE_URL}${path}`, init)
  } catch {
    throw new NetworkError(endpoint)
  }

  if (!res.ok) {
    throw new ApiError(endpoint, res.status, await readErrorCode(res))
  }

  return (await res.json()) as T
}

export function apiGet<T>(endpoint: ApiEndpoint, path: string): Promise<T> {
  return request<T>(endpoint, path, { headers: { Accept: 'application/json' } })
}

export function apiPost<TRequest, TResponse>(
  endpoint: ApiEndpoint,
  path: string,
  body: TRequest,
): Promise<TResponse> {
  return request<TResponse>(endpoint, path, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json', Accept: 'application/json' },
    body: JSON.stringify(body),
  })
}
