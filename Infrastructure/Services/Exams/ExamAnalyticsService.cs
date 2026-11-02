using Application.Exams;
using Application.Abstractions.Services;
using Application.ViewModels.ExamAnalytics;
using Infrastructure.Persistence.Postgres;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Infrastructure.Services.Exams
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

            var exam = await _context.Exams.FirstOrDefaultAsync(e => e.Id == examId);
            if (exam == null) return null;

            var studentResult = await _context.StudentResults
                .AsNoTracking()
                .FirstOrDefaultAsync(sr => sr.StudentId == studentId && sr.ExamId == examId);

            var componentsQuery =
                from esc in _context.ExamSubjectComponents
                join es in _context.ExamSubjects on esc.ExamSubjectId equals es.Id
                join s in _context.Subjects on es.SubjectId equals s.Id
                where es.ExamId == examId
                select new
                {
                    esc.Id,
                    SubjectId = s.Id,
                    SubjectName = s.Name,
                    esc.MaxMarks
                };

            var marksQuery =
                from m in _context.Marks
                join esc in _context.ExamSubjectComponents on m.ExamSubjectComponentId equals esc.Id
                join es in _context.ExamSubjects on esc.ExamSubjectId equals es.Id
                join s in _context.Subjects on es.SubjectId equals s.Id
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
                from sr in _context.StudentResults.AsNoTracking()
                join e in _context.Exams.AsNoTracking() on sr.ExamId equals e.Id
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
                from m in _context.Marks.AsNoTracking()
                join esc in _context.ExamSubjectComponents.AsNoTracking() on m.ExamSubjectComponentId equals esc.Id
                join es in _context.ExamSubjects.AsNoTracking() on esc.ExamSubjectId equals es.Id
                join e in _context.Exams.AsNoTracking() on es.ExamId equals e.Id
                join s in _context.Subjects.AsNoTracking() on es.SubjectId equals s.Id
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

            var studentsQuery = _context.Students.AsNoTracking()
                .Where(s => s.ClassId == classId && s.DivisionId == divisionId && (s.IsActive == true));

            var students = await studentsQuery
                .Select(s => new { s.Id })
                .ToListAsync();

            if (students.Count == 0)
            {
                return null;
            }

            var studentIds = students.Select(s => s.Id).ToList();

            var results = await _context.StudentResults.AsNoTracking()
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
                from m in _context.Marks.AsNoTracking()
                join esc in _context.ExamSubjectComponents.AsNoTracking() on m.ExamSubjectComponentId equals esc.Id
                join es in _context.ExamSubjects.AsNoTracking() on esc.ExamSubjectId equals es.Id
                join s in _context.Subjects.AsNoTracking() on es.SubjectId equals s.Id
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
                from m in _context.Marks.AsNoTracking()
                join esc in _context.ExamSubjectComponents.AsNoTracking() on m.ExamSubjectComponentId equals esc.Id
                join es in _context.ExamSubjects.AsNoTracking() on esc.ExamSubjectId equals es.Id
                join s in _context.Subjects.AsNoTracking() on es.SubjectId equals s.Id
                join st in _context.Students.AsNoTracking() on m.StudentId equals st.Id
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

        // ---- Teacher performance ----
        // A teacher teaches an exam subject when one of their teacher-subject assignments matches the subject and the
        // exam's class (an assignment without class / division covers all of them); their students are those of the
        // assigned division. Results come from ResultCalculator, the same rules as the students' own results.

        public async Task<IReadOnlyList<TeacherExamPerformanceDto>> GetTeacherExamPerformanceAsync(Guid teacherId, TeacherExamFilterDto filter)
        {
            var assignments = await AssignmentsAsync(teacherId);
            var exams = await _context.Exams.AsNoTracking()
                .Where(e => (filter.AcademicYearId == null || e.AcademicYearId == filter.AcademicYearId)
                            && (filter.ClassId == null || e.ClassId == filter.ClassId))
                .OrderBy(e => e.StartDate)
                .ToListAsync();

            var result = new List<TeacherExamPerformanceDto>();
            foreach (var exam in exams)
            {
                var stats = await TeacherStatsAsync(exam, assignments, filter.SubjectId);
                if (stats.StudentCount == 0)
                    continue;
                result.Add(new TeacherExamPerformanceDto
                {
                    TeacherId = teacherId, ExamId = exam.Id, ExamName = exam.Name,
                    AveragePercentage = stats.AveragePercentage, StudentCount = stats.StudentCount,
                    PassedCount = stats.Passed, FailedCount = stats.Failed,
                });
            }
            return result;
        }

        public async Task<IReadOnlyList<TeacherSubjectPerformanceDto>> GetTeacherSubjectPerformanceAsync(Guid teacherId, TeacherSubjectFilterDto filter)
        {
            var assignments = await AssignmentsAsync(teacherId);
            var subjectIds = assignments.Where(a => a.SubjectId != null).Select(a => a.SubjectId!.Value).Distinct().ToList();
            var subjectNames = await _context.Subjects.AsNoTracking().Where(sub => subjectIds.Contains(sub.Id)).ToDictionaryAsync(sub => sub.Id, sub => sub.Name);
            var exams = await _context.Exams.AsNoTracking()
                .Where(e => (filter.AcademicYearId == null || e.AcademicYearId == filter.AcademicYearId)
                            && (filter.FromDate == null || e.StartDate >= filter.FromDate)
                            && (filter.ToDate == null || e.StartDate <= filter.ToDate))
                .OrderBy(e => e.StartDate)
                .ToListAsync();

            var result = new List<TeacherSubjectPerformanceDto>();
            foreach (var subjectId in subjectIds)
            {
                var dto = new TeacherSubjectPerformanceDto { TeacherId = teacherId, SubjectId = subjectId, SubjectName = subjectNames.GetValueOrDefault(subjectId) };
                foreach (var exam in exams)
                {
                    var stats = await TeacherStatsAsync(exam, assignments, subjectId);
                    if (stats.StudentCount == 0)
                        continue;
                    dto.Points.Add(new TeacherSubjectPerformancePointDto
                    {
                        ExamId = exam.Id, ExamName = exam.Name, ExamDate = exam.StartDate,
                        AverageMarks = stats.AverageMarks, MaxMarks = stats.MaxMarks,
                    });
                }
                if (dto.Points.Count > 0)
                    result.Add(dto);
            }
            return result;
        }

        public async Task<IReadOnlyList<TeacherComparisonPointDto>> CompareTeachersAsync(TeacherComparisonFilterDto filter)
        {
            var exam = await _context.Exams.AsNoTracking().FirstOrDefaultAsync(e => e.Id == filter.ExamId)
                       ?? throw new Application.Exceptions.BaseServiceException($"Exam {filter.ExamId} not found.", Application.Common.ExceptionCodes.ItemNotFound);
            var classId = filter.ClassId ?? exam.ClassId;
            var teacherIds = await _context.TeacherSubjectMaps.AsNoTracking()
                .Where(a => a.StaffId != null && (a.ClassId == null || classId == null || a.ClassId == classId)
                            && (filter.SubjectId == null || a.SubjectId == filter.SubjectId)
                            && (filter.DivisionId == null || a.DivisionId == null || a.DivisionId == filter.DivisionId))
                .Select(a => a.StaffId!.Value).Distinct().ToListAsync();
            var names = await _context.Staff.AsNoTracking().Where(st => teacherIds.Contains(st.Id))
                .ToDictionaryAsync(st => st.Id, st => $"{st.FirstName} {st.LastName}".Trim());

            var result = new List<TeacherComparisonPointDto>();
            foreach (var teacherId in teacherIds)
            {
                var assignments = (await AssignmentsAsync(teacherId))
                    .Where(a => filter.DivisionId == null || a.DivisionId == null || a.DivisionId == filter.DivisionId).ToList();
                var stats = await TeacherStatsAsync(exam, assignments, filter.SubjectId, filter.DivisionId);
                if (stats.StudentCount == 0)
                    continue;
                result.Add(new TeacherComparisonPointDto
                {
                    TeacherId = teacherId, TeacherName = names.GetValueOrDefault(teacherId),
                    AveragePercentage = stats.AveragePercentage, StudentCount = stats.StudentCount,
                    PassedCount = stats.Passed, FailedCount = stats.Failed,
                });
            }
            return result.OrderByDescending(r => r.AveragePercentage).ToList();
        }

        private sealed record TeacherStats(int StudentCount, int Passed, int Failed, decimal? AveragePercentage, decimal? AverageMarks, decimal? MaxMarks)
        {
            public static readonly TeacherStats None = new(0, 0, 0, null, null, null);
        }

        private Task<List<TeacherSubjectMap>> AssignmentsAsync(Guid teacherId) =>
            _context.TeacherSubjectMaps.AsNoTracking().Where(a => a.StaffId == teacherId && a.SubjectId != null).ToListAsync();

        /// <summary>Results of the teacher's students in the exam subjects the teacher teaches (optionally one subject / division).</summary>
        private async Task<TeacherStats> TeacherStatsAsync(Exam exam, IReadOnlyList<TeacherSubjectMap> assignments, Guid? subjectId, Guid? divisionId = null)
        {
            var taught = assignments.Where(a => (subjectId == null || a.SubjectId == subjectId)
                                                && (a.ClassId == null || exam.ClassId == null || a.ClassId == exam.ClassId)).ToList();
            if (taught.Count == 0)
                return TeacherStats.None;

            var taughtSubjects = taught.Select(a => a.SubjectId!.Value).Distinct().ToList();
            var examSubjects = await _context.ExamSubjects.AsNoTracking()
                .Where(es => es.ExamId == exam.Id && es.SubjectId != null && taughtSubjects.Contains(es.SubjectId.Value)).ToListAsync();
            if (examSubjects.Count == 0)
                return TeacherStats.None;
            var examSubjectIds = examSubjects.Select(es => es.Id).ToList();
            var components = await _context.ExamSubjectComponents.AsNoTracking()
                .Where(c => c.ExamSubjectId != null && examSubjectIds.Contains(c.ExamSubjectId.Value)).ToListAsync();
            var componentIds = components.Select(c => c.Id).ToList();
            var marks = await _context.Marks.AsNoTracking()
                .Where(m => m.ExamSubjectComponentId != null && m.StudentId != null && componentIds.Contains(m.ExamSubjectComponentId.Value)).ToListAsync();

            // Only the students of the divisions the teacher is assigned to (no division on the assignment = all).
            var studentIds = marks.Select(m => m.StudentId!.Value).Distinct().ToList();
            var divisions = await _context.Students.AsNoTracking().Where(st => studentIds.Contains(st.Id)).ToDictionaryAsync(st => st.Id, st => st.DivisionId);
            bool Teaches(Guid studentId, Guid examSubjectId)
            {
                var subject = examSubjects.First(es => es.Id == examSubjectId).SubjectId;
                var division = divisions.GetValueOrDefault(studentId);
                return (divisionId == null || division == divisionId)
                       && taught.Any(a => a.SubjectId == subject && (a.DivisionId == null || a.DivisionId == division));
            }

            var componentSubject = components.ToDictionary(c => c.Id, c => c.ExamSubjectId!.Value);
            var entries = marks
                .Where(m => Teaches(m.StudentId!.Value, componentSubject[m.ExamSubjectComponentId!.Value]))
                .Select(m => new MarkEntry(m.StudentId!.Value, m.ExamSubjectComponentId!.Value, m.MarksObtained,
                    IsAbsent: m.IsAbsent == true || m.IsMedical == true, IsExempt: m.IsExempt == true))
                .ToList();
            if (entries.Count == 0)
                return TeacherStats.None;

            var schemes = examSubjects.Select(es => new SubjectScheme(es.Id, es.TotalMaxMarks, es.TotalPassMarks,
                components.Where(c => c.ExamSubjectId == es.Id).Select(c => new ComponentScheme(c.Id, c.MaxMarks ?? 0, c.PassMarks)).ToList())).ToList();
            var outcomes = ResultCalculator.Calculate(schemes, entries, Array.Empty<GradeBand>())
                .Where(o => o.Result != ResultCalculator.Incomplete).ToList();
            if (outcomes.Count == 0)
                return TeacherStats.None;

            return new TeacherStats(
                outcomes.Count,
                outcomes.Count(o => o.Result == ResultCalculator.Pass),
                outcomes.Count(o => o.Result == ResultCalculator.Fail),
                Math.Round(outcomes.Average(o => o.Percentage), 2),
                Math.Round(outcomes.Average(o => o.TotalMarks), 2),
                outcomes.Max(o => o.MaxMarks));
        }
    }
}

