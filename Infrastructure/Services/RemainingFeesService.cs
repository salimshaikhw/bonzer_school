using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Application.Abstractions.Services;
using Application.ViewModels.Fees;
using Domain.Entities.Postgres.Scaffolded;
using Infrastructure.Persistence.Postgres;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services
{
    /// <summary>
    /// Service for calculating and retrieving remaining fees information
    /// </summary>
    public class RemainingFeesService : IRemainingFeesService
    {
        private readonly BonzerSchoolDbContext _context;
        private readonly ILogger<RemainingFeesService> _logger;

        public RemainingFeesService(BonzerSchoolDbContext context, ILogger<RemainingFeesService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<StudentRemainingFeesDto> GetStudentRemainingFeesAsync(Guid studentId)
        {
            _logger.LogInformation($"Fetching remaining fees for student: {studentId}");

            var student = await _context.students
                .FirstOrDefaultAsync(s => s.Id == studentId);

            if (student == null)
            {
                _logger.LogWarning($"Student not found: {studentId}");
                return null;
            }

            var totalDueFees = await _context.class_fee_maps
                .Where(cfm => cfm.ClassId == student.ClassId && cfm.AcademicYearId == student.AcademicYearId)
                .SumAsync(cfm => cfm.Amount ?? 0);

            var paidAmount = await _context.payments
                .Where(p => p.StudentId == studentId)
                .SumAsync(p => p.Amount ?? 0);

            return new StudentRemainingFeesDto
            {
                StudentId = student.Id,
                StudentName = $"{student.FirstName} {student.LastName}".Trim(),
                AdmissionNo = student.AdmissionNo,
                ClassId = student.ClassId,
                DivisionId = student.DivisionId,
                BranchId = student.BranchId,
                TotalDueFees = totalDueFees,
                PaidAmount = paidAmount,
                RemainingFees = totalDueFees - paidAmount
            };
        }

        public async Task<ClassRemainingFeesDto> GetClassRemainingFeesAsync(Guid classId)
        {
            _logger.LogInformation($"Fetching remaining fees for class: {classId}");

            var classEntity = await _context.classes
                .FirstOrDefaultAsync(c => c.Id == classId);

            if (classEntity == null)
            {
                _logger.LogWarning($"Class not found: {classId}");
                return null;
            }

            var students = await _context.students
                .Where(s => s.ClassId == classId)
                .ToListAsync();

            var totalDueFees = await _context.class_fee_maps
                .Where(cfm => cfm.ClassId == classId)
                .SumAsync(cfm => cfm.Amount ?? 0);

            var paidAmount = await _context.payments
                .Where(p => students.Select(s => s.Id).Contains(p.StudentId ?? Guid.Empty))
                .SumAsync(p => p.Amount ?? 0);

            var X =  new ClassRemainingFeesDto
            {
                ClassId = classEntity.Id,
                ClassName = classEntity.Name,
                BranchId = classEntity.BranchId,
                TotalStudents = students.Count,
                TotalClassDueFees = totalDueFees * students.Count,
                TotalPaidAmount = paidAmount,
                TotalRemainingFees = (totalDueFees * students.Count) - paidAmount,
                PercentagePaid = totalDueFees > 0 
                    ? Math.Round((paidAmount / (totalDueFees * students.Count)) * 100, 2) 
                    : 0
            };


            return X;
        }
        public async Task<IEnumerable<ClassRemainingFeesDto>> GetAllClassesRemainingFeesAsync()
        {
            _logger.LogInformation("Fetching remaining fees for all classes");

            var classes = await _context.classes.ToListAsync();
            var result = new List<ClassRemainingFeesDto>();

            foreach (var classEntity in classes)
            {
                var classFees = await GetClassRemainingFeesAsync(classEntity.Id);
                if (classFees != null)
                {
                    result.Add(classFees);
                }
            }

            return result.OrderByDescending(x => x.TotalRemainingFees);
        }

        public async Task<DivisionRemainingFeesDto> GetDivisionRemainingFeesAsync(Guid divisionId)
        {
            _logger.LogInformation($"Fetching remaining fees for division: {divisionId}");

            var division = await _context.divisions
                .FirstOrDefaultAsync(d => d.Id == divisionId);

            if (division == null)
            {
                _logger.LogWarning($"Division not found: {divisionId}");
                return null;
            }

            var classEntity = await _context.classes
                .FirstOrDefaultAsync(c => c.Id == division.ClassId);

            var students = await _context.students
                .Where(s => s.DivisionId == divisionId)
                .ToListAsync();

            var totalDueFees = await _context.class_fee_maps
                .Where(cfm => cfm.ClassId == division.ClassId)
                .SumAsync(cfm => cfm.Amount ?? 0);

            var paidAmount = await _context.payments
                .Where(p => students.Select(s => s.Id).Contains(p.StudentId ?? Guid.Empty))
                .SumAsync(p => p.Amount ?? 0);

            var totalDivisionDue = totalDueFees * students.Count; // Store this in a variable to keep it clean

            return new DivisionRemainingFeesDto
            {
                DivisionId = division.Id,
                DivisionName = division.Name,
                DivisionCode = division.Code,
                ClassName = classEntity?.Name,
                BranchId = division.BranchId,
                TotalStudents = students.Count,
                TotalDivisionDueFees = totalDivisionDue,
                TotalPaidAmount = paidAmount,
                TotalRemainingFees = totalDivisionDue - paidAmount,

                PercentagePaid = totalDivisionDue > 0
                    ? Math.Round((paidAmount / totalDivisionDue) * 100, 2)
                    : 0
            };
        }

        public async Task<IEnumerable<DivisionRemainingFeesDto>> GetAllDivisionsRemainingFeesAsync()
        {
            _logger.LogInformation("Fetching remaining fees for all divisions");

            var divisions = await _context.divisions.ToListAsync();
            var result = new List<DivisionRemainingFeesDto>();

            foreach (var division in divisions)
            {
                var divisionFees = await GetDivisionRemainingFeesAsync(division.Id);
                if (divisionFees != null)
                {
                    result.Add(divisionFees);
                }
            }

            return result.OrderByDescending(x => x.TotalRemainingFees);
        }

        //public async Task<BranchRemainingFeesDto> GetBranchRemainingFeesAsync(Guid branchId)
        //{
        //    _logger.LogInformation($"Fetching remaining fees for branch: {branchId}");

        //    var branch = await _context.branches
        //        .FirstOrDefaultAsync(b => b.Id == branchId);

        //    if (branch == null)
        //    {
        //        _logger.LogWarning($"Branch not found: {branchId}");
        //        return null;
        //    }

        //    var classes = await _context.classes
        //        .Where(c => c.BranchId == branchId)
        //        .Select(c => c.Id)
        //        .ToListAsync();

        //    var students = await _context.students
        //        .Where(s => s.BranchId == branchId)
        //        .ToListAsync();

        //    var totalDueFees = await _context.class_fee_maps
        //        .Where(cfm => classes.Contains(cfm.ClassId ?? Guid.Empty))
        //        .SumAsync(cfm => cfm.Amount ?? 0);

        //    var paidAmount = await _context.payments
        //        .Where(p => students.Select(s => s.Id).Contains(p.StudentId ?? Guid.Empty))
        //        .SumAsync(p => p.Amount ?? 0);

        //    return new BranchRemainingFeesDto
        //    {
        //        BranchId = branch.Id,
        //        BranchName = branch.Name,
        //        Address = branch.Address,
        //        City = branch.City,
        //        Phone = branch.Phone,
        //        TotalStudents = students.Count,
        //        TotalClasses = classes.Count,
        //        TotalBranchDueFees = totalDueFees * students.Count,
        //        TotalPaidAmount = paidAmount,
        //        TotalRemainingFees = (totalDueFees * students.Count) - paidAmount,
        //        PercentagePaid = totalDueFees > 0 
        //            ? Math.Round((paidAmount / totalDueFees) * 100, 2) 
        //            : 0
        //    };
        //}

        public async Task<BranchRemainingFeesDto> GetBranchRemainingFeesAsync(Guid branchId)
        {
            _logger.LogInformation($"Fetching remaining fees for branch: {branchId}");

            var branch = await _context.branches.FirstOrDefaultAsync(b => b.Id == branchId);
            if (branch == null) return null;

            // 1. Get class IDs and their respective student counts
            var classData = await _context.students
                .Where(s => s.BranchId == branchId)
                .GroupBy(s => s.ClassId)
                .Select(g => new {
                    ClassId = g.Key ?? Guid.Empty,
                    StudentCount = g.Count()
                })
                .ToListAsync();

            var classIds = classData.Select(cd => cd.ClassId).ToList();

            // 2. Get the Fee Map totals for those classes
            var classFeeTotals = await _context.class_fee_maps
                .Where(cfm => classIds.Contains(cfm.ClassId ?? Guid.Empty))
                .GroupBy(cfm => cfm.ClassId)
                .Select(g => new {
                    ClassId = g.Key ?? Guid.Empty,
                    TotalFeePerStudent = g.Sum(cfm => cfm.Amount ?? 0)
                })
                .ToListAsync();

            // 3. Calculate Actual Total Due for the Branch
            decimal totalBranchDue = 0;
            foreach (var data in classData)
            {
                var feePerStudent = classFeeTotals
                    .FirstOrDefault(f => f.ClassId == data.ClassId)?.TotalFeePerStudent ?? 0;

                totalBranchDue += (feePerStudent * data.StudentCount);
            }

            // 4. Get Total Paid for the Branch
            var totalPaidAmount = await _context.payments
                .Where(p => p.BranchId == branchId) // Assuming BranchId exists on Payment, else join via Students
                .SumAsync(p => p.Amount ?? 0);

            return new BranchRemainingFeesDto
            {
                BranchId = branch.Id,
                BranchName = branch.Name,
                Address = branch.Address,
                City = branch.City,
                Phone = branch.Phone,
                TotalStudents = classData.Sum(c => c.StudentCount),
                TotalClasses = classIds.Count,
                TotalBranchDueFees = totalBranchDue,
                TotalPaidAmount = totalPaidAmount,
                TotalRemainingFees = totalBranchDue - totalPaidAmount,

                // FIX: Compare apples to apples (Total Paid vs Total Branch Due)
                PercentagePaid = totalBranchDue > 0
                    ? Math.Round((totalPaidAmount / totalBranchDue) * 100, 2)
                    : 0
            };
        }

        public async Task<IEnumerable<BranchRemainingFeesDto>> GetAllBranchesRemainingFeesAsync()
        {
            _logger.LogInformation("Fetching remaining fees for all branches");

            var branches = await _context.branches.ToListAsync();
            var result = new List<BranchRemainingFeesDto>();

            foreach (var branch in branches)
            {
                var branchFees = await GetBranchRemainingFeesAsync(branch.Id);
                if (branchFees != null)
                {
                    result.Add(branchFees);
                }
            }

            return result.OrderByDescending(x => x.TotalRemainingFees);
        }

        public async Task<IEnumerable<StudentFeeDetailDto>> GetStudentFeeDetailsAsync(Guid studentId)
        {
            _logger.LogInformation($"Fetching fee details for student: {studentId}");

            var student = await _context.students
                .FirstOrDefaultAsync(s => s.Id == studentId);

            if (student == null)
            {
                _logger.LogWarning($"Student not found: {studentId}");
                return new List<StudentFeeDetailDto>();
            }

            //var classFeeMaps = await _context.class_fee_maps
            //    .Where(cfm => cfm.ClassId == student.ClassId && cfm.AcademicYearId == student.AcademicYearId)
            //    .Include(cfm => cfm.FeeHeadId)
            //    .ToListAsync();

            var classFeeMaps = await _context.class_fee_maps
            .Where(cfm => cfm.ClassId == student.ClassId && cfm.AcademicYearId == student.AcademicYearId)
            .ToListAsync();

            //var payments = await _context.payments
            //    .Where(p => p.StudentId == studentId)
            //    .Include(p => p.Id)
            //    .ToListAsync();

            var payments = await _context.payments
            .Where(p => p.StudentId == studentId)
            .ToListAsync();

            var feeDetails = new List<StudentFeeDetailDto>();

            foreach (var feeMap in classFeeMaps)
            {
                var feeHead = await _context.fee_heads
                    .FirstOrDefaultAsync(fh => fh.Id == feeMap.FeeHeadId);

                var paidAmount = 0m;
                if (feeMap.FeeHeadId.HasValue)
                {
                    paidAmount = await _context.payment_fee_lines
                        .Where(pfl => pfl.FeeHeadId == feeMap.FeeHeadId && 
                                    payments.Select(p => p.Id).Contains(pfl.PaymentId))
                        .SumAsync(pfl => pfl.Amount);
                }

                feeDetails.Add(new StudentFeeDetailDto
                {
                    StudentId = student.Id,
                    StudentName = $"{student.FirstName} {student.LastName}".Trim(),
                    FeeHeadId = feeHead?.Id,
                    FeeName = feeHead?.Name ?? "Unknown",
                    Installment = feeMap.InstallmentNo,
                    DueFees = feeMap.Amount ?? 0,
                    PaidAmount = paidAmount,
                    RemainingFees = (feeMap.Amount ?? 0) - paidAmount
                });
            }

            return feeDetails;
        }

        public async Task<IEnumerable<PendingFeesStudentDto>> GetPendingFeesStudentsByDivisionAsync(Guid divisionId)
        {
            _logger.LogInformation($"Fetching students with pending fees in division: {divisionId}");

            var students = await _context.students
                .Where(s => s.DivisionId == divisionId && s.IsActive == true)
                .ToListAsync();

            var division = await _context.divisions
                .FirstOrDefaultAsync(d => d.Id == divisionId);

            var result = new List<PendingFeesStudentDto>();

            foreach (var student in students)
            {
                var studentFees = await GetStudentRemainingFeesAsync(student.Id);
                if (studentFees != null && studentFees.RemainingFees > 0)
                {
                    var classEntity = await _context.classes
                        .FirstOrDefaultAsync(c => c.Id == student.ClassId);

                    result.Add(new PendingFeesStudentDto
                    {
                        StudentId = studentFees.StudentId,
                        StudentName = studentFees.StudentName,
                        AdmissionNo = studentFees.AdmissionNo,
                        DivisionName = division?.Name,
                        ClassName = classEntity?.Name,
                        BranchId = studentFees.BranchId,
                        TotalDueFees = studentFees.TotalDueFees,
                        PaidAmount = studentFees.PaidAmount,
                        RemainingFees = studentFees.RemainingFees
                    });
                }
            }

            return result.OrderByDescending(x => x.RemainingFees);
        }

        public async Task<IEnumerable<PendingFeesStudentDto>> GetPendingFeesStudentsByClassAsync(Guid classId)
        {
            _logger.LogInformation($"Fetching students with pending fees in class: {classId}");

            var students = await _context.students
                .Where(s => s.ClassId == classId && s.IsActive == true)
                .ToListAsync();

            var classEntity = await _context.classes
                .FirstOrDefaultAsync(c => c.Id == classId);

            var result = new List<PendingFeesStudentDto>();

            foreach (var student in students)
            {
                var studentFees = await GetStudentRemainingFeesAsync(student.Id);
                if (studentFees != null && studentFees.RemainingFees > 0)
                {
                    var division = await _context.divisions
                        .FirstOrDefaultAsync(d => d.Id == student.DivisionId);

                    result.Add(new PendingFeesStudentDto
                    {
                        StudentId = studentFees.StudentId,
                        StudentName = studentFees.StudentName,
                        AdmissionNo = studentFees.AdmissionNo,
                        DivisionName = division?.Name,
                        ClassName = classEntity?.Name,
                        BranchId = studentFees.BranchId,
                        TotalDueFees = studentFees.TotalDueFees,
                        PaidAmount = studentFees.PaidAmount,
                        RemainingFees = studentFees.RemainingFees
                    });
                }
            }

            return result.OrderByDescending(x => x.RemainingFees);
        }

        public async Task<IEnumerable<PendingFeesStudentDto>> GetPendingFeesStudentsByBranchAsync(Guid branchId)
        {
            _logger.LogInformation($"Fetching students with pending fees in branch: {branchId}");

            var students = await _context.students
                .Where(s => s.BranchId == branchId && s.IsActive == true)
                .ToListAsync();

            var result = new List<PendingFeesStudentDto>();

            foreach (var student in students)
            {
                var studentFees = await GetStudentRemainingFeesAsync(student.Id);
                if (studentFees != null && studentFees.RemainingFees > 0)
                {
                    var classEntity = await _context.classes
                        .FirstOrDefaultAsync(c => c.Id == student.ClassId);

                    var division = await _context.divisions
                        .FirstOrDefaultAsync(d => d.Id == student.DivisionId);

                    result.Add(new PendingFeesStudentDto
                    {
                        StudentId = studentFees.StudentId,
                        StudentName = studentFees.StudentName,
                        AdmissionNo = studentFees.AdmissionNo,
                        DivisionName = division?.Name,
                        ClassName = classEntity?.Name,
                        BranchId = studentFees.BranchId,
                        TotalDueFees = studentFees.TotalDueFees,
                        PaidAmount = studentFees.PaidAmount,
                        RemainingFees = studentFees.RemainingFees
                    });
                }
            }

            return result.OrderByDescending(x => x.RemainingFees);
        }

        public async Task<IEnumerable<MonthlyPaymentSummaryDto>> GetMonthlyPaymentSummaryAsync(int monthsBack = 12)
        {
            _logger.LogInformation($"Fetching monthly payment summary for last {monthsBack} months");

            var startDate = DateOnly.FromDateTime(DateTime.Now.AddMonths(-monthsBack));

            var monthlySummary = await _context.payments
                .Where(p => p.PaymentDate >= startDate)
                .GroupBy(p => new { p.PaymentDate.Value.Year, p.PaymentDate.Value.Month, p.BranchId })
                .Select(g => new MonthlyPaymentSummaryDto
                {
                    PaymentMonth = new DateTime(g.Key.Year, g.Key.Month, 1),
                    BranchName = _context.branches
                        .Where(b => b.Id == g.Key.BranchId)
                        .Select(b => b.Name)
                        .FirstOrDefault() ?? "Unknown",
                    StudentsWithPayments = g.Select(p => p.StudentId).Distinct().Count(),
                    TotalPayments = g.Count(),
                    TotalCollected = g.Sum(p => p.Amount ?? 0)
                })
                .OrderByDescending(x => x.PaymentMonth)
                .ToListAsync();

            return monthlySummary;
        }

        public async Task<FeeAnalyticsDashboardDto> GetFeeAnalyticsDashboardAsync()
        {
            _logger.LogInformation("Fetching fee analytics dashboard");

            var totalDueFees = await _context.class_fee_maps
                .SumAsync(cfm => cfm.Amount ?? 0);

            var totalPaidAmount = await _context.payments
                .SumAsync(p => p.Amount ?? 0);

            var totalStudents = await _context.students.CountAsync();

            var studentsWithPendingFees = 0;
            var allStudents = await _context.students.ToListAsync();
            foreach (var student in allStudents)
            {
                var studentFees = await GetStudentRemainingFeesAsync(student.Id);
                if (studentFees != null && studentFees.RemainingFees > 0)
                {
                    studentsWithPendingFees++;
                }
            }

            var topClasses = await GetAllClassesRemainingFeesAsync();
            var branches = await GetAllBranchesRemainingFeesAsync();
            var monthlyPayments = await GetMonthlyPaymentSummaryAsync();
            var divisions = await GetAllDivisionsRemainingFeesAsync();

            return new FeeAnalyticsDashboardDto
            {
                TotalDueFees = totalDueFees,
                TotalPaidAmount = totalPaidAmount,
                TotalRemainingFees = totalDueFees - totalPaidAmount,
                OverallPercentagePaid = totalDueFees > 0 
                    ? Math.Round((totalPaidAmount / totalDueFees) * 100, 2) 
                    : 0,
                TotalStudents = totalStudents,
                StudentsWithPendingFees = studentsWithPendingFees,
                TotalBranches = branches.Count(),
                TotalClasses = (int)await _context.classes.CountAsync(),
                TotalDivisions = divisions.Count(),
                TopClassesByRemainingFees = topClasses.OrderByDescending(x => x.TotalRemainingFees).Take(10).ToList(),
                BranchWiseSummary = branches.OrderByDescending(x => x.TotalRemainingFees).ToList(),
                MonthlyPaymentTrend = monthlyPayments.Take(12).ToList()
            };
        }

        public async Task<FeeAnalyticsDashboardDto> GetBranchFeeAnalyticsAsync(Guid branchId)
        {
            _logger.LogInformation($"Fetching fee analytics for branch: {branchId}");

            var branchClasses = await _context.classes
                .Where(c => c.BranchId == branchId)
                .Select(c => c.Id)
                .ToListAsync();

            var branchStudents = await _context.students
                .Where(s => s.BranchId == branchId)
                .ToListAsync();

            var totalDueFees = await _context.class_fee_maps
                .Where(cfm => branchClasses.Contains(cfm.ClassId ?? Guid.Empty))
                .SumAsync(cfm => cfm.Amount ?? 0);

            var totalPaidAmount = await _context.payments
                .Where(p => branchStudents.Select(s => s.Id).Contains(p.StudentId ?? Guid.Empty))
                .SumAsync(p => p.Amount ?? 0);

            var studentsWithPendingFees = await GetPendingFeesStudentsByBranchAsync(branchId);

            var topClasses = (await GetAllClassesRemainingFeesAsync())
                .Where(c => branchClasses.Contains(c.ClassId));

            var monthlyPayments = await GetMonthlyPaymentSummaryAsync();
            var branchMonthlyPayments = monthlyPayments
                .Where(mp => _context.payments
                    .Where(p => branchStudents.Select(s => s.Id).Contains(p.StudentId ?? Guid.Empty))
                    .Select(p => p.BranchId)
                    .Contains(branchId))
                .ToList();

            return new FeeAnalyticsDashboardDto
            {
                TotalDueFees = totalDueFees,
                TotalPaidAmount = totalPaidAmount,
                TotalRemainingFees = totalDueFees - totalPaidAmount,
                OverallPercentagePaid = totalDueFees > 0 
                    ? Math.Round((totalPaidAmount / totalDueFees) * 100, 2) 
                    : 0,
                TotalStudents = branchStudents.Count,
                StudentsWithPendingFees = studentsWithPendingFees.Count(),
                TotalBranches = 1,
                TotalClasses = branchClasses.Count,
                TotalDivisions = await _context.divisions
                    .Where(d => branchClasses.Contains(d.ClassId ?? Guid.Empty))
                    .CountAsync(),
                TopClassesByRemainingFees = topClasses.OrderByDescending(x => x.TotalRemainingFees).Take(10).ToList(),
                BranchWiseSummary = new List<BranchRemainingFeesDto> { await GetBranchRemainingFeesAsync(branchId) },
                MonthlyPaymentTrend = branchMonthlyPayments.Take(12).ToList()
            };
        }

        public async Task<IEnumerable<StudentFeesByPaymentHeadDto>> GetStudentFeesByPaymentHeadAsync(Guid studentId)
        {
            _logger.LogInformation($"Fetching fees by payment head for student: {studentId}");

            var student = await _context.students
                .FirstOrDefaultAsync(s => s.Id == studentId);

            if (student == null)
            {
                _logger.LogWarning($"Student not found: {studentId}");
                return new List<StudentFeesByPaymentHeadDto>();
            }

            var classFeeMaps = await _context.class_fee_maps
                .Where(cfm => cfm.ClassId == student.ClassId && cfm.AcademicYearId == student.AcademicYearId)
                .ToListAsync();

            var payments = await _context.payments
                .Where(p => p.StudentId == studentId)
                .ToListAsync();

            var paymentIds = payments.Select(p => p.Id).ToList();

            var result = new List<StudentFeesByPaymentHeadDto>();

            foreach (var feeMap in classFeeMaps)
            {
                var feeHead = await _context.fee_heads
                    .FirstOrDefaultAsync(fh => fh.Id == feeMap.FeeHeadId);

                var paidAmount = 0m;
                if (feeMap.FeeHeadId.HasValue)
                {
                    paidAmount = await _context.payment_fee_lines
                        .Where(pfl => pfl.FeeHeadId == feeMap.FeeHeadId && paymentIds.Contains(pfl.PaymentId))
                        .SumAsync(pfl => pfl.Amount);
                }

                var lastPayment = payments
                    .Where(p => paymentIds.Contains(p.Id))
                    .OrderByDescending(p => p.PaymentDate)
                    .FirstOrDefault();

                //result.Add(new StudentFeesByPaymentHeadDto
                //{
                //    StudentId = student.Id,
                //    StudentName = $"{student.FirstName} {student.LastName}".Trim(),
                //    AdmissionNo = student.AdmissionNo,
                //    ClassId = student.ClassId,
                //    DivisionId = student.DivisionId,
                //    BranchId = student.BranchId,
                //    FeeHeadId = feeHead?.Id,
                //    FeeHeadName = feeHead?.Name ?? "Unknown",
                //    AccountCode = feeHead?.AccountCode,
                //    Installment = feeMap.InstallmentNo,
                //    DueFeeAmount = feeMap.Amount ?? 0,
                //    PaidAmount = paidAmount,
                //    RemainingFees = (feeMap.Amount ?? 0) - paidAmount,
                //    PaymentCount = payments.Count,
                //    LastPaymentDate = lastPayment?.PaymentDate.HasValue == true
                //    ? lastPayment.PaymentDate.Value.ToDateTime(TimeOnly.MinValue)
                //    : null,
                //    LastPaymentMethod = lastPayment?.PaymentMethod
                //});

                var aggregatedData = result
    .GroupBy(x => new { x.FeeHeadId, x.FeeHeadName, x.AccountCode })
    .Select(group => {
        // 1. Properly sum up the Due and Paid amounts for ALL students in this group
        var totalDue = group.Sum(x => x.DueFeeAmount);
        var totalPaid = group.Sum(x => x.PaidAmount);

        return new
        {
            FeeHeadId = group.Key.FeeHeadId,
            FeeHeadName = group.Key.FeeHeadName,
            AccountCode = group.Key.AccountCode,
            StudentsAffected = group.Count(),

            TotalDueAmount = totalDue,
            TotalPaidAmount = totalPaid,
            TotalOutstandingAmount = totalDue - totalPaid, 

            OverallPercentagePaid = totalDue > 0
                ? Math.Round(((decimal)totalPaid / totalDue) * 100, 2)
                : 0
        };
    }).ToList();
            }

            return result.OrderBy(x => x.Installment ?? 0).ThenBy(x => x.FeeHeadName);
        }

        public async Task<IEnumerable<StudentPaymentHistoryDto>> GetStudentPaymentHistoryByFeeHeadAsync(Guid studentId)
        {
            _logger.LogInformation($"Fetching payment history by fee head for student: {studentId}");

            var payments = await _context.payments
                .Where(p => p.StudentId == studentId)
                .OrderByDescending(p => p.PaymentDate)
                .ToListAsync();

            var result = new List<StudentPaymentHistoryDto>();

            foreach (var payment in payments)
            {
                var paymentFeeLines = await _context.payment_fee_lines
                    .Where(pfl => pfl.PaymentId == payment.Id)
                    .ToListAsync();

                var student = await _context.students
                    .FirstOrDefaultAsync(s => s.Id == studentId);

                foreach (var feeLine in paymentFeeLines)
                {
                    var feeHead = await _context.fee_heads
                        .FirstOrDefaultAsync(fh => fh.Id == feeLine.FeeHeadId);

                    result.Add(new StudentPaymentHistoryDto
                    {
                        StudentId = student?.Id ?? studentId,
                        StudentName = student != null ? $"{student.FirstName} {student.LastName}".Trim() : "Unknown",
                        PaymentId = payment.Id,
                        InvoiceId = payment.InvoiceId,
                        PaymentDate = payment.PaymentDate.HasValue
                        ? payment.PaymentDate.Value.ToDateTime(TimeOnly.MinValue)
                        : null,
                        PaymentMethod = payment.PaymentMethod,
                        BankName = payment.BankName,
                        FeeHeadId = feeHead?.Id,
                        FeeHeadName = feeHead?.Name ?? "Custom Fee",
                        PaidAmount = feeLine.Amount,
                        Remarks = payment.Remarks
                    });
                }
            }

            return result;
        }

        public async Task<IEnumerable<ClassPaymentHeadAnalysisDto>> GetClassPaymentHeadAnalysisAsync(Guid classId)
        {
            _logger.LogInformation($"Fetching payment head analysis for class: {classId}");

            var classEntity = await _context.classes
                .FirstOrDefaultAsync(c => c.Id == classId);

            if (classEntity == null)
            {
                return new List<ClassPaymentHeadAnalysisDto>();
            }

            var students = await _context.students
                .Where(s => s.ClassId == classId)
                .Select(s => s.Id)
                .ToListAsync();

            var classFeeMaps = await _context.class_fee_maps
                .Where(cfm => cfm.ClassId == classId)
                .ToListAsync();

            var feeHeads = await _context.fee_heads.ToListAsync();

            var result = new List<ClassPaymentHeadAnalysisDto>();

            foreach (var feeHead in feeHeads)
            {
                var dueFees = classFeeMaps
                    .Where(cfm => cfm.FeeHeadId == feeHead.Id)
                    .Sum(cfm => cfm.Amount ?? 0);

                var paidAmount = await _context.payment_fee_lines
                    .Where(pfl => pfl.FeeHeadId == feeHead.Id &&
                                _context.payments
                                    .Where(p => students.Contains(p.StudentId ?? Guid.Empty))
                                    .Select(p => p.Id)
                                    .Contains(pfl.PaymentId))
                    .SumAsync(pfl => pfl.Amount);

                //result.Add(new ClassPaymentHeadAnalysisDto
                //{
                //    ClassId = classEntity.Id,
                //    ClassName = classEntity.Name,
                //    FeeHeadId = feeHead.Id,
                //    FeeHeadName = feeHead.Name,
                //    TotalStudents = students.Count,
                //    TotalDueAmount = dueFees * students.Count,
                //    TotalPaidAmount = paidAmount,
                //    TotalRemainingFees = (dueFees * students.Count) - paidAmount,
                //    PercentagePaid = dueFees > 0 ? Math.Round((paidAmount / dueFees) * 100, 2) : 0
                //});

                var totalDueAmount = dueFees * students.Count;

                result.Add(new ClassPaymentHeadAnalysisDto
                {
                    ClassId = classEntity.Id,
                    ClassName = classEntity.Name,
                    FeeHeadId = feeHead.Id,
                    FeeHeadName = feeHead.Name,
                    TotalStudents = students.Count,
                    TotalDueAmount = totalDueAmount, 
                    TotalPaidAmount = paidAmount,
                    TotalRemainingFees = totalDueAmount - paidAmount,

                    PercentagePaid = totalDueAmount > 0
                                     ? Math.Round(((decimal)paidAmount / totalDueAmount) * 100, 2)
                                     : 0
                });

            }

            return result.OrderBy(x => x.FeeHeadName);
        }

        public async Task<IEnumerable<DivisionPaymentHeadAnalysisDto>> GetDivisionPaymentHeadAnalysisAsync(Guid divisionId)
        {
            _logger.LogInformation($"Fetching payment head analysis for division: {divisionId}");

            var division = await _context.divisions
                .FirstOrDefaultAsync(d => d.Id == divisionId);

            if (division == null)
            {
                return new List<DivisionPaymentHeadAnalysisDto>();
            }

            var classEntity = await _context.classes
                .FirstOrDefaultAsync(c => c.Id == division.ClassId);

            var students = await _context.students
                .Where(s => s.DivisionId == divisionId)
                .Select(s => s.Id)
                .ToListAsync();

            var classFeeMaps = await _context.class_fee_maps
                .Where(cfm => cfm.ClassId == division.ClassId)
                .ToListAsync();

            var feeHeads = await _context.fee_heads.ToListAsync();

            var result = new List<DivisionPaymentHeadAnalysisDto>();

            foreach (var feeHead in feeHeads)
            {
                var dueFees = classFeeMaps
                    .Where(cfm => cfm.FeeHeadId == feeHead.Id)
                    .Sum(cfm => cfm.Amount ?? 0);

                var paidAmount = await _context.payment_fee_lines
                    .Where(pfl => pfl.FeeHeadId == feeHead.Id &&
                                _context.payments
                                    .Where(p => students.Contains(p.StudentId ?? Guid.Empty))
                                    .Select(p => p.Id)
                                    .Contains(pfl.PaymentId))
                    .SumAsync(pfl => pfl.Amount);

                //result.Add(new DivisionPaymentHeadAnalysisDto
                //{
                //    DivisionId = division.Id,
                //    DivisionName = division.Name,
                //    DivisionCode = division.Code,
                //    ClassName = classEntity?.Name,
                //    FeeHeadId = feeHead.Id,
                //    FeeHeadName = feeHead.Name,
                //    TotalStudents = students.Count,
                //    TotalDueAmount = dueFees,
                //    TotalPaidAmount = paidAmount,
                //    TotalRemainingFees = dueFees - paidAmount,
                //    PercentagePaid = dueFees > 0 ? Math.Round((paidAmount / dueFees) * 100, 2) : 0
                //});
                // 1. Calculate the true total due for the entire division (500 * 61 = 30,500)
                var grandTotalDue = dueFees * students.Count;

                result.Add(new DivisionPaymentHeadAnalysisDto
                {
                    DivisionId = division.Id,
                    DivisionName = division.Name,
                    DivisionCode = division.Code,
                    ClassName = classEntity?.Name,
                    FeeHeadId = feeHead.Id,
                    FeeHeadName = feeHead.Name,
                    TotalStudents = students.Count,

                    TotalDueAmount = grandTotalDue,
                    TotalPaidAmount = paidAmount,
                    TotalRemainingFees = grandTotalDue - paidAmount,

                    PercentagePaid = grandTotalDue > 0
                                     ? Math.Round(((decimal)paidAmount / grandTotalDue) * 100, 2)
                                     : 0
                });

            }

            return result.OrderBy(x => x.FeeHeadName);
        }

        public async Task<IEnumerable<PaymentStatusMatrixDto>> GetPaymentStatusMatrixByDivisionAsync(Guid divisionId)
        {
            _logger.LogInformation($"Fetching payment status matrix for division: {divisionId}");

            var students = await _context.students
                .Where(s => s.DivisionId == divisionId && s.IsActive == true)
                .ToListAsync();

            var result = new List<PaymentStatusMatrixDto>();

            foreach (var student in students)
            {
                var studentFees = await GetStudentFeesByPaymentHeadAsync(student.Id);

                foreach (var fee in studentFees)
                {
                    var status = "NO FEE";
                    if (fee.DueFeeAmount > 0)
                    {
                        status = fee.PaidAmount > 0 ? "PAID" : "PENDING";
                    }

                    result.Add(new PaymentStatusMatrixDto
                    {
                        StudentId = student.Id,
                        StudentName = $"{student.FirstName} {student.LastName}".Trim(),
                        AdmissionNo = student.AdmissionNo,
                        FeeHeadId = fee.FeeHeadId,
                        FeeHeadName = fee.FeeHeadName,
                        PaymentStatus = status,
                        DueAmount = fee.DueFeeAmount,
                        PaidAmount = fee.PaidAmount,
                        RemainingAmount = fee.RemainingFees
                    });
                }
            }

            return result.OrderBy(x => x.StudentName).ThenBy(x => x.FeeHeadName);
        }

        //public async Task<IEnumerable<OutstandingFeesByHeadDto>> GetOutstandingFeesByHeadAsync()
        //{
        //    _logger.LogInformation("Fetching outstanding fees by fee head");

        //    var feeHeads = await _context.fee_heads.ToListAsync();
        //    var result = new List<OutstandingFeesByHeadDto>();

        //    foreach (var feeHead in feeHeads)
        //    {
        //        var classFeeMaps = await _context.class_fee_maps
        //            .Where(cfm => cfm.FeeHeadId == feeHead.Id)
        //            .ToListAsync();

        //        var affectedStudents = await _context.students
        //            .Where(s => _context.class_fee_maps
        //                .Where(cfm => cfm.FeeHeadId == feeHead.Id)
        //                .Select(cfm => cfm.ClassId)
        //                .Contains(s.ClassId ?? Guid.Empty))
        //            .Select(s => s.Id)
        //            .Distinct()
        //            .CountAsync();

        //        var totalDueAmount = classFeeMaps.Sum(cfm => cfm.Amount ?? 0);

        //        var totalPaidAmount = await _context.payment_fee_lines
        //            .Where(pfl => pfl.FeeHeadId == feeHead.Id)
        //            .SumAsync(pfl => pfl.Amount);

        //        result.Add(new OutstandingFeesByHeadDto
        //        {
        //            FeeHeadId = feeHead.Id,
        //            FeeHeadName = feeHead.Name,
        //            AccountCode = feeHead.AccountCode,
        //            StudentsAffected = affectedStudents,
        //            TotalDueAmount = totalDueAmount,
        //            TotalPaidAmount = totalPaidAmount,
        //            TotalOutstandingAmount = totalDueAmount - totalPaidAmount,
        //            OverallPercentagePaid = totalDueAmount > 0 
        //                ? Math.Round((totalPaidAmount / totalDueAmount) * 100, 2) 
        //                : 0
        //        });
        //    }

        //    return result.OrderByDescending(x => x.TotalOutstandingAmount);
        //}

        public async Task<IEnumerable<OutstandingFeesByHeadDto>> GetOutstandingFeesByHeadAsync()
        {
            _logger.LogInformation("Fetching outstanding fees by fee head");

            var feeHeads = await _context.fee_heads.ToListAsync();
            var result = new List<OutstandingFeesByHeadDto>();

            foreach (var feeHead in feeHeads)
            {
                // 1. Get all classes that have this fee assigned and how much it costs
                var classFees = await _context.class_fee_maps
                    .Where(cfm => cfm.FeeHeadId == feeHead.Id)
                    .Select(cfm => new { cfm.ClassId, Amount = cfm.Amount ?? 0 })
                    .ToListAsync();

                var classIds = classFees.Select(cf => cf.ClassId).ToList();

                // 2. Get student counts per class for these specific classes
                var studentCounts = await _context.students
                    .Where(s => classIds.Contains(s.ClassId ?? Guid.Empty))
                    .GroupBy(s => s.ClassId)
                    .Select(g => new { ClassId = g.Key, Count = g.Count() })
                    .ToListAsync();

                // 3. Calculate Total Due: Sum of (Fee Amount * Number of Students in that class)
                decimal totalDueAmount = 0;
                int totalAffectedStudents = 0;

                foreach (var cf in classFees)
                {
                    var count = studentCounts.FirstOrDefault(sc => sc.ClassId == cf.ClassId)?.Count ?? 0;
                    totalDueAmount += (cf.Amount * count);
                    totalAffectedStudents += count;
                }

                // 4. Total Paid remains the same - it's the aggregate of all payment lines
                var totalPaidAmount = await _context.payment_fee_lines
                    .Where(pfl => pfl.FeeHeadId == feeHead.Id)
                    .SumAsync(pfl => pfl.Amount);

                result.Add(new OutstandingFeesByHeadDto
                {
                    FeeHeadId = feeHead.Id,
                    FeeHeadName = feeHead.Name,
                    AccountCode = feeHead.AccountCode,
                    StudentsAffected = totalAffectedStudents,
                    TotalDueAmount = totalDueAmount,
                    TotalPaidAmount = totalPaidAmount,
                    TotalOutstandingAmount = totalDueAmount - totalPaidAmount,
                    OverallPercentagePaid = totalDueAmount > 0
                        ? Math.Round((totalPaidAmount / totalDueAmount) * 100, 2)
                        : 0
                });
            }

            return result.OrderByDescending(x => x.TotalOutstandingAmount);
        }

        public async Task<IEnumerable<StudentFeeComplianceReportDto>> GetStudentComplianceReportByDivisionAsync(Guid divisionId)
        {
            _logger.LogInformation($"Fetching compliance report for division: {divisionId}");

            var students = await _context.students
                .Where(s => s.DivisionId == divisionId && s.IsActive == true)
                .ToListAsync();

            var result = new List<StudentFeeComplianceReportDto>();

            foreach (var student in students)
            {
                var classEntity = await _context.classes
                    .FirstOrDefaultAsync(c => c.Id == student.ClassId);

                var division = await _context.divisions
                    .FirstOrDefaultAsync(d => d.Id == student.DivisionId);

                var classFeeMaps = await _context.class_fee_maps
                    .Where(cfm => cfm.ClassId == student.ClassId && cfm.AcademicYearId == student.AcademicYearId)
                    .ToListAsync();

                var totalDueAmount = classFeeMaps.Sum(cfm => cfm.Amount ?? 0);

                var payments = await _context.payments
                    .Where(p => p.StudentId == student.Id)
                    .ToListAsync();

                var totalPaidAmount = payments.Sum(p => p.Amount ?? 0);

                var paymentIds = payments.Select(p => p.Id).ToList();

                var paymentFeeLines = await _context.payment_fee_lines
                    .Where(pfl => paymentIds.Contains(pfl.PaymentId))
                    .ToListAsync();

                var feeHeadsPaid = paymentFeeLines.Select(pfl => pfl.FeeHeadId).Distinct().Count();
                var feeHeadsTotal = classFeeMaps.Select(cfm => cfm.FeeHeadId).Distinct().Count();
                var feeHeadsPending = feeHeadsTotal - feeHeadsPaid;

                result.Add(new StudentFeeComplianceReportDto
                {
                    StudentId = student.Id,
                    StudentName = $"{student.FirstName} {student.LastName}".Trim(),
                    AdmissionNo = student.AdmissionNo,
                    ClassName = classEntity?.Name,
                    DivisionName = division?.Name,
                    TotalDueAmount = totalDueAmount,
                    TotalPaidAmount = totalPaidAmount,
                    TotalRemainingFees = totalDueAmount - totalPaidAmount,
                    OverallPercentagePaid = totalDueAmount > 0 
                        ? Math.Round((totalPaidAmount / totalDueAmount) * 100, 2) 
                        : 0,
                    FeeHeadsPaid = feeHeadsPaid,
                    FeeHeadsPending = feeHeadsPending,
                    LastPaymentDate = payments
                    .OrderByDescending(p => p.PaymentDate)
                    .Select(p => p.PaymentDate)
                    .FirstOrDefault()
                    ?.ToDateTime(TimeOnly.MinValue),
                    TotalPayments = payments.Count
                });
            }

            return result.OrderByDescending(x => x.TotalRemainingFees);
        }
    }
}
