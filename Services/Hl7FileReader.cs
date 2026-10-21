using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hl7VaccinationSender.Services
{

    public static class Hl7FileReader
    {
        public static string Read(string path)
        {
            // Solo leemos y no se modifica el archivo
            var bytes = File.ReadAllBytes(path);
            var content = Encoding.UTF8.GetString(bytes);

            // Paseamos el archivo .hl7, no se modifica
            return content
                .Replace("\r\n", "\r")
                .Replace("\n", "\r");
        }
    }
}
