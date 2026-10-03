using Application.Abstractions.Services;
using Application.ViewModels.ExamAnalytics;
using Infrastructure.Persistence.Postgres;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Infrastructure.Services
{
    /// <summary>
    /// EF Core based implementation of exam analytics.
    /// Note: methods can be optimized further based on real reporting needs.
    /// </summary>
    public class ExamAnalyticsService : IExamAnalyticsService
    {
        private readonly BonzerSchoolDbContext _context;
        private readonly ILogger<ExamAnalyticsService> _logger;

        public ExamAnalyticsService(BonzerSchoolDbContext context, ILogger<ExamAnalyticsService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<StudentExamSummaryDto?> GetStudentExamSummaryAsync(Guid studentId, Guid examId)
        {
            _logger.LogInformation("ExamAnalytics: student {StudentId}, exam {ExamId} summary", studentId, examId);

            var exam = await _context.exams.FirstOrDefaultAsync(e => e.Id == examId);
            if (exam == null) return null;

            var studentResult = await _context.student_results
                .AsNoTracking()
                .FirstOrDefaultAsync(sr => sr.StudentId == studentId && sr.ExamId == examId);

            var componentsQuery =
                from esc in _context.exam_subject_components
                join es in _context.exam_subjects on esc.ExamSubjectId equals es.Id
                join s in _context.subjects on es.SubjectId equals s.Id
                where es.ExamId == examId
                select new
                {
                    esc.Id,
                    SubjectId = s.Id,
                    SubjectName = s.Name,
                    esc.MaxMarks
                };

            var marksQuery =
                from m in _context.marks
                join esc in _context.exam_subject_components on m.ExamSubjectComponentId equals esc.Id
                join es in _context.exam_subjects on esc.ExamSubjectId equals es.Id
                join s in _context.subjects on es.SubjectId equals s.Id
                where es.ExamId == examId && m.StudentId == studentId
                select new
                {
                    SubjectId = s.Id,
                    SubjectName = s.Name,
                    m.MarksObtained,
                    esc.MaxMarks
                };

            var marks = await marksQuery.ToListAsync();
            var summary = new StudentExamSummaryDto
            {
                StudentId = studentId,
                ExamId = examId,
                ExamName = exam.Name,
                TotalMarks = studentResult?.TotalMarks,
                Percentage = studentResult?.Percentage,
                Grade = studentResult?.Grade,
                Result = studentResult?.Result,
                Rank = studentResult?.Rank
            };

            if (marks.Count > 0)
            {
                var grouped = marks
                    .GroupBy(x => new { x.SubjectId, x.SubjectName })
                    .Select(g =>
                    {
                        var obtained = g.Sum(x => x.MarksObtained ?? 0);
                        var max = g.Sum(x => x.MaxMarks ?? 0);
                        return new StudentExamSubjectLineDto
                        {
                            SubjectId = g.Key.SubjectId,
                            SubjectName = g.Key.SubjectName,
                            MarksObtained = obtained,
                            MaxMarks = max,
                            Percentage = max > 0 ? Math.Round(obtained * 100m / max, 2) : null
                        };
                    })
                    .OrderBy(x => x.SubjectName)
                    .ToList();

                summary.Subjects = grouped;
                summary.MaxTotalMarks = grouped.Sum(x => x.MaxMarks ?? 0);
                if (summary.TotalMarks == null && summary.MaxTotalMarks > 0)
                {
                    var totalObtained = grouped.Sum(x => x.MarksObtained ?? 0);
                    summary.TotalMarks = totalObtained;
                    summary.Percentage = Math.Round(totalObtained * 100m / summary.MaxTotalMarks.Value, 2);
                }
            }

            return summary;
        }

        public async Task<IReadOnlyList<StudentExamTrendPointDto>> GetStudentExamTrendAsync(Guid studentId, ExamFilterDto filter)
        {
            _logger.LogInformation("ExamAnalytics: student {StudentId} exam trend", studentId);

            var query =
                from sr in _context.student_results.AsNoTracking()
                join e in _context.exams.AsNoTracking() on sr.ExamId equals e.Id
                where sr.StudentId == studentId
                select new { sr, e };

            if (filter.ExamIds != null && filter.ExamIds.Count > 0)
            {
                query = query.Where(x => filter.ExamIds.Contains(x.e.Id));
            }
            if (filter.AcademicYearId.HasValue)
            {
                query = query.Where(x => x.e.AcademicYearId == filter.AcademicYearId.Value);
            }
            if (filter.ExamTypeId.HasValue)
            {
                query = query.Where(x => x.e.ExamTypeId == filter.ExamTypeId.Value);
            }
            if (filter.FromDate.HasValue)
            {
                query = query.Where(x => x.e.StartDate >= filter.FromDate.Value);
            }
            if (filter.ToDate.HasValue)
            {
                query = query.Where(x => x.e.EndDate <= filter.ToDate.Value);
            }

            var list = await query
                .OrderBy(x => x.e.StartDate)
                .Select(x => new StudentExamTrendPointDto
                {
                    ExamId = x.e.Id,
                    ExamName = x.e.Name,
                    ExamDate = x.e.StartDate,
                    TotalMarks = x.sr.TotalMarks,
                    MaxTotalMarks = null, // can be computed later if needed
                    Percentage = x.sr.Percentage,
                    Grade = x.sr.Grade,
                    Result = x.sr.Result
                })
                .ToListAsync();

            return list;
        }

        public async Task<StudentSubjectGrowthDto> GetStudentSubjectGrowthAsync(Guid studentId, ExamFilterDto filter)
        {
            _logger.LogInformation("ExamAnalytics: student {StudentId} subject growth", studentId);

            var marksQuery =
                from m in _context.marks.AsNoTracking()
                join esc in _context.exam_subject_components.AsNoTracking() on m.ExamSubjectComponentId equals esc.Id
                join es in _context.exam_subjects.AsNoTracking() on esc.ExamSubjectId equals es.Id
                join e in _context.exams.AsNoTracking() on es.ExamId equals e.Id
                join s in _context.subjects.AsNoTracking() on es.SubjectId equals s.Id
                where m.StudentId == studentId
                select new { m, esc, es, e, s };

            if (filter.ExamIds != null && filter.ExamIds.Count > 0)
            {
                marksQuery = marksQuery.Where(x => filter.ExamIds.Contains(x.e.Id));
            }
            if (filter.AcademicYearId.HasValue)
            {
                marksQuery = marksQuery.Where(x => x.e.AcademicYearId == filter.AcademicYearId.Value);
            }
            if (filter.ExamTypeId.HasValue)
            {
                marksQuery = marksQuery.Where(x => x.e.ExamTypeId == filter.ExamTypeId.Value);
            }
            if (filter.FromDate.HasValue)
            {
                marksQuery = marksQuery.Where(x => x.e.StartDate >= filter.FromDate.Value);
            }
            if (filter.ToDate.HasValue)
            {
                marksQuery = marksQuery.Where(x => x.e.EndDate <= filter.ToDate.Value);
            }

            var rows = await marksQuery.ToListAsync();

            var result = new StudentSubjectGrowthDto
            {
                StudentId = studentId
            };

            var subjectGroups = rows
                .GroupBy(x => new { x.s.Id, x.s.Name });

            foreach (var subjectGroup in subjectGroups)
            {
                var series = new StudentSubjectGrowthSeriesDto
                {
                    SubjectId = subjectGroup.Key.Id,
                    SubjectName = subjectGroup.Key.Name
                };

                var examGroups = subjectGroup
                    .GroupBy(x => new { x.e.Id, x.e.Name, x.e.StartDate })
                    .OrderBy(g => g.Key.StartDate);

                foreach (var examGroup in examGroups)
                {
                    var obtained = examGroup.Sum(r => r.m.MarksObtained ?? 0);
                    var max = examGroup.Sum(r => r.esc.MaxMarks ?? 0);
                    var point = new StudentSubjectGrowthPointDto
                    {
                        ExamId = examGroup.Key.Id,
                        ExamName = examGroup.Key.Name,
                        ExamDate = examGroup.Key.StartDate,
                        MarksObtained = obtained,
                        MaxMarks = max,
                        Percentage = max > 0 ? Math.Round(obtained * 100m / max, 2) : null
                    };
                    series.Points.Add(point);
                }

                result.Subjects.Add(series);
            }

            return result;
        }

        public async Task<ClassExamDistributionDto?> GetClassExamDistributionAsync(Guid examId, Guid classId, Guid divisionId)
        {
            _logger.LogInformation("ExamAnalytics: class exam distribution exam {ExamId}, class {ClassId}, division {DivisionId}", examId, classId, divisionId);

            var studentsQuery = _context.students.AsNoTracking()
                .Where(s => s.ClassId == classId && s.DivisionId == divisionId && (s.IsActive == true));

            var students = await studentsQuery
                .Select(s => new { s.Id })
                .ToListAsync();

            if (students.Count == 0)
            {
                return null;
            }

            var studentIds = students.Select(s => s.Id).ToList();

            var results = await _context.student_results.AsNoTracking()
                .Where(sr => sr.ExamId == examId && sr.StudentId.HasValue && studentIds.Contains(sr.StudentId.Value))
                .ToListAsync();

            if (results.Count == 0)
            {
                return null;
            }

            var dto = new ClassExamDistributionDto
            {
                ExamId = examId,
                ClassId = classId,
                DivisionId = divisionId,
                TotalStudents = studentIds.Count,
                PassedCount = results.Count(r => r.Result == "PASS"),
                FailedCount = results.Count(r => r.Result == "FAIL")
            };

            dto.GradeBuckets = results
                .GroupBy(r => r.Grade ?? string.Empty)
                .Select(g => new GradeBucketDto
                {
                    Grade = g.Key,
                    Count = g.Count()
                })
                .OrderByDescending(g => g.Grade)
                .ToList();

            var subjectMarksQuery =
                from m in _context.marks.AsNoTracking()
                join esc in _context.exam_subject_components.AsNoTracking() on m.ExamSubjectComponentId equals esc.Id
                join es in _context.exam_subjects.AsNoTracking() on esc.ExamSubjectId equals es.Id
                join s in _context.subjects.AsNoTracking() on es.SubjectId equals s.Id
                where es.ExamId == examId && studentIds.Contains(m.StudentId ?? Guid.Empty)
                select new { m, esc, es, s };

            var subjectRows = await subjectMarksQuery.ToListAsync();

            dto.SubjectAverages = subjectRows
                .GroupBy(r => new { r.s.Id, r.s.Name })
                .Select(g =>
                {
                    var totalObtained = g.Sum(x => x.m.MarksObtained ?? 0);
                    var totalMax = g.Sum(x => x.esc.MaxMarks ?? 0);
                    var count = g.Select(x => x.m.StudentId).Distinct().Count();
                    decimal? avgMarks = null;
                    decimal? avgPercentage = null;
                    if (count > 0 && totalMax > 0)
                    {
                        avgMarks = Math.Round(totalObtained / count, 2);
                        avgPercentage = Math.Round((totalObtained / totalMax) * 100m, 2);
                    }

                    return new SubjectAverageDto
                    {
                        SubjectId = g.Key.Id,
                        SubjectName = g.Key.Name,
                        AverageMarks = avgMarks,
                        MaxMarks = count > 0 ? totalMax / count : null,
                        AveragePercentage = avgPercentage
                    };
                })
                .OrderBy(x => x.SubjectName)
                .ToList();

            return dto;
        }

        public async Task<SubjectStudentComparisonDto?> GetSubjectStudentComparisonAsync(Guid examId, Guid subjectId)
        {
            _logger.LogInformation("ExamAnalytics: subject student comparison exam {ExamId}, subject {SubjectId}", examId, subjectId);

            var query =
                from m in _context.marks.AsNoTracking()
                join esc in _context.exam_subject_components.AsNoTracking() on m.ExamSubjectComponentId equals esc.Id
                join es in _context.exam_subjects.AsNoTracking() on esc.ExamSubjectId equals es.Id
                join s in _context.subjects.AsNoTracking() on es.SubjectId equals s.Id
                join st in _context.students.AsNoTracking() on m.StudentId equals st.Id
                where es.ExamId == examId && es.SubjectId == subjectId
                select new { m, esc, es, s, st };

            var rows = await query.ToListAsync();
            if (rows.Count == 0)
            {
                return null;
            }

            var dto = new SubjectStudentComparisonDto
            {
                ExamId = examId,
                SubjectId = subjectId,
                SubjectName = rows[0].s.Name
            };

            dto.Students = rows
                .GroupBy(r => new { r.st.Id, r.st.FirstName, r.st.MiddleName, r.st.LastName, r.st.RollNumber })
                .Select(g =>
                {
                    var obtained = g.Sum(x => x.m.MarksObtained ?? 0);
                    var max = g.Sum(x => x.esc.MaxMarks ?? 0);
                    var fullName = string.Join(" ", new[]
                    {
                        g.Key.FirstName,
                        g.Key.MiddleName,
                        g.Key.LastName
                    }.Where(x => !string.IsNullOrWhiteSpace(x))).Trim();

                    return new SubjectStudentLineDto
                    {
                        StudentId = g.Key.Id,
                        StudentName = fullName,
                        RollNumber = g.Key.RollNumber,
                        MarksObtained = obtained,
                        MaxMarks = max,
                        Percentage = max > 0 ? Math.Round(obtained * 100m / max, 2) : null
                    };
                })
                .OrderByDescending(x => x.Percentage)
                .ToList();

            return dto;
        }

        public Task<IReadOnlyList<TeacherExamPerformanceDto>> GetTeacherExamPerformanceAsync(Guid teacherId, TeacherExamFilterDto filter)
        {
            throw new NotImplementedException("Teacher exam performance analytics is not implemented yet.");
        }

        public Task<IReadOnlyList<TeacherSubjectPerformanceDto>> GetTeacherSubjectPerformanceAsync(Guid teacherId, TeacherSubjectFilterDto filter)
        {
            throw new NotImplementedException("Teacher subject performance analytics is not implemented yet.");
        }

        public Task<IReadOnlyList<TeacherComparisonPointDto>> CompareTeachersAsync(TeacherComparisonFilterDto filter)
        {
            throw new NotImplementedException("Teacher comparison analytics is not implemented yet.");
        }
    }
}

