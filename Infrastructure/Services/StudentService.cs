using Application.Abstractions;
using Application.Abstractions.Repositories;
using Application.Abstractions.Services;
using Application.Common.Helpers;
using Application.ViewModels.Student;
using AutoMapper;
using Domain.Entities.Postgres.Scaffolded;
using Infrastructure;
using Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;


namespace Infrastructure.Services
{
    public class StudentService : IStudentService
    {
        private readonly ILogger<StudentService> _logger;
        private readonly IMapper _mapper;
        private readonly IStudentRepository _studentRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICacheService _cacheService;
        private readonly ITeacherSubjectMapRepository _teacherSubjectMapRepository;
        private readonly IInvoiceRepository _invoiceRepository;
        private readonly IAttendanceRepository _attendanceRepository;
        private readonly IClassRepository _classRepository;
        private readonly IDivisionRepository _divisionRepository;
        private readonly IBranchRepository _branchRepository;
        private readonly IAcademicYearRepository _academicYearRepository;

        public StudentService(
            ILogger<StudentService> logger,
            IStudentRepository studentRepository,
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ICacheService cacheService,
            ITeacherSubjectMapRepository teacherSubjectMapRepository,
            IInvoiceRepository invoiceRepository,
            IAttendanceRepository attendanceRepository,
            IClassRepository classRepository,
            IDivisionRepository divisionRepository,
            IBranchRepository branchRepository,
            IAcademicYearRepository academicYearRepository)
        {
            _logger = logger;
            _studentRepository = studentRepository;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _cacheService = cacheService;
            _teacherSubjectMapRepository = teacherSubjectMapRepository;
            _invoiceRepository = invoiceRepository;
            _attendanceRepository = attendanceRepository;
            _classRepository = classRepository;
            _divisionRepository = divisionRepository;
            _branchRepository = branchRepository;
            _academicYearRepository = academicYearRepository;
        }
        public async Task<IEnumerable<StudentResponse>> GetAllAsync()
        {
            try
            {
                _logger.LogInformation("Fetching all students");
                string cacheKey = "students:all";
                if (_cacheService.Exists(cacheKey))
                {
                    _logger.LogInformation("Returning students from cache");
                    return _cacheService.Get<IEnumerable<StudentResponse>>(cacheKey);
                }
                var students = await _studentRepository.GetAllAsync();
                var result = students.Select(MapStudentToResponse).ToList();
                _cacheService.Set(cacheKey, result);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching all students");
                throw new Application.Exceptions.BaseServiceException("Error fetching all students", ex, Application.Common.ExceptionCodes.Database);
            }
        }

