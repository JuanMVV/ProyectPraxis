using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Hl7VaccinationSender.Models;

namespace Hl7VaccinationSender.Services
{
    public sealed class ApiClient
    {
        private readonly HttpClient _http;
        private readonly ApiOptions _options;

        public ApiClient(HttpClient http, ApiOptions options)
        {
            _http = http;
            _options = options;

            _http.BaseAddress = new Uri(options.BaseUrl);

            var apiKey = Convert.ToBase64String(
                Encoding.UTF8.GetBytes($"{options.ApiUsername}:{options.ApiPassword}"));

            _http.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Basic", apiKey);
        }

        public async Task<HttpResponseMessage> EchoAsync(CancellationToken ct)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, _options.EchoPath);

            var json = JsonSerializer.Serialize(new
            {
                user = _options.Hl7Username,
                password = _options.Hl7Password
            });

            request.Content = new StringContent(json, Encoding.UTF8, "application/json");

            return await _http.SendAsync(request, ct);
        }

        public async Task<HttpResponseMessage> SendFileAsync(
            string fileName,
            string hl7Content,
            CancellationToken ct)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, _options.SendFilePath);

            var json = JsonSerializer.Serialize(new
            {
                clinicOID = _options.ClinicOid,
                username = _options.Hl7Username,
                password = _options.Hl7Password,
                hl7 = new[] { hl7Content },
                nameHl7 = fileName,
                dateSend = DateTimeOffset.UtcNow
            });

            request.Content = new StringContent(json, Encoding.UTF8, "application/json");

            return await _http.SendAsync(request, ct);
        }
    }
}
