/**
 * Student types aligned with API StudentRequest / StudentResponse.
 */
export interface Student {
  id: string
  branchId?: string
  academicYearId?: string
  admissionNo?: string
  admissionDate?: string
  aparId?: string
  photoUrl?: string
  signUrl?: string
  firstName?: string
  middleName?: string
  lastName?: string
  gender?: string
  dob?: string
  birthPlace?: string
  birthCertificateNo?: string
  birthCertificateIssueDate?: string
  childAadhaar?: string
  religion?: string
  caste?: string
  category?: string
  nationality?: string
  bloodGroup?: string
  markIdentification?: string
  heightCm?: number
  weightKg?: number
  medicalConditions?: string
  allergies?: string
  disability?: string
  previousSchool?: string
  previousClass?: string
  previousExam?: string
  previousExamResult?: string
  previousPercentage?: number
  previousSchoolContactNumber?: string
  previousExamBord?: string
  addressLine1?: string
  addressLine2?: string
  city?: string
  state?: string
  pincode?: string
  phone?: string
  email?: string
  fatherName?: string
  fatherPhone?: string
  fatherAadhaar?: string
  fatherOccupation?: string
  motherName?: string
  motherPhone?: string
  motherAadhaar?: string
  motherOccupation?: string
  guardianName?: string
  guardianPhone?: string
  familyIncome?: number
  classId?: string
  divisionId?: string
  rollNumber?: string
  studentStatus?: string
  enrollmentNo?: string
  isAadhaarVerified?: boolean
  promotedToClassId?: string
  leavingDate?: string
  isTcIssued?: boolean
  lastExamName?: string
  lastExamResult?: string
  hasScholarship?: boolean
  scholarshipType?: string
  scholarshipStatus?: string
  scholarshipAmount?: number
  scholarshipEligible?: boolean
  isRteSeat?: boolean
  rteEligible?: boolean
  rteApplicationNo?: string
  ewsDgCategory?: string
  isMinority?: boolean
  isBpl?: boolean
  bplCardNo?: string
  udiseCode?: string
  singleGirlChild?: boolean
  firstGraduate?: boolean
  [key: string]: unknown
}

export interface StudentRequest {
  branchId?: string
  academicYearId?: string
  admissionNo?: string
  admissionDate?: string
  aparId?: string
  photoUrl?: string
  signUrl?: string
  firstName?: string
  middleName?: string
  lastName?: string
  gender?: string
  dob?: string
  birthPlace?: string
  birthCertificateNo?: string
  birthCertificateIssueDate?: string
  childAadhaar?: string
  religion?: string
  caste?: string
  category?: string
  nationality?: string
  bloodGroup?: string
  markIdentification?: string
  heightCm?: number
  weightKg?: number
  medicalConditions?: string
  allergies?: string
  disability?: string
  previousSchool?: string
  previousClass?: string
  previousExam?: string
  previousExamResult?: string
  previousPercentage?: number
  previousSchoolContactNumber?: string
  previousExamBord?: string
  addressLine1?: string
  addressLine2?: string
  city?: string
  state?: string
  pincode?: string
  phone?: string
  email?: string
  fatherName?: string
  fatherPhone?: string
  fatherAadhaar?: string
  fatherOccupation?: string
  motherName?: string
  motherPhone?: string
  motherAadhaar?: string
  motherOccupation?: string
  guardianName?: string
  guardianPhone?: string
  familyIncome?: number
  classId?: string
  divisionId?: string
  rollNumber?: string
  studentStatus?: string
  enrollmentNo?: string
  isAadhaarVerified?: boolean
  promotedToClassId?: string
  leavingDate?: string
  isTcIssued?: boolean
  lastExamName?: string
  lastExamResult?: string
  hasScholarship?: boolean
  scholarshipType?: string
  scholarshipStatus?: string
  scholarshipAmount?: number
  scholarshipEligible?: boolean
  isRteSeat?: boolean
  rteEligible?: boolean
  rteApplicationNo?: string
  ewsDgCategory?: string
  isMinority?: boolean
  isBpl?: boolean
  bplCardNo?: string
  udiseCode?: string
  singleGirlChild?: boolean
  firstGraduate?: boolean
  [key: string]: unknown
}

/** Response from GET /Student/paged */
export interface PagedStudentsResponse {
  items: Student[]
  totalCount: number
  page: number
  pageSize: number
  totalPages: number
}

export interface StudentSearchFilter {
  branchId?: string
  classId?: string
  divisionId?: string
  academicYearId?: string
  status?: string
  /** Maps to API `keyword` (roll, name fragment, admission no, etc.). */
  searchTerm?: string
  /** Sent as `keyword` to POST /Student/search when provided. */
  keyword?: string
}

/** Default empty form state for new admission (draft) */
export const emptyStudentRequest = (): StudentRequest => ({
  studentStatus: 'DRAFT',
  firstName: '',
  middleName: '',
  lastName: '',
  gender: '',
  admissionNo: '',
  rollNumber: '',
})
