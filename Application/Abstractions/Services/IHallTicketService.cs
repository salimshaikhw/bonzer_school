using Application.ViewModels.Certificates;
using System;
using System.Threading.Tasks;

namespace Application.Abstractions.Services;

public interface IHallTicketService
{
    Task<HallTicketResponse> GenerateAsync(HallTicketRequest request);
    Task<HallTicketResponse?> GetByIdAsync(Guid id);
    Task<HallTicketResponse?> GetByStudentAndExamAsync(Guid studentId, Guid examId);
}
