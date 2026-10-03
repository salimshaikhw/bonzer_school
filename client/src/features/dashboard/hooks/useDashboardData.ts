import { useEffect, useState } from 'react'
import { branchesApi } from '../../branches'
import { examsApi } from '../../exams'
import type { Branch } from '../../branches'
import type { Exam } from '../../exams'

interface DashboardData {
  branches: Branch[]
  exams: Exam[]
}

export function useDashboardData() {
  const [data, setData] = useState<DashboardData>({ branches: [], exams: [] })
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    let cancelled = false
    setLoading(true)
    setError(null)

    void (async () => {
      const [brResult, exResult] = await Promise.all([
        branchesApi.getAll().then(
          (v) => ({ ok: true as const, v }),
          () => ({ ok: false as const, v: [] as Branch[] }),
        ),
        examsApi.getAll().then(
          (v: Exam[]) => ({ ok: true as const, v }),
          () => ({ ok: false as const, v: [] as Exam[] }),
        ),
      ])

      if (cancelled) return

      if (!brResult.ok && !exResult.ok) {
        setError('Failed to load data. Is the API running, database up, and are you signed in?')
        setData({ branches: [], exams: [] })
      } else {
        setData({ branches: brResult.v, exams: exResult.v })
        if (!brResult.ok) {
          setError('Branches could not be loaded. Exams shown if available.')
        } else if (!exResult.ok) {
          setError('Exams could not be loaded. Branches shown if available.')
        } else {
          setError(null)
        }
      }
    })().finally(() => {
      if (!cancelled) setLoading(false)
    })

    return () => {
      cancelled = true
    }
  }, [])

  return { ...data, loading, error }
}
