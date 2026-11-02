using Application.Validation.Common;
using Application.ViewModels.Exam;
using Application.ViewModels.ExamComponentType;
using Application.ViewModels.ExamSubject;
using Application.ViewModels.ExamSubjectComponent;
using Application.ViewModels.ExamType;
using Application.ViewModels.GradeMaster;
using Application.ViewModels.Mark;
using FluentValidation;

namespace Application.Validation.Exams
{
    public class ExamRequestValidator : AbstractValidator<ExamRequest>
    {
        public ExamRequestValidator()
        {
            RuleFor(x => x.Name).RequiredName(100);
            RuleFor(x => x.Status).MaximumLength(20);
            RuleFor(x => x.EndDate)
                .GreaterThanOrEqualTo(x => x.StartDate!.Value).WithMessage("The exam cannot end before it starts.")
                .When(x => x.StartDate.HasValue && x.EndDate.HasValue);
        }
    }

    public class ExamSubjectRequestValidator : AbstractValidator<ExamSubjectRequest>
    {
        public ExamSubjectRequestValidator()
        {
            RuleFor(x => x.ExamId).NotNull();
            RuleFor(x => x.SubjectId).NotNull();
            RuleFor(x => x.TotalMaxMarks).GreaterThan(0).When(x => x.TotalMaxMarks.HasValue);
            RuleFor(x => x.TotalPassMarks).GreaterThanOrEqualTo(0).When(x => x.TotalPassMarks.HasValue);
            RuleFor(x => x.TotalPassMarks)
                .LessThanOrEqualTo(x => x.TotalMaxMarks).WithMessage("Pass marks cannot exceed the maximum marks.")
                .When(x => x.TotalPassMarks.HasValue && x.TotalMaxMarks.HasValue);
            RuleFor(x => x.EndTime)
                .GreaterThan(x => x.StartTime!.Value).WithMessage("The paper must end after it starts.")
                .When(x => x.StartTime.HasValue && x.EndTime.HasValue);
            RuleFor(x => x.DurationMinutes).InclusiveBetween(1, 600).When(x => x.DurationMinutes.HasValue);
        }
    }

    public class ExamSubjectComponentRequestValidator : AbstractValidator<ExamSubjectComponentRequest>
    {
        public ExamSubjectComponentRequestValidator()
        {
            RuleFor(x => x.ExamSubjectId).NotNull();
            RuleFor(x => x.MaxMarks).NotNull().GreaterThan(0);
            RuleFor(x => x.PassMarks).GreaterThanOrEqualTo(0).When(x => x.PassMarks.HasValue);
            RuleFor(x => x.PassMarks)
                .LessThanOrEqualTo(x => x.MaxMarks).WithMessage("Pass marks cannot exceed the maximum marks.")
                .When(x => x.PassMarks.HasValue && x.MaxMarks.HasValue);
            RuleFor(x => x.WeightagePercent).InclusiveBetween(0, 100).When(x => x.WeightagePercent.HasValue);
        }
    }

    public class MarkRequestValidator : AbstractValidator<MarkRequest>
    {
        public MarkRequestValidator()
        {
            RuleFor(x => x.ExamSubjectComponentId).NotNull();
            RuleFor(x => x.StudentId).NotNull();
            RuleFor(x => x.MarksObtained).GreaterThanOrEqualTo(0).When(x => x.MarksObtained.HasValue);
            RuleFor(x => x.Remark).MaximumLength(500);
        }
    }

    /// <summary>A band covers part of 0-100 %, lower bound first. Overlaps with other bands are checked by GradeMasterService.</summary>
    public class GradeMasterRequestValidator : AbstractValidator<GradeMasterRequest>
    {
        public GradeMasterRequestValidator()
        {
            RuleFor(x => x.Grade).RequiredName(5);
            RuleFor(x => x.MinPercent).NotNull().InclusiveBetween(0, 100);
            RuleFor(x => x.MaxPercent).NotNull().InclusiveBetween(0, 100);
            RuleFor(x => x.MaxPercent)
                .GreaterThanOrEqualTo(x => x.MinPercent!.Value).WithMessage("The upper bound must not be below the lower bound.")
                .When(x => x.MinPercent.HasValue && x.MaxPercent.HasValue);
        }
    }

    public class ExamTypeRequestValidator : AbstractValidator<ExamTypeRequest>
    {
        public ExamTypeRequestValidator() => RuleFor(x => x.Name).RequiredName(100);
    }

    public class ExamComponentTypeRequestValidator : AbstractValidator<ExamComponentTypeRequest>
    {
        public ExamComponentTypeRequestValidator()
        {
            RuleFor(x => x.Name).RequiredName(100);
            RuleFor(x => x.Code).MaximumLength(20);
        }
    }
}
