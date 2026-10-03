/**
 * User & account types aligned with API (ApplicationUserDetails, RegisterInput, etc.)
 */
export interface UserDetails {
  id: string
  firstName: string
  lastName: string
  fullName: string
  email: string
  userName: string
  phoneNumber?: string
  emailConfirmed: boolean
  phoneNumberConfirmed: boolean
  twoFactorEnabled: boolean
  lockoutEnd?: string
  lockoutEnabled: boolean
  accessFailedCount: number
}

export interface RegisterInput {
  firstName: string
  lastName: string
  email: string
  password: string
  role: string
}

export interface RegistrationResult {
  isSuccessful: boolean
  message?: string
}

export interface ResetPasswordInput {
  email: string
  password: string
  code: string
}

export interface ChangePasswordInput {
  email: string
  oldPassword: string
  newPassword: string
}

export interface UserUpdateInput {
  firstName: string
  lastName: string
}

export interface RoleInput {
  email: string
  role: string
}
