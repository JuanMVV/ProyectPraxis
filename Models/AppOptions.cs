using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hl7VaccinationSender.Models
{

    public sealed class AppOptions
    {
        public ApiOptions Api { get; set; } = new();
        public InputOptions Input { get; set; } = new();
        public OutputOptions Output { get; set; } = new();
    }

    public sealed class ApiOptions
    {
        public string BaseUrl { get; set; } = "";
        public string EchoPath { get; set; } = "/echo";
        public string SendFilePath { get; set; } = "/sendFile";
        public string ClinicOid { get; set; } = "";
        public string ApiUsername { get; set; } = "";
        public string ApiPassword { get; set; } = "";
        public string Hl7Username { get; set; } = "";
        public string Hl7Password { get; set; } = "";
    }

    public sealed class InputOptions
    {
        public string Folder { get; set; } = "./messages";
    }

    public sealed class OutputOptions
    {
        public string ReportCsv { get; set; } = "./output/report.csv";
        public string ReportJson { get; set; } = "./output/report.json";
    }
}
