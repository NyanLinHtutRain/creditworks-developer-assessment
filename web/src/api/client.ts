interface ProblemDetails {
  title?: string
  errors?: string[]
}

// `errors` holds the API's list of user-ready messages; `message` is the short summary.
export class ApiError extends Error {
  readonly status: number
  readonly errors: string[]

  constructor(status: number, message: string, errors: string[] = []) {
    super(message)
    this.status = status
    this.errors = errors
  }
}

const jsonHeaders = { 'Content-Type': 'application/json' }

async function request<T>(url: string, init?: RequestInit): Promise<T> {
  let response: Response

  try {
    response = await fetch(url, init)
  } catch {
    throw new ApiError(0, 'The server could not be reached. Check that the API is running and try again.')
  }

  if (!response.ok) {
    const problem: ProblemDetails | null = await response.json().catch(() => null)
    throw new ApiError(response.status, problem?.title ?? 'Something went wrong. Please try again.', problem?.errors)
  }

  return response.status === 204 ? (undefined as T) : ((await response.json()) as T)
}

export const api = {
  get: <T>(url: string) => request<T>(url),
  post: <T>(url: string, body: unknown) =>
    request<T>(url, { method: 'POST', headers: jsonHeaders, body: JSON.stringify(body) }),
  put: <T>(url: string, body: unknown) =>
    request<T>(url, { method: 'PUT', headers: jsonHeaders, body: JSON.stringify(body) }),
  delete: (url: string) => request<void>(url, { method: 'DELETE' }),
}

// The messages to show a user for any failure, without technical detail.
export function errorMessages(error: unknown): string[] {
  if (error instanceof ApiError) {
    return error.errors.length > 0 ? error.errors : [error.message]
  }

  return ['Something went wrong. Please try again.']
}
