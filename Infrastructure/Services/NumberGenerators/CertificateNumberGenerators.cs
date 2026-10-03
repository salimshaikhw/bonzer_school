using System;
using System.Threading.Tasks;
using Application.Abstractions.Repositories;
using Application.Abstractions.Services;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services.NumberGenerators
{
    // Design Pattern: Strategy (Behavioral) for TC number generation
    public class TcNumberGenerator : ITcNumberGenerator
    {
        private readonly ITcCertificateRepository _tcRepository;
        private readonly IAcademicYearRepository _academicYearRepository;
        private readonly IClassRepository _classRepository;

        public TcNumberGenerator(
            ITcCertificateRepository tcRepository,
            IAcademicYearRepository academicYearRepository,
            IClassRepository classRepository)
        {
            _tcRepository = tcRepository;
            _academicYearRepository = academicYearRepository;
            _classRepository = classRepository;
        }

        public async Task<(string Number, int Sequence)> GenerateAsync(Guid academicYearId, Guid classId)
        {
            var year = await _academicYearRepository.GetByIdAsync(academicYearId);
            var @class = await _classRepository.GetByIdAsync(classId);

            var maxSeq = await _tcRepository
                .Query(t => t.AcademicYearId == academicYearId && t.ClassId == classId)
                .MaxAsync(t => (int?)t.Sequence) ?? 0;

            var nextSeq = maxSeq + 1;
            var yearPart = year?.YearName?.Replace(" ", string.Empty) ?? "NA";
            var classPart = @class?.Name?.Replace(" ", string.Empty) ?? "NA";
            var number = $"TC/{yearPart}/{classPart}/{nextSeq:D4}";

            return (number, nextSeq);
        }
    }

    // Design Pattern: Strategy (Behavioral) for Bonafide number generation
    public class BonafideNumberGenerator : IBonafideNumberGenerator
    {
        private readonly IBonafideCertificateRepository _bonafideRepository;
        private readonly IAcademicYearRepository _academicYearRepository;
        private readonly IClassRepository _classRepository;

        public BonafideNumberGenerator(
            IBonafideCertificateRepository bonafideRepository,
            IAcademicYearRepository academicYearRepository,
            IClassRepository classRepository)
        {
            _bonafideRepository = bonafideRepository;
            _academicYearRepository = academicYearRepository;
            _classRepository = classRepository;
        }

        public async Task<(string Number, int Sequence)> GenerateAsync(Guid academicYearId, Guid classId)
        {
            var year = await _academicYearRepository.GetByIdAsync(academicYearId);
            var @class = await _classRepository.GetByIdAsync(classId);

            var maxSeq = await _bonafideRepository
                .Query(t => t.AcademicYearId == academicYearId && t.ClassId == classId)
                .MaxAsync(t => (int?)t.Sequence) ?? 0;

            var nextSeq = maxSeq + 1;
            var yearPart = year?.YearName?.Replace(" ", string.Empty) ?? "NA";
            var classPart = @class?.Name?.Replace(" ", string.Empty) ?? "NA";
            var number = $"BF/{yearPart}/{classPart}/{nextSeq:D4}";

            return (number, nextSeq);
        }
    }

    // Design Pattern: Strategy (Behavioral) for Hall ticket number generation
    public class HallTicketNumberGenerator : IHallTicketNumberGenerator
    {
        private readonly IHallTicketRepository _hallTicketRepository;

        public HallTicketNumberGenerator(IHallTicketRepository hallTicketRepository)
        {
            _hallTicketRepository = hallTicketRepository;
        }

        public async Task<string> GenerateAsync(Guid examId, string? examName, string? admissionNo, Guid studentId)
        {
            var existingCount = await _hallTicketRepository
                .Query(h => h.ExamId == examId)
                .CountAsync();

            var examPart = examName?.Replace(" ", string.Empty) ?? "EXAM";
            var studentPart = admissionNo ?? studentId.ToString("N")[..8];
            var ticketNumber = $"HT/{examPart}/{studentPart}/{existingCount + 1:D4}";

            return ticketNumber;
        }
    }
}

