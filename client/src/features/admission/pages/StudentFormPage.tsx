import type { CSSProperties } from 'react'
import { useState, useEffect } from 'react'
import { useParams, useNavigate, Link } from 'react-router-dom'
import { ROUTES } from '../../../config/routes'
import { Button, Input, Spinner } from '../../../shared'
import { useOrgHierarchy } from '../../../layouts/OrgHierarchyProvider'
import { studentsApi } from '../api/students.api'
import { useStudent } from '../hooks/useStudent'
import type { StudentRequest } from '../students.types'
import { emptyStudentRequest } from '../students.types'
import { AdmissionOrgScopeBanner } from '../components/AdmissionOrgScopeBanner'
import { canStartNewAdmission } from '../utils/loadStudentsForOrgTree'

function placementOk(form: StudentRequest): boolean {
  return !!(form.branchId?.trim() && form.classId?.trim())
}

function norm(s: string | undefined | null): string {
  return String(s ?? '')
    .trim()
    .toLowerCase()
}

const TABS = [
  { id: 'intake', label: 'Intake & placement' },
  { id: 'personal', label: 'Personal Info' },
  { id: 'address', label: 'Address Details' },
  { id: 'parent', label: 'Parent / Guardian' },
  { id: 'educational', label: 'Educational Details' },
  { id: 'welfare', label: 'Welfare & Other' },
  { id: 'consent', label: 'Consent & Declaration' },
] as const

function toApiPayload(form: StudentRequest): StudentRequest {
  const payload = { ...form }
  if (form.dob === '') delete payload.dob
  if (form.admissionDate === '') delete payload.admissionDate
  if (form.birthCertificateIssueDate === '') delete payload.birthCertificateIssueDate
  if (form.leavingDate === '') delete payload.leavingDate
  if (!form.divisionId?.trim()) delete payload.divisionId
  return payload
}

