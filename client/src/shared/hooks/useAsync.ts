import { useCallback, useState } from 'react'

interface AsyncState<T> {
  data: T | null
  error: string | null
  loading: boolean
}

export function useAsync<T>() {
  const [state, setState] = useState<AsyncState<T>>({
    data: null,
    error: null,
    loading: false,
  })

  const execute = useCallback(async (promise: Promise<T>) => {
    setState({ data: null, error: null, loading: true })
    try {
      const data = await promise
      setState({ data, error: null, loading: false })
      return data
    } catch (err: unknown) {
      const message =
        err && typeof err === 'object' && 'response' in err
          ? (err as { response?: { data?: { message?: string } } }).response?.data?.message
          : 'Request failed'
      setState({ data: null, error: message ?? 'Request failed', loading: false })
      throw err
    }
  }, [])

  const reset = useCallback(() => {
    setState({ data: null, error: null, loading: false })
  }, [])

  return { ...state, execute, reset }
}
