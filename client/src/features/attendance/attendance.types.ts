/**
 * Attendance types aligned with API DTOs.
 */
export type AttendancePeriodType = 'Day' | 'Week' | 'Month' | 'AcademicYear' | 'Custom'

export interface AttendanceSummary {
  presentCount: number
  absentCount: number
  leaveCount: number
  halfDayCount: number
  lateCount: number
  totalDays: number
  percentage: number
}

export interface AttendanceDashboard {
  todayPresent: number
  todayAbsent: number
  todayLeave: number
  todayPercentage: number
  totalStudents: number
  monthSummary?: AttendanceSummary
  pendingLeaveApprovals: number
  pendingCorrectionRequests: number
}

export interface AttendanceReport {
  periodType: AttendancePeriodType
  startDate: string
  endDate: string
  summary: AttendanceSummary
  detailedList: AttendanceRecord[]
}

export interface AttendanceRecord {
  id: string
  studentId?: string
  attendanceDate?: string
  status?: string
  leaveType?: string
  remark?: string
}

export interface AttendanceTrendPoint {
  date: string
  status: string
}
