using System;
using System.Collections.Generic;
using System.Linq;
using Domain.Entities.Fees;
using Domain.Entities.Organization;
using Domain.Entities.Students;

namespace Application.Fees
{
    /// <summary>What a student owes for their current academic year and what they paid towards it.</summary>
    public sealed record StudentFeePosition(Guid StudentId, decimal Due, decimal Paid)
    {
        /// <summary>Negative when the student paid in advance.</summary>
        public decimal Remaining => Due - Paid;
    }

    /// <summary>
    /// The fee rules every fee report uses, so student, class, division and branch totals always add up:
    ///   - a student owes the class fees of their class for their academic year, plus fees set for their own
    ///     division (fees set for another division do not apply);
    ///   - payments count towards that year when they are dated within the academic year (a student promoted
    ///     to a new year starts again; last year's payments do not reduce this year's fees);
    ///   - without academic-year dates, all of the student's payments count.
    /// </summary>
    public static class FeeLedger
    {
        public static bool Applies(ClassFeeMap fee, Student student) =>
            fee.ClassId == student.ClassId
            && fee.AcademicYearId == student.AcademicYearId
            && (fee.DivisionId == null || fee.DivisionId == student.DivisionId);

        public static bool CountsTowards(Payment payment, AcademicYear? year) =>
            year?.StartDate is not { } start || year.EndDate is not { } end
            || (payment.PaymentDate is { } paid && paid >= start && paid <= end);

        public static StudentFeePosition PositionOf(Student student, IEnumerable<ClassFeeMap> fees, IEnumerable<Payment> payments, AcademicYear? year) =>
            new(student.Id,
                fees.Where(f => Applies(f, student)).Sum(f => f.Amount ?? 0),
                payments.Where(p => p.StudentId == student.Id && CountsTowards(p, year)).Sum(p => p.Amount ?? 0));
    }
}
