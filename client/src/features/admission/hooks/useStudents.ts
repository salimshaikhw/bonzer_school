import { useCallback, useEffect, useState } from 'react'
import { studentsApi } from '../api/students.api'
import type { PagedStudentsResponse, Student } from '../students.types'

const DEFAULT_PAGE_SIZE = 20

export function useStudents() {
  const [students, setStudents] = useState<Student[]>([])
  const [totalCount, setTotalCount] = useState(0)
  const [page, setPage] = useState(1)
  const [pageSize, setPageSize] = useState(DEFAULT_PAGE_SIZE)
  const [totalPages, setTotalPages] = useState(0)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  const applyPage = useCallback((p: PagedStudentsResponse) => {
    setStudents(p.items)
    setTotalCount(p.totalCount)
    setPage(p.page)
    setPageSize(p.pageSize)
    setTotalPages(p.totalPages)
  }, [])

  const load = useCallback(
    (nextPage: number, nextSize: number) => {
      setLoading(true)
      setError(null)
      studentsApi
        .getPaged(nextPage, nextSize)
        .then(applyPage)
        .catch(() => setError('Failed to load students'))
        .finally(() => setLoading(false))
    },
    [applyPage]
  )

  useEffect(() => {
    load(1, pageSize)
  }, [])

  const refetch = useCallback(() => {
    load(page, pageSize)
  }, [load, page, pageSize])

  const goToPage = useCallback(
    (next: number) => {
      const p = Math.max(1, Math.min(next, totalPages || 1))
      load(p, pageSize)
    },
    [load, pageSize, totalPages]
  )

  const changePageSize = useCallback(
    (nextSize: number) => {
      const size = Math.max(5, Math.min(100, nextSize))
      setPageSize(size)
      load(1, size)
    },
    [load]
  )

  return {
    students,
    totalCount,
    page,
    pageSize,
    totalPages,
    loading,
    error,
    refetch,
    goToPage,
    changePageSize,
  }
}
