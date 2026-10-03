import { apiClient } from '../../../infrastructure/http'

/** Matches API: none | allSaturdays | secondFourth | firstThirdFifth */
export type SaturdayPattern = 'none' | 'allSaturdays' | 'secondFourth' | 'firstThirdFifth'

export type WeeklyOffSettings = {
  branchId: string
  saturdayPattern: SaturdayPattern
  sundayOff: boolean
}

export type SchoolHolidayDto = {
  id: string
  branchId: string
  holidayDate: string
  name?: string | null
}

export const holidayManagementApi = {
  getWeeklyOff: async (branchId: string): Promise<WeeklyOffSettings> => {
    const { data } = await apiClient.get<WeeklyOffSettings>(`/AttendanceHoliday/branch/${branchId}/weekly-off`)
    return data
  },

  updateWeeklyOff: async (
    branchId: string,
    body: { saturdayPattern: SaturdayPattern; sundayOff: boolean }
  ): Promise<WeeklyOffSettings> => {
    const { data } = await apiClient.put<WeeklyOffSettings>(
      `/AttendanceHoliday/branch/${branchId}/weekly-off`,
      body
    )
    return data
  },

  listHolidays: async (
    branchId: string,
    params?: { from?: string; to?: string }
  ): Promise<SchoolHolidayDto[]> => {
    const { data } = await apiClient.get<SchoolHolidayDto[]>(`/AttendanceHoliday/branch/${branchId}/holidays`, {
      params,
    })
    return data
  },

  addHoliday: async (
    branchId: string,
    body: { holidayDate: string; name?: string | null }
  ): Promise<SchoolHolidayDto> => {
    const { data } = await apiClient.post<SchoolHolidayDto>(
      `/AttendanceHoliday/branch/${branchId}/holidays`,
      body
    )
    return data
  },

  deleteHoliday: async (id: string): Promise<void> => {
    await apiClient.delete(`/AttendanceHoliday/${id}`)
  },
}
