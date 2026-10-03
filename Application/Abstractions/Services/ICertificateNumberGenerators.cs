using System;
using System.Threading.Tasks;

namespace Application.Abstractions.Services
{
    // Design Pattern: Strategy (Behavioral)
    // Purpose: Encapsulates algorithms for generating document numbers so they can vary independently from services.

    public interface ITcNumberGenerator
    {
        Task<(string Number, int Sequence)> GenerateAsync(Guid academicYearId, Guid classId);
    }

    public interface IBonafideNumberGenerator
    {
        Task<(string Number, int Sequence)> GenerateAsync(Guid academicYearId, Guid classId);
    }

    public interface IHallTicketNumberGenerator
    {
        Task<string> GenerateAsync(Guid examId, string? examName, string? admissionNo, Guid studentId);
    }
}

