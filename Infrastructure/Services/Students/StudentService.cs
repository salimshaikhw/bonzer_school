using Application.Abstractions.Repositories;
using Application.Abstractions.Services.Academics;
using Application.Abstractions.Services.Students;
using Application.Abstractions.Services;
using Application.Abstractions;
using Application.Common.Helpers;
using Application.Common;
using Application.Exceptions;
using Application.Students;
using Application.Validation.Common;
using Application.ViewModels.Student;
using AutoMapper;
using Infrastructure.Services;
using Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System;

namespace Infrastructure.Services.Students
{
    /// <summary>
    /// Admission, edits and the enrolment lifecycle of students: the write side of the student module.
    /// Lookups, analytics, reports and bulk upload are separate services (see <see cref="StudentServiceBase"/>).
    /// </summary>
    public class StudentService : StudentServiceBase, IStudentService
    {
        public StudentService(StudentServiceDependencies deps, ILogger<StudentService> logger)
            : base(deps, logger)
        {
        }

        public async Task<IEnumerable<StudentResponse>> GetAllAsync()
        {
            try
            {
                _logger.LogDebug("Fetching all students");
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

                _logger.LogDebug("Fetching students paged: page {Page}, pageSize {PageSize}", page, pageSize);
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
                throw new Application.Exceptions.BaseServiceException("Error fetching paged students", ex, Application.Common.ExceptionCodes.Database);
            }
        }

        public async Task<StudentResponse?> GetByIdAsync(Guid id)
        {
            try
            {
                _logger.LogDebug("Fetching student by id: {Id}", id);
                string cacheKey = $"student:{id}";
                if (_cacheService.Exists(cacheKey))
                {
                    _logger.LogInformation("Returning student {Id} from cache", id);
                    return _cacheService.Get<StudentResponse>(cacheKey);
                }
                var student = await _studentRepository.GetByIdAsync(id);
                if (student == null)
                {
                    _logger.LogWarning("Student not found: {Id}", id);
                    return null;
                }
                var response = MapStudentToResponse(student);
                _cacheService.Set(cacheKey, response);
                return response;
            }
            catch (Exception ex)
            {
                throw new Application.Exceptions.BaseServiceException($"Error fetching student by id: {id}", ex, Application.Common.ExceptionCodes.Database);
            }
        }

        public async Task<StudentResponse> CreateAsync(StudentRequest request)
        {
            _logger.LogInformation("Creating student in branch {BranchId}", request.BranchId);
            await ValidateEnrolmentAsync(request, exceptId: null);

            var student = _mapper.Map<Student>(request);
            if (student.Id == Guid.Empty)
                student.Id = Guid.NewGuid();
            // A new student starts ACTIVE unless a historical record is imported with another known status.
            student.StudentStatus = StudentStatuses.IsKnown(request.StudentStatus)
                ? request.StudentStatus!.Trim().ToUpperInvariant()
                : StudentStatuses.Active;
            student.IsActive = student.StudentStatus is StudentStatuses.Active or StudentStatuses.Suspended;
            student.IsTcIssued ??= false;
            NormalizeIdentifiers(student);

            await _studentRepository.AddAsync(student);
            InvalidateStudentCaches(student.Id, student);
            return MapStudentToResponse(student);
        }

        public async Task<StudentResponse?> UpdateAsync(Guid id, StudentRequest request)
        {
            _logger.LogInformation("Updating student: {Id}", id);
            var student = await _studentRepository.GetByIdAsync(id);
            EnsureStatusUnchanged(student, request.StudentStatus);
            await ValidateEnrolmentAsync(request, id);

            var staleCacheKeys = StudentCacheKeys(student).ToList();
            // Status, active flag and TC are owned by the lifecycle actions; an edit form must not reset them.
            var (status, isActive, tcIssued, promotedTo) = (student.StudentStatus, student.IsActive, student.IsTcIssued, student.PromotedToClassId);
            _mapper.Map(request, student);
            student.Id = id;
            (student.StudentStatus, student.IsActive, student.IsTcIssued, student.PromotedToClassId) = (status, isActive, tcIssued, promotedTo);
            NormalizeIdentifiers(student);

            await _studentRepository.UpdateAsync(student);
            RemoveCacheKeys(staleCacheKeys.Concat(StudentCacheKeys(student)));
            return MapStudentToResponse(student);
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
                EnsureStatusUnchanged(student, dto.StudentStatus);
                if (dto.EnrollmentNo != null) student.EnrollmentNo = dto.EnrollmentNo;
                if (dto.IsAadhaarVerified.HasValue) student.IsAadhaarVerified = dto.IsAadhaarVerified;
                if (dto.PromotedToClassId.HasValue) student.PromotedToClassId = dto.PromotedToClassId;
                if (dto.LeavingDate.HasValue) student.LeavingDate = dto.LeavingDate;
                if (dto.IsTcIssued.HasValue && dto.IsTcIssued != (student.IsTcIssued ?? false))
                    throw new BaseServiceException("A transfer certificate is issued through the TC action, not by editing the student.", ExceptionCodes.Operation);
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
                await EnsureConsistentPlacementAsync(student.BranchId, student.ClassId, student.DivisionId);
                NormalizeIdentifiers(student);
                await _studentRepository.UpdateAsync(student);
                await _unitOfWork.Commit();
                _cacheService.Remove("students:all");
                _cacheService.Remove($"student:{id}");
                _logger.LogInformation("Student patched successfully: {Id}", id);
                return MapStudentToResponse(student);
            }
            catch (Exception ex)
            {
                _unitOfWork.Rollback();
                throw new Application.Exceptions.BaseServiceException($"Error patching student: {id}", ex, Application.Common.ExceptionCodes.Database);
            }
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            try
            {
                _logger.LogInformation("Deleting student: {Id}", id);
                await _studentRepository.DeleteAsync(id);
                await _unitOfWork.Commit();
                // Invalidate cache
                _cacheService.Remove("students:all");
                _cacheService.Remove($"student:{id}");
                return true;
            }
            catch (Exception ex)
            {
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
                _unitOfWork.Rollback();
                throw new Application.Exceptions.BaseServiceException($"Error restoring student: {id}", ex, Application.Common.ExceptionCodes.Database);
            }
        }

