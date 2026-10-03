import { AdminOrgCrudPanel } from '../components/AdminOrgCrudPanel'

export function AdminSchoolGroupsPage() {
  return (
    <div>
      <h2>Admin - School Groups</h2>
      <AdminOrgCrudPanel scope="groups" />
    </div>
  )
}
