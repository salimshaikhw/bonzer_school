import { apiClient } from '../../../infrastructure/http'
import type {
  UserDetails,
  RegisterInput,
  RegistrationResult,
  ResetPasswordInput,
  ChangePasswordInput,
  UserUpdateInput,
  RoleInput,
} from '../user.types'

export const userApi = {
  getAll: async (): Promise<UserDetails[]> => {
    const { data } = await apiClient.get<UserDetails[]>('/User')
    return data
  },

  getById: async (id: string): Promise<UserDetails> => {
    const { data } = await apiClient.get<UserDetails>(`/User/${id}`)
    return data
  },

  register: async (input: RegisterInput): Promise<RegistrationResult> => {
    const { data } = await apiClient.post<RegistrationResult>('/User/register', input)
    return data
  },

  confirmEmail: async (userId: string, code: string): Promise<void> => {
    await apiClient.post('/User/confirm-email', null, {
      params: { userId, code },
    })
  },

  sendPasswordReset: async (email: string): Promise<void> => {
    await apiClient.post('/User/send-password-reset', null, {
      params: { email },
    })
  },

  resetPassword: async (input: ResetPasswordInput): Promise<void> => {
    await apiClient.post('/User/reset-password', input)
  },

  sendEmailVerification: async (email: string): Promise<void> => {
    await apiClient.post('/User/send-email-verification', null, {
      params: { email },
    })
  },

  changePassword: async (input: ChangePasswordInput): Promise<void> => {
    await apiClient.post('/User/change-password', input)
  },

  addRole: async (input: RoleInput): Promise<void> => {
    await apiClient.post('/User/add-role', input)
  },

  removeRole: async (input: RoleInput): Promise<void> => {
    await apiClient.post('/User/remove-role', input)
  },

  update: async (id: string, input: UserUpdateInput): Promise<void> => {
    await apiClient.put(`/User/${id}`, input)
  },
}
