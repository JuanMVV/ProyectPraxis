using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Hl7VaccinationSender.Models;

namespace Hl7VaccinationSender.Services
{

    public static class AckClassifier
    {
        public static TransactionResult Classify(
            string fileName,
            Hl7Message original,
            HttpResponseMessage response,
            string responseBody)
        {
            var result = new TransactionResult
            {
                FileName = fileName,
                MessageControlId = original.MessageControlId,
                MessageType = original.MessageType,
                ProcessingId = original.ProcessingId,
                HttpStatusCode = (int)response.StatusCode,
                RawResponse = Truncate(responseBody),
                SentAtUtc = DateTimeOffset.UtcNow
            };

            if (!response.IsSuccessStatusCode)
            {
                result.Status = TransactionStatus.Unknown;
                result.Error = $"HTTP {(int)response.StatusCode}: {response.ReasonPhrase}";
                result.Reason = "No se recibió una respuesta procesable del servicio.";
                return result;
            }

            var serviceResult = ExtractResult(responseBody);

            if (string.IsNullOrWhiteSpace(serviceResult))
            {
                result.Status = TransactionStatus.Unknown;
                result.Error = "La respuesta no contiene el campo 'result'.";
                result.Reason = "No se pudo determinar el resultado del envío.";
                return result;
            }

            // La API puede devolver una excepción en 'result'.
            if (IsInfrastructureFailure(serviceResult, out var infrastructureReason))
            {
                result.Status = TransactionStatus.Unknown;
                result.Error = Truncate(serviceResult);
                result.Reason = infrastructureReason;
                return result;
            }

            var ack = ParseAck(serviceResult);

            if (ack is null)
            {
                result.Status = TransactionStatus.Unknown;
                result.Error = "El campo 'result' no contiene un ACK HL7 válido.";
                result.Reason = "No se pudo determinar el resultado porque no hay segmento MSA.";
                return result;
            }

            result.AckCode = ack.AckCode;
            result.CorrelationId = ack.MessageControlId;

            result.Status = ack.AckCode.ToUpperInvariant() switch
            {
                "AA" or "CA" => TransactionStatus.Accepted,
                "AE" or "CE" or "AR" or "CR" => TransactionStatus.RejectedOrObserved,
                _ => TransactionStatus.Unknown
            };

            result.Reason = ack.AckCode.ToUpperInvariant() switch
            {
                "AA" or "CA" => "El registro aceptó el mensaje.",
                "AE" or "CE" => "El registro devolvió un error u observación en el ACK.",
                "AR" or "CR" => "El registro rechazó el mensaje.",
                _ => $"Código de ACK no reconocido: {ack.AckCode}."
            };

            if (ack.ErrorText.Length > 0)
                result.Reason += $" Detalle: {ack.ErrorText}";

            if (!string.IsNullOrWhiteSpace(result.CorrelationId) &&
                !string.Equals(result.CorrelationId, original.MessageControlId, StringComparison.OrdinalIgnoreCase))
            {
                result.Status = TransactionStatus.Unknown;
                result.Error = "MSA-2 no coincide con MSH-10 del mensaje enviado.";
                result.Reason = "No se pudo correlacionar confiablemente la respuesta con el mensaje.";
            }

            return result;
        }

        private static string? ExtractResult(string responseBody)
        {
            try
            {
                using var document = JsonDocument.Parse(responseBody);
                return document.RootElement.TryGetProperty("result", out var result)
                    ? result.GetString()
                    : null;
            }
            catch (JsonException)
            {
                return null;
            }
        }

        private static bool IsInfrastructureFailure(string serviceResult, out string reason)
        {
            reason = "";

            if (serviceResult.Contains("ServiceUnavailableProjectException", StringComparison.OrdinalIgnoreCase) ||
                serviceResult.Contains("Error in communication with the gateway", StringComparison.OrdinalIgnoreCase) ||
                serviceResult.Contains("security token could not be authenticated", StringComparison.OrdinalIgnoreCase))
            {
                reason = "El servicio de pruebas no pudo comunicarse con el registro o autenticar el token; el mensaje no llegó a procesarse.";
                return true;
            }

            return false;
        }

        private static AckInfo? ParseAck(string serviceResult)
        {
            var normalized = serviceResult
                .Replace("\\r\\n", "\r")
                .Replace("\\r", "\r")
                .Replace("\\n", "\r")
                .Replace("\r\n", "\r")
                .Replace("\n", "\r");

            var segments = normalized.Split('\r', StringSplitOptions.RemoveEmptyEntries);
            var msa = segments.FirstOrDefault(s => s.StartsWith("MSA", StringComparison.Ordinal));

            if (msa is null)
                return null;

            var fields = msa.Split('|');
            var errors = segments
                .Where(s => s.StartsWith("ERR", StringComparison.Ordinal))
                .ToList();

            return new AckInfo
            {
                AckCode = fields.Length > 1 ? fields[1] : "",
                MessageControlId = fields.Length > 2 ? fields[2] : "",
                ErrorText = errors.Count > 0 ? string.Join(" | ", errors) : ""
            };
        }

        private static string Truncate(string value)
            => value.Length <= 4000 ? value : value[..4000];

        private sealed class AckInfo
        {
            public string AckCode { get; set; } = "";
            public string MessageControlId { get; set; } = "";
            public string ErrorText { get; set; } = "";
        }
    }
}
