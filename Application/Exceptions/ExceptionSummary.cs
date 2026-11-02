using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Application.Exceptions
{
    /// <summary>
    /// Error response body of the API (written by the global exception handler). The original five fields
    /// keep their names and casing because the client reads them; the others were added later.
    /// </summary>
    public class ExceptionSummary
    {
        /// <summary>Message to show the user.</summary>
        public string UserMessage { get; set; }

        /// <summary>Why the request was rejected (4xx only); null for server errors.</summary>
        public string ValidationMessage { get; set; }

        /// <summary>Support hint with the reference; exception details in Development.</summary>
        public string SupportMessages { get; set; }

        /// <summary>Error reference: the same value is in the log entry (ErrorReference).</summary>
        public string ReferenceId { get; set; }

        /// <summary>UTC, ISO 8601.</summary>
        public string TimeStamp { get; set; } = DateTime.UtcNow.ToString("o");

        public int StatusCode { get; set; }

        /// <summary>Stable category, e.g. "Validation", "ItemNotFound", "UniqueViolation", "Unexpected".</summary>
        public string ErrorCode { get; set; }

        /// <summary>Request id (X-Correlation-ID response header); finds every log line of the request.</summary>
        public string CorrelationId { get; set; }

        /// <summary>Field errors for validation failures (property name -> messages).</summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public IDictionary<string, string[]> Errors { get; set; }
    }
}
