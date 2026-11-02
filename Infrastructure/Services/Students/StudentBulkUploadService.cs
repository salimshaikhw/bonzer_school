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
    /// <summary>Excel template and bulk import of students; rows follow the same admission rules as the API.</summary>
    public class StudentBulkUploadService : StudentServiceBase, IStudentBulkUploadService
    {
        public StudentBulkUploadService(StudentServiceDependencies deps, ILogger<StudentBulkUploadService> logger)
            : base(deps, logger)
        {
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
                        // Same rules as the API: only known statuses; an existing student's status changes only
                        // through the lifecycle actions.
                        var statusColumn = GetString(row, "StudentStatus")?.Trim().ToUpperInvariant();
                        if (statusColumn != null && !StudentStatuses.IsKnown(statusColumn))
                            throw new BaseServiceException($"Unknown StudentStatus '{statusColumn}'. Use one of: {string.Join(", ", StudentStatuses.All)}.", ExceptionCodes.Validation);
                        var status = statusColumn ?? StudentStatuses.Active;
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
                                IsActive = status is StudentStatuses.Active or StudentStatuses.Suspended
                            };
                            await EnsureConsistentPlacementAsync(student.BranchId, student.ClassId, student.DivisionId);
                            NormalizeIdentifiers(student);
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
                            EnsureStatusUnchanged(existing, statusColumn);
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
                            await EnsureConsistentPlacementAsync(existing.BranchId, existing.ClassId, existing.DivisionId);
                            NormalizeIdentifiers(existing);
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
    }
}