        // ---- Enrolment lifecycle: every action goes through StudentLifecycle (the state machine). ----

        public Task<StudentResponse?> ActivateStudentAsync(Guid id) => ChangeEnrolmentAsync(id, StudentAction.Activate);

        public Task<StudentResponse?> SuspendStudentAsync(Guid id) => ChangeEnrolmentAsync(id, StudentAction.Suspend);

        public Task<StudentResponse?> DropStudentAsync(Guid id) => ChangeEnrolmentAsync(id, StudentAction.Drop);

        public Task<StudentResponse?> MarkStudentPassedOutAsync(Guid id) => ChangeEnrolmentAsync(id, StudentAction.PassOut);

        public Task<StudentResponse?> IssueTcAsync(Guid id) => ChangeEnrolmentAsync(id, StudentAction.IssueTc);

        public Task<StudentResponse?> ReEnrollStudentAsync(Guid id) => ChangeEnrolmentAsync(id, StudentAction.ReEnroll);

        /// <summary>Generic status change: mapped to the matching lifecycle action, so the same rules apply.</summary>
        public Task<StudentResponse?> UpdateStudentStatusAsync(Guid id, string status) =>
            ChangeEnrolmentAsync(id, student =>
            {
                StudentLifecycle.Apply(student, StudentLifecycle.ActionFor(student, status), Today);
                return Task.CompletedTask;
            });

        /// <summary>
        /// Moves an active student to another branch of the school group. Classes and divisions belong to a
        /// branch, so the old class, division and roll number are cleared; assign the new class afterwards.
        /// </summary>
        public Task<StudentResponse?> TransferStudentAsync(Guid id, Guid newBranchId) =>
            ChangeEnrolmentAsync(id, async student =>
            {
                if (student.BranchId == newBranchId)
                    throw new BaseServiceException("The student is already in that branch.", ExceptionCodes.Operation);
                await _structure.EnsureBranchExistsAsync(newBranchId);
                StudentLifecycle.Apply(student, StudentAction.TransferBranch, Today);
                student.BranchId = newBranchId;
                student.ClassId = null;
                student.DivisionId = null;
                student.RollNumber = null;
            });

        /// <summary>
        /// Promotes an active student to another class of the same branch for an academic year. The old division
        /// and roll number belong to the old class, so they are cleared; assign the new division afterwards.
        /// </summary>
        public Task<StudentResponse?> PromoteStudentAsync(Guid id, Guid newClassId, Guid academicYearId) =>
            ChangeEnrolmentAsync(id, async student =>
            {
                StudentLifecycle.Apply(student, StudentAction.Promote, Today);
                if (student.ClassId == newClassId)
                    throw new BaseServiceException("The student is already in that class.", ExceptionCodes.Operation);
                var newClass = await _structure.GetClassAsync(newClassId);
                if (newClass.BranchId != student.BranchId)
                    throw new BaseServiceException("A student can only be promoted to a class of their own branch. Transfer them first.", ExceptionCodes.Validation);
                if (!await _academicYearRepository.IsExistAsync(y => y.Id == academicYearId))
                    throw new BaseServiceException($"Academic year {academicYearId} does not exist.", ExceptionCodes.Validation);

                student.PromotedToClassId = newClassId;
                student.ClassId = newClassId;
                student.DivisionId = null;
                student.RollNumber = null;
                student.AcademicYearId = academicYearId;
            });

        public Task<StudentResponse?> UpdateLeavingDateAsync(Guid id, DateOnly date) =>
            ChangeEnrolmentAsync(id, student =>
            {
                if (student.AdmissionDate is { } admitted && date < admitted)
                    throw new BaseServiceException("The leaving date cannot be before the admission date.", ExceptionCodes.Validation);
                student.LeavingDate = date;
                return Task.CompletedTask;
            });

        private Task<StudentResponse?> ChangeEnrolmentAsync(Guid id, StudentAction action) =>
            ChangeEnrolmentAsync(id, student =>
            {
                StudentLifecycle.Apply(student, action, Today);
                return Task.CompletedTask;
            });

