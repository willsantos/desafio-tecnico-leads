// Fetch wrapper shared by every feature's `{step}Api.ts` (design.md "Frontend — src/features/{step}/").
// Base URL and Problem Details (RFC 9457) parsing live here once so every feature gets a
// real error message instead of re-implementing fetch + error handling per step.
export const API_BASE_URL = import.meta.env.VITE_API_URL ?? 'http://localhost:8080'

/** RFC 9457 Problem Details shape (README seção 5), plus the ASP.NET Core `ValidationProblem`
 * shape (`errors`) that 400 responses from this API use for request-level validation. */
export interface ProblemDetails {
  type?: string
  title?: string
  status?: number
  detail?: string
  extensions?: Record<string, unknown>
  errors?: Record<string, string[]>
}

export class ApiError extends Error {
  readonly status: number
  readonly problem: ProblemDetails

  constructor(status: number, problem: ProblemDetails) {
    super(problem.detail ?? problem.title ?? `Requisição falhou com status ${status}`)
    this.status = status
    this.problem = problem
  }
}

/** Extracts a human-readable message from any error thrown by `httpClient`, including the
 * pendency reasons (`extensions.reasons`) and validation errors (`errors`) this API returns. */
export function getErrorMessage(error: unknown): string {
  if (error instanceof ApiError) {
    const reasons = error.problem.extensions?.reasons
    if (Array.isArray(reasons) && reasons.length > 0) {
      return reasons.join('; ')
    }

    if (error.problem.errors) {
      const messages = Object.values(error.problem.errors).flat()
      if (messages.length > 0) {
        return messages.join('; ')
      }
    }

    return error.problem.detail ?? error.problem.title ?? `Erro (status ${error.status})`
  }

  if (error instanceof Error) {
    return error.message
  }

  return 'Erro inesperado.'
}

/** Pendency reasons from a 422 Problem Details body (`extensions.reasons`), or `null` when absent. */
export function getPendingReasons(error: unknown): string[] | null {
  if (!(error instanceof ApiError)) {
    return null
  }

  const reasons = error.problem.extensions?.reasons
  return Array.isArray(reasons) ? reasons.map(String) : null
}

async function request<T>(path: string, init: RequestInit = {}): Promise<T> {
  const isFormData = init.body instanceof FormData
  const headers: HeadersInit = isFormData
    ? (init.headers ?? {})
    : { 'Content-Type': 'application/json', ...(init.headers ?? {}) }

  const response = await fetch(`${API_BASE_URL}${path}`, { ...init, headers })

  if (response.status === 204) {
    return undefined as T
  }

  const text = await response.text()
  const data = text.length > 0 ? JSON.parse(text) : undefined

  if (!response.ok) {
    throw new ApiError(response.status, (data ?? {}) as ProblemDetails)
  }

  return data as T
}

export const httpClient = {
  get: <T>(path: string) => request<T>(path),
  post: <T>(path: string, body?: unknown) =>
    request<T>(path, { method: 'POST', body: body !== undefined ? JSON.stringify(body) : undefined }),
  put: <T>(path: string, body: unknown) => request<T>(path, { method: 'PUT', body: JSON.stringify(body) }),
  patch: <T>(path: string, body?: unknown) =>
    request<T>(path, { method: 'PATCH', body: body !== undefined ? JSON.stringify(body) : undefined }),
  delete: <T>(path: string) => request<T>(path, { method: 'DELETE' }),
  postForm: <T>(path: string, form: FormData) => request<T>(path, { method: 'POST', body: form }),
}
