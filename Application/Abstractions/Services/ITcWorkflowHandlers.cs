using System;
using System.Threading.Tasks;

namespace Application.Abstractions.Services
{
    // Design Pattern: Chain of Responsibility (Behavioral)
    // Purpose: Encapsulates each TC workflow step (head clerk, class teacher, principal, issue, reject)
    // as a handler in a chain. The request flows through the chain until a handler processes it.

    public class TcApprovalRequest
    {
        public Guid TcId { get; init; }
        public Guid StaffId { get; init; }
        public string Action { get; init; } = string.Empty; // e.g. HEAD_CLERK_APPROVE, CLASS_TEACHER_APPROVE, PRINCIPAL_APPROVE, ISSUE, REJECT
        public string? Reason { get; init; } // used only for REJECT
    }

    public interface ITcApprovalHandler
    {
        ITcApprovalHandler SetNext(ITcApprovalHandler next);

        Task HandleAsync(TcApprovalRequest request);
    }
}