export function StudentFormPage() {
  const { id } = useParams<{ id: string }>()
  const navigate = useNavigate()
  const isEdit = !!id
  const { student, loading: loadStudent } = useStudent(id ?? null)
  const org = useOrgHierarchy()

  const [activeTab, setActiveTab] = useState(0)
  const [form, setForm] = useState<StudentRequest>(emptyStudentRequest())
  const [consentAccepted, setConsentAccepted] = useState(false)
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState('')

  useEffect(() => {
    if (student) {
      setForm({
        branchId: student.branchId,
        academicYearId: student.academicYearId,
        admissionNo: student.admissionNo ?? '',
        admissionDate: student.admissionDate ?? '',
        firstName: student.firstName ?? '',
        middleName: student.middleName ?? '',
        lastName: student.lastName ?? '',
        gender: student.gender ?? '',
        dob: student.dob ?? '',
        birthPlace: student.birthPlace ?? '',
        birthCertificateNo: student.birthCertificateNo ?? '',
        birthCertificateIssueDate: student.birthCertificateIssueDate ?? '',
        childAadhaar: student.childAadhaar ?? '',
        religion: student.religion ?? '',
        caste: student.caste ?? '',
        category: student.category ?? '',
        nationality: student.nationality ?? '',
        bloodGroup: student.bloodGroup ?? '',
        markIdentification: student.markIdentification ?? '',
        heightCm: student.heightCm,
        weightKg: student.weightKg,
        medicalConditions: student.medicalConditions ?? '',
        allergies: student.allergies ?? '',
        disability: student.disability ?? '',
        addressLine1: student.addressLine1 ?? '',
        addressLine2: student.addressLine2 ?? '',
        city: student.city ?? '',
        state: student.state ?? '',
        pincode: student.pincode ?? '',
        phone: student.phone ?? '',
        email: student.email ?? '',
        fatherName: student.fatherName ?? '',
        fatherPhone: student.fatherPhone ?? '',
        fatherAadhaar: student.fatherAadhaar ?? '',
        fatherOccupation: student.fatherOccupation ?? '',
        motherName: student.motherName ?? '',
        motherPhone: student.motherPhone ?? '',
        motherAadhaar: student.motherAadhaar ?? '',
        motherOccupation: student.motherOccupation ?? '',
        guardianName: student.guardianName ?? '',
        guardianPhone: student.guardianPhone ?? '',
        familyIncome: student.familyIncome,
        previousSchool: student.previousSchool ?? '',
        previousClass: student.previousClass ?? '',
        previousExam: student.previousExam ?? '',
        previousExamResult: student.previousExamResult ?? '',
        previousPercentage: student.previousPercentage,
        previousSchoolContactNumber: student.previousSchoolContactNumber ?? '',
        previousExamBord: student.previousExamBord ?? '',
        classId: student.classId,
        divisionId: student.divisionId,
        rollNumber: student.rollNumber ?? '',
        studentStatus: student.studentStatus ?? 'ACTIVE',
        enrollmentNo: student.enrollmentNo ?? '',
        lastExamName: student.lastExamName ?? '',
        lastExamResult: student.lastExamResult ?? '',
        hasScholarship: student.hasScholarship,
        scholarshipType: student.scholarshipType ?? '',
        scholarshipStatus: student.scholarshipStatus ?? '',
        scholarshipAmount: student.scholarshipAmount,
        scholarshipEligible: student.scholarshipEligible,
        isRteSeat: student.isRteSeat,
        rteEligible: student.rteEligible,
        rteApplicationNo: student.rteApplicationNo ?? '',
        ewsDgCategory: student.ewsDgCategory ?? '',
        isMinority: student.isMinority,
        isBpl: student.isBpl,
        bplCardNo: student.bplCardNo ?? '',
        udiseCode: student.udiseCode ?? '',
        singleGirlChild: student.singleGirlChild,
        firstGraduate: student.firstGraduate,
      })
    }
  }, [student])

  /** New admissions: branch from tree only; class/division are chosen on the form. Edits keep stored placement. */
  useEffect(() => {
    if (isEdit || !org.ready || org.loading) return
    if (canStartNewAdmission(org)) {
      setForm((f) => ({
        ...f,
        branchId: org.branchId,
        classId: '',
        divisionId: undefined,
      }))
      return
    }
    setForm((f) => ({
      ...f,
      branchId: org.branchId,
      classId: org.classId,
      divisionId: org.divisionId.trim() ? org.divisionId : undefined,
    }))
  }, [isEdit, org.ready, org.loading, org.branchId, org.classId, org.divisionId])

  async function handleSaveDraft() {
    if (!placementOk(form)) {
      setError('Select school (branch) and class. Division can be assigned later from the office.')
      setActiveTab(0)
      return
    }
    setError('')
    setSaving(true)
    try {
      const payload = toApiPayload({ ...form, studentStatus: 'DRAFT' })
      if (isEdit && id) {
        await studentsApi.update(id, payload)
      } else {
        const created = await studentsApi.create(payload)
        navigate(ROUTES.STUDENT_EDIT(created.id), { replace: true })
      }
    } catch {
      setError('Failed to save draft.')
    } finally {
      setSaving(false)
    }
  }

  async function handleSubmit() {
    if (!placementOk(form)) {
      setError('Select school (branch) and class. Division can be assigned later from the office.')
      setActiveTab(0)
      return
    }
    if (!consentAccepted && !isEdit) {
      setError('You must accept the declaration before submitting.')
      setActiveTab(TABS.length - 1)
      return
    }
    setError('')
    setSaving(true)
    try {
      const payload = toApiPayload({ ...form, studentStatus: 'ACTIVE' })
      if (isEdit && id) {
        await studentsApi.update(id, payload)
      } else {
        const created = await studentsApi.create(payload)
        navigate(ROUTES.STUDENT_DETAIL(created.id))
        return
      }
      navigate(ROUTES.STUDENTS)
    } catch {
      setError('Submit failed.')
    } finally {
      setSaving(false)
    }
  }

  const update = (key: keyof StudentRequest, value: string | number | boolean | undefined) =>
    setForm((f) => ({ ...f, [key]: value }))

  if (isEdit && loadStudent) return <p><Spinner /> Loading…</p>

  if (!isEdit && org.ready && !org.loading && !canStartNewAdmission(org)) {
    return (
      <div style={styles.wrap}>
        <Link to={ROUTES.STUDENTS} style={styles.back}>
          ← Back
        </Link>
        <h2>New admission</h2>
        <AdmissionOrgScopeBanner />
        <p style={styles.hint}>
          Select a <strong>school</strong> in the organization tree (click the school name — not a group, class, or division row),
          then return here. Class and division are set on this form; division can be finalized later.
        </p>
      </div>
    )
  }

  return (
    <div style={styles.wrap}>
      <Link to={isEdit ? ROUTES.STUDENT_DETAIL(id!) : ROUTES.STUDENTS} style={styles.back}>
        ← Back
      </Link>
      <h2>{isEdit ? 'Edit student' : 'Student admission / registration'}</h2>

      <div style={styles.tabBar}>
        {TABS.map((tab, i) => (
          <button
            key={tab.id}
            type="button"
            style={{
              ...styles.tab,
              ...(activeTab === i ? styles.tabActive : {}),
            }}
            onClick={() => setActiveTab(i)}
          >
            {tab.label}
          </button>
        ))}
      </div>

      <div style={styles.tabContent}>
        {activeTab === 0 && (
          <>
            <h3>Intake &amp; placement</h3>
            <p style={styles.hint}>
              {isEdit
                ? 'Recorded placement for this student (tree on the left is global scope only).'
                : 'School, class, and division follow the organization tree on the left. Division can be left unset and assigned later.'}
            </p>
            {!org.ready || org.loading ? (
              <p>
                <Spinner /> Loading…
              </p>
            ) : isEdit ? (
              <div style={styles.recordedBox}>
                <strong style={styles.recordedK}>On this record</strong>
                <p style={styles.recordedLine}>
                  {[
                    org.branches.find((b) => norm(b.id) === norm(form.branchId))?.name ?? form.branchId ?? '—',
                    org.treeClasses.find((c) => norm(c.id) === norm(form.classId))?.name ?? form.classId ?? '—',
                    form.divisionId?.trim()
                      ? org.treeDivisions.find((d) => norm(d.id) === norm(form.divisionId))?.name ?? form.divisionId
                      : 'Division pending',
                  ].join(' · ')}
                </p>
              </div>
            ) : (
              <AdmissionOrgScopeBanner />
            )}
          </>
        )}

        {activeTab === 1 && (
          <>
            <h3>Personal information</h3>
            <div className="form-grid" style={styles.grid}>
              <Input label="First name *" value={form.firstName ?? ''} onChange={(e) => update('firstName', e.target.value)} required />
              <Input label="Middle name" value={form.middleName ?? ''} onChange={(e) => update('middleName', e.target.value)} />
              <Input label="Last name *" value={form.lastName ?? ''} onChange={(e) => update('lastName', e.target.value)} required />
              <Input label="Date of birth" type="date" value={form.dob ?? ''} onChange={(e) => update('dob', e.target.value)} />
              <label style={styles.label}>
                Gender
                <select value={form.gender ?? ''} onChange={(e) => update('gender', e.target.value)} style={styles.select}>
                  <option value="">—</option>
                  <option value="Male">Male</option>
                  <option value="Female">Female</option>
                  <option value="Other">Other</option>
                </select>
              </label>
              <Input label="Birth place" value={form.birthPlace ?? ''} onChange={(e) => update('birthPlace', e.target.value)} />
              <Input label="Birth certificate no" value={form.birthCertificateNo ?? ''} onChange={(e) => update('birthCertificateNo', e.target.value)} />
              <Input label="Birth certificate issue date" type="date" value={form.birthCertificateIssueDate ?? ''} onChange={(e) => update('birthCertificateIssueDate', e.target.value)} />
              <Input label="Child Aadhaar" value={form.childAadhaar ?? ''} onChange={(e) => update('childAadhaar', e.target.value)} />
              <Input label="Religion" value={form.religion ?? ''} onChange={(e) => update('religion', e.target.value)} />
              <Input label="Caste" value={form.caste ?? ''} onChange={(e) => update('caste', e.target.value)} />
              <Input label="Category" value={form.category ?? ''} onChange={(e) => update('category', e.target.value)} />
              <Input label="Nationality" value={form.nationality ?? ''} onChange={(e) => update('nationality', e.target.value)} />
              <Input label="Blood group" value={form.bloodGroup ?? ''} onChange={(e) => update('bloodGroup', e.target.value)} />
              <Input label="Identification mark" value={form.markIdentification ?? ''} onChange={(e) => update('markIdentification', e.target.value)} />
              <Input label="Height (cm)" type="number" value={form.heightCm ?? ''} onChange={(e) => update('heightCm', e.target.value ? Number(e.target.value) : undefined)} />
              <Input label="Weight (kg)" type="number" value={form.weightKg ?? ''} onChange={(e) => update('weightKg', e.target.value ? Number(e.target.value) : undefined)} />
              <Input label="Medical conditions" value={form.medicalConditions ?? ''} onChange={(e) => update('medicalConditions', e.target.value)} />
              <Input label="Allergies" value={form.allergies ?? ''} onChange={(e) => update('allergies', e.target.value)} />
              <Input label="Disability" value={form.disability ?? ''} onChange={(e) => update('disability', e.target.value)} />
            </div>
          </>
        )}

        {activeTab === 2 && (
          <>
            <h3>Address details</h3>
            <div className="form-grid" style={styles.grid}>
              <Input label="Address line 1" value={form.addressLine1 ?? ''} onChange={(e) => update('addressLine1', e.target.value)} />
              <Input label="Address line 2" value={form.addressLine2 ?? ''} onChange={(e) => update('addressLine2', e.target.value)} />
              <Input label="City" value={form.city ?? ''} onChange={(e) => update('city', e.target.value)} />
              <Input label="State" value={form.state ?? ''} onChange={(e) => update('state', e.target.value)} />
              <Input label="Pincode" value={form.pincode ?? ''} onChange={(e) => update('pincode', e.target.value)} />
              <Input label="Phone" value={form.phone ?? ''} onChange={(e) => update('phone', e.target.value)} />
              <Input label="Email" type="email" value={form.email ?? ''} onChange={(e) => update('email', e.target.value)} />
            </div>
          </>
        )}

        {activeTab === 3 && (
          <>
            <h3>Parent / guardian</h3>
            <div className="form-grid" style={styles.grid}>
              <Input label="Father name" value={form.fatherName ?? ''} onChange={(e) => update('fatherName', e.target.value)} />
              <Input label="Father phone" value={form.fatherPhone ?? ''} onChange={(e) => update('fatherPhone', e.target.value)} />
              <Input label="Father Aadhaar" value={form.fatherAadhaar ?? ''} onChange={(e) => update('fatherAadhaar', e.target.value)} />
              <Input label="Father occupation" value={form.fatherOccupation ?? ''} onChange={(e) => update('fatherOccupation', e.target.value)} />
              <Input label="Mother name" value={form.motherName ?? ''} onChange={(e) => update('motherName', e.target.value)} />
              <Input label="Mother phone" value={form.motherPhone ?? ''} onChange={(e) => update('motherPhone', e.target.value)} />
              <Input label="Mother Aadhaar" value={form.motherAadhaar ?? ''} onChange={(e) => update('motherAadhaar', e.target.value)} />
              <Input label="Mother occupation" value={form.motherOccupation ?? ''} onChange={(e) => update('motherOccupation', e.target.value)} />
              <Input label="Guardian name" value={form.guardianName ?? ''} onChange={(e) => update('guardianName', e.target.value)} />
              <Input label="Guardian phone" value={form.guardianPhone ?? ''} onChange={(e) => update('guardianPhone', e.target.value)} />
              <Input label="Family income" type="number" value={form.familyIncome ?? ''} onChange={(e) => update('familyIncome', e.target.value ? Number(e.target.value) : undefined)} />
            </div>
          </>
        )}

        {activeTab === 4 && (
          <>
            <h3>Educational details</h3>
            <div className="form-grid" style={styles.grid}>
              <Input label="Admission no" value={form.admissionNo ?? ''} onChange={(e) => update('admissionNo', e.target.value)} />
              <Input label="Admission date" type="date" value={form.admissionDate ?? ''} onChange={(e) => update('admissionDate', e.target.value)} />
              <Input label="Roll number" value={form.rollNumber ?? ''} onChange={(e) => update('rollNumber', e.target.value)} />
              <Input label="Enrollment no" value={form.enrollmentNo ?? ''} onChange={(e) => update('enrollmentNo', e.target.value)} />
              <Input label="Previous school" value={form.previousSchool ?? ''} onChange={(e) => update('previousSchool', e.target.value)} />
              <Input label="Previous class" value={form.previousClass ?? ''} onChange={(e) => update('previousClass', e.target.value)} />
              <Input label="Previous exam" value={form.previousExam ?? ''} onChange={(e) => update('previousExam', e.target.value)} />
              <Input label="Previous exam result" value={form.previousExamResult ?? ''} onChange={(e) => update('previousExamResult', e.target.value)} />
              <Input label="Previous %" type="number" value={form.previousPercentage ?? ''} onChange={(e) => update('previousPercentage', e.target.value ? Number(e.target.value) : undefined)} />
              <Input label="Previous school contact" value={form.previousSchoolContactNumber ?? ''} onChange={(e) => update('previousSchoolContactNumber', e.target.value)} />
              <Input label="Previous exam board" value={form.previousExamBord ?? ''} onChange={(e) => update('previousExamBord', e.target.value)} />
              <Input label="Last exam name" value={form.lastExamName ?? ''} onChange={(e) => update('lastExamName', e.target.value)} />
              <Input label="Last exam result" value={form.lastExamResult ?? ''} onChange={(e) => update('lastExamResult', e.target.value)} />
            </div>
          </>
        )}

        {activeTab === 5 && (
          <>
            <h3>Welfare & other</h3>
            <div className="form-grid" style={styles.grid}>
              <label style={styles.checkLabel}>
                <input type="checkbox" checked={!!form.hasScholarship} onChange={(e) => update('hasScholarship', e.target.checked)} />
                Has scholarship
              </label>
              <Input label="Scholarship type" value={form.scholarshipType ?? ''} onChange={(e) => update('scholarshipType', e.target.value)} />
              <Input label="Scholarship status" value={form.scholarshipStatus ?? ''} onChange={(e) => update('scholarshipStatus', e.target.value)} />
              <Input label="Scholarship amount" type="number" value={form.scholarshipAmount ?? ''} onChange={(e) => update('scholarshipAmount', e.target.value ? Number(e.target.value) : undefined)} />
              <label style={styles.checkLabel}>
                <input type="checkbox" checked={!!form.scholarshipEligible} onChange={(e) => update('scholarshipEligible', e.target.checked)} />
                Scholarship eligible
              </label>
              <label style={styles.checkLabel}>
                <input type="checkbox" checked={!!form.isRteSeat} onChange={(e) => update('isRteSeat', e.target.checked)} />
                RTE seat
              </label>
              <label style={styles.checkLabel}>
                <input type="checkbox" checked={!!form.rteEligible} onChange={(e) => update('rteEligible', e.target.checked)} />
                RTE eligible
              </label>
              <Input label="RTE application no" value={form.rteApplicationNo ?? ''} onChange={(e) => update('rteApplicationNo', e.target.value)} />
              <Input label="EWS/DG category" value={form.ewsDgCategory ?? ''} onChange={(e) => update('ewsDgCategory', e.target.value)} />
              <label style={styles.checkLabel}>
                <input type="checkbox" checked={!!form.isMinority} onChange={(e) => update('isMinority', e.target.checked)} />
                Minority
              </label>
              <label style={styles.checkLabel}>
                <input type="checkbox" checked={!!form.isBpl} onChange={(e) => update('isBpl', e.target.checked)} />
                BPL
              </label>
              <Input label="BPL card no" value={form.bplCardNo ?? ''} onChange={(e) => update('bplCardNo', e.target.value)} />
              <Input label="UDISE code" value={form.udiseCode ?? ''} onChange={(e) => update('udiseCode', e.target.value)} />
              <label style={styles.checkLabel}>
                <input type="checkbox" checked={!!form.singleGirlChild} onChange={(e) => update('singleGirlChild', e.target.checked)} />
                Single girl child
              </label>
              <label style={styles.checkLabel}>
                <input type="checkbox" checked={!!form.firstGraduate} onChange={(e) => update('firstGraduate', e.target.checked)} />
                First graduate
              </label>
            </div>
          </>
        )}

        {activeTab === 6 && (
          <>
            <h3>Consent & declaration</h3>
            <div style={styles.declaration}>
              <p>
                I hereby declare that all the information furnished above is true and correct to the best of my knowledge.
                I understand that if any information is found to be false or misleading, my application may be cancelled
                and I shall not be eligible for admission / my admission shall be liable to be cancelled.
              </p>
              <label style={styles.checkLabel}>
                <input
                  type="checkbox"
                  checked={consentAccepted}
                  onChange={(e) => setConsentAccepted(e.target.checked)}
                />
                I accept the above declaration and confirm that all information provided is true. I understand that in case of
                any mismatch or false information, I will not be applicable / my admission will not be valid.
              </label>
            </div>
          </>
        )}
      </div>

      {error && <div style={styles.error}>{error}</div>}

      <div style={styles.actions}>
        <Button variant="secondary" onClick={handleSaveDraft} disabled={saving}>
          {saving ? 'Saving…' : 'Save as draft'}
        </Button>
        <Button onClick={handleSubmit} disabled={saving}>
          {saving ? 'Submitting…' : isEdit ? 'Update' : 'Submit registration'}
        </Button>
      </div>
    </div>
  )
}

