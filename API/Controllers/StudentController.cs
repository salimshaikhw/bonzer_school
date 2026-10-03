using Application.Abstractions.Services;
using Application.ViewModels.Attendance;
using Application.ViewModels.Student;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class StudentController : ControllerBase
    {
        private readonly IStudentService _studentService;
        private readonly IAttendanceService _attendanceService;
        private readonly IFeeStudentService _feeStudentService;
        private readonly IMarkService _markService;
        private readonly ILogger<StudentController> _logger;

        public StudentController(
            IStudentService studentService,
            IAttendanceService attendanceService,
            IFeeStudentService feeStudentService,
            IMarkService markService,
            ILogger<StudentController> logger)
        {
            _studentService = studentService;
            _attendanceService = attendanceService;
            _feeStudentService = feeStudentService;
            _markService = markService;
            _logger = logger;
        }

        /// <summary>Get all students. Uses caching and logging.</summary>
        [HttpGet]
        public async Task<IActionResult> GetAllStudents()
        
        {
            _logger.LogInformation("GetAllStudents: fetching all students");
            var result = await _studentService.GetAllAsync();
            return Ok(result);
        }

        /// <summary>Get total number of students.</summary>
        [HttpGet("total")]
        public async Task<IActionResult> GetTotalStudents()
        {
            _logger.LogInformation("GetTotalStudents");
            var result = await _studentService.GetTotalStudentsAsync();
            return Ok(new { total = result });
        }

        /// <summary>Get students with server-side pagination (for admission list UI).</summary>
        [HttpGet("paged")]
        public async Task<IActionResult> GetStudentsPaged([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 20;
            if (pageSize > 100) pageSize = 100;
            _logger.LogInformation("GetStudentsPaged: page {Page}, pageSize {PageSize}", page, pageSize);
            var result = await _studentService.GetPagedAsync(page, pageSize);
            return Ok(result);
        }

        /// <summary>Get students admitted today.</summary>
        [HttpGet("today-admissions")]
        public async Task<IActionResult> GetTodayAdmissions()
        {
            _logger.LogInformation("GetTodayAdmissions");
            var result = await _studentService.GetTodayAdmissionsAsync();
            return Ok(result);
        }

        /// <summary>Get students whose birthday is today.</summary>
        [HttpGet("birthday-today")]
        public async Task<IActionResult> GetStudentsWithBirthdayToday()
        {
            _logger.LogInformation("GetStudentsWithBirthdayToday");
            var result = await _studentService.GetStudentsWithBirthdayTodayAsync();
            return Ok(result);
        }

        /// <summary>Get students with pending documents (Aadhaar, birth certificate, or photo missing).</summary>
        [HttpGet("pending-documents")]
        public async Task<IActionResult> GetStudentsWithPendingDocuments()
        {
            _logger.LogInformation("GetStudentsWithPendingDocuments");
            var result = await _studentService.GetStudentsWithPendingDocumentsAsync();
            return Ok(result);
        }

        /// <summary>Get students with attendance below the given percentage (default 75%) for the current month.</summary>
        [HttpGet("low-attendance")]
        public async Task<IActionResult> GetStudentsWithLowAttendance([FromQuery] decimal minAttendancePercent = 75)
        {
            _logger.LogInformation("GetStudentsWithLowAttendance: min {Min}%", minAttendancePercent);
            var result = await _studentService.GetStudentsWithLowAttendanceAsync(minAttendancePercent);
            return Ok(result);
        }

        /// <summary>Get students eligible for board exam (active, status ACTIVE, with class).</summary>
        [HttpGet("board-exam-eligible")]
        public async Task<IActionResult> GetStudentsEligibleForBoardExam()
        {
            
            _logger.LogInformation("GetStudentsEligibleForBoardExam");
            var result = await _studentService.GetStudentsEligibleForBoardExamAsync();
            return Ok(result);
        }




        /// <summary>Get student by id. Uses caching and logging.</summary>
        [HttpGet("{id:guid}/admission-portfolio")]
        public async Task<IActionResult> GetStudentAdmissionPortfolio(Guid id)
        {
            _logger.LogInformation("GetStudentAdmissionPortfolio: {Id}", id);
            var student = await _studentService.GetByIdAsync(id);
            if (student == null)
                return NotFound();

            var dto = new StudentAdmissionPortfolioDto { Student = student };

            try
            {
                dto.AttendanceAcademicYear = await _attendanceService.GetStudentAttendanceSummaryAsync(
                    id,
                    AttendancePeriodType.AcademicYear,
                    null,
                    null);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Admission portfolio: attendance failed for {StudentId}", id);
            }

            if (student.AcademicYearId is { } ay && ay != Guid.Empty)
            {
                try
                {
                    dto.Fees = await _feeStudentService.GetStudentDashboard(id, ay);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Admission portfolio: fees failed for {StudentId}", id);
                }
            }

            try
            {
                dto.Marks = (await _markService.GetByStudentIdAsync(id)).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Admission portfolio: marks failed for {StudentId}", id);
            }

            return Ok(dto);
        }

        /// <summary>Get student by id. Uses caching and logging.</summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetStudentById(Guid id)
        {
            _logger.LogInformation("GetStudentById: {Id}", id);
            var result = await _studentService.GetByIdAsync(id);
            if (result == null)
                return NotFound();
            return Ok(result);
        }

        /// <summary>Create a new student. Exception handling via global middleware.</summary>
        [HttpPost]
        public async Task<IActionResult> CreateStudent([FromBody] StudentRequest dto)
        {
            _logger.LogInformation("CreateStudent: creating new student");
            var result = await _studentService.CreateAsync(dto);
            return CreatedAtAction(nameof(GetStudentById), new { id = result.Id }, result);
        }

        /// <summary>Download Excel template for student bulk upload (NPOI, fully open-source).</summary>
        
        [HttpGet("bulk-upload/template")]
        public async Task<IActionResult> GetStudentBulkUploadTemplate()
        {
            _logger.LogInformation("GetStudentBulkUploadTemplate");
            var bytes = await _studentService.GetStudentBulkUploadTemplateAsync();
            const string fileName = "StudentBulkUploadTemplate.xlsx";
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }

        /// <summary>Bulk upload students from Excel (.xlsx) using the provided template.</summary>

        [HttpPost("bulk-upload")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> BulkUploadStudents([FromForm] StudentBulkUploadRequest request)
        {
            // Use request.File instead of file
            if (request.File == null || request.File.Length == 0)
                return BadRequest("Excel file is required.");

            _logger.LogInformation("BulkUploadStudents: {FileName}, {Size} bytes", request.File.FileName, request.File.Length);

            await using var stream = request.File.OpenReadStream();
            var result = await _studentService.BulkUploadStudentsAsync(stream);

            // This returns the result object you shared
            return Ok(result);
        }

       

        /// <summary>Full update of student. Uses caching invalidation and logging.</summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateStudent(Guid id, [FromBody] StudentRequest dto)
        {
            _logger.LogInformation("UpdateStudent: {Id}", id);
            var result = await _studentService.UpdateAsync(id, dto);
            if (result == null)
                return NotFound();
            return Ok(result);
        }

        /// <summary>Partial update; only sent properties are applied. Uses caching invalidation and logging.</summary>
        [HttpPatch("{id}")]
        public async Task<IActionResult> PatchStudent(Guid id, [FromBody] StudentRequest dto)
        {
            _logger.LogInformation("PatchStudent: {Id}", id);
            var result = await _studentService.PatchAsync(id, dto);
            if (result == null)
                return NotFound();
            return Ok(result);
        }

        /// <summary>Permanent (hard) delete. Uses caching invalidation and logging.</summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteStudent(Guid id)
        {
            _logger.LogInformation("DeleteStudent: {Id}", id);
            var result = await _studentService.DeleteAsync(id);
            if (!result)
                return NotFound();
            return NoContent();
        }

        /// <summary>Soft delete: sets IsActive = false. Uses caching invalidation and logging.</summary>
        [HttpDelete("{id}/soft")]
        public async Task<IActionResult> SoftDeleteStudent(Guid id)
        {
            _logger.LogInformation("SoftDeleteStudent: {Id}", id);
            var result = await _studentService.SoftDeleteAsync(id);
            if (!result)
                return NotFound();
            return NoContent();
        }

        /// <summary>Restore a soft-deleted student: sets IsActive = true. Uses caching invalidation and logging.</summary>
        [HttpPost("{id}/restore")]
        public async Task<IActionResult> RestoreStudent(Guid id)
        {
            _logger.LogInformation("RestoreStudent: {Id}", id);
            var result = await _studentService.RestoreAsync(id);
            if (!result)
                return NotFound();
            return NoContent();
        }

        // ---------------------------------------------------------
        // Lifecycle / status endpoints (caching, logging, exception handling in service)
        // ---------------------------------------------------------

        [HttpPost("{id}/activate")]
        public async Task<IActionResult> ActivateStudent(Guid id)
        {
            _logger.LogInformation("ActivateStudent: {Id}", id);
            var result = await _studentService.ActivateStudentAsync(id);
            if (result == null) return NotFound();
            return Ok(result);
        }

        [HttpPost("{id}/suspend")]
        public async Task<IActionResult> SuspendStudent(Guid id)
        {
            _logger.LogInformation("SuspendStudent: {Id}", id);
            var result = await _studentService.SuspendStudentAsync(id);
            if (result == null) return NotFound();
            return Ok(result);
        }

        [HttpPost("{id}/drop")]
        public async Task<IActionResult> DropStudent(Guid id)
        {
            _logger.LogInformation("DropStudent: {Id}", id);
            var result = await _studentService.DropStudentAsync(id);
            if (result == null) return NotFound();
            return Ok(result);
        }

        [HttpPost("{id}/transfer")]
        public async Task<IActionResult> TransferStudent(Guid id, [FromQuery] Guid newBranchId)
        {
            _logger.LogInformation("TransferStudent: {Id}, newBranchId {BranchId}", id, newBranchId);
            var result = await _studentService.TransferStudentAsync(id, newBranchId);
            if (result == null) return NotFound();
            return Ok(result);
        }

        [HttpPost("{id}/promote")]
        public async Task<IActionResult> PromoteStudent(Guid id, [FromQuery] Guid newClassId, [FromQuery] Guid academicYearId)
        {
            _logger.LogInformation("PromoteStudent: {Id}, newClassId {ClassId}, academicYearId {YearId}", id, newClassId, academicYearId);
            var result = await _studentService.PromoteStudentAsync(id, newClassId, academicYearId);
            if (result == null) return NotFound();
            return Ok(result);
        }

        [HttpPost("{id}/passed-out")]
        public async Task<IActionResult> MarkStudentPassedOut(Guid id)
        {
            _logger.LogInformation("MarkStudentPassedOut: {Id}", id);
            var result = await _studentService.MarkStudentPassedOutAsync(id);
            if (result == null) return NotFound();
            return Ok(result);
        }

        [HttpPost("{id}/issue-tc")]
        public async Task<IActionResult> IssueTc(Guid id)
        {
            _logger.LogInformation("IssueTc: {Id}", id);
            var result = await _studentService.IssueTcAsync(id);
            if (result == null) return NotFound();
            return Ok(result);
        }

        [HttpPost("{id}/re-enroll")]
        public async Task<IActionResult> ReEnrollStudent(Guid id)
        {
            _logger.LogInformation("ReEnrollStudent: {Id}", id);
            var result = await _studentService.ReEnrollStudentAsync(id);
            if (result == null) return NotFound();
            return Ok(result);
        }

        [HttpPatch("{id}/status")]
        public async Task<IActionResult> UpdateStudentStatus(Guid id, [FromQuery] string status)
        {
            _logger.LogInformation("UpdateStudentStatus: {Id}, status {Status}", id, status);
            var result = await _studentService.UpdateStudentStatusAsync(id, status);
            if (result == null) return NotFound();
            return Ok(result);
        }

        [HttpPatch("{id}/leaving-date")]
        public async Task<IActionResult> UpdateLeavingDate(Guid id, [FromQuery] DateOnly date)
        {
            _logger.LogInformation("UpdateLeavingDate: {Id}, date {Date}", id, date);
            var result = await _studentService.UpdateLeavingDateAsync(id, date);
            if (result == null) return NotFound();
            return Ok(result);
        }

        // ---------------------------------------------------------
        // Additional student query endpoints
        // ---------------------------------------------------------

        [HttpGet("division/{divisionId}")]
        public async Task<IActionResult> GetByDivision(Guid divisionId)
        {
            _logger.LogInformation("Getting students by division {DivisionId}", divisionId);
            var result = await _studentService.GetByDivisionIdAsync(divisionId);
            return Ok(result);
        }

        [HttpGet("class/{classId}")]
        public async Task<IActionResult> GetByClass(Guid classId)
        {
            _logger.LogInformation("Getting students by class {ClassId}", classId);
            var result = await _studentService.GetByClassIdAsync(classId);
            return Ok(result);
        }

        [HttpGet("branch/{branchId}")]
        public async Task<IActionResult> GetByBranch(Guid branchId)
        {
            _logger.LogInformation("Getting students by branch {BranchId}", branchId);
            var result = await _studentService.GetByBranchIdAsync(branchId);
            return Ok(result);
        }

        [HttpGet("academic-year/{academicYearId}")]
        public async Task<IActionResult> GetByAcademicYear(Guid academicYearId)
        {
            _logger.LogInformation("Getting students by academic year {AcademicYearId}", academicYearId);
            var result = await _studentService.GetByAcademicYearIdAsync(academicYearId);
            return Ok(result);
        }

        [HttpGet("class-teacher/{staffId}")]
        public async Task<IActionResult> GetByClassTeacher(Guid staffId)
        {
            _logger.LogInformation("Getting students by class teacher staffId {StaffId}", staffId);
            var result = await _studentService.GetByClassTeacherIdAsync(staffId);
            return Ok(result);
        }

        /// <summary>Get student by Aadhaar (child). Caching, logging, exception handling in service.</summary>
        [HttpGet("aadhaar/{aadhaar}")]
        public async Task<IActionResult> GetStudentByAadhaar(string aadhaar)
        {
            _logger.LogInformation("GetStudentByAadhaar: {Aadhaar}", aadhaar);
            var result = await _studentService.GetByAadhaarAsync(aadhaar);
            if (result == null) return NotFound();
            return Ok(result);
        }

        [HttpGet("by-admission")]
        public async Task<IActionResult> GetStudentByAdmissionNumber([FromQuery] Guid branchId, [FromQuery] string admissionNo)
        {
            _logger.LogInformation("GetStudentByAdmissionNumber: BranchId {BranchId}, AdmissionNo {AdmissionNo}", branchId, admissionNo);
            var result = await _studentService.GetByAdmissionNumberAsync(branchId, admissionNo ?? "");
            if (result == null) return NotFound();
            return Ok(result);
        }

        [HttpGet("by-roll")]
        public async Task<IActionResult> GetStudentByRollNumber([FromQuery] Guid classId, [FromQuery] string rollNo)
        {
            _logger.LogInformation("GetStudentByRollNumber: ClassId {ClassId}, RollNo {RollNo}", classId, rollNo);
            var result = await _studentService.GetByRollNumberAsync(classId, rollNo ?? "");
            if (result == null) return NotFound();
            return Ok(result);
        }

        [HttpGet("by-father-phone")]
        public async Task<IActionResult> GetStudentByFatherPhone([FromQuery] string phone)
        {
            _logger.LogInformation("GetStudentByFatherPhone: {Phone}", phone);
            var result = await _studentService.GetByFatherPhoneAsync(phone ?? "");
            return Ok(result);
        }

        [HttpGet("by-mother-phone")]
        public async Task<IActionResult> GetStudentByMotherPhone([FromQuery] string phone)
        {
            _logger.LogInformation("GetStudentByMotherPhone: {Phone}", phone);
            var result = await _studentService.GetByMotherPhoneAsync(phone ?? "");
            return Ok(result);
        }

        [HttpGet("by-guardian-phone")]
        public async Task<IActionResult> GetStudentByGuardianPhone([FromQuery] string phone)
        {
            _logger.LogInformation("GetStudentByGuardianPhone: {Phone}", phone);
            var result = await _studentService.GetByGuardianPhoneAsync(phone ?? "");
            return Ok(result);
        }

        [HttpGet("by-email")]
        public async Task<IActionResult> GetStudentByEmail([FromQuery] string email)
        {
            _logger.LogInformation("GetStudentByEmail: {Email}", email);
            var result = await _studentService.GetByEmailAsync(email ?? "");
            if (result == null) return NotFound();
            return Ok(result);
        }

        [HttpGet("by-gender")]
        public async Task<IActionResult> GetStudentsByGender([FromQuery] string gender)
        {
            _logger.LogInformation("GetStudentsByGender: {Gender}", gender);
            var result = await _studentService.GetByGenderAsync(gender ?? "");
            return Ok(result);
        }

        [HttpGet("by-caste")]
        public async Task<IActionResult> GetStudentsByCaste([FromQuery] string caste)
        {
            _logger.LogInformation("GetStudentsByCaste: {Caste}", caste);
            var result = await _studentService.GetByCasteAsync(caste ?? "");
            return Ok(result);
        }

        [HttpGet("by-category")]
        public async Task<IActionResult> GetStudentsByCategory([FromQuery] string category)
        {
            _logger.LogInformation("GetStudentsByCategory: {Category}", category);
            var result = await _studentService.GetByCategoryAsync(category ?? "");
            return Ok(result);
        }

        [HttpGet("by-religion")]
        public async Task<IActionResult> GetStudentsByReligion([FromQuery] string religion)
        {
            _logger.LogInformation("GetStudentsByReligion: {Religion}", religion);
            var result = await _studentService.GetByReligionAsync(religion ?? "");
            return Ok(result);
        }

        [HttpGet("by-nationality")]
        public async Task<IActionResult> GetStudentsByNationality([FromQuery] string nationality)
        {
            _logger.LogInformation("GetStudentsByNationality: {Nationality}", nationality);
            var result = await _studentService.GetByNationalityAsync(nationality ?? "");
            return Ok(result);
        }

        [HttpGet("by-blood-group")]
        public async Task<IActionResult> GetStudentsByBloodGroup([FromQuery] string bloodGroup)
        {
            _logger.LogInformation("GetStudentsByBloodGroup: {BloodGroup}", bloodGroup);
            var result = await _studentService.GetByBloodGroupAsync(bloodGroup ?? "");
            return Ok(result);
        }

        [HttpGet("by-disability")]
        public async Task<IActionResult> GetStudentsByDisability([FromQuery] string disability)
        {
            _logger.LogInformation("GetStudentsByDisability: {Disability}", disability);
            var result = await _studentService.GetByDisabilityAsync(disability ?? "");
            return Ok(result);
        }

        [HttpGet("by-city")]
        public async Task<IActionResult> GetStudentsByCity([FromQuery] string city)
        {
            _logger.LogInformation("GetStudentsByCity: {City}", city);
            var result = await _studentService.GetByCityAsync(city ?? "");
            return Ok(result);
        }

        [HttpGet("by-state")]
        public async Task<IActionResult> GetStudentsByState([FromQuery] string state)
        {
            _logger.LogInformation("GetStudentsByState: {State}", state);
            var result = await _studentService.GetByStateAsync(state ?? "");
            return Ok(result);
        }

        [HttpGet("by-pincode")]
        public async Task<IActionResult> GetStudentsByPincode([FromQuery] string pincode)
        {
            _logger.LogInformation("GetStudentsByPincode: {Pincode}", pincode);
            var result = await _studentService.GetByPincodeAsync(pincode ?? "");
            return Ok(result);
        }

        [HttpGet("by-address-keyword")]
        public async Task<IActionResult> GetStudentsByAddressKeyword([FromQuery] string keyword)
        {
            _logger.LogInformation("GetStudentsByAddressKeyword: {Keyword}", keyword);
            var result = await _studentService.GetByAddressKeywordAsync(keyword ?? "");
            return Ok(result);
        }

        [HttpGet("by-previous-school")]
        public async Task<IActionResult> GetStudentsByPreviousSchool([FromQuery] string schoolName)
        {
            _logger.LogInformation("GetStudentsByPreviousSchool: {SchoolName}", schoolName);
            var result = await _studentService.GetByPreviousSchoolAsync(schoolName ?? "");
            return Ok(result);
        }

        [HttpGet("by-previous-board")]
        public async Task<IActionResult> GetStudentsByPreviousBoard([FromQuery] string boardName)
        {
            _logger.LogInformation("GetStudentsByPreviousBoard: {BoardName}", boardName);
            var result = await _studentService.GetByPreviousBoardAsync(boardName ?? "");
            return Ok(result);
        }

        [HttpGet("by-family-income-range")]
        public async Task<IActionResult> GetStudentsByFamilyIncomeRange([FromQuery] decimal min, [FromQuery] decimal max)
        {
            _logger.LogInformation("GetStudentsByFamilyIncomeRange: Min {Min}, Max {Max}", min, max);
            var result = await _studentService.GetByFamilyIncomeRangeAsync(min, max);
            return Ok(result);
        }

        [HttpGet("with-outstanding-fees")]
        public async Task<IActionResult> GetStudentsWithOutstandingFees()
        {
            _logger.LogInformation("GetStudentsWithOutstandingFees");
            var result = await _studentService.GetWithOutstandingFeesAsync();
            return Ok(result);
        }

        [HttpGet("with-scholarship")]
        public async Task<IActionResult> GetStudentsWithScholarship()
        {
            _logger.LogInformation("GetStudentsWithScholarship");
            var result = await _studentService.GetWithScholarshipAsync();
            return Ok(result);
        }

        [HttpGet("with-medical-condition")]
        public async Task<IActionResult> GetStudentsWithMedicalCondition()
        {
            _logger.LogInformation("GetStudentsWithMedicalCondition");
            var result = await _studentService.GetWithMedicalConditionAsync();
            return Ok(result);
        }

        [HttpGet("with-allergies")]
        public async Task<IActionResult> GetStudentsWithAllergies()
        {
            _logger.LogInformation("GetStudentsWithAllergies");
            var result = await _studentService.GetWithAllergiesAsync();
            return Ok(result);
        }

        [HttpGet("by-height-range")]
        public async Task<IActionResult> GetStudentsByHeightRange([FromQuery] decimal min, [FromQuery] decimal max)
        {
            _logger.LogInformation("GetStudentsByHeightRange: Min {Min}, Max {Max}", min, max);
            var result = await _studentService.GetByHeightRangeAsync(min, max);
            return Ok(result);
        }

        [HttpGet("by-weight-range")]
        public async Task<IActionResult> GetStudentsByWeightRange([FromQuery] decimal min, [FromQuery] decimal max)
        {
            _logger.LogInformation("GetStudentsByWeightRange: Min {Min}, Max {Max}", min, max);
            var result = await _studentService.GetByWeightRangeAsync(min, max);
            return Ok(result);
        }

        [HttpGet("by-dob-range")]
        public async Task<IActionResult> GetStudentsByDobRange([FromQuery] DateOnly fromDate, [FromQuery] DateOnly toDate)
        {
            _logger.LogInformation("GetStudentsByDobRange: From {FromDate}, To {ToDate}", fromDate, toDate);
            var result = await _studentService.GetByDobRangeAsync(fromDate, toDate);
            return Ok(result);
        }

        [HttpGet("by-admission-date-range")]
        public async Task<IActionResult> GetStudentsByAdmissionDateRange([FromQuery] DateOnly fromDate, [FromQuery] DateOnly toDate)
        {
            _logger.LogInformation("GetStudentsByAdmissionDateRange: From {FromDate}, To {ToDate}", fromDate, toDate);
            var result = await _studentService.GetByAdmissionDateRangeAsync(fromDate, toDate);
            return Ok(result);
        }

        [HttpGet("by-leaving-date-range")]
        public async Task<IActionResult> GetStudentsByLeavingDateRange([FromQuery] DateOnly fromDate, [FromQuery] DateOnly toDate)
        {
            _logger.LogInformation("GetStudentsByLeavingDateRange: From {FromDate}, To {ToDate}", fromDate, toDate);
            var result = await _studentService.GetByLeavingDateRangeAsync(fromDate, toDate);
            return Ok(result);
        }

        [HttpGet("by-status")]
        public async Task<IActionResult> GetStudentsByStatus([FromQuery] string status)
        {
            _logger.LogInformation("GetStudentsByStatus: {Status}", status);
            var result = await _studentService.GetByStatusAsync(status ?? "");
            return Ok(result);
        }

        [HttpGet("active")]
        public async Task<IActionResult> GetActiveStudents()
        {
            _logger.LogInformation("GetActiveStudents");
            var result = await _studentService.GetActiveStudentsAsync();
            return Ok(result);
        }

        [HttpGet("inactive")]
        public async Task<IActionResult> GetInactiveStudents()
        {
            _logger.LogInformation("GetInactiveStudents");
            var result = await _studentService.GetInactiveStudentsAsync();
            return Ok(result);
        }

        [HttpGet("dropped")]
        public async Task<IActionResult> GetDroppedStudents()
        {
            _logger.LogInformation("GetDroppedStudents");
            var result = await _studentService.GetDroppedStudentsAsync();
            return Ok(result);
        }

        [HttpGet("transferred")]
        public async Task<IActionResult> GetTransferredStudents()
        {
            _logger.LogInformation("GetTransferredStudents");
            var result = await _studentService.GetTransferredStudentsAsync();
            return Ok(result);
        }

        [HttpGet("passed-out")]
        public async Task<IActionResult> GetPassedOutStudents()
        {
            _logger.LogInformation("GetPassedOutStudents");
            var result = await _studentService.GetPassedOutStudentsAsync();
            return Ok(result);
        }

        /// <summary>Search students by optional filter (keyword, branch, class, status, location, etc.). Uses caching and logging.</summary>
        [HttpPost("search")]
        public async Task<IActionResult> SearchStudents([FromBody] StudentFilterDto filterDto)
        {
            _logger.LogInformation("SearchStudents");
            var result = await _studentService.SearchStudentsAsync(filterDto ?? new StudentFilterDto());
            return Ok(result);
        }

        /// <summary>Advanced multi-criteria search. All criteria are ANDed. Uses caching and logging.</summary>
        [HttpPost("search/advanced")]
        public async Task<IActionResult> AdvancedStudentSearch([FromBody] AdvancedStudentSearchDto multiCriteriaDto)
        {
            _logger.LogInformation("AdvancedStudentSearch");
            var result = await _studentService.AdvancedStudentSearchAsync(multiCriteriaDto ?? new AdvancedStudentSearchDto());
            return Ok(result);
        }

        /// <summary>Global keyword search across name, admission no, email, phone, address, roll, enrollment, aadhaar, parent/guardian names. Uses caching and logging.</summary>
        [HttpGet("search/global")]
        public async Task<IActionResult> GlobalStudentSearch([FromQuery] string keyword)
        {
            _logger.LogInformation("GlobalStudentSearch: {Keyword}", keyword);
            var result = await _studentService.GlobalStudentSearchAsync(keyword ?? "");
            return Ok(result);
        }

        // ---------------------------------------------------------
        // Analytics: strength and distribution by branch (caching, logging, exception handling in service)
        // ---------------------------------------------------------

        [HttpGet("analytics/strength-by-class")]
        public async Task<IActionResult> GetStudentStrengthByClass([FromQuery] Guid branchId)
        {
            _logger.LogInformation("GetStudentStrengthByClass: BranchId {BranchId}", branchId);
            var result = await _studentService.GetStudentStrengthByClassAsync(branchId);
            return Ok(result);
        }

        [HttpGet("analytics/strength-by-division")]
        public async Task<IActionResult> GetStudentStrengthByDivision([FromQuery] Guid branchId)
        {
            _logger.LogInformation("GetStudentStrengthByDivision: BranchId {BranchId}", branchId);
            var result = await _studentService.GetStudentStrengthByDivisionAsync(branchId);
            return Ok(result);
        }

        [HttpGet("analytics/gender-distribution")]
        public async Task<IActionResult> GetGenderDistribution([FromQuery] Guid branchId)
        {
            _logger.LogInformation("GetGenderDistribution: BranchId {BranchId}", branchId);
            var result = await _studentService.GetGenderDistributionAsync(branchId);
            return Ok(result);
        }

        [HttpGet("analytics/category-distribution")]
        public async Task<IActionResult> GetCategoryWiseDistribution([FromQuery] Guid branchId)
        {
            _logger.LogInformation("GetCategoryWiseDistribution: BranchId {BranchId}", branchId);
            var result = await _studentService.GetCategoryWiseDistributionAsync(branchId);
            return Ok(result);
        }

        [HttpGet("analytics/caste-distribution")]
        public async Task<IActionResult> GetCasteWiseDistribution([FromQuery] Guid branchId)
        {
            _logger.LogInformation("GetCasteWiseDistribution: BranchId {BranchId}", branchId);
            var result = await _studentService.GetCasteWiseDistributionAsync(branchId);
            return Ok(result);
        }

        [HttpGet("analytics/religion-distribution")]
        public async Task<IActionResult> GetReligionWiseDistribution([FromQuery] Guid branchId)
        {
            _logger.LogInformation("GetReligionWiseDistribution: BranchId {BranchId}", branchId);
            var result = await _studentService.GetReligionWiseDistributionAsync(branchId);
            return Ok(result);
        }

        [HttpGet("analytics/city-distribution")]
        public async Task<IActionResult> GetCityWiseDistribution([FromQuery] Guid branchId)
        {
            _logger.LogInformation("GetCityWiseDistribution: BranchId {BranchId}", branchId);
            var result = await _studentService.GetCityWiseDistributionAsync(branchId);
            return Ok(result);
        }

        [HttpGet("analytics/state-distribution")]
        public async Task<IActionResult> GetStateWiseDistribution([FromQuery] Guid branchId)
        {
            _logger.LogInformation("GetStateWiseDistribution: BranchId {BranchId}", branchId);
            var result = await _studentService.GetStateWiseDistributionAsync(branchId);
            return Ok(result);
        }

        [HttpGet("analytics/blood-group-distribution")]
        public async Task<IActionResult> GetBloodGroupDistribution([FromQuery] Guid branchId)
        {
            _logger.LogInformation("GetBloodGroupDistribution: BranchId {BranchId}", branchId);
            var result = await _studentService.GetBloodGroupDistributionAsync(branchId);
            return Ok(result);
        }

        [HttpGet("analytics/age-distribution")]
        public async Task<IActionResult> GetAgeWiseDistribution([FromQuery] Guid branchId)
        {
            _logger.LogInformation("GetAgeWiseDistribution: BranchId {BranchId}", branchId);
            var result = await _studentService.GetAgeWiseDistributionAsync(branchId);
            return Ok(result);
        }

        [HttpGet("analytics/income-distribution")]
        public async Task<IActionResult> GetIncomeDistribution([FromQuery] Guid branchId)
        {
            _logger.LogInformation("GetIncomeDistribution: BranchId {BranchId}", branchId);
            var result = await _studentService.GetIncomeDistributionAsync(branchId);
            return Ok(result);
        }

        [HttpGet("analytics/growth-trend")]
        public async Task<IActionResult> GetStudentGrowthTrend([FromQuery] Guid branchId)
        {
            _logger.LogInformation("GetStudentGrowthTrend: BranchId {BranchId}", branchId);
            var result = await _studentService.GetStudentGrowthTrendAsync(branchId);
            return Ok(result);
        }

        [HttpGet("analytics/admission-trend")]
        public async Task<IActionResult> GetYearWiseAdmissionTrend([FromQuery] Guid branchId)
        {
            _logger.LogInformation("GetYearWiseAdmissionTrend: BranchId {BranchId}", branchId);
            var result = await _studentService.GetYearWiseAdmissionTrendAsync(branchId);
            return Ok(result);
        }

        [HttpGet("analytics/dropout-rate")]
        public async Task<IActionResult> DropoutRateAnalysis([FromQuery] Guid branchId)
        {
            _logger.LogInformation("DropoutRateAnalysis: BranchId {BranchId}", branchId);
            var result = await _studentService.DropoutRateAnalysisAsync(branchId);
            return Ok(result);
        }

        [HttpGet("analytics/promotion-success-rate")]
        public async Task<IActionResult> PromotionSuccessRate([FromQuery] Guid academicYearId)
        {
            _logger.LogInformation("PromotionSuccessRate: AcademicYearId {AcademicYearId}", academicYearId);
            var result = await _studentService.PromotionSuccessRateAsync(academicYearId);
            return Ok(result);
        }

        [HttpGet("analytics/retention-rate")]
        public async Task<IActionResult> StudentRetentionRate()
        {
            _logger.LogInformation("StudentRetentionRate");
            var result = await _studentService.StudentRetentionRateAsync();
            return Ok(result);
        }

        [HttpGet("analytics/transfer-rate")]
        public async Task<IActionResult> TransferRateAnalysis([FromQuery] Guid branchId)
        {
            _logger.LogInformation("TransferRateAnalysis: BranchId {BranchId}", branchId);
            var result = await _studentService.TransferRateAnalysisAsync(branchId);
            return Ok(result);
        }

        [HttpGet("analytics/gender-ratio-trend")]
        public async Task<IActionResult> GetGenderRatioTrend([FromQuery] Guid branchId)
        {
            _logger.LogInformation("GetGenderRatioTrend: BranchId {BranchId}", branchId);
            var result = await _studentService.GetGenderRatioTrendAsync(branchId);
            return Ok(result);
        }

        [HttpGet("analytics/caste-trend")]
        public async Task<IActionResult> GetCasteTrendAnalysis([FromQuery] Guid branchId)
        {
            _logger.LogInformation("GetCasteTrendAnalysis: BranchId {BranchId}", branchId);
            var result = await _studentService.GetCasteTrendAnalysisAsync(branchId);
            return Ok(result);
        }

        [HttpGet("analytics/urban-rural-distribution")]
        public async Task<IActionResult> UrbanRuralDistributionAnalysis([FromQuery] Guid branchId)
        {
            _logger.LogInformation("UrbanRuralDistributionAnalysis: BranchId {BranchId}", branchId);
            var result = await _studentService.UrbanRuralDistributionAnalysisAsync(branchId);
            return Ok(result);
        }

        [HttpGet("analytics/admission-conversion-rate")]
        public async Task<IActionResult> AdmissionConversionRate()
        {
            _logger.LogInformation("AdmissionConversionRate");
            var result = await _studentService.AdmissionConversionRateAsync();
            return Ok(result);
        }

        // ---------------------------------------------------------
        // Reports (caching, logging, exception handling in service)
        // ---------------------------------------------------------

        [HttpGet("reports/udise")]
        public async Task<IActionResult> GenerateUDISEReport()
        {
            _logger.LogInformation("GenerateUDISEReport");
            var result = await _studentService.GenerateUDISEReportAsync();
            return Ok(result);
        }

        [HttpGet("reports/minority")]
        public async Task<IActionResult> GenerateMinorityReport()
        {
            _logger.LogInformation("GenerateMinorityReport");
            var result = await _studentService.GenerateMinorityReportAsync();
            return Ok(result);
        }

        [HttpGet("reports/scstobc")]
        public async Task<IActionResult> GenerateSCSTOBCReport()
        {
            _logger.LogInformation("GenerateSCSTOBCReport");
            var result = await _studentService.GenerateSCSTOBCReportAsync();
            return Ok(result);
        }

        [HttpGet("reports/aadhaar-linked")]
        public async Task<IActionResult> GenerateAadhaarLinkedStudentsReport()
        {
            _logger.LogInformation("GenerateAadhaarLinkedStudentsReport");
            var result = await _studentService.GenerateAadhaarLinkedStudentsReportAsync();
            return Ok(result);
        }

        [HttpGet("reports/scholarship-eligible")]
        public async Task<IActionResult> GenerateScholarshipEligibleStudentsReport()
        {
            _logger.LogInformation("GenerateScholarshipEligibleStudentsReport");
            var result = await _studentService.GenerateScholarshipEligibleStudentsReportAsync();
            return Ok(result);
        }

        [HttpGet("reports/rte-eligible")]
        public async Task<IActionResult> GenerateRTEEligibleStudentsReport()
        {
            _logger.LogInformation("GenerateRTEEligibleStudentsReport");
            var result = await _studentService.GenerateRTEEligibleStudentsReportAsync();
            return Ok(result);
        }

        // ---------------------------------------------------------
        // Validation / check endpoints (caching, logging, exception handling in service)
        // ---------------------------------------------------------

        [HttpGet("check/admission")]
        public async Task<IActionResult> CheckAdmissionNumberExists([FromQuery] Guid branchId, [FromQuery] string admissionNo)
        {
            _logger.LogInformation("CheckAdmissionNumberExists: BranchId {BranchId}, AdmissionNo {AdmissionNo}", branchId, admissionNo);
            var exists = await _studentService.CheckAdmissionNumberExistsAsync(branchId, admissionNo ?? "");
            return Ok(new { exists });
        }

        [HttpGet("check/roll")]
        public async Task<IActionResult> CheckRollNumberExists([FromQuery] Guid classId, [FromQuery] string rollNo)
        {
            _logger.LogInformation("CheckRollNumberExists: ClassId {ClassId}, RollNo {RollNo}", classId, rollNo);
            var exists = await _studentService.CheckRollNumberExistsAsync(classId, rollNo ?? "");
            return Ok(new { exists });
        }

        [HttpGet("check/aadhaar")]
        public async Task<IActionResult> CheckAadhaarExists([FromQuery] string childAadhaar)
        {
            _logger.LogInformation("CheckAadhaarExists");
            var exists = await _studentService.CheckAadhaarExistsAsync(childAadhaar ?? "");
            return Ok(new { exists });
        }

        [HttpGet("validate/aadhaar")]
        public async Task<IActionResult> ValidateAadhaar([FromQuery] string childAadhaar)
        {
            _logger.LogInformation("ValidateAadhaar");
            var result = await _studentService.ValidateAadhaarAsync(childAadhaar ?? "");
            return Ok(result);
        }

        [HttpGet("validate/age-eligibility")]
        public async Task<IActionResult> ValidateAgeEligibility([FromQuery] DateOnly dob, [FromQuery] Guid? classId)
        {
            _logger.LogInformation("ValidateAgeEligibility: Dob {Dob}, ClassId {ClassId}", dob, classId);
            var result = await _studentService.ValidateAgeEligibilityAsync(dob, classId);
            return Ok(result);
        }

        [HttpPost("validate/duplicate")]
        public async Task<IActionResult> ValidateDuplicateStudent([FromBody] StudentRequest dto)
        {
            _logger.LogInformation("ValidateDuplicateStudent");
            var result = await _studentService.ValidateDuplicateStudentAsync(dto);
            return Ok(result);
        }

        [HttpGet("validate/promotion-eligibility/{id}")]
        public async Task<IActionResult> ValidatePromotionEligibility(Guid id)
        {
            _logger.LogInformation("ValidatePromotionEligibility: {Id}", id);
            var result = await _studentService.ValidatePromotionEligibilityAsync(id);
            return Ok(result);
        }

        [HttpGet("validate/transfer-eligibility/{id}")]
        public async Task<IActionResult> ValidateTransferEligibility(Guid id)
        {
            _logger.LogInformation("ValidateTransferEligibility: {Id}", id);
            var result = await _studentService.ValidateTransferEligibilityAsync(id);
            return Ok(result);
        }
    }
}
