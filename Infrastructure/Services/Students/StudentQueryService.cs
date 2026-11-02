using Application.Attendances;
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
    /// <summary>Student lookups, filters and search (read only).</summary>
    public class StudentQueryService : StudentServiceBase, IStudentQueryService
    {
        public StudentQueryService(StudentServiceDependencies deps, ILogger<StudentQueryService> logger)
            : base(deps, logger)
        {
        }



        public async Task<IEnumerable<StudentResponse>> GetByDivisionIdAsync(Guid divisionId)
        {
            try
            {
                _logger.LogDebug("Fetching students by division {DivisionId}", divisionId);
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
                _logger.LogDebug("Fetching students by class {ClassId}", classId);
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
                _logger.LogDebug("Fetching students by academic year {AcademicYearId}", academicYearId);
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
                _logger.LogDebug("Fetching students by branch {BranchId}", branchId);
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
                _logger.LogDebug("Fetching students by class teacher staffId {StaffId}", staffId);
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
                _logger.LogDebug("Fetching student by Aadhaar (normalized)");
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
                var students = await _studentRepository.FindAllAsync(s => s.PreviousExamBoard != null && s.PreviousExamBoard.Trim() == key);
                var result = students.Select(MapStudentToResponse).ToList();
                _cacheService.Set(cacheKey, result);
                return result;
            }
            catch (Exception ex)
            {
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
                    query = query.Where(s => s.PreviousExamBoard != null && s.PreviousExamBoard.Trim() == pb);
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
                throw new Application.Exceptions.BaseServiceException("Error in global student search", ex, Application.Common.ExceptionCodes.Database);
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
                    // Holidays are not working days; the percentage follows the shared AttendanceTally rule.
                    .Select(g => new { StudentId = g.Key, Tally = AttendanceTally.Of(g.Where(a => a.IsHoliday != true).Select(a => a.Status)) })
                    .Where(x => x.Tally.WorkingDays > 0 && x.Tally.Percentage < minAttendancePercent)
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
                throw new Application.Exceptions.BaseServiceException("Error getting board exam eligible students", ex, Application.Common.ExceptionCodes.Database);
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
                throw new Application.Exceptions.BaseServiceException("Error getting full name", ex, Application.Common.ExceptionCodes.Database);
            }
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
    }
}
