using Application.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Exceptions
{

    /// <summary>Base of the application exceptions: an ExceptionCodes category (mapped to an HTTP status) and a reference id shown to the user and logged.</summary>
    public abstract class HCBException : Exception
    {
        public Guid ReferenceId { get; }
        public ExceptionCodes ExceptionCode { get; set; }
        public HCBException(string message, Exception innerException = null, ExceptionCodes exceptionCode = ExceptionCodes.Default)
            : base(message, innerException)
        {
            ExceptionCode = exceptionCode;
            ReferenceId = (innerException as HCBException)?.ReferenceId ?? Guid.NewGuid();
        }
    }
}
