using System;
using System.Collections.Generic;
using System.Linq;

namespace Application.Exams
{
    /// <summary>One marked part of an exam subject (theory, practical, oral, ...).</summary>
    public sealed record ComponentScheme(Guid ComponentId, decimal MaxMarks, decimal? PassMarks);

    /// <summary>A subject of an exam with its components. TotalPassMarks, when set, is the subject's pass line.</summary>
    public sealed record SubjectScheme(Guid ExamSubjectId, decimal? TotalMaxMarks, decimal? TotalPassMarks, IReadOnlyList<ComponentScheme> Components);

    public sealed record MarkEntry(Guid StudentId, Guid ComponentId, decimal? MarksObtained, bool IsAbsent, bool IsExempt);

    public sealed record GradeBand(string Grade, decimal MinPercent, decimal MaxPercent);

    public sealed record StudentOutcome(Guid StudentId, decimal TotalMarks, decimal MaxMarks, decimal Percentage, string Result, string? Grade, int? Rank);

    /// <summary>
    /// Turns the marks of one exam into each student's result. Pure calculation, no database, so the rules are
    /// easy to read and to test:
    ///   - a subject's marks are the sum of its components; absent counts as 0; exempt components are left out
    ///     of both marks and maximum (a fully exempt subject is left out entirely);
    ///   - a subject is passed when its marks reach TotalPassMarks, or, without one, when every component
    ///     reaches its own PassMarks (no pass marks defined = passed);
    ///   - percentage = total / maximum x 100, rounded to 2 decimals;
    ///   - PASS when every subject is passed, otherwise FAIL; INCOMPLETE when a mark is still missing
    ///     (no grade and no rank until the marks are complete);
    ///   - the grade is the band containing the percentage (the highest matching band wins on a shared edge);
    ///   - ranks follow percentage, ties share a rank and the next rank is skipped (1, 2, 2, 4).
    /// </summary>
    public static class ResultCalculator
    {
        public const string Pass = "PASS";
        public const string Fail = "FAIL";
        public const string Incomplete = "INCOMPLETE";

        public static IReadOnlyList<StudentOutcome> Calculate(
            IReadOnlyList<SubjectScheme> subjects, IEnumerable<MarkEntry> marks, IReadOnlyList<GradeBand> grades)
        {
            var byStudent = marks.GroupBy(m => m.StudentId)
                .ToDictionary(g => g.Key, g => g.GroupBy(m => m.ComponentId).ToDictionary(c => c.Key, c => c.Last()));

            var outcomes = byStudent.Select(s => Evaluate(s.Key, s.Value, subjects, grades)).ToList();
            return Rank(outcomes);
        }

        private static StudentOutcome Evaluate(Guid studentId, IReadOnlyDictionary<Guid, MarkEntry> marks,
            IReadOnlyList<SubjectScheme> subjects, IReadOnlyList<GradeBand> grades)
        {
            decimal total = 0, maximum = 0;
            var allPassed = true;
            var complete = true;

            foreach (var subject in subjects)
            {
                decimal obtained = 0, subjectMax = 0;
                var componentsPassed = true;
                var anyCounted = false;

                foreach (var component in subject.Components)
                {
                    if (!marks.TryGetValue(component.ComponentId, out var mark))
                    {
                        complete = false;
                        continue;
                    }
                    if (mark.IsExempt)
                        continue;

                    anyCounted = true;
                    var score = mark.IsAbsent ? 0 : mark.MarksObtained ?? 0;
                    obtained += score;
                    subjectMax += component.MaxMarks;
                    if (component.PassMarks is { } componentPass && score < componentPass)
                        componentsPassed = false;
                }

                if (!anyCounted)
                    continue; // fully exempt (or not marked at all, which already made the result incomplete)

                var hasExemptPart = subject.Components.Any(c => marks.TryGetValue(c.ComponentId, out var m) && m.IsExempt);
                if (!hasExemptPart && subject.TotalMaxMarks is { } declaredMax and > 0)
                    subjectMax = declaredMax;

                var passed = subject.TotalPassMarks is { } passLine ? obtained >= passLine : componentsPassed;
                allPassed &= passed;
                total += obtained;
                maximum += subjectMax;
            }

            var percentage = maximum > 0 ? Math.Round(total / maximum * 100, 2, MidpointRounding.AwayFromZero) : 0;
            var result = !complete ? Incomplete : allPassed ? Pass : Fail;
            var grade = complete ? GradeFor(percentage, grades) : null;
            return new StudentOutcome(studentId, total, maximum, percentage, result, grade, Rank: null);
        }

        public static string? GradeFor(decimal percentage, IReadOnlyList<GradeBand> grades) =>
            grades.Where(g => g.MinPercent <= percentage && percentage <= g.MaxPercent)
                .OrderByDescending(g => g.MinPercent)
                .Select(g => g.Grade)
                .FirstOrDefault();

        private static IReadOnlyList<StudentOutcome> Rank(List<StudentOutcome> outcomes)
        {
            var ranked = outcomes.Where(o => o.Result != Incomplete).OrderByDescending(o => o.Percentage).ToList();
            var ranks = new Dictionary<Guid, int>();
            for (var i = 0; i < ranked.Count; i++)
            {
                var rank = i > 0 && ranked[i].Percentage == ranked[i - 1].Percentage ? ranks[ranked[i - 1].StudentId] : i + 1;
                ranks[ranked[i].StudentId] = rank;
            }

            return outcomes
                .Select(o => ranks.TryGetValue(o.StudentId, out var rank) ? o with { Rank = rank } : o)
                .OrderBy(o => o.Rank ?? int.MaxValue)
                .ToList();
        }
    }
}