const styles: Record<string, CSSProperties> = {
  wrap: { padding: 0, maxWidth: 900 },
  back: { display: 'inline-block', marginBottom: 16, color: '#1e3a5f' },
  tabBar: { display: 'flex', flexWrap: 'wrap', gap: 8, marginBottom: 24, borderBottom: '1px solid #ccc', paddingBottom: 8 },
  tab: { padding: '8px 14px', border: '1px solid #ccc', borderRadius: 6, background: '#f5f5f5', cursor: 'pointer', fontSize: 14 },
  tabActive: { background: '#1e3a5f', color: '#fff', borderColor: '#1e3a5f' },
  tabContent: { marginBottom: 24, minHeight: 200 },
  grid: { gap: '16px' },
  label: { display: 'flex', flexDirection: 'column', gap: 4, fontSize: 14, fontWeight: 500 },
  select: { padding: 10, border: '1px solid #ccc', borderRadius: 6, fontSize: 16 },
  checkLabel: { display: 'flex', alignItems: 'center', gap: 8, cursor: 'pointer', fontSize: 14 },
  declaration: { maxWidth: 560 },
  error: { background: '#fee', color: '#c00', padding: 10, borderRadius: 6, marginBottom: 16 },
  actions: { display: 'flex', gap: 12, flexWrap: 'wrap' },
  hint: { margin: '0 0 16px', fontSize: 14, color: '#64748b', maxWidth: 640, lineHeight: 1.45 },
  recordedBox: {
    marginBottom: 16,
    padding: '14px 16px',
    borderRadius: 10,
    border: '1px solid #e2e8f0',
    background: '#f8fafc',
  },
  recordedK: { fontSize: 11, fontWeight: 700, color: '#475569', textTransform: 'uppercase', letterSpacing: '0.05em' },
  recordedLine: { margin: '8px 0 0', fontSize: 15, fontWeight: 600, color: '#0f172a' },
}