        public async Task<PagedStudentsResponse> GetPagedAsync(int page, int pageSize)
        {
            try
            {
                if (page < 1) page = 1;
                if (pageSize < 1) pageSize = 20;
                if (pageSize > 100) pageSize = 100;

                _logger.LogInformation("Fetching students paged: page {Page}, pageSize {PageSize}", page, pageSize);
                var (items, totalCount) = await _studentRepository.GetPagedAsync(page, pageSize);
                var mapped = items.Select(MapStudentToResponse).ToList();
                return new PagedStudentsResponse
                {
                    Items = mapped,
                    TotalCount = totalCount,
                    Page = page,
                    PageSize = pageSize,
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching paged students");
                throw new Application.Exceptions.BaseServiceException("Error fetching paged students", ex, Application.Common.ExceptionCodes.Database);
            }
        }

        public async Task<StudentResponse?> GetByIdAsync(Guid id)
        {
            try
            {
                _logger.LogInformation($"Fetching student by id: {id}");
                string cacheKey = $"student:{id}";
                if (_cacheService.Exists(cacheKey))
                {
                    _logger.LogInformation($"Returning student {id} from cache");
                    return _cacheService.Get<StudentResponse>(cacheKey);
                }
                var student = await _studentRepository.GetByIdAsync(id);
                if (student == null)
                {
                    _logger.LogWarning($"Student not found: {id}");
                    return null;
                }
                var response = MapStudentToResponse(student);
                _cacheService.Set(cacheKey, response);
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error fetching student by id: {id}");
                throw new Application.Exceptions.BaseServiceException($"Error fetching student by id: {id}", ex, Application.Common.ExceptionCodes.Database);
            }
        }

        public async Task<StudentResponse> CreateAsync(StudentRequest request)
        {
            try
            {
                _logger.LogInformation("Creating student");

                var student = _mapper.Map<Student>(request);
                await _studentRepository.AddAsync(student);
                await _unitOfWork.Commit();
                // Invalidate cache
                _cacheService.Remove("students:all");
                return MapStudentToResponse(student);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating student");
                _unitOfWork.Rollback();
                throw new Application.Exceptions.BaseServiceException("Error creating student", ex, Application.Common.ExceptionCodes.Database);
            }
        }

        public async Task<StudentResponse?> UpdateAsync(Guid id, StudentRequest request)
        {
            try
            {
                _logger.LogInformation($"Updating student: {id}");
                var student = await _studentRepository.GetByIdAsync(id);
                if (student == null)
                {
                    _logger.LogWarning($"Student not found: {id}");
                    return null;
                }
                _mapper.Map(request, student);

                await _studentRepository.UpdateAsync(student);
                await _unitOfWork.Commit();
                // Invalidate cache
                _cacheService.Remove("students:all");
                _cacheService.Remove($"student:{id}");
                return MapStudentToResponse(student);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating student: {id}");
                _unitOfWork.Rollback();
                throw new Application.Exceptions.BaseServiceException($"Error updating student: {id}", ex, Application.Common.ExceptionCodes.Database);
            }
        }

        public async Task<StudentResponse?> PatchAsync(Guid id, StudentRequest dto)
        {
            try
            {
                _logger.LogInformation("Patching student: {Id}", id);
                var student = await _studentRepository.GetByIdAsync(id);
                if (student == null)
                {
                    _logger.LogWarning("Student not found for patch: {Id}", id);
                    return null;
                }
                if (dto.BranchId.HasValue) student.BranchId = dto.BranchId;
                if (dto.AcademicYearId.HasValue) student.AcademicYearId = dto.AcademicYearId;
                if (dto.AdmissionNo != null) student.AdmissionNo = dto.AdmissionNo;
                if (dto.AdmissionDate.HasValue) student.AdmissionDate = dto.AdmissionDate;
                if (dto.AparId != null) student.AparId = dto.AparId;
                if (dto.PhotoUrl != null) student.PhotoUrl = dto.PhotoUrl;
                if (dto.SignUrl != null) student.SignUrl = dto.SignUrl;
                if (dto.FirstName != null) student.FirstName = dto.FirstName;
                if (dto.MiddleName != null) student.MiddleName = dto.MiddleName;
                if (dto.LastName != null) student.LastName = dto.LastName;
                if (dto.Gender != null) student.Gender = dto.Gender;
                if (dto.Dob.HasValue) student.Dob = dto.Dob;
                if (dto.BirthPlace != null) student.BirthPlace = dto.BirthPlace;
                if (dto.BirthCertificateNo != null) student.BirthCertificateNo = dto.BirthCertificateNo;
                if (dto.BirthCertificateIssueDate.HasValue) student.BirthCertificateIssueDate = dto.BirthCertificateIssueDate;
                if (dto.ChildAadhaar != null) student.ChildAadhaar = dto.ChildAadhaar;
                if (dto.Religion != null) student.Religion = dto.Religion;
                if (dto.Caste != null) student.Caste = dto.Caste;
                if (dto.Category != null) student.Category = dto.Category;
                if (dto.Nationality != null) student.Nationality = dto.Nationality;
                if (dto.BloodGroup != null) student.BloodGroup = dto.BloodGroup;
                if (dto.MarkIdentification != null) student.MarkIdentification = dto.MarkIdentification;
                if (dto.HeightCm.HasValue) student.HeightCm = dto.HeightCm;
                if (dto.WeightKg.HasValue) student.WeightKg = dto.WeightKg;
                if (dto.MedicalConditions != null) student.MedicalConditions = dto.MedicalConditions;
                if (dto.Allergies != null) student.Allergies = dto.Allergies;
                if (dto.Disability != null) student.Disability = dto.Disability;
                if (dto.RollNumber != null) student.RollNumber = dto.RollNumber;
                if (dto.StudentStatus != null) student.StudentStatus = dto.StudentStatus;
                if (dto.EnrollmentNo != null) student.EnrollmentNo = dto.EnrollmentNo;
                if (dto.IsAadhaarVerified.HasValue) student.IsAadhaarVerified = dto.IsAadhaarVerified;
                if (dto.PromotedToClassId.HasValue) student.PromotedToClassId = dto.PromotedToClassId;
                if (dto.LeavingDate.HasValue) student.LeavingDate = dto.LeavingDate;
                if (dto.IsTcIssued.HasValue) student.IsTcIssued = dto.IsTcIssued;
                if (dto.LastExamName != null) student.LastExamName = dto.LastExamName;
                if (dto.LastExamResult != null) student.LastExamResult = dto.LastExamResult;
                if (dto.HasScholarship.HasValue) student.HasScholarship = dto.HasScholarship;
                if (dto.ScholarshipType != null) student.ScholarshipType = dto.ScholarshipType;
                if (dto.ScholarshipStatus != null) student.ScholarshipStatus = dto.ScholarshipStatus;
                if (dto.ScholarshipAmount.HasValue) student.ScholarshipAmount = dto.ScholarshipAmount;
                if (dto.ScholarshipEligible.HasValue) student.ScholarshipEligible = dto.ScholarshipEligible;
                if (dto.IsRteSeat.HasValue) student.IsRteSeat = dto.IsRteSeat;
                if (dto.RteEligible.HasValue) student.RteEligible = dto.RteEligible;
                if (dto.RteApplicationNo != null) student.RteApplicationNo = dto.RteApplicationNo;
                if (dto.EwsDgCategory != null) student.EwsDgCategory = dto.EwsDgCategory;
                if (dto.IsMinority.HasValue) student.IsMinority = dto.IsMinority;
                if (dto.IsBpl.HasValue) student.IsBpl = dto.IsBpl;
                if (dto.BplCardNo != null) student.BplCardNo = dto.BplCardNo;
                if (dto.UdiseCode != null) student.UdiseCode = dto.UdiseCode;
                if (dto.SingleGirlChild.HasValue) student.SingleGirlChild = dto.SingleGirlChild;
                if (dto.FirstGraduate.HasValue) student.FirstGraduate = dto.FirstGraduate;
                student.UpdatedAt = DateTime.UtcNow;
                await _studentRepository.UpdateAsync(student);
                await _unitOfWork.Commit();
                _cacheService.Remove("students:all");
                _cacheService.Remove($"student:{id}");
                _logger.LogInformation("Student patched successfully: {Id}", id);
                return MapStudentToResponse(student);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error patching student: {Id}", id);
                _unitOfWork.Rollback();
                throw new Application.Exceptions.BaseServiceException($"Error patching student: {id}", ex, Application.Common.ExceptionCodes.Database);
            }
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            try
            {
                _logger.LogInformation($"Deleting student: {id}");
                await _studentRepository.DeleteAsync(id);
                await _unitOfWork.Commit();
                // Invalidate cache
                _cacheService.Remove("students:all");
                _cacheService.Remove($"student:{id}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting student: {id}");
                _unitOfWork.Rollback();
                throw new Application.Exceptions.BaseServiceException($"Error deleting student: {id}", ex, Application.Common.ExceptionCodes.Database);
            }
        }

        public async Task<bool> SoftDeleteAsync(Guid id)
        {
            try
            {
                _logger.LogInformation("Soft-deleting student: {Id}", id);
                var student = await _studentRepository.GetByIdAsync(id);
                if (student == null)
                {
                    _logger.LogWarning("Student not found for soft delete: {Id}", id);
                    return false;
                }
                student.IsActive = false;
                student.UpdatedAt = DateTime.UtcNow;
                await _studentRepository.UpdateAsync(student);
                await _unitOfWork.Commit();
                _cacheService.Remove("students:all");
                _cacheService.Remove($"student:{id}");
                _logger.LogInformation("Student soft-deleted successfully: {Id}", id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error soft-deleting student: {Id}", id);
                _unitOfWork.Rollback();
                throw new Application.Exceptions.BaseServiceException($"Error soft-deleting student: {id}", ex, Application.Common.ExceptionCodes.Database);
            }
        }

        public async Task<bool> RestoreAsync(Guid id)
        {
            try
            {
                _logger.LogInformation("Restoring student: {Id}", id);
                var student = await _studentRepository.GetByIdAsync(id);
                if (student == null)
                {
                    _logger.LogWarning("Student not found for restore: {Id}", id);
                    return false;
                }
                student.IsActive = true;
                student.UpdatedAt = DateTime.UtcNow;
                await _studentRepository.UpdateAsync(student);
                await _unitOfWork.Commit();
                _cacheService.Remove("students:all");
                _cacheService.Remove($"student:{id}");
                _logger.LogInformation("Student restored successfully: {Id}", id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error restoring student: {Id}", id);
                _unitOfWork.Rollback();
                throw new Application.Exceptions.BaseServiceException($"Error restoring student: {id}", ex, Application.Common.ExceptionCodes.Database);
            }
        }

        public async Task<StudentResponse?> ActivateStudentAsync(Guid id)
        {
            try
            {
                _logger.LogInformation("ActivateStudent: {Id}", id);
                var student = await _studentRepository.GetByIdAsync(id);
                if (student == null) { _logger.LogWarning("Student not found: {Id}", id); return null; }
                student.IsActive = true;
                student.StudentStatus = "ACTIVE";
                student.UpdatedAt = DateTime.UtcNow;
                await _studentRepository.UpdateAsync(student);
                await _unitOfWork.Commit();
                InvalidateStudentCaches(id, student);
                return MapStudentToResponse(student);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error activating student: {Id}", id);
                _unitOfWork.Rollback();
                throw new Application.Exceptions.BaseServiceException($"Error activating student: {id}", ex, Application.Common.ExceptionCodes.Database);
            }
        }

        public async Task<StudentResponse?> SuspendStudentAsync(Guid id)
        {
            try
            {
                _logger.LogInformation("SuspendStudent: {Id}", id);
                var student = await _studentRepository.GetByIdAsync(id);
                if (student == null) { _logger.LogWarning("Student not found: {Id}", id); return null; }
                student.StudentStatus = "SUSPENDED";
                student.UpdatedAt = DateTime.UtcNow;
                await _studentRepository.UpdateAsync(student);
                await _unitOfWork.Commit();
                InvalidateStudentCaches(id, student);
                return MapStudentToResponse(student);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error suspending student: {Id}", id);
                _unitOfWork.Rollback();
                throw new Application.Exceptions.BaseServiceException($"Error suspending student: {id}", ex, Application.Common.ExceptionCodes.Database);
            }
        }

        public async Task<StudentResponse?> DropStudentAsync(Guid id)
        {
            try
            {
                _logger.LogInformation("DropStudent: {Id}", id);
                var student = await _studentRepository.GetByIdAsync(id);
                if (student == null) { _logger.LogWarning("Student not found: {Id}", id); return null; }
                student.StudentStatus = "DROPPED";
                student.UpdatedAt = DateTime.UtcNow;
                await _studentRepository.UpdateAsync(student);
                await _unitOfWork.Commit();
                InvalidateStudentCaches(id, student);
                return MapStudentToResponse(student);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error dropping student: {Id}", id);
                _unitOfWork.Rollback();
                throw new Application.Exceptions.BaseServiceException($"Error dropping student: {id}", ex, Application.Common.ExceptionCodes.Database);
            }
        }

        public async Task<StudentResponse?> TransferStudentAsync(Guid id, Guid newBranchId)
        {
            try
            {
                _logger.LogInformation("TransferStudent: {Id}, newBranchId {BranchId}", id, newBranchId);
                var student = await _studentRepository.GetByIdAsync(id);
                if (student == null) { _logger.LogWarning("Student not found: {Id}", id); return null; }
                student.BranchId = newBranchId;
                student.StudentStatus = "TRANSFERRED";
                student.UpdatedAt = DateTime.UtcNow;
                await _studentRepository.UpdateAsync(student);
                await _unitOfWork.Commit();
                InvalidateStudentCaches(id, student);
                return MapStudentToResponse(student);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error transferring student: {Id}", id);
                _unitOfWork.Rollback();
                throw new Application.Exceptions.BaseServiceException($"Error transferring student: {id}", ex, Application.Common.ExceptionCodes.Database);
            }
        }

        public async Task<StudentResponse?> PromoteStudentAsync(Guid id, Guid newClassId, Guid academicYearId)
        {
            try
            {
                _logger.LogInformation("PromoteStudent: {Id}, newClassId {ClassId}, academicYearId {YearId}", id, newClassId, academicYearId);
                var student = await _studentRepository.GetByIdAsync(id);
                if (student == null) { _logger.LogWarning("Student not found: {Id}", id); return null; }
                student.PromotedToClassId = newClassId;
                student.ClassId = newClassId;
                student.AcademicYearId = academicYearId;
                student.UpdatedAt = DateTime.UtcNow;
                await _studentRepository.UpdateAsync(student);
                await _unitOfWork.Commit();
                InvalidateStudentCaches(id, student);
                return MapStudentToResponse(student);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error promoting student: {Id}", id);
                _unitOfWork.Rollback();
                throw new Application.Exceptions.BaseServiceException($"Error promoting student: {id}", ex, Application.Common.ExceptionCodes.Database);
            }
        }

        public async Task<StudentResponse?> MarkStudentPassedOutAsync(Guid id)
        {
            try
            {
                _logger.LogInformation("MarkStudentPassedOut: {Id}", id);
                var student = await _studentRepository.GetByIdAsync(id);
                if (student == null) { _logger.LogWarning("Student not found: {Id}", id); return null; }
                student.StudentStatus = "PASSED_OUT";
                student.UpdatedAt = DateTime.UtcNow;
                await _studentRepository.UpdateAsync(student);
                await _unitOfWork.Commit();
                InvalidateStudentCaches(id, student);
                return MapStudentToResponse(student);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking student passed out: {Id}", id);
                _unitOfWork.Rollback();
                throw new Application.Exceptions.BaseServiceException($"Error marking student passed out: {id}", ex, Application.Common.ExceptionCodes.Database);
            }
        }

        public async Task<StudentResponse?> IssueTcAsync(Guid id)
        {
            try
            {
                _logger.LogInformation("IssueTc: {Id}", id);
                var student = await _studentRepository.GetByIdAsync(id);
                if (student == null) { _logger.LogWarning("Student not found: {Id}", id); return null; }
                student.IsTcIssued = true;
                student.StudentStatus = "TRANSFERRED";
                student.UpdatedAt = DateTime.UtcNow;
                await _studentRepository.UpdateAsync(student);
                await _unitOfWork.Commit();
                InvalidateStudentCaches(id, student);
                return MapStudentToResponse(student);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error issuing TC for student: {Id}", id);
                _unitOfWork.Rollback();
                throw new Application.Exceptions.BaseServiceException($"Error issuing TC for student: {id}", ex, Application.Common.ExceptionCodes.Database);
            }
        }

        public async Task<StudentResponse?> ReEnrollStudentAsync(Guid id)
        {
            try
            {
                _logger.LogInformation("ReEnrollStudent: {Id}", id);
                var student = await _studentRepository.GetByIdAsync(id);
                if (student == null) { _logger.LogWarning("Student not found: {Id}", id); return null; }
                student.IsActive = true;
                student.StudentStatus = "ACTIVE";
                student.LeavingDate = null;
                student.UpdatedAt = DateTime.UtcNow;
                await _studentRepository.UpdateAsync(student);
                await _unitOfWork.Commit();
                InvalidateStudentCaches(id, student);
                return MapStudentToResponse(student);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error re-enrolling student: {Id}", id);
                _unitOfWork.Rollback();
                throw new Application.Exceptions.BaseServiceException($"Error re-enrolling student: {id}", ex, Application.Common.ExceptionCodes.Database);
            }
        }

        public async Task<StudentResponse?> UpdateStudentStatusAsync(Guid id, string status)
        {
            try
            {
                _logger.LogInformation("UpdateStudentStatus: {Id}, status {Status}", id, status);
                var student = await _studentRepository.GetByIdAsync(id);
                if (student == null) { _logger.LogWarning("Student not found: {Id}", id); return null; }
                student.StudentStatus = status;
                student.UpdatedAt = DateTime.UtcNow;
                await _studentRepository.UpdateAsync(student);
                await _unitOfWork.Commit();
                InvalidateStudentCaches(id, student);
                return MapStudentToResponse(student);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating student status: {Id}", id);
                _unitOfWork.Rollback();
                throw new Application.Exceptions.BaseServiceException($"Error updating student status: {id}", ex, Application.Common.ExceptionCodes.Database);
            }
        }

        public async Task<StudentResponse?> UpdateLeavingDateAsync(Guid id, DateOnly date)
        {
            try
            {
                _logger.LogInformation("UpdateLeavingDate: {Id}, date {Date}", id, date);
                var student = await _studentRepository.GetByIdAsync(id);
                if (student == null) { _logger.LogWarning("Student not found: {Id}", id); return null; }
                student.LeavingDate = date;
                student.UpdatedAt = DateTime.UtcNow;
                await _studentRepository.UpdateAsync(student);
                await _unitOfWork.Commit();
                InvalidateStudentCaches(id, student);
                return MapStudentToResponse(student);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating leaving date: {Id}", id);
                _unitOfWork.Rollback();
                throw new Application.Exceptions.BaseServiceException($"Error updating leaving date: {id}", ex, Application.Common.ExceptionCodes.Database);
            }
        }

        public async Task<bool> CheckAdmissionNumberExistsAsync(Guid branchId, string admissionNo)
        {
            try
            {
                _logger.LogInformation("CheckAdmissionNumberExists: BranchId {BranchId}, AdmissionNo {AdmissionNo}", branchId, admissionNo);
                if (string.IsNullOrWhiteSpace(admissionNo)) return false;
                var cacheKey = $"check:admission:{branchId}:{admissionNo.Trim()}";
                if (_cacheService.Exists(cacheKey))
                    return _cacheService.Get<bool>(cacheKey);
                var exists = await _studentRepository.IsExistAsync(s => s.BranchId == branchId && s.AdmissionNo != null && s.AdmissionNo.Trim() == admissionNo.Trim());
                _cacheService.Set(cacheKey, exists);
                return exists;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking admission number exists");
                throw new Application.Exceptions.BaseServiceException("Error checking admission number", ex, Application.Common.ExceptionCodes.Database);
            }
        }

        public async Task<bool> CheckRollNumberExistsAsync(Guid classId, string rollNo)
        {
            try
            {
                _logger.LogInformation("CheckRollNumberExists: ClassId {ClassId}, RollNo {RollNo}", classId, rollNo);
                if (string.IsNullOrWhiteSpace(rollNo)) return false;
                var cacheKey = $"check:roll:{classId}:{rollNo.Trim()}";
                if (_cacheService.Exists(cacheKey))
                    return _cacheService.Get<bool>(cacheKey);
                var exists = await _studentRepository.IsExistAsync(s => s.ClassId == classId && s.RollNumber != null && s.RollNumber.Trim() == rollNo.Trim());
                _cacheService.Set(cacheKey, exists);
                return exists;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking roll number exists");
                throw new Application.Exceptions.BaseServiceException("Error checking roll number", ex, Application.Common.ExceptionCodes.Database);
            }
        }

        public async Task<bool> CheckAadhaarExistsAsync(string childAadhaar)
        {
            try
            {
                _logger.LogInformation("CheckAadhaarExists");
                if (string.IsNullOrWhiteSpace(childAadhaar)) return false;
                var normalized = childAadhaar.Trim().Replace(" ", "");
                var cacheKey = $"check:aadhaar:{normalized}";
                if (_cacheService.Exists(cacheKey))
                    return _cacheService.Get<bool>(cacheKey);
                var exists = await _studentRepository.IsExistAsync(s => s.ChildAadhaar != null && s.ChildAadhaar.Replace(" ", "") == normalized);
                _cacheService.Set(cacheKey, exists);
                return exists;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking Aadhaar exists");
                throw new Application.Exceptions.BaseServiceException("Error checking Aadhaar", ex, Application.Common.ExceptionCodes.Database);
            }
        }

        public async Task<ValidationResultResponse> ValidateAadhaarAsync(string childAadhaar)
        {
            try
            {
                _logger.LogInformation("ValidateAadhaar");
                if (string.IsNullOrWhiteSpace(childAadhaar))
                    return new ValidationResultResponse { IsValid = false, Message = "Aadhaar is required." };
                var digits = childAadhaar.Trim().Replace(" ", "");
                if (digits.Length != 12 || !digits.All(char.IsDigit))
                    return new ValidationResultResponse { IsValid = false, Message = "Aadhaar must be exactly 12 digits." };
                return new ValidationResultResponse { IsValid = true, Message = "Valid." };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating Aadhaar");
                throw new Application.Exceptions.BaseServiceException("Error validating Aadhaar", ex, Application.Common.ExceptionCodes.Database);
            }
        }

        public async Task<ValidationResultResponse> ValidateAgeEligibilityAsync(DateOnly dob, Guid? classId)
        {
            try
            {
                _logger.LogInformation("ValidateAgeEligibility: Dob {Dob}, ClassId {ClassId}", dob, classId);
                var today = DateOnly.FromDateTime(DateTime.UtcNow);
                var age = today.Year - dob.Year;
                if (today < dob.AddYears(age)) age--;
                const int minAge = 3;
                const int maxAge = 25;
                if (age < minAge)
                    return new ValidationResultResponse { IsValid = false, Message = $"Age must be at least {minAge} years." };
                if (age > maxAge)
                    return new ValidationResultResponse { IsValid = false, Message = $"Age must not exceed {maxAge} years." };
                return new ValidationResultResponse { IsValid = true, Message = "Eligible." };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating age eligibility");
                throw new Application.Exceptions.BaseServiceException("Error validating age eligibility", ex, Application.Common.ExceptionCodes.Database);
            }
        }

        public async Task<ValidationResultResponse> ValidateDuplicateStudentAsync(StudentRequest dto)
        {
            try
            {
                _logger.LogInformation("ValidateDuplicateStudent");
                if (dto.BranchId.HasValue && !string.IsNullOrWhiteSpace(dto.AdmissionNo))
                {
                    var exists = await _studentRepository.IsExistAsync(s => s.BranchId == dto.BranchId && s.AdmissionNo != null && s.AdmissionNo.Trim() == dto.AdmissionNo.Trim());
                    if (exists)
                        return new ValidationResultResponse { IsValid = false, Message = "A student with this admission number already exists in this branch." };
                }
                if (!string.IsNullOrWhiteSpace(dto.ChildAadhaar))
                {
                    var normalized = dto.ChildAadhaar.Trim().Replace(" ", "");
                    var exists = await _studentRepository.IsExistAsync(s => s.ChildAadhaar != null && s.ChildAadhaar.Replace(" ", "") == normalized);
                    if (exists)
                        return new ValidationResultResponse { IsValid = false, Message = "A student with this Aadhaar already exists." };
                }
                return new ValidationResultResponse { IsValid = true, Message = "No duplicate found." };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating duplicate student");
                throw new Application.Exceptions.BaseServiceException("Error validating duplicate student", ex, Application.Common.ExceptionCodes.Database);
            }
        }

        public async Task<ValidationResultResponse> ValidatePromotionEligibilityAsync(Guid studentId)
        {
            try
            {
                _logger.LogInformation("ValidatePromotionEligibility: {StudentId}", studentId);
                var student = await _studentRepository.GetByIdAsync(studentId);
                if (student == null)
                    return new ValidationResultResponse { IsValid = false, Message = "Student not found." };
                if (student.IsActive != true)
                    return new ValidationResultResponse { IsValid = false, Message = "Student is not active." };
                if (student.StudentStatus != null && student.StudentStatus != "ACTIVE" && !student.StudentStatus.Equals("ACTIVE", StringComparison.OrdinalIgnoreCase))
                    return new ValidationResultResponse { IsValid = false, Message = "Student status must be ACTIVE for promotion." };
                if (!student.ClassId.HasValue)
                    return new ValidationResultResponse { IsValid = false, Message = "Student has no current class." };
                return new ValidationResultResponse { IsValid = true, Message = "Eligible for promotion." };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating promotion eligibility");
                throw new Application.Exceptions.BaseServiceException("Error validating promotion eligibility", ex, Application.Common.ExceptionCodes.Database);
            }
        }

        public async Task<ValidationResultResponse> ValidateTransferEligibilityAsync(Guid studentId)
        {
            try
            {
                _logger.LogInformation("ValidateTransferEligibility: {StudentId}", studentId);
                var student = await _studentRepository.GetByIdAsync(studentId);
                if (student == null)
                    return new ValidationResultResponse { IsValid = false, Message = "Student not found." };
                if (student.IsActive != true)
                    return new ValidationResultResponse { IsValid = false, Message = "Student is not active." };
                if (student.IsTcIssued == true)
                    return new ValidationResultResponse { IsValid = false, Message = "TC already issued; transfer not applicable." };
                return new ValidationResultResponse { IsValid = true, Message = "Eligible for transfer." };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating transfer eligibility");
                throw new Application.Exceptions.BaseServiceException("Error validating transfer eligibility", ex, Application.Common.ExceptionCodes.Database);
            }
        }

        private void InvalidateStudentCaches(Guid id, Student student)
        {
            _cacheService.Remove("students:all");
            _cacheService.Remove("students:count:total");
            _cacheService.Remove($"student:{id}");
            if (student.DivisionId.HasValue) _cacheService.Remove($"students:division:{student.DivisionId.Value}");
            if (student.ClassId.HasValue) _cacheService.Remove($"students:class:{student.ClassId.Value}");
            if (student.BranchId.HasValue) _cacheService.Remove($"students:branch:{student.BranchId.Value}");
            if (student.AcademicYearId.HasValue) _cacheService.Remove($"students:academicyear:{student.AcademicYearId.Value}");
            _cacheService.Remove("students:active");
            _cacheService.Remove("students:inactive");
            _cacheService.Remove("students:dropped");
            _cacheService.Remove("students:transferred");
            _cacheService.Remove("students:passedout");
            if (student.StudentStatus != null && !string.IsNullOrWhiteSpace(student.StudentStatus))
                _cacheService.Remove($"students:status:{student.StudentStatus.Trim()}");
        }

        public async Task<IEnumerable<StudentResponse>> GetByDivisionIdAsync(Guid divisionId)
        {
            try
            {
                _logger.LogInformation("Fetching students by division {DivisionId}", divisionId);
                var cacheKey = $"students:division:{divisionId}";
                if (_cacheService.Exists(cacheKey))
                {
                    _logger.LogInformation("Returning students for division {DivisionId} from cache", divisionId);
                    return _cacheService.Get<IEnumerable<StudentResponse>>(cacheKey);
                }

                var students = await _studentRepository.FindAllAsync(s => s.DivisionId == divisionId);
                var result = students.Select(MapStudentToResponse).ToList();
                _cacheService.Set(cacheKey, result);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching students by division {DivisionId}", divisionId);
                throw new Application.Exceptions.BaseServiceException(
                    $"Error fetching students by division: {divisionId}",
                    ex,
                    Application.Common.ExceptionCodes.Database);
            }
        }

        public async Task<IEnumerable<StudentResponse>> GetByClassIdAsync(Guid classId)
        {
            try
            {
                _logger.LogInformation("Fetching students by class {ClassId}", classId);
                var cacheKey = $"students:class:{classId}";
                if (_cacheService.Exists(cacheKey))
                {
                    _logger.LogInformation("Returning students for class {ClassId} from cache", classId);
                    return _cacheService.Get<IEnumerable<StudentResponse>>(cacheKey);
                }

                var students = await _studentRepository.FindAllAsync(s => s.ClassId == classId);
                var result = students.Select(MapStudentToResponse).ToList();
                _cacheService.Set(cacheKey, result);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching students by class {ClassId}", classId);
                throw new Application.Exceptions.BaseServiceException(
                    $"Error fetching students by class: {classId}",
                    ex,
                    Application.Common.ExceptionCodes.Database);
            }
        }

        public async Task<IEnumerable<StudentResponse>> GetByAcademicYearIdAsync(Guid academicYearId)
        {
            try
            {
                _logger.LogInformation("Fetching students by academic year {AcademicYearId}", academicYearId);
                var cacheKey = $"students:academicyear:{academicYearId}";
                if (_cacheService.Exists(cacheKey))
                {
                    _logger.LogInformation("Returning students for academic year {AcademicYearId} from cache", academicYearId);
                    return _cacheService.Get<IEnumerable<StudentResponse>>(cacheKey);
                }

                var students = await _studentRepository.FindAllAsync(s => s.AcademicYearId == academicYearId);
                var result = students.Select(MapStudentToResponse).ToList();
                _cacheService.Set(cacheKey, result);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching students by academic year {AcademicYearId}", academicYearId);
                throw new Application.Exceptions.BaseServiceException(
                    $"Error fetching students by academic year: {academicYearId}",
                    ex,
                    Application.Common.ExceptionCodes.Database);
            }
        }

        public async Task<IEnumerable<StudentResponse>> GetByBranchIdAsync(Guid branchId)
        {
            try
            {
                _logger.LogInformation("Fetching students by branch {BranchId}", branchId);
                var cacheKey = $"students:branch:{branchId}";
                if (_cacheService.Exists(cacheKey))
                {
                    _logger.LogInformation("Returning students for branch {BranchId} from cache", branchId);
                    return _cacheService.Get<IEnumerable<StudentResponse>>(cacheKey);
                }

                var students = await _studentRepository.FindAllAsync(s => s.BranchId == branchId);
                var result = students.Select(MapStudentToResponse).ToList();
                _cacheService.Set(cacheKey, result);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching students by branch {BranchId}", branchId);
                throw new Application.Exceptions.BaseServiceException(
                    $"Error fetching students by branch: {branchId}",
                    ex,
                    Application.Common.ExceptionCodes.Database);
            }
        }

        public async Task<IEnumerable<StudentResponse>> GetByClassTeacherIdAsync(Guid staffId)
        {
            try
            {
                _logger.LogInformation("Fetching students by class teacher staffId {StaffId}", staffId);
                var cacheKey = $"students:classteacher:{staffId}";
                if (_cacheService.Exists(cacheKey))
                {
                    _logger.LogInformation("Returning students for class teacher {StaffId} from cache", staffId);
                    return _cacheService.Get<IEnumerable<StudentResponse>>(cacheKey);
                }

                var mappings = await _teacherSubjectMapRepository.FindAllAsync(m => m.StaffId == staffId);
                var classIds = mappings
                    .Where(m => m.ClassId.HasValue)
                    .Select(m => m.ClassId!.Value)
                    .Distinct()
                    .ToList();

                if (!classIds.Any())
                {
                    return Enumerable.Empty<StudentResponse>();
                }

                var students = await _studentRepository.FindAllAsync(s => s.ClassId.HasValue && classIds.Contains(s.ClassId.Value));
                var result = students.Select(MapStudentToResponse).ToList();
                _cacheService.Set(cacheKey, result);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching students by class teacher staffId {StaffId}", staffId);
                throw new Application.Exceptions.BaseServiceException(
                    $"Error fetching students by class teacher: {staffId}",
                    ex,
                    Application.Common.ExceptionCodes.Database);
            }
        }

        public async Task<StudentResponse?> GetByAadhaarAsync(string aadhaar)
        {
            if (string.IsNullOrWhiteSpace(aadhaar))
            {
                _logger.LogWarning("GetByAadhaarAsync called with null or empty aadhaar");
                return null;
            }

            try
            {
                var normalized = aadhaar.Trim().Replace(" ", "");
                _logger.LogInformation("Fetching student by Aadhaar (normalized)");
                var cacheKey = $"student:aadhaar:{normalized}";
                if (_cacheService.Exists(cacheKey))
                {
                    _logger.LogInformation("Returning student by Aadhaar from cache");
                    return _cacheService.Get<StudentResponse>(cacheKey);
                }

                var student = await _studentRepository.FindAsync(
                    s => s.ChildAadhaar != null && s.ChildAadhaar.Replace(" ", "") == normalized);
                if (student == null)
                {
                    _logger.LogWarning("Student not found for Aadhaar");
                    return null;
                }

                var response = MapStudentToResponse(student);
                _cacheService.Set(cacheKey, response);
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching student by Aadhaar");
                throw new Application.Exceptions.BaseServiceException(
                    "Error fetching student by Aadhaar",
                    ex,
                    Application.Common.ExceptionCodes.Database);
            }
        }

        public async Task<StudentResponse?> GetByAdmissionNumberAsync(Guid branchId, string admissionNo)
        {
            if (string.IsNullOrWhiteSpace(admissionNo))
            {
                _logger.LogWarning("GetByAdmissionNumberAsync: empty admissionNo");
                return null;
            }
            try
            {
                _logger.LogInformation("GetByAdmissionNumber: BranchId {BranchId}, AdmissionNo {AdmissionNo}", branchId, admissionNo);
                var key = admissionNo.Trim();
                var cacheKey = $"student:admission:{branchId}:{key}";
                if (_cacheService.Exists(cacheKey))
                {
                    _logger.LogInformation("Returning student by admission number from cache");
                    return _cacheService.Get<StudentResponse>(cacheKey);
                }
                var student = await _studentRepository.FindAsync(s => s.BranchId == branchId && s.AdmissionNo != null && s.AdmissionNo.Trim() == key);
                if (student == null) { _logger.LogWarning("Student not found for admission number"); return null; }
                var response = MapStudentToResponse(student);
                _cacheService.Set(cacheKey, response);
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching student by admission number");
                throw new Application.Exceptions.BaseServiceException("Error fetching student by admission number", ex, Application.Common.ExceptionCodes.Database);
            }
        }

        public async Task<StudentResponse?> GetByRollNumberAsync(Guid classId, string rollNo)
        {
            if (string.IsNullOrWhiteSpace(rollNo))
            {
                _logger.LogWarning("GetByRollNumberAsync: empty rollNo");
                return null;
            }
            try
            {
                _logger.LogInformation("GetByRollNumber: ClassId {ClassId}, RollNo {RollNo}", classId, rollNo);
                var key = rollNo.Trim();
                var cacheKey = $"student:roll:{classId}:{key}";
                if (_cacheService.Exists(cacheKey))
                {
                    _logger.LogInformation("Returning student by roll number from cache");
                    return _cacheService.Get<StudentResponse>(cacheKey);
                }
                var student = await _studentRepository.FindAsync(s => s.ClassId == classId && s.RollNumber != null && s.RollNumber.Trim() == key);
                if (student == null) { _logger.LogWarning("Student not found for roll number"); return null; }
                var response = MapStudentToResponse(student);
                _cacheService.Set(cacheKey, response);
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching student by roll number");
                throw new Application.Exceptions.BaseServiceException("Error fetching student by roll number", ex, Application.Common.ExceptionCodes.Database);
            }
        }

        public async Task<IEnumerable<StudentResponse>> GetByFatherPhoneAsync(string phone)
        {
            if (string.IsNullOrWhiteSpace(phone)) return Enumerable.Empty<StudentResponse>();
            try
            {
                _logger.LogInformation("GetByFatherPhone: {Phone}", phone);
                var normalized = phone.Trim().Replace(" ", "");
                var cacheKey = $"students:fatherphone:{normalized}";
                if (_cacheService.Exists(cacheKey))
                {
                    _logger.LogInformation("Returning students by father phone from cache");
                    return _cacheService.Get<IEnumerable<StudentResponse>>(cacheKey);
                }
                var students = await _studentRepository.FindAllAsync(s => s.FatherPhone != null && s.FatherPhone.Replace(" ", "") == normalized);
                var result = students.Select(MapStudentToResponse).ToList();
                _cacheService.Set(cacheKey, result);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching students by father phone");
                throw new Application.Exceptions.BaseServiceException("Error fetching students by father phone", ex, Application.Common.ExceptionCodes.Database);
            }
        }

        public async Task<IEnumerable<StudentResponse>> GetByMotherPhoneAsync(string phone)
        {
            if (string.IsNullOrWhiteSpace(phone)) return Enumerable.Empty<StudentResponse>();
            try
            {
                _logger.LogInformation("GetByMotherPhone: {Phone}", phone);
                var normalized = phone.Trim().Replace(" ", "");
                var cacheKey = $"students:motherphone:{normalized}";
                if (_cacheService.Exists(cacheKey))
                {
                    _logger.LogInformation("Returning students by mother phone from cache");
                    return _cacheService.Get<IEnumerable<StudentResponse>>(cacheKey);
                }
                var students = await _studentRepository.FindAllAsync(s => s.MotherPhone != null && s.MotherPhone.Replace(" ", "") == normalized);
                var result = students.Select(MapStudentToResponse).ToList();
                _cacheService.Set(cacheKey, result);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching students by mother phone");
                throw new Application.Exceptions.BaseServiceException("Error fetching students by mother phone", ex, Application.Common.ExceptionCodes.Database);
            }
        }

        public async Task<IEnumerable<StudentResponse>> GetByGuardianPhoneAsync(string phone)
        {
            if (string.IsNullOrWhiteSpace(phone)) return Enumerable.Empty<StudentResponse>();
            try
            {
                _logger.LogInformation("GetByGuardianPhone: {Phone}", phone);
                var normalized = phone.Trim().Replace(" ", "");
                var cacheKey = $"students:guardianphone:{normalized}";
                if (_cacheService.Exists(cacheKey))
                {
                    _logger.LogInformation("Returning students by guardian phone from cache");
                    return _cacheService.Get<IEnumerable<StudentResponse>>(cacheKey);
                }
                var students = await _studentRepository.FindAllAsync(s => s.GuardianPhone != null && s.GuardianPhone.Replace(" ", "") == normalized);
                var result = students.Select(MapStudentToResponse).ToList();
                _cacheService.Set(cacheKey, result);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching students by guardian phone");
                throw new Application.Exceptions.BaseServiceException("Error fetching students by guardian phone", ex, Application.Common.ExceptionCodes.Database);
            }
        }

        public async Task<StudentResponse?> GetByEmailAsync(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                _logger.LogWarning("GetByEmailAsync: empty email");
                return null;
            }
            try
            {
                _logger.LogInformation("GetByEmail: {Email}", email);
                var key = email.Trim();
                var cacheKey = $"student:email:{key}";
                if (_cacheService.Exists(cacheKey))
                {
                    _logger.LogInformation("Returning student by email from cache");
                    return _cacheService.Get<StudentResponse>(cacheKey);
                }
                var student = await _studentRepository.FindAsync(s => s.Email != null && s.Email.Trim() == key);
                if (student == null) { _logger.LogWarning("Student not found for email"); return null; }
                var response = MapStudentToResponse(student);
                _cacheService.Set(cacheKey, response);
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching student by email");
                throw new Application.Exceptions.BaseServiceException("Error fetching student by email", ex, Application.Common.ExceptionCodes.Database);
            }
        }

        public async Task<IEnumerable<StudentResponse>> GetByGenderAsync(string gender)
        {
            if (string.IsNullOrWhiteSpace(gender)) return Enumerable.Empty<StudentResponse>();
            try
            {
                _logger.LogInformation("GetByGender: {Gender}", gender);
                var key = gender.Trim();
                var cacheKey = $"students:gender:{key}";
                if (_cacheService.Exists(cacheKey))
                {
                    _logger.LogInformation("Returning students by gender from cache");
                    return _cacheService.Get<IEnumerable<StudentResponse>>(cacheKey);
                }
                var students = await _studentRepository.FindAllAsync(s => s.Gender != null && s.Gender.Trim() == key);
                var result = students.Select(MapStudentToResponse).ToList();
                _cacheService.Set(cacheKey, result);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching students by gender");
                throw new Application.Exceptions.BaseServiceException("Error fetching students by gender", ex, Application.Common.ExceptionCodes.Database);
            }
        }

        public async Task<IEnumerable<StudentResponse>> GetByCasteAsync(string caste)
        {
            if (string.IsNullOrWhiteSpace(caste)) return Enumerable.Empty<StudentResponse>();
            try
            {
                _logger.LogInformation("GetByCaste: {Caste}", caste);
                var key = caste.Trim();
                var cacheKey = $"students:caste:{key}";
                if (_cacheService.Exists(cacheKey))
                {
                    _logger.LogInformation("Returning students by caste from cache");
                    return _cacheService.Get<IEnumerable<StudentResponse>>(cacheKey);
                }
                var students = await _studentRepository.FindAllAsync(s => s.Caste != null && s.Caste.Trim() == key);
                var result = students.Select(MapStudentToResponse).ToList();
                _cacheService.Set(cacheKey, result);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching students by caste");
                throw new Application.Exceptions.BaseServiceException("Error fetching students by caste", ex, Application.Common.ExceptionCodes.Database);
            }
        }

        public async Task<IEnumerable<StudentResponse>> GetByCategoryAsync(string category)
        {
            if (string.IsNullOrWhiteSpace(category)) return Enumerable.Empty<StudentResponse>();
            try
            {
                _logger.LogInformation("GetByCategory: {Category}", category);
                var key = category.Trim();
                var cacheKey = $"students:category:{key}";
                if (_cacheService.Exists(cacheKey))
                {
                    _logger.LogInformation("Returning students by category from cache");
                    return _cacheService.Get<IEnumerable<StudentResponse>>(cacheKey);
                }
                var students = await _studentRepository.FindAllAsync(s => s.Category != null && s.Category.Trim() == key);
                var result = students.Select(MapStudentToResponse).ToList();
                _cacheService.Set(cacheKey, result);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching students by category");
                throw new Application.Exceptions.BaseServiceException("Error fetching students by category", ex, Application.Common.ExceptionCodes.Database);
            }
        }

        public async Task<IEnumerable<StudentResponse>> GetByReligionAsync(string religion)
        {
            if (string.IsNullOrWhiteSpace(religion)) return Enumerable.Empty<StudentResponse>();
            try
            {
                _logger.LogInformation("GetByReligion: {Religion}", religion);
                var key = religion.Trim();
                var cacheKey = $"students:religion:{key}";
                if (_cacheService.Exists(cacheKey))
                {
                    _logger.LogInformation("Returning students by religion from cache");
                    return _cacheService.Get<IEnumerable<StudentResponse>>(cacheKey);
                }
                var students = await _studentRepository.FindAllAsync(s => s.Religion != null && s.Religion.Trim() == key);
                var result = students.Select(MapStudentToResponse).ToList();
                _cacheService.Set(cacheKey, result);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching students by religion");
                throw new Application.Exceptions.BaseServiceException("Error fetching students by religion", ex, Application.Common.ExceptionCodes.Database);
            }
        }

        public async Task<IEnumerable<StudentResponse>> GetByNationalityAsync(string nationality)
        {
            if (string.IsNullOrWhiteSpace(nationality)) return Enumerable.Empty<StudentResponse>();
            try
            {
                _logger.LogInformation("GetByNationality: {Nationality}", nationality);
                var key = nationality.Trim();
                var cacheKey = $"students:nationality:{key}";
                if (_cacheService.Exists(cacheKey))
                {
                    _logger.LogInformation("Returning students by nationality from cache");
                    return _cacheService.Get<IEnumerable<StudentResponse>>(cacheKey);
                }
                var students = await _studentRepository.FindAllAsync(s => s.Nationality != null && s.Nationality.Trim() == key);
                var result = students.Select(MapStudentToResponse).ToList();
                _cacheService.Set(cacheKey, result);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching students by nationality");
                throw new Application.Exceptions.BaseServiceException("Error fetching students by nationality", ex, Application.Common.ExceptionCodes.Database);
            }
        }

        public async Task<IEnumerable<StudentResponse>> GetByBloodGroupAsync(string bloodGroup)
        {
            if (string.IsNullOrWhiteSpace(bloodGroup)) return Enumerable.Empty<StudentResponse>();
            try
            {
                _logger.LogInformation("GetByBloodGroup: {BloodGroup}", bloodGroup);
                var key = bloodGroup.Trim();
                var cacheKey = $"students:bloodgroup:{key}";
                if (_cacheService.Exists(cacheKey))
                {
                    _logger.LogInformation("Returning students by blood group from cache");
                    return _cacheService.Get<IEnumerable<StudentResponse>>(cacheKey);
                }
                var students = await _studentRepository.FindAllAsync(s => s.BloodGroup != null && s.BloodGroup.Trim() == key);
                var result = students.Select(MapStudentToResponse).ToList();
                _cacheService.Set(cacheKey, result);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching students by blood group");
                throw new Application.Exceptions.BaseServiceException("Error fetching students by blood group", ex, Application.Common.ExceptionCodes.Database);
            }
        }

        public async Task<IEnumerable<StudentResponse>> GetByDisabilityAsync(string disability)
        {
            if (string.IsNullOrWhiteSpace(disability)) return Enumerable.Empty<StudentResponse>();
            try
            {
                _logger.LogInformation("GetByDisability: {Disability}", disability);
                var key = disability.Trim();
                var cacheKey = $"students:disability:{key}";
                if (_cacheService.Exists(cacheKey))
                {
                    _logger.LogInformation("Returning students by disability from cache");
                    return _cacheService.Get<IEnumerable<StudentResponse>>(cacheKey);
                }
                var students = await _studentRepository.FindAllAsync(s => s.Disability != null && s.Disability.Trim() == key);
                var result = students.Select(MapStudentToResponse).ToList();
                _cacheService.Set(cacheKey, result);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching students by disability");
                throw new Application.Exceptions.BaseServiceException("Error fetching students by disability", ex, Application.Common.ExceptionCodes.Database);
            }
        }

        public async Task<IEnumerable<StudentResponse>> GetByCityAsync(string city)
        {
            if (string.IsNullOrWhiteSpace(city)) return Enumerable.Empty<StudentResponse>();
            try
            {
                _logger.LogInformation("GetByCity: {City}", city);
                var key = city.Trim();
                var cacheKey = $"students:city:{key}";
                if (_cacheService.Exists(cacheKey))
                {
                    _logger.LogInformation("Returning students by city from cache");
                    return _cacheService.Get<IEnumerable<StudentResponse>>(cacheKey);
                }
                var students = await _studentRepository.FindAllAsync(s => s.City != null && s.City.Trim() == key);
                var result = students.Select(MapStudentToResponse).ToList();
                _cacheService.Set(cacheKey, result);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching students by city");
                throw new Application.Exceptions.BaseServiceException("Error fetching students by city", ex, Application.Common.ExceptionCodes.Database);
            }
        }

        public async Task<IEnumerable<StudentResponse>> GetByStateAsync(string state)
        {
            if (string.IsNullOrWhiteSpace(state)) return Enumerable.Empty<StudentResponse>();
            try
            {
                _logger.LogInformation("GetByState: {State}", state);
                var key = state.Trim();
                var cacheKey = $"students:state:{key}";
                if (_cacheService.Exists(cacheKey))
                {
                    _logger.LogInformation("Returning students by state from cache");
                    return _cacheService.Get<IEnumerable<StudentResponse>>(cacheKey);
                }
                var students = await _studentRepository.FindAllAsync(s => s.State != null && s.State.Trim() == key);
                var result = students.Select(MapStudentToResponse).ToList();
                _cacheService.Set(cacheKey, result);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching students by state");
                throw new Application.Exceptions.BaseServiceException("Error fetching students by state", ex, Application.Common.ExceptionCodes.Database);
            }
        }

        public async Task<IEnumerable<StudentResponse>> GetByPincodeAsync(string pincode)
        {
            if (string.IsNullOrWhiteSpace(pincode)) return Enumerable.Empty<StudentResponse>();
            try
            {
                _logger.LogInformation("GetByPincode: {Pincode}", pincode);
                var key = pincode.Trim();
                var cacheKey = $"students:pincode:{key}";
                if (_cacheService.Exists(cacheKey))
                {
                    _logger.LogInformation("Returning students by pincode from cache");
                    return _cacheService.Get<IEnumerable<StudentResponse>>(cacheKey);
                }
                var students = await _studentRepository.FindAllAsync(s => s.Pincode != null && s.Pincode.Trim() == key);
                var result = students.Select(MapStudentToResponse).ToList();
                _cacheService.Set(cacheKey, result);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching students by pincode");
                throw new Application.Exceptions.BaseServiceException("Error fetching students by pincode", ex, Application.Common.ExceptionCodes.Database);
            }
        }

        public async Task<IEnumerable<StudentResponse>> GetByAddressKeywordAsync(string keyword)
        {
            if (string.IsNullOrWhiteSpace(keyword)) return Enumerable.Empty<StudentResponse>();
            try
            {
                _logger.LogInformation("GetByAddressKeyword: {Keyword}", keyword);
                var key = keyword.Trim();
                var cacheKey = $"students:addresskeyword:{key}";
                if (_cacheService.Exists(cacheKey))
                {
                    _logger.LogInformation("Returning students by address keyword from cache");
                    return _cacheService.Get<IEnumerable<StudentResponse>>(cacheKey);
                }
                var students = await _studentRepository.FindAllAsync(s =>
                    (s.AddressLine1 != null && s.AddressLine1.Contains(key)) ||
                    (s.AddressLine2 != null && s.AddressLine2.Contains(key)));
                var result = students.Select(MapStudentToResponse).ToList();
                _cacheService.Set(cacheKey, result);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching students by address keyword");
                throw new Application.Exceptions.BaseServiceException("Error fetching students by address keyword", ex, Application.Common.ExceptionCodes.Database);
            }
        }

        public async Task<IEnumerable<StudentResponse>> GetByPreviousSchoolAsync(string schoolName)
        {
            if (string.IsNullOrWhiteSpace(schoolName)) return Enumerable.Empty<StudentResponse>();
            try
            {
                _logger.LogInformation("GetByPreviousSchool: {SchoolName}", schoolName);
                var key = schoolName.Trim();
                var cacheKey = $"students:previousschool:{key}";
                if (_cacheService.Exists(cacheKey))
                {
                    _logger.LogInformation("Returning students by previous school from cache");
                    return _cacheService.Get<IEnumerable<StudentResponse>>(cacheKey);
                }
                var students = await _studentRepository.FindAllAsync(s => s.PreviousSchool != null && s.PreviousSchool.Trim() == key);
                var result = students.Select(MapStudentToResponse).ToList();
                _cacheService.Set(cacheKey, result);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching students by previous school");
                throw new Application.Exceptions.BaseServiceException("Error fetching students by previous school", ex, Application.Common.ExceptionCodes.Database);
            }
        }

        public async Task<IEnumerable<StudentResponse>> GetByPreviousBoardAsync(string boardName)
        {
            if (string.IsNullOrWhiteSpace(boardName)) return Enumerable.Empty<StudentResponse>();
            try
            {
                _logger.LogInformation("GetByPreviousBoard: {BoardName}", boardName);
                var key = boardName.Trim();
                var cacheKey = $"students:previousboard:{key}";
                if (_cacheService.Exists(cacheKey))
                {
                    _logger.LogInformation("Returning students by previous board from cache");
                    return _cacheService.Get<IEnumerable<StudentResponse>>(cacheKey);
                }
                var students = await _studentRepository.FindAllAsync(s => s.PreviousExamBord != null && s.PreviousExamBord.Trim() == key);
                var result = students.Select(MapStudentToResponse).ToList();
                _cacheService.Set(cacheKey, result);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching students by previous board");
                throw new Application.Exceptions.BaseServiceException("Error fetching students by previous board", ex, Application.Common.ExceptionCodes.Database);
            }
        }

        public async Task<IEnumerable<StudentResponse>> GetByFamilyIncomeRangeAsync(decimal min, decimal max)
        {
            if (min > max) return Enumerable.Empty<StudentResponse>();
            try
            {
                _logger.LogInformation("GetByFamilyIncomeRange: Min {Min}, Max {Max}", min, max);
                var cacheKey = $"students:familyincome:{min}:{max}";
                if (_cacheService.Exists(cacheKey))
                {
                    _logger.LogInformation("Returning students by family income range from cache");
                    return _cacheService.Get<IEnumerable<StudentResponse>>(cacheKey);
                }
                var students = await _studentRepository.FindAllAsync(s =>
                    s.FamilyIncome.HasValue && s.FamilyIncome.Value >= min && s.FamilyIncome.Value <= max);
                var result = students.Select(MapStudentToResponse).ToList();
                _cacheService.Set(cacheKey, result);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching students by family income range");
                throw new Application.Exceptions.BaseServiceException("Error fetching students by family income range", ex, Application.Common.ExceptionCodes.Database);
            }
        }

        public async Task<IEnumerable<StudentResponse>> GetWithOutstandingFeesAsync()
        {
            try
            {
                _logger.LogInformation("GetWithOutstandingFees: fetching students with outstanding fees");
                const string cacheKey = "students:outstandingfees";
                if (_cacheService.Exists(cacheKey))
                {
                    _logger.LogInformation("Returning students with outstanding fees from cache");
                    return _cacheService.Get<IEnumerable<StudentResponse>>(cacheKey);
                }
                var invoicesWithBalance = await _invoiceRepository.FindAllAsync(i =>
                    i.StudentId.HasValue && i.RemainingAmount.HasValue && i.RemainingAmount.Value > 0);
                var studentIds = invoicesWithBalance
                    .Where(i => i.StudentId != null)
                    .Select(i => i.StudentId!.Value)
                    .Distinct()
                    .ToList();
                if (studentIds.Count == 0)
                {
                    _cacheService.Set(cacheKey, new List<StudentResponse>());
                    return Enumerable.Empty<StudentResponse>();
                }
                var students = await _studentRepository.FindAllAsync(s => studentIds.Contains(s.Id));
                var result = students.Select(MapStudentToResponse).ToList();
                _cacheService.Set(cacheKey, result);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching students with outstanding fees");
                throw new Application.Exceptions.BaseServiceException("Error fetching students with outstanding fees", ex, Application.Common.ExceptionCodes.Database);
            }
        }

        public async Task<IEnumerable<StudentResponse>> GetWithScholarshipAsync()
        {
            try
            {
                _logger.LogInformation("GetWithScholarship: fetching students with scholarship");
                const string cacheKey = "students:scholarship";
                if (_cacheService.Exists(cacheKey))
                {
                    _logger.LogInformation("Returning students with scholarship from cache");
                    return _cacheService.Get<IEnumerable<StudentResponse>>(cacheKey);
                }
                // Scholarship is not stored on Student entity in current schema; return empty until column/table exists
                _logger.LogWarning("GetWithScholarship: no scholarship data on Student entity - returning empty list");
                var result = Enumerable.Empty<StudentResponse>().ToList();
                _cacheService.Set(cacheKey, result);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching students with scholarship");
                throw new Application.Exceptions.BaseServiceException("Error fetching students with scholarship", ex, Application.Common.ExceptionCodes.Database);
            }
        }

        public async Task<IEnumerable<StudentResponse>> GetWithMedicalConditionAsync()
        {
            try
            {
                _logger.LogInformation("GetWithMedicalCondition: fetching students with medical condition");
                const string cacheKey = "students:medicalcondition";
                if (_cacheService.Exists(cacheKey))
                {
                    _logger.LogInformation("Returning students with medical condition from cache");
                    return _cacheService.Get<IEnumerable<StudentResponse>>(cacheKey);
                }
                var students = await _studentRepository.FindAllAsync(s =>
                    s.MedicalConditions != null && !string.IsNullOrWhiteSpace(s.MedicalConditions));
                var result = students.Select(MapStudentToResponse).ToList();
                _cacheService.Set(cacheKey, result);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching students with medical condition");
                throw new Application.Exceptions.BaseServiceException("Error fetching students with medical condition", ex, Application.Common.ExceptionCodes.Database);
            }
        }

        public async Task<IEnumerable<StudentResponse>> GetWithAllergiesAsync()
        {
            try
            {
                _logger.LogInformation("GetWithAllergies: fetching students with allergies");
                const string cacheKey = "students:allergies";
                if (_cacheService.Exists(cacheKey))
                {
                    _logger.LogInformation("Returning students with allergies from cache");
                    return _cacheService.Get<IEnumerable<StudentResponse>>(cacheKey);
                }
                var students = await _studentRepository.FindAllAsync(s =>
                    s.Allergies != null && !string.IsNullOrWhiteSpace(s.Allergies));
                var result = students.Select(MapStudentToResponse).ToList();
                _cacheService.Set(cacheKey, result);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching students with allergies");
                throw new Application.Exceptions.BaseServiceException("Error fetching students with allergies", ex, Application.Common.ExceptionCodes.Database);
            }
        }

        public async Task<IEnumerable<StudentResponse>> GetByHeightRangeAsync(decimal min, decimal max)
        {
            if (min > max) return Enumerable.Empty<StudentResponse>();
            try
            {
                _logger.LogInformation("GetByHeightRange: Min {Min}, Max {Max} cm", min, max);
                var cacheKey = $"students:height:{min}:{max}";
                if (_cacheService.Exists(cacheKey))
                {
                    _logger.LogInformation("Returning students by height range from cache");
                    return _cacheService.Get<IEnumerable<StudentResponse>>(cacheKey);
                }
                var students = await _studentRepository.FindAllAsync(s =>
                    s.HeightCm.HasValue && s.HeightCm.Value >= min && s.HeightCm.Value <= max);
                var result = students.Select(MapStudentToResponse).ToList();
                _cacheService.Set(cacheKey, result);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching students by height range");
                throw new Application.Exceptions.BaseServiceException("Error fetching students by height range", ex, Application.Common.ExceptionCodes.Database);
            }
        }

        public async Task<IEnumerable<StudentResponse>> GetByWeightRangeAsync(decimal min, decimal max)
        {
            if (min > max) return Enumerable.Empty<StudentResponse>();
            try
            {
                _logger.LogInformation("GetByWeightRange: Min {Min}, Max {Max} kg", min, max);
                var cacheKey = $"students:weight:{min}:{max}";
                if (_cacheService.Exists(cacheKey))
                {
                    _logger.LogInformation("Returning students by weight range from cache");
                    return _cacheService.Get<IEnumerable<StudentResponse>>(cacheKey);
                }
                var students = await _studentRepository.FindAllAsync(s =>
                    s.WeightKg.HasValue && s.WeightKg.Value >= min && s.WeightKg.Value <= max);
                var result = students.Select(MapStudentToResponse).ToList();
                _cacheService.Set(cacheKey, result);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching students by weight range");
                throw new Application.Exceptions.BaseServiceException("Error fetching students by weight range", ex, Application.Common.ExceptionCodes.Database);
            }
        }

        public async Task<IEnumerable<StudentResponse>> GetByDobRangeAsync(DateOnly fromDate, DateOnly toDate)
        {
            if (fromDate > toDate) return Enumerable.Empty<StudentResponse>();
            try
            {
                _logger.LogInformation("GetByDobRange: From {FromDate}, To {ToDate}", fromDate, toDate);
                var cacheKey = $"students:dob:{fromDate:yyyy-MM-dd}:{toDate:yyyy-MM-dd}";
                if (_cacheService.Exists(cacheKey))
                {
                    _logger.LogInformation("Returning students by DOB range from cache");
                    return _cacheService.Get<IEnumerable<StudentResponse>>(cacheKey);
                }
                var students = await _studentRepository.FindAllAsync(s =>
                    s.Dob.HasValue && s.Dob.Value >= fromDate && s.Dob.Value <= toDate);
                var result = students.Select(MapStudentToResponse).ToList();
                _cacheService.Set(cacheKey, result);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching students by DOB range");
                throw new Application.Exceptions.BaseServiceException("Error fetching students by DOB range", ex, Application.Common.ExceptionCodes.Database);
            }
        }

        public async Task<IEnumerable<StudentResponse>> GetByAdmissionDateRangeAsync(DateOnly fromDate, DateOnly toDate)
        {
            if (fromDate > toDate) return Enumerable.Empty<StudentResponse>();
            try
            {
                _logger.LogInformation("GetByAdmissionDateRange: From {FromDate}, To {ToDate}", fromDate, toDate);
                var cacheKey = $"students:admissiondate:{fromDate:yyyy-MM-dd}:{toDate:yyyy-MM-dd}";
                if (_cacheService.Exists(cacheKey))
                {
                    _logger.LogInformation("Returning students by admission date range from cache");
                    return _cacheService.Get<IEnumerable<StudentResponse>>(cacheKey);
                }
                var students = await _studentRepository.FindAllAsync(s =>
                    s.AdmissionDate.HasValue && s.AdmissionDate.Value >= fromDate && s.AdmissionDate.Value <= toDate);
                var result = students.Select(MapStudentToResponse).ToList();
                _cacheService.Set(cacheKey, result);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching students by admission date range");
                throw new Application.Exceptions.BaseServiceException("Error fetching students by admission date range", ex, Application.Common.ExceptionCodes.Database);
            }
        }

        public async Task<IEnumerable<StudentResponse>> GetByLeavingDateRangeAsync(DateOnly fromDate, DateOnly toDate)
        {
            if (fromDate > toDate) return Enumerable.Empty<StudentResponse>();
            try
            {
                _logger.LogInformation("GetByLeavingDateRange: From {FromDate}, To {ToDate}", fromDate, toDate);
                var cacheKey = $"students:leavingdate:{fromDate:yyyy-MM-dd}:{toDate:yyyy-MM-dd}";
                if (_cacheService.Exists(cacheKey))
                {
                    _logger.LogInformation("Returning students by leaving date range from cache");
                    return _cacheService.Get<IEnumerable<StudentResponse>>(cacheKey);
                }
                var students = await _studentRepository.FindAllAsync(s =>
                    s.LeavingDate.HasValue && s.LeavingDate.Value >= fromDate && s.LeavingDate.Value <= toDate);
                var result = students.Select(MapStudentToResponse).ToList();
                _cacheService.Set(cacheKey, result);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching students by leaving date range");
                throw new Application.Exceptions.BaseServiceException("Error fetching students by leaving date range", ex, Application.Common.ExceptionCodes.Database);
            }
        }

        public async Task<IEnumerable<StudentResponse>> GetByStatusAsync(string status)
        {
            if (string.IsNullOrWhiteSpace(status)) return Enumerable.Empty<StudentResponse>();
            try
            {
                _logger.LogInformation("GetByStatus: {Status}", status);
                var key = status.Trim();
                var cacheKey = $"students:status:{key}";
                if (_cacheService.Exists(cacheKey))
                {
                    _logger.LogInformation("Returning students by status from cache");
                    return _cacheService.Get<IEnumerable<StudentResponse>>(cacheKey);
                }
                var students = await _studentRepository.FindAllAsync(s =>
                    s.StudentStatus != null && s.StudentStatus.Trim() == key);
                var result = students.Select(MapStudentToResponse).ToList();
                _cacheService.Set(cacheKey, result);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching students by status");
                throw new Application.Exceptions.BaseServiceException("Error fetching students by status", ex, Application.Common.ExceptionCodes.Database);
            }
        }

        public async Task<IEnumerable<StudentResponse>> GetActiveStudentsAsync()
        {
            try
            {
                _logger.LogInformation("GetActiveStudents: fetching active students");
                const string cacheKey = "students:active";
                if (_cacheService.Exists(cacheKey))
                {
                    _logger.LogInformation("Returning active students from cache");
                    return _cacheService.Get<IEnumerable<StudentResponse>>(cacheKey);
                }
                var students = await _studentRepository.FindAllAsync(s => s.IsActive == true);
                var result = students.Select(MapStudentToResponse).ToList();
                _cacheService.Set(cacheKey, result);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching active students");
                throw new Application.Exceptions.BaseServiceException("Error fetching active students", ex, Application.Common.ExceptionCodes.Database);
            }
        }

        public async Task<IEnumerable<StudentResponse>> GetInactiveStudentsAsync()
        {
            try
            {
                _logger.LogInformation("GetInactiveStudents: fetching inactive students");
                const string cacheKey = "students:inactive";
                if (_cacheService.Exists(cacheKey))
                {
                    _logger.LogInformation("Returning inactive students from cache");
                    return _cacheService.Get<IEnumerable<StudentResponse>>(cacheKey);
                }
                var students = await _studentRepository.FindAllAsync(s => s.IsActive == false);
                var result = students.Select(MapStudentToResponse).ToList();
                _cacheService.Set(cacheKey, result);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching inactive students");
                throw new Application.Exceptions.BaseServiceException("Error fetching inactive students", ex, Application.Common.ExceptionCodes.Database);
            }
        }

        public async Task<IEnumerable<StudentResponse>> GetDroppedStudentsAsync()
        {
            try
            {
                _logger.LogInformation("GetDroppedStudents: fetching dropped students");
                const string cacheKey = "students:dropped";
                if (_cacheService.Exists(cacheKey))
                {
                    _logger.LogInformation("Returning dropped students from cache");
                    return _cacheService.Get<IEnumerable<StudentResponse>>(cacheKey);
                }
                var students = await _studentRepository.FindAllAsync(s =>
                    s.StudentStatus != null && s.StudentStatus.Trim() == "DROPPED");
                var result = students.Select(MapStudentToResponse).ToList();
                _cacheService.Set(cacheKey, result);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching dropped students");
                throw new Application.Exceptions.BaseServiceException("Error fetching dropped students", ex, Application.Common.ExceptionCodes.Database);
            }
        }

        public async Task<IEnumerable<StudentResponse>> GetTransferredStudentsAsync()
        {
            try
            {
                _logger.LogInformation("GetTransferredStudents: fetching transferred students");
                const string cacheKey = "students:transferred";
                if (_cacheService.Exists(cacheKey))
                {
                    _logger.LogInformation("Returning transferred students from cache");
                    return _cacheService.Get<IEnumerable<StudentResponse>>(cacheKey);
                }
                var students = await _studentRepository.FindAllAsync(s =>
                    s.StudentStatus != null && s.StudentStatus.Trim() == "TRANSFERRED");
                var result = students.Select(MapStudentToResponse).ToList();
                _cacheService.Set(cacheKey, result);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching transferred students");
                throw new Application.Exceptions.BaseServiceException("Error fetching transferred students", ex, Application.Common.ExceptionCodes.Database);
            }
        }

        public async Task<IEnumerable<StudentResponse>> GetPassedOutStudentsAsync()
        {
            try
            {
                _logger.LogInformation("GetPassedOutStudents: fetching passed-out students");
                const string cacheKey = "students:passedout";
                if (_cacheService.Exists(cacheKey))
                {
                    _logger.LogInformation("Returning passed-out students from cache");
                    return _cacheService.Get<IEnumerable<StudentResponse>>(cacheKey);
                }
                var students = await _studentRepository.FindAllAsync(s =>
                    s.StudentStatus != null && s.StudentStatus.Trim() == "PASSED_OUT");
                var result = students.Select(MapStudentToResponse).ToList();
                _cacheService.Set(cacheKey, result);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching passed-out students");
                throw new Application.Exceptions.BaseServiceException("Error fetching passed-out students", ex, Application.Common.ExceptionCodes.Database);
            }
        }

        public async Task<IEnumerable<StudentResponse>> SearchStudentsAsync(StudentFilterDto filterDto)
        {
            if (filterDto == null) return Enumerable.Empty<StudentResponse>();
            try
            {
                _logger.LogInformation("SearchStudents with filter");
                var query = _studentRepository.Query();
                var key = BuildFilterCacheKey(filterDto);
                if (!string.IsNullOrEmpty(key) && _cacheService.Exists(key))
                {
                    _logger.LogInformation("Returning SearchStudents result from cache");
                    return _cacheService.Get<IEnumerable<StudentResponse>>(key);
                }

                if (filterDto.BranchId.HasValue)
                    query = query.Where(s => s.BranchId == filterDto.BranchId.Value);
                if (filterDto.ClassId.HasValue)
                    query = query.Where(s => s.ClassId == filterDto.ClassId.Value);
                if (filterDto.DivisionId.HasValue)
                    query = query.Where(s => s.DivisionId == filterDto.DivisionId.Value);
                if (filterDto.AcademicYearId.HasValue)
                    query = query.Where(s => s.AcademicYearId == filterDto.AcademicYearId.Value);
                if (!string.IsNullOrWhiteSpace(filterDto.StudentStatus))
                {
                    var status = filterDto.StudentStatus.Trim();
                    query = query.Where(s => s.StudentStatus != null && s.StudentStatus.Trim() == status);
                }
                if (!string.IsNullOrWhiteSpace(filterDto.Gender))
                {
                    var gender = filterDto.Gender.Trim();
                    query = query.Where(s => s.Gender != null && s.Gender.Trim() == gender);
                }
                if (!string.IsNullOrWhiteSpace(filterDto.City))
                {
                    var city = filterDto.City.Trim();
                    query = query.Where(s => s.City != null && s.City.Trim() == city);
                }
                if (!string.IsNullOrWhiteSpace(filterDto.State))
                {
                    var state = filterDto.State.Trim();
                    query = query.Where(s => s.State != null && s.State.Trim() == state);
                }
                if (!string.IsNullOrWhiteSpace(filterDto.Pincode))
                {
                    var pincode = filterDto.Pincode.Trim();
                    query = query.Where(s => s.Pincode != null && s.Pincode.Trim() == pincode);
                }
                if (!string.IsNullOrWhiteSpace(filterDto.Keyword))
                {
                    var kw = filterDto.Keyword.Trim();
                    query = query.Where(s =>
                        (s.FirstName != null && s.FirstName.Contains(kw)) ||
                        (s.MiddleName != null && s.MiddleName.Contains(kw)) ||
                        (s.LastName != null && s.LastName.Contains(kw)) ||
                        (s.AdmissionNo != null && s.AdmissionNo.Contains(kw)) ||
                        (s.Email != null && s.Email.Contains(kw)) ||
                        (s.Phone != null && s.Phone.Contains(kw)) ||
                        (s.AddressLine1 != null && s.AddressLine1.Contains(kw)) ||
                        (s.AddressLine2 != null && s.AddressLine2.Contains(kw)) ||
                        (s.RollNumber != null && s.RollNumber.Contains(kw)) ||
                        (s.EnrollmentNo != null && s.EnrollmentNo.Contains(kw)));
                }

                var students = await query.ToListAsync();
                var result = students.Select(MapStudentToResponse).ToList();
                if (!string.IsNullOrEmpty(key)) _cacheService.Set(key, result);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in SearchStudents");
                throw new Application.Exceptions.BaseServiceException("Error searching students", ex, Application.Common.ExceptionCodes.Database);
            }
        }

        public async Task<IEnumerable<StudentResponse>> AdvancedStudentSearchAsync(AdvancedStudentSearchDto multiCriteriaDto)
        {
            if (multiCriteriaDto == null) return Enumerable.Empty<StudentResponse>();
            try
            {
                _logger.LogInformation("AdvancedStudentSearch with multi-criteria");
                var query = _studentRepository.Query();
                var key = BuildAdvancedSearchCacheKey(multiCriteriaDto);
                if (!string.IsNullOrEmpty(key) && _cacheService.Exists(key))
                {
                    _logger.LogInformation("Returning AdvancedStudentSearch result from cache");
                    return _cacheService.Get<IEnumerable<StudentResponse>>(key);
                }

                if (multiCriteriaDto.BranchId.HasValue)
                    query = query.Where(s => s.BranchId == multiCriteriaDto.BranchId.Value);
                if (multiCriteriaDto.ClassId.HasValue)
                    query = query.Where(s => s.ClassId == multiCriteriaDto.ClassId.Value);
                if (multiCriteriaDto.DivisionId.HasValue)
                    query = query.Where(s => s.DivisionId == multiCriteriaDto.DivisionId.Value);
                if (multiCriteriaDto.AcademicYearId.HasValue)
                    query = query.Where(s => s.AcademicYearId == multiCriteriaDto.AcademicYearId.Value);
                if (!string.IsNullOrWhiteSpace(multiCriteriaDto.StudentStatus))
                {
                    var status = multiCriteriaDto.StudentStatus.Trim();
                    query = query.Where(s => s.StudentStatus != null && s.StudentStatus.Trim() == status);
                }
                if (!string.IsNullOrWhiteSpace(multiCriteriaDto.Gender))
                {
                    var gender = multiCriteriaDto.Gender.Trim();
                    query = query.Where(s => s.Gender != null && s.Gender.Trim() == gender);
                }
                if (!string.IsNullOrWhiteSpace(multiCriteriaDto.City))
                {
                    var city = multiCriteriaDto.City.Trim();
                    query = query.Where(s => s.City != null && s.City.Trim() == city);
                }
                if (!string.IsNullOrWhiteSpace(multiCriteriaDto.State))
                {
                    var state = multiCriteriaDto.State.Trim();
                    query = query.Where(s => s.State != null && s.State.Trim() == state);
                }
                if (!string.IsNullOrWhiteSpace(multiCriteriaDto.Pincode))
                {
                    var pincode = multiCriteriaDto.Pincode.Trim();
                    query = query.Where(s => s.Pincode != null && s.Pincode.Trim() == pincode);
                }
                if (multiCriteriaDto.DobFrom.HasValue)
                    query = query.Where(s => s.Dob.HasValue && s.Dob.Value >= multiCriteriaDto.DobFrom.Value);
                if (multiCriteriaDto.DobTo.HasValue)
                    query = query.Where(s => s.Dob.HasValue && s.Dob.Value <= multiCriteriaDto.DobTo.Value);
                if (multiCriteriaDto.AdmissionDateFrom.HasValue)
                    query = query.Where(s => s.AdmissionDate.HasValue && s.AdmissionDate.Value >= multiCriteriaDto.AdmissionDateFrom.Value);
                if (multiCriteriaDto.AdmissionDateTo.HasValue)
                    query = query.Where(s => s.AdmissionDate.HasValue && s.AdmissionDate.Value <= multiCriteriaDto.AdmissionDateTo.Value);
                if (multiCriteriaDto.LeavingDateFrom.HasValue)
                    query = query.Where(s => s.LeavingDate.HasValue && s.LeavingDate.Value >= multiCriteriaDto.LeavingDateFrom.Value);
                if (multiCriteriaDto.LeavingDateTo.HasValue)
                    query = query.Where(s => s.LeavingDate.HasValue && s.LeavingDate.Value <= multiCriteriaDto.LeavingDateTo.Value);
                if (multiCriteriaDto.HeightMin.HasValue)
                    query = query.Where(s => s.HeightCm.HasValue && s.HeightCm.Value >= multiCriteriaDto.HeightMin.Value);
                if (multiCriteriaDto.HeightMax.HasValue)
                    query = query.Where(s => s.HeightCm.HasValue && s.HeightCm.Value <= multiCriteriaDto.HeightMax.Value);
                if (multiCriteriaDto.WeightMin.HasValue)
                    query = query.Where(s => s.WeightKg.HasValue && s.WeightKg.Value >= multiCriteriaDto.WeightMin.Value);
                if (multiCriteriaDto.WeightMax.HasValue)
                    query = query.Where(s => s.WeightKg.HasValue && s.WeightKg.Value <= multiCriteriaDto.WeightMax.Value);
                if (multiCriteriaDto.FamilyIncomeMin.HasValue)
                    query = query.Where(s => s.FamilyIncome.HasValue && s.FamilyIncome.Value >= multiCriteriaDto.FamilyIncomeMin.Value);
                if (multiCriteriaDto.FamilyIncomeMax.HasValue)
                    query = query.Where(s => s.FamilyIncome.HasValue && s.FamilyIncome.Value <= multiCriteriaDto.FamilyIncomeMax.Value);
                if (!string.IsNullOrWhiteSpace(multiCriteriaDto.Religion))
                {
                    var r = multiCriteriaDto.Religion.Trim();
                    query = query.Where(s => s.Religion != null && s.Religion.Trim() == r);
                }
                if (!string.IsNullOrWhiteSpace(multiCriteriaDto.Caste))
                {
                    var c = multiCriteriaDto.Caste.Trim();
                    query = query.Where(s => s.Caste != null && s.Caste.Trim() == c);
                }
                if (!string.IsNullOrWhiteSpace(multiCriteriaDto.Category))
                {
                    var cat = multiCriteriaDto.Category.Trim();
                    query = query.Where(s => s.Category != null && s.Category.Trim() == cat);
                }
                if (!string.IsNullOrWhiteSpace(multiCriteriaDto.BloodGroup))
                {
                    var bg = multiCriteriaDto.BloodGroup.Trim();
                    query = query.Where(s => s.BloodGroup != null && s.BloodGroup.Trim() == bg);
                }
                if (!string.IsNullOrWhiteSpace(multiCriteriaDto.Disability))
                {
                    var d = multiCriteriaDto.Disability.Trim();
                    query = query.Where(s => s.Disability != null && s.Disability.Trim() == d);
                }
                if (!string.IsNullOrWhiteSpace(multiCriteriaDto.PreviousSchool))
                {
                    var ps = multiCriteriaDto.PreviousSchool.Trim();
                    query = query.Where(s => s.PreviousSchool != null && s.PreviousSchool.Trim() == ps);
                }
                if (!string.IsNullOrWhiteSpace(multiCriteriaDto.PreviousBoard))
                {
                    var pb = multiCriteriaDto.PreviousBoard.Trim();
                    query = query.Where(s => s.PreviousExamBord != null && s.PreviousExamBord.Trim() == pb);
                }
                if (!string.IsNullOrWhiteSpace(multiCriteriaDto.Keyword))
                {
                    var kw = multiCriteriaDto.Keyword.Trim();
                    query = query.Where(s =>
                        (s.FirstName != null && s.FirstName.Contains(kw)) ||
                        (s.MiddleName != null && s.MiddleName.Contains(kw)) ||
                        (s.LastName != null && s.LastName.Contains(kw)) ||
                        (s.AdmissionNo != null && s.AdmissionNo.Contains(kw)) ||
                        (s.Email != null && s.Email.Contains(kw)) ||
                        (s.Phone != null && s.Phone.Contains(kw)) ||
                        (s.AddressLine1 != null && s.AddressLine1.Contains(kw)) ||
                        (s.AddressLine2 != null && s.AddressLine2.Contains(kw)) ||
                        (s.RollNumber != null && s.RollNumber.Contains(kw)) ||
                        (s.EnrollmentNo != null && s.EnrollmentNo.Contains(kw)));
                }

                var students = await query.ToListAsync();
                var result = students.Select(MapStudentToResponse).ToList();
                if (!string.IsNullOrEmpty(key)) _cacheService.Set(key, result);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in AdvancedStudentSearch");
                throw new Application.Exceptions.BaseServiceException("Error in advanced student search", ex, Application.Common.ExceptionCodes.Database);
            }
        }

        public async Task<IEnumerable<StudentResponse>> GlobalStudentSearchAsync(string keyword)
        {
            if (string.IsNullOrWhiteSpace(keyword)) return Enumerable.Empty<StudentResponse>();
            try
            {
                var kw = keyword.Trim();
                _logger.LogInformation("GlobalStudentSearch: {Keyword}", kw);
                var cacheKey = $"students:globalsearch:{kw}";
                if (_cacheService.Exists(cacheKey))
                {
                    _logger.LogInformation("Returning GlobalStudentSearch result from cache");
                    return _cacheService.Get<IEnumerable<StudentResponse>>(cacheKey);
                }
                var students = await _studentRepository.FindAllAsync(s =>
                    (s.FirstName != null && s.FirstName.Contains(kw)) ||
                    (s.MiddleName != null && s.MiddleName.Contains(kw)) ||
                    (s.LastName != null && s.LastName.Contains(kw)) ||
                    (s.AdmissionNo != null && s.AdmissionNo.Contains(kw)) ||
                    (s.Email != null && s.Email.Contains(kw)) ||
                    (s.Phone != null && s.Phone.Contains(kw)) ||
                    (s.AddressLine1 != null && s.AddressLine1.Contains(kw)) ||
                    (s.AddressLine2 != null && s.AddressLine2.Contains(kw)) ||
                    (s.RollNumber != null && s.RollNumber.Contains(kw)) ||
                    (s.EnrollmentNo != null && s.EnrollmentNo.Contains(kw)) ||
                    (s.ChildAadhaar != null && s.ChildAadhaar.Contains(kw)) ||
                    (s.FatherName != null && s.FatherName.Contains(kw)) ||
                    (s.MotherName != null && s.MotherName.Contains(kw)) ||
                    (s.GuardianName != null && s.GuardianName.Contains(kw)));
                var result = students.Select(MapStudentToResponse).ToList();
                _cacheService.Set(cacheKey, result);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GlobalStudentSearch");
                throw new Application.Exceptions.BaseServiceException("Error in global student search", ex, Application.Common.ExceptionCodes.Database);
            }
        }

        public async Task<IEnumerable<StudentStrengthItemDto>> GetStudentStrengthByClassAsync(Guid branchId)
        {
            return await GetStrengthByBranchAsync(branchId, "class", s => s.ClassId);
        }

        public async Task<IEnumerable<StudentStrengthItemDto>> GetStudentStrengthByDivisionAsync(Guid branchId)
        {
            return await GetStrengthByBranchAsync(branchId, "division", s => s.DivisionId);
        }

        private async Task<IEnumerable<StudentStrengthItemDto>> GetStrengthByBranchAsync(Guid branchId, string suffix, System.Linq.Expressions.Expression<System.Func<Student, Guid?>> groupSelector)
        {
            try
            {
                _logger.LogInformation("GetStudentStrengthBy{Suffix}: BranchId {BranchId}", suffix, branchId);
                var cacheKey = $"students:strength:{suffix}:{branchId}";
                if (_cacheService.Exists(cacheKey))
                {
                    _logger.LogInformation("Returning strength by {Suffix} from cache", suffix);
                    return _cacheService.Get<IEnumerable<StudentStrengthItemDto>>(cacheKey);
                }
                var query = _studentRepository.Query(s => s.BranchId == branchId);
                var grouped = await query.GroupBy(groupSelector).Select(g => new StudentStrengthItemDto { Id = g.Key, Count = g.Count() }).ToListAsync();
                _cacheService.Set(cacheKey, grouped);
                return grouped;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting student strength by {Suffix} for branch {BranchId}", suffix, branchId);
                throw new Application.Exceptions.BaseServiceException($"Error getting student strength by {suffix}", ex, Application.Common.ExceptionCodes.Database);
            }
        }

        public async Task<IEnumerable<DistributionItemDto>> GetGenderDistributionAsync(Guid branchId)
        {
            return await GetDistributionByBranchAsync(branchId, "gender", s => s.Gender);
        }

        public async Task<IEnumerable<DistributionItemDto>> GetCategoryWiseDistributionAsync(Guid branchId)
        {
            return await GetDistributionByBranchAsync(branchId, "category", s => s.Category);
        }

        public async Task<IEnumerable<DistributionItemDto>> GetCasteWiseDistributionAsync(Guid branchId)
        {
            return await GetDistributionByBranchAsync(branchId, "caste", s => s.Caste);
        }

        public async Task<IEnumerable<DistributionItemDto>> GetReligionWiseDistributionAsync(Guid branchId)
        {
            return await GetDistributionByBranchAsync(branchId, "religion", s => s.Religion);
        }

        public async Task<IEnumerable<DistributionItemDto>> GetCityWiseDistributionAsync(Guid branchId)
        {
            return await GetDistributionByBranchAsync(branchId, "city", s => s.City);
        }

        public async Task<IEnumerable<DistributionItemDto>> GetStateWiseDistributionAsync(Guid branchId)
        {
            return await GetDistributionByBranchAsync(branchId, "state", s => s.State);
        }

        public async Task<IEnumerable<DistributionItemDto>> GetBloodGroupDistributionAsync(Guid branchId)
        {
            return await GetDistributionByBranchAsync(branchId, "bloodgroup", s => s.BloodGroup);
        }

        private async Task<IEnumerable<DistributionItemDto>> GetDistributionByBranchAsync(Guid branchId, string suffix, System.Linq.Expressions.Expression<System.Func<Student, string?>> groupSelector)
        {
            try
            {
                _logger.LogInformation("Get{Suffix}Distribution: BranchId {BranchId}", suffix, branchId);
                var cacheKey = $"students:dist:{suffix}:{branchId}";
                if (_cacheService.Exists(cacheKey))
                {
                    _logger.LogInformation("Returning {Suffix} distribution from cache", suffix);
                    return _cacheService.Get<IEnumerable<DistributionItemDto>>(cacheKey);
                }
                var query = _studentRepository.Query(s => s.BranchId == branchId);
                var grouped = await query.GroupBy(groupSelector).Select(g => new DistributionItemDto { Key = g.Key ?? "Not specified", Count = g.Count() }).ToListAsync();
                _cacheService.Set(cacheKey, grouped);
                return grouped;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting {Suffix} distribution for branch {BranchId}", suffix, branchId);
                throw new Application.Exceptions.BaseServiceException($"Error getting {suffix} distribution", ex, Application.Common.ExceptionCodes.Database);
            }
        }

        public async Task<IEnumerable<DistributionItemDto>> GetAgeWiseDistributionAsync(Guid branchId)
        {
            try
            {
                _logger.LogInformation("GetAgeWiseDistribution: BranchId {BranchId}", branchId);
                var cacheKey = $"students:dist:age:{branchId}";
                if (_cacheService.Exists(cacheKey))
                {
                    _logger.LogInformation("Returning age-wise distribution from cache");
                    return _cacheService.Get<IEnumerable<DistributionItemDto>>(cacheKey);
                }
                var students = await _studentRepository.FindAllAsync(s => s.BranchId == branchId && s.Dob.HasValue);
                var today = DateOnly.FromDateTime(DateTime.UtcNow);
                var buckets = new[] { "0-5", "6-10", "11-15", "16-18", "19+" };
                var grouped = students
                    .Select(s => new { Age = today.Year - s.Dob!.Value.Year - (today.DayOfYear < s.Dob.Value.DayOfYear ? 1 : 0) })
                    .GroupBy(x => x.Age switch { <= 5 => "0-5", <= 10 => "6-10", <= 15 => "11-15", <= 18 => "16-18", _ => "19+" })
                    .Select(g => new DistributionItemDto { Key = g.Key, Count = g.Count() })
                    .OrderBy(d => Array.IndexOf(buckets, d.Key))
                    .ToList();
                _cacheService.Set(cacheKey, grouped);
                return grouped;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting age-wise distribution for branch {BranchId}", branchId);
                throw new Application.Exceptions.BaseServiceException("Error getting age-wise distribution", ex, Application.Common.ExceptionCodes.Database);
            }
        }

        public async Task<IEnumerable<DistributionItemDto>> GetIncomeDistributionAsync(Guid branchId)
        {
            try
            {
                _logger.LogInformation("GetIncomeDistribution: BranchId {BranchId}", branchId);
                var cacheKey = $"students:dist:income:{branchId}";
                if (_cacheService.Exists(cacheKey))
                {
                    _logger.LogInformation("Returning income distribution from cache");
                    return _cacheService.Get<IEnumerable<DistributionItemDto>>(cacheKey);
                }
                var students = await _studentRepository.FindAllAsync(s => s.BranchId == branchId);
                var buckets = new[] { "0-50K", "50K-1L", "1L-2L", "2L-5L", "5L+" };
                var grouped = students
                    .Select(s => s.FamilyIncome)
                    .GroupBy(inc => inc == null ? "Not specified" : inc.Value switch { <= 50000 => "0-50K", <= 100000 => "50K-1L", <= 200000 => "1L-2L", <= 500000 => "2L-5L", _ => "5L+" })
                    .Select(g => new DistributionItemDto { Key = g.Key, Count = g.Count() })
                    .OrderBy(d => d.Key == "Not specified" ? -1 : Array.IndexOf(buckets, d.Key))
                    .ToList();
                _cacheService.Set(cacheKey, grouped);
                return grouped;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting income distribution for branch {BranchId}", branchId);
                throw new Application.Exceptions.BaseServiceException("Error getting income distribution", ex, Application.Common.ExceptionCodes.Database);
            }
        }

        public async Task<IEnumerable<StudentStrengthItemDto>> GetStudentGrowthTrendAsync(Guid branchId)
        {
            try
            {
                _logger.LogInformation("GetStudentGrowthTrend: BranchId {BranchId}", branchId);
                var cacheKey = $"students:trend:growth:{branchId}";
                if (_cacheService.Exists(cacheKey))
                {
                    _logger.LogInformation("Returning student growth trend from cache");
                    return _cacheService.Get<IEnumerable<StudentStrengthItemDto>>(cacheKey);
                }
                var query = _studentRepository.Query(s => s.BranchId == branchId);
                var grouped = await query.GroupBy(s => s.AcademicYearId).Select(g => new StudentStrengthItemDto { Id = g.Key, Count = g.Count() }).ToListAsync();
                _cacheService.Set(cacheKey, grouped);
                return grouped;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting student growth trend for branch {BranchId}", branchId);
                throw new Application.Exceptions.BaseServiceException("Error getting student growth trend", ex, Application.Common.ExceptionCodes.Database);
            }
        }

        public async Task<IEnumerable<YearCountDto>> GetYearWiseAdmissionTrendAsync(Guid branchId)
        {
            try
            {
                _logger.LogInformation("GetYearWiseAdmissionTrend: BranchId {BranchId}", branchId);
                var cacheKey = $"students:trend:admission:{branchId}";
                if (_cacheService.Exists(cacheKey))
                {
                    _logger.LogInformation("Returning year-wise admission trend from cache");
                    return _cacheService.Get<IEnumerable<YearCountDto>>(cacheKey);
                }
                var query = _studentRepository.Query(s => s.BranchId == branchId && s.AdmissionDate.HasValue);
                var grouped = await query.GroupBy(s => s.AdmissionDate!.Value.Year).Select(g => new YearCountDto { Year = g.Key, Count = g.Count() }).OrderBy(x => x.Year).ToListAsync();
                _cacheService.Set(cacheKey, grouped);
                return grouped;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting year-wise admission trend for branch {BranchId}", branchId);
                throw new Application.Exceptions.BaseServiceException("Error getting year-wise admission trend", ex, Application.Common.ExceptionCodes.Database);
            }
        }

        public async Task<RateAnalysisDto> DropoutRateAnalysisAsync(Guid branchId)
        {
            try
            {
                _logger.LogInformation("DropoutRateAnalysis: BranchId {BranchId}", branchId);
                var cacheKey = $"students:rate:dropout:{branchId}";
                if (_cacheService.Exists(cacheKey))
                {
                    _logger.LogInformation("Returning dropout rate analysis from cache");
                    return _cacheService.Get<RateAnalysisDto>(cacheKey);
                }
                var all = await _studentRepository.FindAllAsync(s => s.BranchId == branchId);
                var total = all.Count;
                var dropped = all.Count(s => s.StudentStatus != null && s.StudentStatus.Trim().Equals("DROPPED", StringComparison.OrdinalIgnoreCase));
                var rate = total > 0 ? (decimal)dropped / total * 100 : 0;
                var result = new RateAnalysisDto { Rate = Math.Round(rate, 2), TotalCount = total, Count = dropped, Label = "Dropout" };
                _cacheService.Set(cacheKey, result);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in dropout rate analysis for branch {BranchId}", branchId);
                throw new Application.Exceptions.BaseServiceException("Error in dropout rate analysis", ex, Application.Common.ExceptionCodes.Database);
            }
        }

        public async Task<RateAnalysisDto> PromotionSuccessRateAsync(Guid academicYearId)
        {
            try
            {
                _logger.LogInformation("PromotionSuccessRate: AcademicYearId {AcademicYearId}", academicYearId);
                var cacheKey = $"students:rate:promotion:{academicYearId}";
                if (_cacheService.Exists(cacheKey))
                {
                    _logger.LogInformation("Returning promotion success rate from cache");
                    return _cacheService.Get<RateAnalysisDto>(cacheKey);
                }
                var all = await _studentRepository.FindAllAsync(s => s.AcademicYearId == academicYearId);
                var total = all.Count;
                var promoted = all.Count(s => s.PromotedToClassId.HasValue);
                var rate = total > 0 ? (decimal)promoted / total * 100 : 0;
                var result = new RateAnalysisDto { Rate = Math.Round(rate, 2), TotalCount = total, Count = promoted, Label = "Promotion" };
                _cacheService.Set(cacheKey, result);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in promotion success rate for academic year {AcademicYearId}", academicYearId);
                throw new Application.Exceptions.BaseServiceException("Error in promotion success rate", ex, Application.Common.ExceptionCodes.Database);
            }
        }

        public async Task<RateAnalysisDto> StudentRetentionRateAsync()
        {
            try
            {
                _logger.LogInformation("StudentRetentionRate");
                const string cacheKey = "students:rate:retention";
                if (_cacheService.Exists(cacheKey))
                {
                    _logger.LogInformation("Returning student retention rate from cache");
                    return _cacheService.Get<RateAnalysisDto>(cacheKey);
                }
                var all = await _studentRepository.GetAllAsync();
                var total = all.Count();
                var active = all.Count(s => s.IsActive == true);
                var rate = total > 0 ? (decimal)active / total * 100 : 0;
                var result = new RateAnalysisDto { Rate = Math.Round(rate, 2), TotalCount = total, Count = active, Label = "Retention" };
                _cacheService.Set(cacheKey, result);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in student retention rate");
                throw new Application.Exceptions.BaseServiceException("Error in student retention rate", ex, Application.Common.ExceptionCodes.Database);
            }
        }

        public async Task<RateAnalysisDto> TransferRateAnalysisAsync(Guid branchId)
        {
            try
            {
                _logger.LogInformation("TransferRateAnalysis: BranchId {BranchId}", branchId);
                var cacheKey = $"students:rate:transfer:{branchId}";
                if (_cacheService.Exists(cacheKey))
                {
                    _logger.LogInformation("Returning transfer rate analysis from cache");
                    return _cacheService.Get<RateAnalysisDto>(cacheKey);
                }
                var all = await _studentRepository.FindAllAsync(s => s.BranchId == branchId);
                var total = all.Count;
                var transferred = all.Count(s => s.StudentStatus != null && s.StudentStatus.Trim().Equals("TRANSFERRED", StringComparison.OrdinalIgnoreCase));
                var rate = total > 0 ? (decimal)transferred / total * 100 : 0;
                var result = new RateAnalysisDto { Rate = Math.Round(rate, 2), TotalCount = total, Count = transferred, Label = "Transfer" };
                _cacheService.Set(cacheKey, result);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in transfer rate analysis for branch {BranchId}", branchId);
                throw new Application.Exceptions.BaseServiceException("Error in transfer rate analysis", ex, Application.Common.ExceptionCodes.Database);
            }
        }

        public async Task<IEnumerable<GenderRatioTrendPointDto>> GetGenderRatioTrendAsync(Guid branchId)
        {
            try
            {
                _logger.LogInformation("GetGenderRatioTrend: BranchId {BranchId}", branchId);
                var cacheKey = $"students:trend:gender:{branchId}";
                if (_cacheService.Exists(cacheKey))
                {
                    _logger.LogInformation("Returning gender ratio trend from cache");
                    return _cacheService.Get<IEnumerable<GenderRatioTrendPointDto>>(cacheKey);
                }
                var students = await _studentRepository.FindAllAsync(s => s.BranchId == branchId && s.AdmissionDate.HasValue);
                var result = students
                    .GroupBy(s => s.AdmissionDate!.Value.Year)
                    .Select(g => new GenderRatioTrendPointDto
                    {
                        Year = g.Key,
                        MaleCount = g.Count(s => string.Equals(s.Gender?.Trim(), "MALE", StringComparison.OrdinalIgnoreCase)),
                        FemaleCount = g.Count(s => string.Equals(s.Gender?.Trim(), "FEMALE", StringComparison.OrdinalIgnoreCase)),
                        OtherCount = g.Count(s => s.Gender == null || string.IsNullOrWhiteSpace(s.Gender) || (!string.Equals(s.Gender.Trim(), "MALE", StringComparison.OrdinalIgnoreCase) && !string.Equals(s.Gender.Trim(), "FEMALE", StringComparison.OrdinalIgnoreCase)))
                    })
                    .OrderBy(x => x.Year).ToList();
                _cacheService.Set(cacheKey, result);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting gender ratio trend for branch {BranchId}", branchId);
                throw new Application.Exceptions.BaseServiceException("Error getting gender ratio trend", ex, Application.Common.ExceptionCodes.Database);
            }
        }

        public async Task<IEnumerable<CasteTrendPointDto>> GetCasteTrendAnalysisAsync(Guid branchId)
        {
            try
            {
                _logger.LogInformation("GetCasteTrendAnalysis: BranchId {BranchId}", branchId);
                var cacheKey = $"students:trend:caste:{branchId}";
                if (_cacheService.Exists(cacheKey))
                {
                    _logger.LogInformation("Returning caste trend analysis from cache");
                    return _cacheService.Get<IEnumerable<CasteTrendPointDto>>(cacheKey);
                }
                var students = await _studentRepository.FindAllAsync(s => s.BranchId == branchId && s.AdmissionDate.HasValue);
                var byYearCaste = students
                    .GroupBy(s => new { Year = s.AdmissionDate!.Value.Year, Caste = s.Caste?.Trim() ?? "Not specified" })
                    .Select(g => new CasteTrendPointDto { Year = g.Key.Year, Caste = g.Key.Caste, Count = g.Count() })
                    .OrderBy(x => x.Year).ThenBy(x => x.Caste).ToList();
                _cacheService.Set(cacheKey, byYearCaste);
                return byYearCaste;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting caste trend analysis for branch {BranchId}", branchId);
                throw new Application.Exceptions.BaseServiceException("Error getting caste trend analysis", ex, Application.Common.ExceptionCodes.Database);
            }
        }

        public async Task<IEnumerable<DistributionItemDto>> UrbanRuralDistributionAnalysisAsync(Guid branchId)
        {
            try
            {
                _logger.LogInformation("UrbanRuralDistributionAnalysis: BranchId {BranchId}", branchId);
                var cacheKey = $"students:dist:urbanrural:{branchId}";
                if (_cacheService.Exists(cacheKey))
                {
                    _logger.LogInformation("Returning urban/rural distribution from cache");
                    return _cacheService.Get<IEnumerable<DistributionItemDto>>(cacheKey);
                }
                var students = await _studentRepository.FindAllAsync(s => s.BranchId == branchId);
                var urban = students.Count(s => !string.IsNullOrWhiteSpace(s.City));
                var rural = students.Count - urban;
                var result = new List<DistributionItemDto> { new DistributionItemDto { Key = "Urban", Count = urban }, new DistributionItemDto { Key = "Rural", Count = rural } };
                _cacheService.Set(cacheKey, result);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in urban/rural distribution for branch {BranchId}", branchId);
                throw new Application.Exceptions.BaseServiceException("Error in urban/rural distribution analysis", ex, Application.Common.ExceptionCodes.Database);
            }
        }

        public async Task<RateAnalysisDto> AdmissionConversionRateAsync()
        {
            try
            {
                _logger.LogInformation("AdmissionConversionRate");
                const string cacheKey = "students:rate:conversion";
                if (_cacheService.Exists(cacheKey))
                {
                    _logger.LogInformation("Returning admission conversion rate from cache");
                    return _cacheService.Get<RateAnalysisDto>(cacheKey);
                }
                var all = await _studentRepository.GetAllAsync();
                var withAdmission = all.Where(s => s.AdmissionDate.HasValue).ToList();
                var thisYear = DateTime.UtcNow.Year;
                var thisYearCount = withAdmission.Count(s => s.AdmissionDate!.Value.Year == thisYear);
                var lastYearCount = withAdmission.Count(s => s.AdmissionDate!.Value.Year == thisYear - 1);
                decimal rate = 0;
                if (lastYearCount > 0) rate = Math.Round((decimal)thisYearCount / lastYearCount * 100, 2);
                var result = new RateAnalysisDto
                {
                    Rate = rate,
                    TotalCount = lastYearCount + thisYearCount,
                    Count = thisYearCount,
                    Label = "Admission conversion (this year vs last year)",
                    Message = lastYearCount == 0 ? "No prior year admissions to compare." : null
                };
                _cacheService.Set(cacheKey, result);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in admission conversion rate");
                throw new Application.Exceptions.BaseServiceException("Error in admission conversion rate", ex, Application.Common.ExceptionCodes.Database);
            }
        }

        public async Task<StudentReportDto> GenerateUDISEReportAsync()
        {
            try
            {
                _logger.LogInformation("GenerateUDISEReport");
                const string cacheKey = "students:report:udise";
                if (_cacheService.Exists(cacheKey))
                {
                    _logger.LogInformation("Returning UDISE report from cache");
                    return _cacheService.Get<StudentReportDto>(cacheKey);
                }
                var students = await _studentRepository.GetAllAsync();
                var list = students.ToList();
                var byGender = list.GroupBy(s => s.Gender?.Trim() ?? "Not specified").Select(g => new DistributionItemDto { Key = g.Key, Count = g.Count() }).ToList();
                var byCategory = list.GroupBy(s => s.Category?.Trim() ?? "Not specified").Select(g => new DistributionItemDto { Key = g.Key, Count = g.Count() }).ToList();
                var byClass = list.Where(s => s.ClassId.HasValue).GroupBy(s => s.ClassId!.Value).Select(g => new DistributionItemDto { Key = g.Key.ToString(), Count = g.Count() }).ToList();
                var report = new StudentReportDto
                {
                    ReportTitle = "UDISE Report",
                    GeneratedOn = DateTime.UtcNow,
                    TotalCount = list.Count,
                    Description = "Unified District Information System for Education - student summary.",
                    Sections = new List<ReportSectionDto>
                    {
                        new ReportSectionDto { Title = "By Gender", Items = byGender },
                        new ReportSectionDto { Title = "By Category", Items = byCategory },
                        new ReportSectionDto { Title = "By Class", Items = byClass }
                    }
                };
                _cacheService.Set(cacheKey, report);
                return report;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating UDISE report");
                throw new Application.Exceptions.BaseServiceException("Error generating UDISE report", ex, Application.Common.ExceptionCodes.Database);
            }
        }

        public async Task<StudentReportDto> GenerateMinorityReportAsync()
        {
            try
            {
                _logger.LogInformation("GenerateMinorityReport");
                const string cacheKey = "students:report:minority";
                if (_cacheService.Exists(cacheKey))
                {
                    _logger.LogInformation("Returning minority report from cache");
                    return _cacheService.Get<StudentReportDto>(cacheKey);
                }
                var minorityReligions = new[] { "MUSLIM", "CHRISTIAN", "SIKH", "BUDDHIST", "JAIN", "PARSI", "OTHER" };
                var students = await _studentRepository.GetAllAsync();
                var list = students.Where(s => s.Religion != null && minorityReligions.Contains(s.Religion.Trim().ToUpperInvariant())).ToList();
                var byReligion = list.GroupBy(s => s.Religion?.Trim() ?? "Not specified").Select(g => new DistributionItemDto { Key = g.Key!, Count = g.Count() }).ToList();
                var report = new StudentReportDto
                {
                    ReportTitle = "Minority Students Report",
                    GeneratedOn = DateTime.UtcNow,
                    TotalCount = list.Count,
                    Description = "Students belonging to minority religions (Muslim, Christian, Sikh, Buddhist, Jain, Parsi, Other).",
                    Sections = new List<ReportSectionDto> { new ReportSectionDto { Title = "By Religion", Items = byReligion } },
                    Students = list.Select(MapStudentToResponse).ToList()
                };
                _cacheService.Set(cacheKey, report);
                return report;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating minority report");
                throw new Application.Exceptions.BaseServiceException("Error generating minority report", ex, Application.Common.ExceptionCodes.Database);
            }
        }

        public async Task<StudentReportDto> GenerateSCSTOBCReportAsync()
        {
            try
            {
                _logger.LogInformation("GenerateSCSTOBCReport");
                const string cacheKey = "students:report:scstobc";
                if (_cacheService.Exists(cacheKey))
                {
                    _logger.LogInformation("Returning SC/ST/OBC report from cache");
                    return _cacheService.Get<StudentReportDto>(cacheKey);
                }
                var students = await _studentRepository.GetAllAsync();
                var list = students.Where(s => s.Category != null && new[] { "SC", "ST", "OBC" }.Contains(s.Category.Trim().ToUpperInvariant())).ToList();
                var byCategory = list.GroupBy(s => s.Category?.Trim() ?? "Not specified").Select(g => new DistributionItemDto { Key = g.Key!, Count = g.Count() }).ToList();
                var report = new StudentReportDto
                {
                    ReportTitle = "SC/ST/OBC Students Report",
                    GeneratedOn = DateTime.UtcNow,
                    TotalCount = list.Count,
                    Description = "Scheduled Caste, Scheduled Tribe and Other Backward Class students.",
                    Sections = new List<ReportSectionDto> { new ReportSectionDto { Title = "By Category", Items = byCategory } },
                    Students = list.Select(MapStudentToResponse).ToList()
                };
                _cacheService.Set(cacheKey, report);
                return report;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating SC/ST/OBC report");
                throw new Application.Exceptions.BaseServiceException("Error generating SC/ST/OBC report", ex, Application.Common.ExceptionCodes.Database);
            }
        }

        public async Task<StudentReportDto> GenerateAadhaarLinkedStudentsReportAsync()
        {
            try
            {
                _logger.LogInformation("GenerateAadhaarLinkedStudentsReport");
                const string cacheKey = "students:report:aadhaarlinked";
                if (_cacheService.Exists(cacheKey))
                {
                    _logger.LogInformation("Returning Aadhaar linked report from cache");
                    return _cacheService.Get<StudentReportDto>(cacheKey);
                }
                var students = await _studentRepository.GetAllAsync();
                var list = students.Where(s => !string.IsNullOrWhiteSpace(s.ChildAadhaar)).ToList();
                var verified = list.Count(s => s.IsAadhaarVerified == true);
                var notVerified = list.Count - verified;
                var report = new StudentReportDto
                {
                    ReportTitle = "Aadhaar Linked Students Report",
                    GeneratedOn = DateTime.UtcNow,
                    TotalCount = list.Count,
                    Description = "Students with Child Aadhaar linked; breakdown by verification status.",
                    Sections = new List<ReportSectionDto>
                    {
                        new ReportSectionDto { Title = "Verification Status", Items = new List<DistributionItemDto> { new DistributionItemDto { Key = "Verified", Count = verified }, new DistributionItemDto { Key = "Not Verified", Count = notVerified } } }
                    },
                    Students = list.Select(MapStudentToResponse).ToList()
                };
                _cacheService.Set(cacheKey, report);
                return report;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating Aadhaar linked report");
                throw new Application.Exceptions.BaseServiceException("Error generating Aadhaar linked report", ex, Application.Common.ExceptionCodes.Database);
            }
        }

        public async Task<StudentReportDto> GenerateScholarshipEligibleStudentsReportAsync()
        {
            try
            {
                _logger.LogInformation("GenerateScholarshipEligibleStudentsReport");
                const string cacheKey = "students:report:scholarshipeligible";
                if (_cacheService.Exists(cacheKey))
                {
                    _logger.LogInformation("Returning scholarship eligible report from cache");
                    return _cacheService.Get<StudentReportDto>(cacheKey);
                }
                const decimal incomeThreshold = 200000;
                var students = await _studentRepository.GetAllAsync();
                var list = students.Where(s =>
                    (s.FamilyIncome.HasValue && s.FamilyIncome.Value <= incomeThreshold) ||
                    (s.Category != null && new[] { "SC", "ST", "OBC" }.Contains(s.Category.Trim().ToUpperInvariant()))).ToList();
                var byEligibility = new List<DistributionItemDto>();
                byEligibility.Add(new DistributionItemDto { Key = "Family income ≤ 2L", Count = students.Count(s => s.FamilyIncome.HasValue && s.FamilyIncome.Value <= incomeThreshold) });
                byEligibility.Add(new DistributionItemDto { Key = "SC/ST/OBC", Count = students.Count(s => s.Category != null && new[] { "SC", "ST", "OBC" }.Contains(s.Category.Trim().ToUpperInvariant())) });
                var report = new StudentReportDto
                {
                    ReportTitle = "Scholarship Eligible Students Report",
                    GeneratedOn = DateTime.UtcNow,
                    TotalCount = list.Count,
                    Description = "Students eligible by family income (≤2L) or SC/ST/OBC category. Eligibility criteria may vary by scheme.",
                    Sections = new List<ReportSectionDto> { new ReportSectionDto { Title = "Eligibility Criteria", Items = byEligibility } },
                    Students = list.Select(MapStudentToResponse).ToList()
                };
                _cacheService.Set(cacheKey, report);
                return report;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating scholarship eligible report");
                throw new Application.Exceptions.BaseServiceException("Error generating scholarship eligible report", ex, Application.Common.ExceptionCodes.Database);
            }
        }

        public async Task<StudentReportDto> GenerateRTEEligibleStudentsReportAsync()
        {
            try
            {
                _logger.LogInformation("GenerateRTEEligibleStudentsReport");
                const string cacheKey = "students:report:rteeligible";
                if (_cacheService.Exists(cacheKey))
                {
                    _logger.LogInformation("Returning RTE eligible report from cache");
                    return _cacheService.Get<StudentReportDto>(cacheKey);
                }
                const decimal incomeThreshold = 200000;
                var students = await _studentRepository.GetAllAsync();
                var list = students.Where(s =>
                    (s.FamilyIncome.HasValue && s.FamilyIncome.Value <= incomeThreshold) ||
                    (s.Category != null && new[] { "SC", "ST", "OBC", "EWS", "DG" }.Contains(s.Category.Trim().ToUpperInvariant()))).ToList();
                var byEligibility = new List<DistributionItemDto>();
                byEligibility.Add(new DistributionItemDto { Key = "Family income ≤ 2L", Count = students.Count(s => s.FamilyIncome.HasValue && s.FamilyIncome.Value <= incomeThreshold) });
                byEligibility.Add(new DistributionItemDto { Key = "SC/ST/OBC/EWS/DG", Count = students.Count(s => s.Category != null && new[] { "SC", "ST", "OBC", "EWS", "DG" }.Contains(s.Category.Trim().ToUpperInvariant())) });
                var report = new StudentReportDto
                {
                    ReportTitle = "RTE Eligible Students Report",
                    GeneratedOn = DateTime.UtcNow,
                    TotalCount = list.Count,
                    Description = "Students eligible under RTE (Right to Education) by income (≤2L) or category (SC/ST/OBC/EWS/DG).",
                    Sections = new List<ReportSectionDto> { new ReportSectionDto { Title = "Eligibility Criteria", Items = byEligibility } },
                    Students = list.Select(MapStudentToResponse).ToList()
                };
                _cacheService.Set(cacheKey, report);
                return report;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating RTE eligible report");
                throw new Application.Exceptions.BaseServiceException("Error generating RTE eligible report", ex, Application.Common.ExceptionCodes.Database);
            }
        }

        public async Task<int> GetTotalStudentsAsync()
        {
            try
            {
                _logger.LogInformation("GetTotalStudents");
                // Do not cache: total must stay correct after SQL seeds, imports, and other out-of-band DB changes.
                return await _studentRepository.CountAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting total students count");
                throw new Application.Exceptions.BaseServiceException("Error getting total students count", ex, Application.Common.ExceptionCodes.Database);
            }
        }

        public async Task<IEnumerable<StudentResponse>> GetTodayAdmissionsAsync()
        {
            try
            {
                var today = DateOnly.FromDateTime(DateTime.UtcNow);
                _logger.LogInformation("GetTodayAdmissions: {Date}", today);
                var cacheKey = $"students:todayadmissions:{today:yyyy-MM-dd}";
                if (_cacheService.Exists(cacheKey))
                {
                    _logger.LogInformation("Returning today admissions from cache");
                    return _cacheService.Get<IEnumerable<StudentResponse>>(cacheKey);
                }
                var students = await _studentRepository.FindAllAsync(s => s.AdmissionDate == today);
                var result = students.Select(MapStudentToResponse).ToList();
                _cacheService.Set(cacheKey, result);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting today admissions");
                throw new Application.Exceptions.BaseServiceException("Error getting today admissions", ex, Application.Common.ExceptionCodes.Database);
            }
        }

        public async Task<IEnumerable<StudentResponse>> GetStudentsWithBirthdayTodayAsync()
        {
            try
            {
                var today = DateOnly.FromDateTime(DateTime.UtcNow);
                _logger.LogInformation("GetStudentsWithBirthdayToday: {Month}-{Day}", today.Month, today.Day);
                var cacheKey = $"students:birthday:{today.Month:D2}-{today.Day:D2}";
                if (_cacheService.Exists(cacheKey))
                {
                    _logger.LogInformation("Returning students with birthday today from cache");
                    return _cacheService.Get<IEnumerable<StudentResponse>>(cacheKey);
                }
                var students = await _studentRepository.FindAllAsync(s => s.Dob.HasValue && s.Dob.Value.Month == today.Month && s.Dob.Value.Day == today.Day);
                var result = students.Select(MapStudentToResponse).ToList();
                _cacheService.Set(cacheKey, result);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting students with birthday today");
                throw new Application.Exceptions.BaseServiceException("Error getting students with birthday today", ex, Application.Common.ExceptionCodes.Database);
            }
        }

        public async Task<IEnumerable<StudentResponse>> GetStudentsWithPendingDocumentsAsync()
        {
            try
            {
                _logger.LogInformation("GetStudentsWithPendingDocuments");
                const string cacheKey = "students:pendingdocuments";
                if (_cacheService.Exists(cacheKey))
                {
                    _logger.LogInformation("Returning students with pending documents from cache");
                    return _cacheService.Get<IEnumerable<StudentResponse>>(cacheKey);
                }
                var students = await _studentRepository.FindAllAsync(s =>
                    string.IsNullOrWhiteSpace(s.ChildAadhaar) ||
                    string.IsNullOrWhiteSpace(s.BirthCertificateNo) ||
                    string.IsNullOrWhiteSpace(s.PhotoUrl));
                var result = students.Select(MapStudentToResponse).ToList();
                _cacheService.Set(cacheKey, result);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting students with pending documents");
                throw new Application.Exceptions.BaseServiceException("Error getting students with pending documents", ex, Application.Common.ExceptionCodes.Database);
            }
        }

        public async Task<IEnumerable<StudentResponse>> GetStudentsWithLowAttendanceAsync(decimal minAttendancePercent = 75)
        {
            try
            {
                _logger.LogInformation("GetStudentsWithLowAttendance: min {Min}%", minAttendancePercent);
                var cacheKey = $"students:lowattendance:{minAttendancePercent}";
                if (_cacheService.Exists(cacheKey))
                {
                    _logger.LogInformation("Returning students with low attendance from cache");
                    return _cacheService.Get<IEnumerable<StudentResponse>>(cacheKey);
                }
                var now = DateTime.UtcNow;
                var startOfMonth = new DateOnly(now.Year, now.Month, 1);
                var today = DateOnly.FromDateTime(now);
                var allAttendance = await _attendanceRepository.FindAllAsync(a =>
                    a.StudentId.HasValue &&
                    a.AttendanceDate.HasValue &&
                    a.AttendanceDate.Value >= startOfMonth &&
                    a.AttendanceDate.Value <= today);
                var byStudent = allAttendance
                    .Where(a => a.StudentId.HasValue)
                    .GroupBy(a => a.StudentId!.Value)
                    .Select(g => new { StudentId = g.Key, Total = g.Count(), Present = g.Count(a => string.Equals(a.Status?.Trim(), "Present", StringComparison.OrdinalIgnoreCase)) })
                    .Where(x => x.Total > 0 && (decimal)x.Present / x.Total * 100 < minAttendancePercent)
                    .Select(x => x.StudentId)
                    .ToList();
                if (byStudent.Count == 0)
                {
                    _cacheService.Set(cacheKey, new List<StudentResponse>());
                    return Enumerable.Empty<StudentResponse>();
                }
                var students = await _studentRepository.FindAllAsync(s => byStudent.Contains(s.Id));
                var result = students.Select(MapStudentToResponse).ToList();
                _cacheService.Set(cacheKey, result);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting students with low attendance");
                throw new Application.Exceptions.BaseServiceException("Error getting students with low attendance", ex, Application.Common.ExceptionCodes.Database);
            }
        }

        //    public async Task<IEnumerable<StudentResponse>> GetStudentsEligibleForBoardExamAsync()
        //    {
        //        try
        //        {
        //            _logger.LogInformation("GetStudentsEligibleForBoardExam");
        //            const string cacheKey = "students:boardexameligible";
        //            if (_cacheService.Exists(cacheKey))
        //            {
        //                _logger.LogInformation("Returning board exam eligible students from cache");
        //                return _cacheService.Get<IEnumerable<StudentResponse>>(cacheKey);
        //            }
        //            //var students = await _studentRepository.FindAllAsync(s =>
        //            //s.IsActive == true &&
        //            //s.StudentStatus != null &&
        //            //s.StudentStatus.Trim().ToUpper() == "ACTIVE" &&
        //            //s.ClassId.HasValue);

        //            var students = await _studentRepository.FindAllAsync(s =>
        //s.IsActive == true &&
        //s.ClassId != null &&
        //EF.Functions.ILike(s.StudentStatus, "ACTIVE"));


        //            var result = students.Select(MapStudentToResponse).ToList();
        //            _cacheService.Set(cacheKey, result);
        //            return result;
        //        }
        //        catch (Exception ex)
        //        {
        //            _logger.LogError(ex, "Error getting board exam eligible students");
        //            throw new Application.Exceptions.BaseServiceException("Error getting board exam eligible students", ex, Application.Common.ExceptionCodes.Database);
        //        }
        //
        //    

        public async Task<IEnumerable<StudentResponse>> GetStudentsEligibleForBoardExamAsync()
        {
            try
            {
                _logger.LogInformation("GetStudentsEligibleForBoardExam");
                const string cacheKey = "students:boardexameligible";
                if (_cacheService.Exists(cacheKey))
                {
                    _logger.LogInformation("Returning board exam eligible students from cache");
                    return _cacheService.Get<IEnumerable<StudentResponse>>(cacheKey);
                }

                var students = await _studentRepository.FindAllAsync(s =>
                    s.IsActive == true &&
                    s.StudentStatus != null &&
                    s.StudentStatus.Trim().ToUpper() == "ACTIVE" &&
                    s.ClassId.HasValue);

                var result = students.Select(MapStudentToResponse).ToList();
                _cacheService.Set(cacheKey, result);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting board exam eligible students");
                throw new Application.Exceptions.BaseServiceException("Error getting board exam eligible students", ex, Application.Common.ExceptionCodes.Database);
            }
        }

        public async Task<StudentProfileSummaryDto?> GetStudentProfileSummaryAsync(Guid studentId)
        {
            try
            {
                _logger.LogInformation("GetStudentProfileSummary: {StudentId}", studentId);
                var student = await _studentRepository.GetByIdAsync(studentId);
                if (student == null) return null;
                var (years, months) = student.Dob.HasValue ? GetAgeFromDob(student.Dob.Value) : (null, null);
                return new StudentProfileSummaryDto
                {
                    StudentId = student.Id,
                    FullName = BuildFullName(student.FirstName, student.MiddleName, student.LastName),
                    AdmissionNo = student.AdmissionNo,
                    RollNumber = student.RollNumber,
                    Gender = student.Gender,
                    Dob = student.Dob,
                    AgeYears = years,
                    BloodGroup = student.BloodGroup,
                    Category = student.Category,
                    Religion = student.Religion,
                    Phone = student.Phone,
                    Email = student.Email,
                    StudentStatus = student.StudentStatus,
                    IsActive = student.IsActive,
                    PhotoUrl = student.PhotoUrl
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting student profile summary for {StudentId}", studentId);
                throw new Application.Exceptions.BaseServiceException("Error getting student profile summary", ex, Application.Common.ExceptionCodes.Database);
            }
        }

        public async Task<StudentAcademicSummaryDto?> GetStudentAcademicSummaryAsync(Guid studentId)
        {
            try
            {
                _logger.LogInformation("GetStudentAcademicSummary: {StudentId}", studentId);
                var student = await _studentRepository.GetByIdAsync(studentId);
                if (student == null) return null;
                string? className = null, divisionName = null, branchName = null, academicYearName = null;
                if (student.ClassId.HasValue)
                {
                    var c = await _classRepository.GetByIdAsync(student.ClassId.Value);
                    className = c?.Name;
                }
                if (student.DivisionId.HasValue)
                {
                    var d = await _divisionRepository.GetByIdAsync(student.DivisionId.Value);
                    divisionName = d?.Name;
                }
                if (student.BranchId.HasValue)
                {
                    var b = await _branchRepository.GetByIdAsync(student.BranchId.Value);
                    branchName = b?.Name;
                }
                if (student.AcademicYearId.HasValue)
                {
                    var y = await _academicYearRepository.GetByIdAsync(student.AcademicYearId.Value);
                    academicYearName = y?.YearName;
                }
                return new StudentAcademicSummaryDto
                {
                    StudentId = student.Id,
                    ClassName = className,
                    ClassId = student.ClassId,
                    DivisionName = divisionName,
                    DivisionId = student.DivisionId,
                    BranchName = branchName,
                    BranchId = student.BranchId,
                    AcademicYearName = academicYearName,
                    AcademicYearId = student.AcademicYearId,
                    AdmissionDate = student.AdmissionDate,
                    RollNumber = student.RollNumber,
                    EnrollmentNo = student.EnrollmentNo,
                    LastExamName = student.LastExamName,
                    LastExamResult = student.LastExamResult
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting student academic summary for {StudentId}", studentId);
                throw new Application.Exceptions.BaseServiceException("Error getting student academic summary", ex, Application.Common.ExceptionCodes.Database);
            }
        }

        public async Task<StudentFeeSummaryDto?> GetStudentFeeSummaryAsync(Guid studentId)
        {
            try
            {
                _logger.LogInformation("GetStudentFeeSummary: {StudentId}", studentId);
                var student = await _studentRepository.GetByIdAsync(studentId);
                if (student == null) return null;
                var invoices = await _invoiceRepository.FindAllAsync(i => i.StudentId == studentId);
                var list = invoices ?? new List<Infrastructure.Invoice>();
                var totalBilled = list.Sum(i => i.TotalAmount ?? 0);
                var totalPaid = list.Sum(i => i.PaidAmount ?? 0);
                var totalOutstanding = list.Sum(i => i.RemainingAmount ?? 0);
                var today = DateOnly.FromDateTime(DateTime.UtcNow);
                var overdue = list.Count(i => i.DueDate.HasValue && i.DueDate.Value < today && (i.RemainingAmount ?? 0) > 0);
                return new StudentFeeSummaryDto
                {
                    StudentId = studentId,
                    TotalInvoices = list.Count,
                    TotalBilled = totalBilled,
                    TotalPaid = totalPaid,
                    TotalOutstanding = totalOutstanding,
                    InvoicesOverdue = overdue
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting student fee summary for {StudentId}", studentId);
                throw new Application.Exceptions.BaseServiceException("Error getting student fee summary", ex, Application.Common.ExceptionCodes.Database);
            }
        }

        public async Task<StudentAttendanceSummaryDto?> GetStudentAttendanceSummaryAsync(Guid studentId)
        {
            try
            {
                _logger.LogInformation("GetStudentAttendanceSummary: {StudentId}", studentId);
                var student = await _studentRepository.GetByIdAsync(studentId);
                if (student == null) return null;
                var now = DateTime.UtcNow;
                var startOfMonth = new DateOnly(now.Year, now.Month, 1);
                var today = DateOnly.FromDateTime(now);
                var records = await _attendanceRepository.FindAllAsync(a =>
                    a.StudentId == studentId &&
                    a.AttendanceDate.HasValue &&
                    a.AttendanceDate.Value >= startOfMonth &&
                    a.AttendanceDate.Value <= today);
                var list = records ?? new List<Infrastructure.Attendance>();
                var totalDays = list.Count;
                var present = list.Count(a => string.Equals(a.Status?.Trim(), "Present", StringComparison.OrdinalIgnoreCase));
                var absent = list.Count(a => string.Equals(a.Status?.Trim(), "Absent", StringComparison.OrdinalIgnoreCase));
                var leave = list.Count(a => !string.IsNullOrWhiteSpace(a.LeaveType) || string.Equals(a.Status?.Trim(), "Leave", StringComparison.OrdinalIgnoreCase));
                var percent = totalDays > 0 ? (decimal)present / totalDays * 100 : 0;
                return new StudentAttendanceSummaryDto
                {
                    StudentId = studentId,
                    PeriodStart = startOfMonth,
                    PeriodEnd = today,
                    TotalDays = totalDays,
                    PresentDays = present,
                    AbsentDays = absent,
                    LeaveDays = leave,
                    AttendancePercent = Math.Round(percent, 2)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting student attendance summary for {StudentId}", studentId);
                throw new Application.Exceptions.BaseServiceException("Error getting student attendance summary", ex, Application.Common.ExceptionCodes.Database);
            }
        }

        public async Task<StudentAgeDto?> CalculateStudentAgeAsync(Guid studentId)
        {
            try
            {
                _logger.LogInformation("CalculateStudentAge: {StudentId}", studentId);
                var student = await _studentRepository.GetByIdAsync(studentId);
                if (student == null || !student.Dob.HasValue) return null;
                var (years, months) = GetAgeFromDob(student.Dob.Value);
                var display = years.HasValue && months.HasValue ? $"{years} years, {months} months" : null;
                return new StudentAgeDto
                {
                    StudentId = studentId,
                    Dob = student.Dob,
                    Years = years,
                    Months = months,
                    Display = display
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating student age for {StudentId}", studentId);
                throw new Application.Exceptions.BaseServiceException("Error calculating student age", ex, Application.Common.ExceptionCodes.Database);
            }
        }

        public async Task<StudentAcademicDurationDto?> CalculateAcademicDurationAsync(Guid studentId)
        {
            try
            {
                _logger.LogInformation("CalculateAcademicDuration: {StudentId}", studentId);
                var student = await _studentRepository.GetByIdAsync(studentId);
                if (student == null || !student.AdmissionDate.HasValue) return null;
                var (years, months) = GetDurationBetween(student.AdmissionDate.Value, DateOnly.FromDateTime(DateTime.UtcNow));
                var display = years.HasValue && months.HasValue ? $"{years} years, {months} months" : null;
                return new StudentAcademicDurationDto
                {
                    StudentId = studentId,
                    AdmissionDate = student.AdmissionDate,
                    Years = years,
                    Months = months,
                    Display = display
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating academic duration for {StudentId}", studentId);
                throw new Application.Exceptions.BaseServiceException("Error calculating academic duration", ex, Application.Common.ExceptionCodes.Database);
            }
        }

        public async Task<string?> GetFullNameAsync(Guid studentId)
        {
            try
            {
                _logger.LogInformation("GetFullName: {StudentId}", studentId);
                var student = await _studentRepository.GetByIdAsync(studentId);
                if (student == null) return null;
                return BuildFullName(student.FirstName, student.MiddleName, student.LastName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting full name for {StudentId}", studentId);
                throw new Application.Exceptions.BaseServiceException("Error getting full name", ex, Application.Common.ExceptionCodes.Database);
            }
        }

        public Task<byte[]> GetStudentBulkUploadTemplateAsync()
        {
            try
            {
                _logger.LogInformation("GetStudentBulkUploadTemplate");
                var bytes = StudentBulkUploadExcelHelper.CreateTemplate();
                return Task.FromResult(bytes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating student bulk upload template");
                throw new Application.Exceptions.BaseServiceException("Error generating student bulk upload template", ex, Application.Common.ExceptionCodes.Database);
            }
        }

        public async Task<StudentBulkUploadResultDto> BulkUploadStudentsAsync(Stream excelStream)
        {
            var result = new StudentBulkUploadResultDto();
            var failedRowsForExcel = new List<(Dictionary<string, string> Row, string Error)>();
            try
            {
                _logger.LogInformation("BulkUploadStudents: starting parse");
                var rows = StudentBulkUploadExcelHelper.ParseRows(excelStream);
                result.TotalRows = rows.Count;

                if (rows.Count == 0)
                    return result;

                for (int i = 0; i < rows.Count; i++)
                {
                    var row = rows[i];
                    var excelRowIndex = i + 2; // header=0, instructions=1, data from 2

                    try
                    {
                        var admissionNo = GetString(row, "AdmissionNo");
                        var firstName = GetString(row, "FirstName");
                        var branchId = GetGuid(row, "BranchId");
                        var academicYearId = GetGuid(row, "AcademicYearId");

                        if (!branchId.HasValue || !academicYearId.HasValue || string.IsNullOrWhiteSpace(firstName))
                        {
                            var errorMessage = "BranchId, AcademicYearId and FirstName are required.";
                            result.FailureCount++;
                            result.Errors.Add(new StudentBulkUploadErrorDto
                            {
                                RowIndex = excelRowIndex,
                                AdmissionNo = admissionNo,
                                Message = errorMessage
                            });
                            failedRowsForExcel.Add((new Dictionary<string, string>(row, StringComparer.OrdinalIgnoreCase), errorMessage));
                            continue;
                        }

                        // Common parsed values
                        var aadhaar = GetString(row, "ChildAadhaar");
                        var admissionDate = GetDate(row, "AdmissionDate");
                        var middleName = GetString(row, "MiddleName");
                        var lastName = GetString(row, "LastName");
                        var gender = GetString(row, "Gender");
                        var dob = GetDate(row, "Dob");
                        var rollNumber = GetString(row, "RollNumber");
                        var status = GetString(row, "StudentStatus") ?? "ACTIVE";
                        var phone = GetString(row, "Phone");
                        var email = GetString(row, "Email");
                        var address1 = GetString(row, "AddressLine1");
                        var address2 = GetString(row, "AddressLine2");
                        var city = GetString(row, "City");
                        var state = GetString(row, "State");
                        var pincode = GetString(row, "Pincode");
                        var fatherName = GetString(row, "FatherName");
                        var fatherPhone = GetString(row, "FatherPhone");
                        var motherName = GetString(row, "MotherName");
                        var motherPhone = GetString(row, "MotherPhone");
                        var guardianName = GetString(row, "GuardianName");
                        var guardianPhone = GetString(row, "GuardianPhone");
                        var category = GetString(row, "Category");
                        var religion = GetString(row, "Religion");
                        var caste = GetString(row, "Caste");
                        var birthCertNo = GetString(row, "BirthCertificateNo");
                        var bloodGroup = GetString(row, "BloodGroup");
                        var enrollmentNo = GetString(row, "EnrollmentNo");
                        var previousSchool = GetString(row, "PreviousSchool");
                        var previousClass = GetString(row, "PreviousClass");
                        var nationality = GetString(row, "Nationality");
                        var classId = GetGuid(row, "ClassId");
                        var divisionId = GetGuid(row, "DivisionId");

                        // Find existing student: prefer Branch + AdmissionNo, fall back to Aadhaar
                        Student? existing = null;
                        if (!string.IsNullOrWhiteSpace(admissionNo))
                        {
                            existing = await _studentRepository.FindAsync(s =>
                                s.BranchId == branchId &&
                                s.AdmissionNo != null &&
                                s.AdmissionNo.Trim() == admissionNo.Trim());
                        }
                        if (existing == null && !string.IsNullOrWhiteSpace(aadhaar))
                        {
                            var normalizedAadhaar = aadhaar.Trim().Replace(" ", string.Empty);
                            existing = await _studentRepository.FindAsync(s =>
                                s.ChildAadhaar != null &&
                                s.ChildAadhaar.Replace(" ", string.Empty) == normalizedAadhaar);
                        }

                        if (existing == null)
                        {
                            // Insert new
                            var student = new Student
                            {
                                BranchId = branchId,
                                AcademicYearId = academicYearId,
                                AdmissionNo = admissionNo,
                                AdmissionDate = admissionDate,
                                FirstName = firstName,
                                MiddleName = middleName,
                                LastName = lastName,
                                Gender = gender,
                                Dob = dob,
                                RollNumber = rollNumber,
                                StudentStatus = status,
                                Phone = phone,
                                Email = email,
                                AddressLine1 = address1,
                                AddressLine2 = address2,
                                City = city,
                                State = state,
                                Pincode = pincode,
                                FatherName = fatherName,
                                FatherPhone = fatherPhone,
                                MotherName = motherName,
                                MotherPhone = motherPhone,
                                GuardianName = guardianName,
                                GuardianPhone = guardianPhone,
                                Category = category,
                                Religion = religion,
                                Caste = caste,
                                ChildAadhaar = aadhaar,
                                BirthCertificateNo = birthCertNo,
                                BloodGroup = bloodGroup,
                                EnrollmentNo = enrollmentNo,
                                PreviousSchool = previousSchool,
                                PreviousClass = previousClass,
                                Nationality = nationality,
                                ClassId = classId,
                                DivisionId = divisionId,
                                CreatedAt = DateTime.UtcNow,
                                IsActive = true
                            };
                            await _studentRepository.AddAsync(student);
                        }
                        else
                        {
                            // Update existing (upsert behavior). Only overwrite with non-empty values to avoid wiping data accidentally.
                            existing.BranchId = branchId ?? existing.BranchId;
                            existing.AcademicYearId = academicYearId ?? existing.AcademicYearId;
                            if (!string.IsNullOrWhiteSpace(admissionNo)) existing.AdmissionNo = admissionNo;
                            if (admissionDate.HasValue) existing.AdmissionDate = admissionDate;
                            if (!string.IsNullOrWhiteSpace(firstName)) existing.FirstName = firstName;
                            if (!string.IsNullOrWhiteSpace(middleName)) existing.MiddleName = middleName;
                            if (!string.IsNullOrWhiteSpace(lastName)) existing.LastName = lastName;
                            if (!string.IsNullOrWhiteSpace(gender)) existing.Gender = gender;
                            if (dob.HasValue) existing.Dob = dob;
                            if (!string.IsNullOrWhiteSpace(rollNumber)) existing.RollNumber = rollNumber;
                            if (!string.IsNullOrWhiteSpace(status)) existing.StudentStatus = status;
                            if (!string.IsNullOrWhiteSpace(phone)) existing.Phone = phone;
                            if (!string.IsNullOrWhiteSpace(email)) existing.Email = email;
                            if (!string.IsNullOrWhiteSpace(address1)) existing.AddressLine1 = address1;
                            if (!string.IsNullOrWhiteSpace(address2)) existing.AddressLine2 = address2;
                            if (!string.IsNullOrWhiteSpace(city)) existing.City = city;
                            if (!string.IsNullOrWhiteSpace(state)) existing.State = state;
                            if (!string.IsNullOrWhiteSpace(pincode)) existing.Pincode = pincode;
                            if (!string.IsNullOrWhiteSpace(fatherName)) existing.FatherName = fatherName;
                            if (!string.IsNullOrWhiteSpace(fatherPhone)) existing.FatherPhone = fatherPhone;
                            if (!string.IsNullOrWhiteSpace(motherName)) existing.MotherName = motherName;
                            if (!string.IsNullOrWhiteSpace(motherPhone)) existing.MotherPhone = motherPhone;
                            if (!string.IsNullOrWhiteSpace(guardianName)) existing.GuardianName = guardianName;
                            if (!string.IsNullOrWhiteSpace(guardianPhone)) existing.GuardianPhone = guardianPhone;
                            if (!string.IsNullOrWhiteSpace(category)) existing.Category = category;
                            if (!string.IsNullOrWhiteSpace(religion)) existing.Religion = religion;
                            if (!string.IsNullOrWhiteSpace(caste)) existing.Caste = caste;
                            if (!string.IsNullOrWhiteSpace(aadhaar)) existing.ChildAadhaar = aadhaar;
                            if (!string.IsNullOrWhiteSpace(birthCertNo)) existing.BirthCertificateNo = birthCertNo;
                            if (!string.IsNullOrWhiteSpace(bloodGroup)) existing.BloodGroup = bloodGroup;
                            if (!string.IsNullOrWhiteSpace(enrollmentNo)) existing.EnrollmentNo = enrollmentNo;
                            if (!string.IsNullOrWhiteSpace(previousSchool)) existing.PreviousSchool = previousSchool;
                            if (!string.IsNullOrWhiteSpace(previousClass)) existing.PreviousClass = previousClass;
                            if (!string.IsNullOrWhiteSpace(nationality)) existing.Nationality = nationality;
                            if (classId.HasValue) existing.ClassId = classId;
                            if (divisionId.HasValue) existing.DivisionId = divisionId;
                            existing.UpdatedAt = DateTime.UtcNow;
                            await _studentRepository.UpdateAsync(existing);
                        }

                        result.SuccessCount++;
                    }
                    catch (Exception rowEx)
                    {
                        var errorMessage = $"Error processing row: {rowEx.Message}";
                        result.FailureCount++;
                        result.Errors.Add(new StudentBulkUploadErrorDto
                        {
                            RowIndex = excelRowIndex,
                            AdmissionNo = GetString(row, "AdmissionNo"),
                            Message = errorMessage
                        });
                        failedRowsForExcel.Add((new Dictionary<string, string>(row, StringComparer.OrdinalIgnoreCase), errorMessage));
                    }
                }

                if (failedRowsForExcel.Count > 0)
                {
                    result.FailedExcelFile = StudentBulkUploadExcelHelper.CreateFailedRowsExcel(failedRowsForExcel);
                }

                if (result.SuccessCount > 0)
                {
                    await _unitOfWork.Commit();
                    _cacheService.Remove("students:all");
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error bulk uploading students");
                _unitOfWork.Rollback();
                throw new Application.Exceptions.BaseServiceException("Error bulk uploading students", ex, Application.Common.ExceptionCodes.Database);
            }
        }

        private static string? GetString(IDictionary<string, string> row, string key)
        {
            if (row == null || string.IsNullOrWhiteSpace(key)) return null;
            return row.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value)
                ? value.Trim()
                : null;
        }

        private static Guid? GetGuid(IDictionary<string, string> row, string key)
        {
            var value = GetString(row, key);
            if (string.IsNullOrWhiteSpace(value)) return null;
            return Guid.TryParse(value, out var guid) ? guid : null;
        }

        private static DateOnly? GetDate(IDictionary<string, string> row, string key)
        {
            var value = GetString(row, key);
            if (string.IsNullOrWhiteSpace(value)) return null;

            if (DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var dt))
                return DateOnly.FromDateTime(dt);

            if (DateOnly.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var d))
                return d;

            return null;
        }

        private static (int? Years, int? Months) GetAgeFromDob(DateOnly dob)
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var totalMonths = (today.Year - dob.Year) * 12 + (today.Month - dob.Month);
            if (today.Day < dob.Day) totalMonths--;
            if (totalMonths < 0) return (null, null);
            var years = totalMonths / 12;
            var months = totalMonths % 12;
            return (years, months);
        }

        private static (int? Years, int? Months) GetDurationBetween(DateOnly from, DateOnly to)
        {
            var totalMonths = (to.Year - from.Year) * 12 + (to.Month - from.Month);
            if (to.Day < from.Day) totalMonths--;
            if (totalMonths < 0) return (null, null);
            var years = totalMonths / 12;
            var months = totalMonths % 12;
            return (years, months);
        }

        private static string? BuildFullName(string? first, string? middle, string? last)
        {
            var parts = new[] { first?.Trim(), middle?.Trim(), last?.Trim() }.Where(s => !string.IsNullOrWhiteSpace(s)).ToList();
            return parts.Count == 0 ? null : string.Join(" ", parts);
        }

        private static string BuildFilterCacheKey(StudentFilterDto dto)
        {
            if (dto == null) return "";
            var parts = new List<string>();
            if (dto.BranchId.HasValue) parts.Add($"b:{dto.BranchId}");
            if (dto.ClassId.HasValue) parts.Add($"c:{dto.ClassId}");
            if (dto.DivisionId.HasValue) parts.Add($"d:{dto.DivisionId}");
            if (dto.AcademicYearId.HasValue) parts.Add($"y:{dto.AcademicYearId}");
            if (!string.IsNullOrWhiteSpace(dto.StudentStatus)) parts.Add($"st:{dto.StudentStatus!.Trim()}");
            if (!string.IsNullOrWhiteSpace(dto.Gender)) parts.Add($"g:{dto.Gender!.Trim()}");
            if (!string.IsNullOrWhiteSpace(dto.City)) parts.Add($"city:{dto.City!.Trim()}");
            if (!string.IsNullOrWhiteSpace(dto.State)) parts.Add($"state:{dto.State!.Trim()}");
            if (!string.IsNullOrWhiteSpace(dto.Pincode)) parts.Add($"pin:{dto.Pincode!.Trim()}");
            if (!string.IsNullOrWhiteSpace(dto.Keyword)) parts.Add($"k:{dto.Keyword!.Trim()}");
            return parts.Count == 0 ? "" : "students:search:" + string.Join("|", parts.OrderBy(x => x));
        }

        private static string BuildAdvancedSearchCacheKey(AdvancedStudentSearchDto dto)
        {
            if (dto == null) return "";
            var parts = new List<string>();
            if (dto.BranchId.HasValue) parts.Add($"b:{dto.BranchId}");
            if (dto.ClassId.HasValue) parts.Add($"c:{dto.ClassId}");
            if (dto.DivisionId.HasValue) parts.Add($"d:{dto.DivisionId}");
            if (dto.AcademicYearId.HasValue) parts.Add($"y:{dto.AcademicYearId}");
            if (!string.IsNullOrWhiteSpace(dto.StudentStatus)) parts.Add($"st:{dto.StudentStatus!.Trim()}");
            if (!string.IsNullOrWhiteSpace(dto.Gender)) parts.Add($"g:{dto.Gender!.Trim()}");
            if (!string.IsNullOrWhiteSpace(dto.City)) parts.Add($"city:{dto.City!.Trim()}");
            if (!string.IsNullOrWhiteSpace(dto.State)) parts.Add($"state:{dto.State!.Trim()}");
            if (!string.IsNullOrWhiteSpace(dto.Pincode)) parts.Add($"pin:{dto.Pincode!.Trim()}");
            if (!string.IsNullOrWhiteSpace(dto.Keyword)) parts.Add($"k:{dto.Keyword!.Trim()}");
            if (dto.DobFrom.HasValue) parts.Add($"dobf:{dto.DobFrom:yyyy-MM-dd}");
            if (dto.DobTo.HasValue) parts.Add($"dobt:{dto.DobTo:yyyy-MM-dd}");
            if (dto.AdmissionDateFrom.HasValue) parts.Add($"admf:{dto.AdmissionDateFrom:yyyy-MM-dd}");
            if (dto.AdmissionDateTo.HasValue) parts.Add($"admt:{dto.AdmissionDateTo:yyyy-MM-dd}");
            if (dto.LeavingDateFrom.HasValue) parts.Add($"levf:{dto.LeavingDateFrom:yyyy-MM-dd}");
            if (dto.LeavingDateTo.HasValue) parts.Add($"levt:{dto.LeavingDateTo:yyyy-MM-dd}");
            if (dto.HeightMin.HasValue) parts.Add($"hmin:{dto.HeightMin}");
            if (dto.HeightMax.HasValue) parts.Add($"hmax:{dto.HeightMax}");
            if (dto.WeightMin.HasValue) parts.Add($"wmin:{dto.WeightMin}");
            if (dto.WeightMax.HasValue) parts.Add($"wmax:{dto.WeightMax}");
            if (dto.FamilyIncomeMin.HasValue) parts.Add($"fmin:{dto.FamilyIncomeMin}");
            if (dto.FamilyIncomeMax.HasValue) parts.Add($"fmax:{dto.FamilyIncomeMax}");
            if (!string.IsNullOrWhiteSpace(dto.Religion)) parts.Add($"r:{dto.Religion!.Trim()}");
            if (!string.IsNullOrWhiteSpace(dto.Caste)) parts.Add($"caste:{dto.Caste!.Trim()}");
            if (!string.IsNullOrWhiteSpace(dto.Category)) parts.Add($"cat:{dto.Category!.Trim()}");
            if (!string.IsNullOrWhiteSpace(dto.BloodGroup)) parts.Add($"bg:{dto.BloodGroup!.Trim()}");
            if (!string.IsNullOrWhiteSpace(dto.Disability)) parts.Add($"dis:{dto.Disability!.Trim()}");
            if (!string.IsNullOrWhiteSpace(dto.PreviousSchool)) parts.Add($"ps:{dto.PreviousSchool!.Trim()}");
            if (!string.IsNullOrWhiteSpace(dto.PreviousBoard)) parts.Add($"pb:{dto.PreviousBoard!.Trim()}");
            return parts.Count == 0 ? "" : "students:advanced:" + string.Join("|", parts.OrderBy(x => x));
        }

        private StudentResponse MapStudentToResponse(Student s) => _mapper.Map<StudentResponse>(s);
    }
}