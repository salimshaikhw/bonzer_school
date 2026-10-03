import { BrowserRouter, Navigate, Route, Routes, useParams } from 'react-router-dom'
import { AuthProvider, useAuth } from './core'
import { ROUTES } from './config/routes'
import { ProtectedRoute } from './layouts/ProtectedRoute'
import { MainLayout } from './layouts/MainLayout'
import { LoginPage } from './features/auth'
import { RegisterPage, ForgotPasswordPage, UserListPage } from './features/user'
import {
  DashboardPage,
  AdminDashboardPage,
  AdminSchoolGroupsPage,
  AdminSchoolsPage,
  AdminClassesPage,
  AdminDivisionsPage,
} from './features/dashboard'
import {
  AdmissionModuleLayout,
  StudentListPage,
  StudentDetailPage,
  StudentFormPage,
  AdmissionAnalyticsPage,
  AdmissionDashboardPage,
} from './features/admission'
import {
  AttendanceBulkImportPage,
  AttendanceDashboardPage,
  AttendanceDivisionExcelPage,
  AttendanceDivisionPage,
  HolidayManagementPage,
} from './features/attendance'
import { ParentFeesPage } from './features/fees'
import { ExamsPage } from './features/exams'
import {
  FinanceModuleLayout,
  FinanceAnalyticsPage,
  FinanceInvoicesPage,
  FinancePaymentsPage,
  FeesCollectionPage,
  FeeMastersLayout,
  FeeHeadsMasterPage,
  FeeInstallmentsMasterPage,
  ClassFeeMapsMasterPage,
  FeeStructuresMasterPage,
  AcademicYearsMasterPage,
} from './features/finance'

function LegacyStudentDetailRedirect() {
  const { id } = useParams<{ id: string }>()
  if (!id) return <Navigate to={ROUTES.STUDENTS} replace />
  return <Navigate to={ROUTES.STUDENT_DETAIL(id)} replace />
}

function LegacyStudentEditRedirect() {
  const { id } = useParams<{ id: string }>()
  if (!id) return <Navigate to={ROUTES.STUDENTS} replace />
  return <Navigate to={ROUTES.STUDENT_EDIT(id)} replace />
}

function AdminUsersPage() {
  const { appPersona } = useAuth()
  if (appPersona !== 'admin') return <Navigate to={ROUTES.HOME} replace />
  return <UserListPage />
}

export default function App() {
  return (
    <AuthProvider>
      <BrowserRouter>
        <Routes>
          <Route path={ROUTES.LOGIN} element={<LoginPage />} />
          <Route path={ROUTES.REGISTER} element={<RegisterPage />} />
          <Route path={ROUTES.FORGOT_PASSWORD} element={<ForgotPasswordPage />} />
          <Route
            path={ROUTES.HOME}
            element={
              <ProtectedRoute>
                <MainLayout />
              </ProtectedRoute>
            }
          >
            <Route index element={<DashboardPage />} />
            <Route path="admin-dashboard" element={<AdminDashboardPage />} />
            <Route path="admin-dashboard/org/school-groups" element={<AdminSchoolGroupsPage />} />
            <Route path="admin-dashboard/org/schools" element={<AdminSchoolsPage />} />
            <Route path="admin-dashboard/org/classes" element={<AdminClassesPage />} />
            <Route path="admin-dashboard/org/divisions" element={<AdminDivisionsPage />} />
            <Route path="admission-analytics" element={<Navigate to={ROUTES.ADMISSION_ANALYTICS} replace />} />
            <Route path="students" element={<Navigate to={ROUTES.STUDENTS} replace />} />
            <Route path="students/new" element={<Navigate to={ROUTES.STUDENTS_NEW} replace />} />
            <Route path="students/:id/edit" element={<LegacyStudentEditRedirect />} />
            <Route path="students/:id" element={<LegacyStudentDetailRedirect />} />
            <Route path="admission" element={<AdmissionModuleLayout />}>
              <Route index element={<Navigate to="analytics" replace />} />
              <Route path="dashboard" element={<Navigate to={ROUTES.ADMISSION_ANALYTICS} replace />} />
              <Route path="overview" element={<AdmissionDashboardPage />} />
              <Route path="students" element={<StudentListPage />} />
              <Route path="students/new" element={<StudentFormPage />} />
              <Route path="students/:id" element={<StudentDetailPage />} />
              <Route path="students/:id/edit" element={<StudentFormPage />} />
              <Route path="analytics" element={<AdmissionAnalyticsPage />} />
            </Route>
            <Route path="users" element={<AdminUsersPage />} />
            <Route path={ROUTES.PARENT_FEES.replace(/^\//, '')} element={<ParentFeesPage />} />
            <Route path="finance" element={<FinanceModuleLayout />}>
              <Route index element={<Navigate to="collect" replace />} />
              <Route path="analytics" element={<FinanceAnalyticsPage />} />
              <Route path="invoices" element={<FinanceInvoicesPage />} />
              <Route path="payments" element={<FinancePaymentsPage />} />
              <Route path="collect" element={<FeesCollectionPage />} />
              <Route path="masters" element={<FeeMastersLayout />}>
                <Route index element={<Navigate to="academic-years" replace />} />
                <Route path="academic-years" element={<AcademicYearsMasterPage />} />
                <Route path="fee-heads" element={<FeeHeadsMasterPage />} />
                <Route path="installments" element={<FeeInstallmentsMasterPage />} />
                <Route path="class-fee-map" element={<ClassFeeMapsMasterPage />} />
                <Route path="fee-structure" element={<FeeStructuresMasterPage />} />
              </Route>
            </Route>
            <Route path="attendance" element={<AttendanceDashboardPage />} />
            <Route path="attendance/division" element={<AttendanceDivisionPage />} />
            <Route path="attendance/student" element={<Navigate to={ROUTES.ATTENDANCE} replace />} />
            <Route path="attendance/holidays" element={<HolidayManagementPage />} />
            <Route path="attendance/bulk-import" element={<AttendanceBulkImportPage />} />
            <Route path="attendance/division-excel" element={<AttendanceDivisionExcelPage />} />
            <Route path="exams" element={<ExamsPage />} />
          </Route>
          <Route path="*" element={<Navigate to={ROUTES.HOME} replace />} />
        </Routes>
      </BrowserRouter>
    </AuthProvider>
  )
}
