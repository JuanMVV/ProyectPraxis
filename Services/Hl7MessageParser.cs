using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hl7VaccinationSender.Services
{
    public sealed class Hl7Message
    {
        public string MessageControlId { get; set; } = "";
        public string MessageType { get; set; } = "";
        public string ProcessingId { get; set; } = "";
    }

    public static class Hl7MessageParser
    {
        public static Hl7Message Parse(string hl7)
        {
            var segments = hl7.Split('\r', StringSplitOptions.RemoveEmptyEntries);
            var msh = segments.FirstOrDefault(s => s.StartsWith("MSH", StringComparison.Ordinal))
                ?? throw new InvalidOperationException("El mensaje no contiene segmento MSH.");

            // En MSH, MSH-1 es el separador de campos '|' y MSH-2 son los caracteres de codificación.
            var fields = msh.Split('|');

            return new Hl7Message
            {
                // MSH-10: identificador para correlacionar con MSA-2.
                MessageControlId = GetField(fields, 9),
                MessageType = GetField(fields, 8),
                ProcessingId = GetField(fields, 10)
            };
        }

        private static string GetField(string[] fields, int position)
            => position < fields.Length ? fields[position].Trim() : "";
    }
}
