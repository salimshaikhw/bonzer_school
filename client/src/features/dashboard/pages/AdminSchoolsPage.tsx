import { AdminOrgCrudPanel } from '../components/AdminOrgCrudPanel'

export function AdminSchoolsPage() {
  return (
    <div>
      <h2>Admin - Schools (Branches)</h2>
      <AdminOrgCrudPanel scope="branches" />
    </div>
  )
}
