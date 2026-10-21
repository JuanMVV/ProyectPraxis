using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hl7VaccinationSender.Models
{

    public enum TransactionStatus
    {
        Accepted,
        RejectedOrObserved,
        Unknown
    }

    public sealed class TransactionResult
    {
        public string FileName { get; set; } = "";
        public string MessageControlId { get; set; } = "";
        public string MessageType { get; set; } = "";
        public string ProcessingId { get; set; } = "";
        public TransactionStatus Status { get; set; }
        public string AckCode { get; set; } = "";
        public string CorrelationId { get; set; } = "";
        public string Reason { get; set; } = "";
        public int? HttpStatusCode { get; set; }
        public string RawResponse { get; set; } = "";
        public string Error { get; set; } = "";
        public DateTimeOffset SentAtUtc { get; set; }
    }
}
