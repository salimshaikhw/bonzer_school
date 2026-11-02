using Application.Validation.Common;
using Application.ViewModels.Class;
using Application.ViewModels.ClassSubjectMap;
using Application.ViewModels.ClassTeacherMap;
using Application.ViewModels.Division;
using Application.ViewModels.Homework;
using Application.ViewModels.Subject;
using Application.ViewModels.TeacherSubjectMap;
using Application.ViewModels.Timetable;
using FluentValidation;

namespace Application.Validation.Academics
{
    public class ClassRequestValidator : AbstractValidator<ClassRequest>
    {
        public ClassRequestValidator()
        {
            RuleFor(x => x.BranchId).NotNull();
            RuleFor(x => x.Name).RequiredName(50);
        }
    }

    public class DivisionRequestValidator : AbstractValidator<DivisionRequest>
    {
        public DivisionRequestValidator()
        {
            RuleFor(x => x.ClassId).NotNull();
            RuleFor(x => x.Name).RequiredName(100);
            RuleFor(x => x.Code).MaximumLength(50);
            RuleFor(x => x.AvailableSeats).InclusiveBetween(0, 1000).When(x => x.AvailableSeats.HasValue);
        }
    }

    public class SubjectRequestValidator : AbstractValidator<SubjectRequest>
    {
        public SubjectRequestValidator()
        {
            RuleFor(x => x.Name).RequiredName(100);
            RuleFor(x => x.Code).MaximumLength(50);
        }
    }

    public class ClassSubjectMapRequestValidator : AbstractValidator<ClassSubjectMapRequest>
    {
        public ClassSubjectMapRequestValidator()
        {
            RuleFor(x => x.ClassId).NotNull();
            RuleFor(x => x.SubjectId).NotNull();
            RuleFor(x => x.AcademicYear).MaximumLength(20);
        }
    }

    public class ClassTeacherMapRequestValidator : AbstractValidator<ClassTeacherMapRequest>
    {
        public ClassTeacherMapRequestValidator()
        {
            RuleFor(x => x.ClassId).NotNull();
            RuleFor(x => x.StaffId).NotNull();
            RuleFor(x => x.AcademicYear).MaximumLength(20);
        }
    }

    public class TeacherSubjectMapRequestValidator : AbstractValidator<TeacherSubjectMapRequest>
    {
        public TeacherSubjectMapRequestValidator()
        {
            RuleFor(x => x.StaffId).NotNull();
            RuleFor(x => x.SubjectId).NotNull();
            RuleFor(x => x.AcademicYear).MaximumLength(20);
        }
    }

    /// <summary>
    /// DayOfWeek accepts 0-7 because existing data and clients use both 0-6 (Sunday = 0) and 1-7 (Monday = 1);
    /// periods are numbered from 1.
    /// </summary>
    public class TimetableRequestValidator : AbstractValidator<TimetableRequest>
    {
        public TimetableRequestValidator()
        {
            RuleFor(x => x.ClassId).NotNull();
            RuleFor(x => x.SubjectId).NotNull();
            RuleFor(x => x.AcademicYearId).NotNull();
            RuleFor(x => x.DayOfWeek).NotNull().InclusiveBetween(0, 7);
            RuleFor(x => x.PeriodNo).NotNull().InclusiveBetween(1, 15);
        }
    }

    public class HomeworkRequestValidator : AbstractValidator<HomeworkRequest>
    {
        public HomeworkRequestValidator()
        {
            RuleFor(x => x.ClassId).NotNull();
            RuleFor(x => x.SubjectId).NotNull();
            RuleFor(x => x.Description).RequiredName(4000);
            RuleFor(x => x.DueDate).NotNull();
        }
    }
}
