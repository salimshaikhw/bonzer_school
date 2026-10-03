import { useEffect, useState } from 'react'
import { studentsApi } from '../api/students.api'
import type { Student } from '../students.types'

export function useStudent(id: string | null) {
  const [student, setStudent] = useState<Student | null>(null)
  const [loading, setLoading] = useState(!!id)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    if (!id) {
      setStudent(null)
      setLoading(false)
      return
    }
    setLoading(true)
    setError(null)
    studentsApi
      .getById(id)
      .then(setStudent)
      .catch(() => setError('Failed to load student'))
      .finally(() => setLoading(false))
  }, [id])

  return { student, loading, error }
}