        /// <summary>Loads the student, applies the change, saves, and clears the cached lists it was and is now in.</summary>
        private async Task<StudentResponse?> ChangeEnrolmentAsync(Guid id, Func<Student, Task> change)
        {
            var student = await _studentRepository.GetByIdAsync(id);
            var staleCacheKeys = StudentCacheKeys(student).ToList();
            var statusBefore = StudentStatuses.Of(student);

            await change(student);

            await _studentRepository.UpdateAsync(student);
            RemoveCacheKeys(staleCacheKeys.Concat(StudentCacheKeys(student)));
            _logger.LogInformation("Student {StudentId} enrolment changed: {StatusBefore} -> {StatusAfter}", id, statusBefore, StudentStatuses.Of(student));
            return MapStudentToResponse(student);
        }

        // Existence checks are indexed queries; they are not cached, because a cached "free" answer goes stale
        // the moment a student is saved with that number.
        public async Task<bool> CheckAdmissionNumberExistsAsync(Guid branchId, string admissionNo)
        {
            if (string.IsNullOrWhiteSpace(admissionNo)) return false;
            var number = admissionNo.Trim();
            return await _studentRepository.IsExistAsync(s => s.BranchId == branchId && s.AdmissionNo == number);
        }

        public async Task<bool> CheckRollNumberExistsAsync(Guid classId, string rollNo)
        {
            if (string.IsNullOrWhiteSpace(rollNo)) return false;
            var number = rollNo.Trim();
            return await _studentRepository.IsExistAsync(s => s.ClassId == classId && s.RollNumber == number);
        }

        public async Task<bool> CheckAadhaarExistsAsync(string childAadhaar)
        {
            var normalized = Aadhaar.Normalize(childAadhaar);
            if (normalized == null) return false;
            return await _studentRepository.IsExistAsync(s => s.ChildAadhaar == normalized);
        }

        public Task<ValidationResultResponse> ValidateAadhaarAsync(string childAadhaar)
        {
            var problem = Aadhaar.Problem(childAadhaar);
            return Task.FromResult(new ValidationResultResponse { IsValid = problem == null, Message = problem ?? "Valid." });
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
                throw new Application.Exceptions.BaseServiceException("Error validating duplicate student", ex, Application.Common.ExceptionCodes.Database);
            }
        }

        public async Task<ValidationResultResponse> ValidatePromotionEligibilityAsync(Guid studentId) =>
            Eligibility(await _studentRepository.GetByIdAsync(studentId), StudentAction.Promote, "Eligible for promotion.");

        public async Task<ValidationResultResponse> ValidateTransferEligibilityAsync(Guid studentId) =>
            Eligibility(await _studentRepository.GetByIdAsync(studentId), StudentAction.TransferBranch, "Eligible for transfer.");

        /// <summary>Asks the same state machine the actions use, so "eligible" always means "the action will succeed".</summary>
        private static ValidationResultResponse Eligibility(Student student, StudentAction action, string okMessage)
        {
            var reason = StudentLifecycle.WhyNot(student, action);
            return new ValidationResultResponse { IsValid = reason == null, Message = reason ?? okMessage };
        }

        // ---- Enrolment data rules ----

        /// <summary>
        /// Rules for creating / editing a student: branch, class, division and academic year exist and fit
        /// together; admission number (per branch), Aadhaar and roll number (per class + division) are unique.
        /// </summary>
        private async Task ValidateEnrolmentAsync(StudentRequest request, Guid? exceptId)
        {
            await EnsureConsistentPlacementAsync(request.BranchId, request.ClassId, request.DivisionId);
            if (request.AcademicYearId is { } yearId && !await _academicYearRepository.IsExistAsync(y => y.Id == yearId))
                throw new BaseServiceException($"Academic year {yearId} does not exist.", ExceptionCodes.Validation);

            var admissionNo = request.AdmissionNo?.Trim();
            if (!string.IsNullOrEmpty(admissionNo)
                && await _studentRepository.IsExistAsync(s => s.BranchId == request.BranchId && s.AdmissionNo == admissionNo && s.Id != exceptId))
                throw new BaseServiceException($"Admission number '{admissionNo}' is already used in this branch.", ExceptionCodes.Operation);

            var aadhaar = Aadhaar.Normalize(request.ChildAadhaar);
            if (aadhaar != null && await _studentRepository.IsExistAsync(s => s.ChildAadhaar == aadhaar && s.Id != exceptId))
                throw new BaseServiceException("Another student is already registered with this Aadhaar number.", ExceptionCodes.Operation);

            var rollNo = request.RollNumber?.Trim();
            if (!string.IsNullOrEmpty(rollNo) && request.ClassId != null
                && await _studentRepository.IsExistAsync(s => s.ClassId == request.ClassId && s.DivisionId == request.DivisionId
                                                             && s.RollNumber == rollNo && s.Id != exceptId))
                throw new BaseServiceException($"Roll number '{rollNo}' is already taken in this class.", ExceptionCodes.Operation);
        }
    }
}
