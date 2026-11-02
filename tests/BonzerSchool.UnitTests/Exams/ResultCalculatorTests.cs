using Application.Exams;

namespace BonzerSchool.UnitTests.Exams;

public class ResultCalculatorTests
{
    private static readonly Guid MathsTheory = Guid.NewGuid(), MathsPractical = Guid.NewGuid(), English = Guid.NewGuid();
    private static readonly Guid Asha = Guid.NewGuid(), Ravi = Guid.NewGuid(), Neha = Guid.NewGuid(), Om = Guid.NewGuid();

    // Maths: theory 80 (pass 28) + practical 20 (pass 7), subject pass line 35 of 100. English: 100, pass 35.
    private static readonly IReadOnlyList<SubjectScheme> Subjects = new[]
    {
        new SubjectScheme(Guid.NewGuid(), 100, 35, new[] { new ComponentScheme(MathsTheory, 80, 28), new ComponentScheme(MathsPractical, 20, 7) }),
        new SubjectScheme(Guid.NewGuid(), 100, 35, new[] { new ComponentScheme(English, 100, 35) }),
    };

    private static readonly IReadOnlyList<GradeBand> Grades = new[]
    {
        new GradeBand("A1", 91, 100), new GradeBand("A2", 81, 90.99m), new GradeBand("B1", 71, 80.99m),
        new GradeBand("B2", 61, 70.99m), new GradeBand("C", 33, 60.99m), new GradeBand("E", 0, 32.99m),
    };

    private static IEnumerable<MarkEntry> Marks(Guid student, decimal? theory, decimal? practical, decimal? english,
        bool absentEnglish = false, bool exemptPractical = false)
    {
        if (theory != null) yield return new MarkEntry(student, MathsTheory, theory, false, false);
        if (practical != null || exemptPractical) yield return new MarkEntry(student, MathsPractical, practical, false, exemptPractical);
        if (english != null || absentEnglish) yield return new MarkEntry(student, English, english, absentEnglish, false);
    }

    private static StudentOutcome For(IReadOnlyList<StudentOutcome> outcomes, Guid student) => outcomes.Single(o => o.StudentId == student);

    [Fact]
    public void Totals_percentage_grade_and_pass_are_calculated()
    {
        var outcome = For(ResultCalculator.Calculate(Subjects, Marks(Asha, 70, 18, 85), Grades), Asha);

        Assert.Equal(173, outcome.TotalMarks);
        Assert.Equal(200, outcome.MaxMarks);
        Assert.Equal(86.5m, outcome.Percentage);
        Assert.Equal("A2", outcome.Grade);
        Assert.Equal(ResultCalculator.Pass, outcome.Result);
        Assert.Equal(1, outcome.Rank);
    }

    [Fact]
    public void A_subject_below_its_pass_line_fails_the_exam_even_with_a_good_total()
    {
        var outcome = For(ResultCalculator.Calculate(Subjects, Marks(Ravi, 20, 10, 95), Grades), Ravi);

        Assert.Equal(ResultCalculator.Fail, outcome.Result); // Maths 30 < 35
        Assert.Equal(62.5m, outcome.Percentage);
    }

    [Fact]
    public void Absent_counts_as_zero_and_fails_the_subject()
    {
        var outcome = For(ResultCalculator.Calculate(Subjects, Marks(Neha, 70, 18, english: null, absentEnglish: true), Grades), Neha);

        Assert.Equal(88, outcome.TotalMarks);
        Assert.Equal(ResultCalculator.Fail, outcome.Result);
    }

    [Fact]
    public void An_exempt_component_is_left_out_of_marks_and_maximum()
    {
        var outcome = For(ResultCalculator.Calculate(Subjects, Marks(Om, 72, practical: null, 90, exemptPractical: true), Grades), Om);

        Assert.Equal(162, outcome.TotalMarks);
        Assert.Equal(180, outcome.MaxMarks); // 80 theory + 100 English
        Assert.Equal(90m, outcome.Percentage);
    }

    [Fact]
    public void Missing_marks_make_the_result_incomplete_without_grade_or_rank()
    {
        var outcome = For(ResultCalculator.Calculate(Subjects, Marks(Asha, 70, practical: null, 85), Grades), Asha);

        Assert.Equal(ResultCalculator.Incomplete, outcome.Result);
        Assert.Null(outcome.Grade);
        Assert.Null(outcome.Rank);
    }

    [Fact]
    public void Ties_share_a_rank_and_the_next_rank_is_skipped()
    {
        var marks = Marks(Asha, 70, 18, 85)    // 86.5
            .Concat(Marks(Ravi, 70, 18, 85))    // 86.5
            .Concat(Marks(Neha, 60, 15, 80))    // 77.5
            .Concat(Marks(Om, 70, practical: null, 85)); // incomplete

        var outcomes = ResultCalculator.Calculate(Subjects, marks, Grades);

        Assert.Equal(1, For(outcomes, Asha).Rank);
        Assert.Equal(1, For(outcomes, Ravi).Rank);
        Assert.Equal(3, For(outcomes, Neha).Rank);
        Assert.Null(For(outcomes, Om).Rank);
    }

    [Fact]
    public void Without_a_subject_pass_line_every_component_must_pass()
    {
        var subjects = new[] { new SubjectScheme(Guid.NewGuid(), null, null,
            new[] { new ComponentScheme(MathsTheory, 80, 28), new ComponentScheme(MathsPractical, 20, 7) }) };

        var outcome = For(ResultCalculator.Calculate(subjects, Marks(Asha, 75, 5, null), Grades), Asha);

        Assert.Equal(ResultCalculator.Fail, outcome.Result); // practical 5 < 7 although 80 of 100 overall
    }

    [Theory]
    [InlineData(100, "A1")]
    [InlineData(91, "A1")]
    [InlineData(90.99, "A2")]
    [InlineData(33, "C")]
    [InlineData(32.99, "E")]
    [InlineData(0, "E")]
    public void Grade_bands_are_inclusive(decimal percentage, string grade) =>
        Assert.Equal(grade, ResultCalculator.GradeFor(percentage, Grades));
}
