# Envío de mensajes HL7 de vacunación

Aplicación de consola desarrollada en **C# / .NET 8** que envía mensajes de
vacunación `VXU^V04` (HL7 v2.5.1) a la API de pruebas, interpreta la respuesta
de cada envío y genera un reporte con el resultado de todas las transacciones.

La aplicación permite identificar:

- Mensajes aceptados por el registro.
- Mensajes rechazados u observados, incluyendo el motivo informado en el ACK.
- Mensajes cuyo resultado no pudo determinarse, por ejemplo por errores de
  gateway, token, red o respuestas no interpretables.

---

## Requisitos

Antes de ejecutar la aplicación necesitás:

- [.NET SDK 8.0 o superior](https://dotnet.microsoft.com/download/dotnet/8.0)
- Git
- PowerShell, CMD o una terminal bash
- Los ocho archivos `.hl7` provistos para la prueba
- Credenciales de acceso a la API
- Credenciales de transferencia HL7
- OID de la clínica de pruebas

---

## 1. Descargar el repositorio

Cloná el repositorio:

```bash
git clone https://github.com/JuanMVV/ProyectPraxis.git
```

Ingresá a la carpeta del proyecto:

```bash
cd ProyectPraxis/Hl7VaccinationSender
```

La estructura esperada es:

```text
ProyectPraxis/
└─ Hl7VaccinationSender/
   ├─ Hl7VaccinationSender.csproj
   ├─ Program.cs
   ├─ appsettings.json
   ├─ Models/
   ├─ Services/
   ├─ messages/
   └─ output/
```

---

## 2. Restaurar dependencias

Desde la carpeta del proyecto, ejecutá:

```bash
dotnet restore
```

Este comando descarga los paquetes NuGet necesarios.

---

## 3. Configurar `appsettings.json`

El archivo `appsettings.json` contiene únicamente configuración no sensible.

Debe verse así:

```json
{
  "Api": {
    "BaseUrl": "[https://test-prxcloud.praxisemr.com/floridashot/api/v1/](https://test-prxcloud.praxisemr.com/floridashot/api/v1/)",
    "EchoPath": "FloridaShots/echo",
    "SendFilePath": "FloridaShots/send",
    "ClinicOid": "TU_OID_DE_CLINICA",
    "ApiUsername": "",
    "ApiPassword": "",
    "Hl7Username": "",
    "Hl7Password": ""
  },
  "Input": {
    "Folder": "./messages"
  },
  "Output": {
    "ReportCsv": "./output/report.csv",
    "ReportJson": "./output/report.json"
  }
}
```

### Campos de configuración

| Campo | Descripción |
|---|---|
| `Api:BaseUrl` | URL base de la API de pruebas. |
| `Api:EchoPath` | Ruta relativa del endpoint `echo`. |
| `Api:SendFilePath` | Ruta relativa del endpoint `send`. |
| `Api:ClinicOid` | OID de la clínica provisto en las credenciales. |
| `Input:Folder` | Carpeta donde se colocan los archivos `.hl7`. |
| `Output:ReportCsv` | Ruta del reporte CSV generado. |
| `Output:ReportJson` | Ruta del reporte JSON generado. |

**Importante:** no incluyas credenciales ni el OID real de la clínica en
`appsettings.json`. Esos valores se cargan mediante `dotnet user-secrets`.

---

## 4. Configurar credenciales y OID

La aplicación utiliza tres datos sensibles:

| Dato | Uso |
|---|---|
| Credenciales de API | Autenticación Basic contra la API. |
| Credenciales de transferencia HL7 | Enviadas en el cuerpo de `echo` y `send`. |
| OID de la clínica | Enviado en el cuerpo de `send`. |

### Inicializar user-secrets

Desde la carpeta del proyecto:

```bash
dotnet user-secrets init
```

### Cargar credenciales de API

```bash
dotnet user-secrets set "Api:ApiUsername" "TU_USUARIO_API"
dotnet user-secrets set "Api:ApiPassword" "TU_PASSWORD_API"
```

### Cargar credenciales HL7

```bash
dotnet user-secrets set "Api:Hl7Username" "TU_USUARIO_HL7"
dotnet user-secrets set "Api:Hl7Password" "TU_PASSWORD_HL7"
```

### Cargar OID de la clínica

```bash
dotnet user-secrets set "Api:ClinicOid" "TU_OID_DE_CLINICA"
```

### Verificar la configuración cargada

```bash
dotnet user-secrets list
```

Deberías ver algo como:

```text
Api:ApiUsername = usuario_api
Api:ApiPassword = password_api
Api:Hl7Username = usuario_hl7
Api:Hl7Password = password_hl7
Api:ClinicOid = oid_de_la_clinica
```

**No compartas estas credenciales ni las incluyas en el repositorio,
logs, capturas de pantalla o documentación.**

---

## 5. Cargar los mensajes HL7

Creá la carpeta `messages` en la raíz del proyecto, si no existe:

```bash
mkdir messages
```

Copiá los ocho archivos `.hl7` dentro de esa carpeta:

```text
Hl7VaccinationSender/
└─ messages/
   ├─ CorrectoVXU.hl7
   ├─ HL7_export_20211230104050_HL7_12302021104102M1HL7E1.hl7
   ├─ HL7_export_20211230104050_HL7_12302021104103M1HL7E1.hl7
   ├─ Imaginari Juliana test2.hl7
   ├─ Imaginary Carlos.hl7
   ├─ Imaginary Juliana test1.hl7
   ├─ Imaginary Juliana test3.hl7
   └─ Imaginary Juliana vaccine.hl7
```

La aplicación lee automáticamente todos los archivos con extensión `.hl7`
que se encuentren en la carpeta `messages`.

Los archivos `.hl7` no deben modificarse.

---

## 6. Compilar y ejecutar

Ejecutá:

```bash
dotnet build
dotnet run
```

La aplicación realizará los siguientes pasos:

1. Validará la conectividad con el endpoint `echo`.
2. Leerá todos los archivos `.hl7` de la carpeta `messages`.
3. Enviará cada mensaje al endpoint `send`.
4. Interpretará la respuesta recibida.
5. Generará `output/report.csv` y `output/report.json`.
6. Mostrará un resumen en consola.

---

## 7. Reportes generados

### Reporte CSV

Se genera en:

```text
output/report.csv
```

Incluye una fila por cada mensaje enviado, con:

- Nombre del archivo.
- `MSH-10`.
- Tipo de mensaje.
- Estado de la transacción.
- Código ACK.
- Correlación `MSA-2`.
- Código HTTP.
- Motivo del resultado.
- Error técnico, si corresponde.
- Fecha y hora de envío en UTC.

### Reporte JSON

Se genera en:

```text
output/report.json
```

Contiene la misma información en formato estructurado, incluyendo la respuesta
completa recibida del servicio en el campo `RawResponse`.

---

## 8. Clasificación de resultados

| Respuesta del servicio | Estado | Descripción |
|---|---|---|
| `MSA-1 = AA` o `CA` | `Accepted` | El registro aceptó el mensaje. |
| `MSA-1 = AE` o `CE` | `RejectedOrObserved` | El registro devolvió un error u observación. |
| `MSA-1 = AR` o `CR` | `RejectedOrObserved` | El registro rechazó el mensaje. |
| Error de gateway o token | `Unknown` | No hay evidencia de que el registro haya procesado el mensaje. |
| Error HTTP, timeout o respuesta inválida | `Unknown` | No se pudo determinar el resultado. |
| `MSA-2` no coincide con `MSH-10` | `Unknown` | No se pudo correlacionar la respuesta con el mensaje. |

Cuando el ACK contiene segmentos `ERR`, la aplicación los incluye en el motivo
para explicar el rechazo u observación.

---

## 9. Endpoints utilizados

La aplicación utiliza únicamente los endpoints habilitados para la prueba:

### Echo

```http
POST /FloridaShots/echo
```

Se utiliza para validar conectividad y credenciales.

### Send

```http
POST /FloridaShots/send
```

Se utiliza para enviar cada mensaje HL7 al registro.

---

## 10. Seguridad

- Las credenciales no están incluidas en el código fuente.
- Las credenciales y el OID real no están incluidos en `appsettings.json`.
- Las credenciales no se suben al repositorio.
- Los archivos `.hl7` y los reportes generados están excluidos mediante `.gitignore`.
- Los secretos se cargan localmente mediante `dotnet user-secrets`.

---

## 11. Problemas frecuentes

### `appsettings.json was not found`

Verificá que el archivo esté en la raíz del proyecto y que el `.csproj`
incluya:

```xml
<None Update="appsettings.json">
  <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
</None>
```

### `HTTP 401 Unauthorized`

Verificá que las credenciales estén correctamente cargadas:

```bash
dotnet user-secrets list
```

Confirmá que estén cargados:

- `Api:ApiUsername`
- `Api:ApiPassword`
- `Api:Hl7Username`
- `Api:Hl7Password`
- `Api:ClinicOid`

### `HTTP 503 Service Unavailable`

Puede ser una indisponibilidad temporaria del servicio de pruebas.
Reintentá más tarde.

### `No se encontraron archivos .hl7`

Verificá que los archivos estén en:

```text
Hl7VaccinationSender/messages
```

y que tengan extensión `.hl7`.

---

## 12. Versión utilizada

- .NET SDK: **8.0**
- Lenguaje: **C#**
- Tipo de aplicación: **Consola**