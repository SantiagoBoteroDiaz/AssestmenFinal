using System.Text.Json;
using assestment.Dto.Kyc;
using assestment.Interfaces.Kyc;
using assestment.Response;
using Google.GenAI;
using Google.GenAI.Types;
using Microsoft.Extensions.Configuration;
using GeminiType = Google.GenAI.Types.Type; // alias para resolver la ambigüedad

namespace assestment.Services.Kyc;

public class AiExtractionService : IAiExtractionService
{
    private readonly Client _geminiClient;

    public AiExtractionService(IConfiguration configuration)
    {
        var apiKey = configuration["Gemini:ApiKey"];
        _geminiClient = new Client(apiKey: apiKey);
    }

    public async Task<SystemResponse<ExtractedDocumentDataDto>> ExtractDocumentData(byte[] imageBytes, string mimeType)
    {
        try
        {
            var schema = new Schema
            {
                Type = GeminiType.Object,
                Properties = new Dictionary<string, Schema>
                {
                    { "nombres", new Schema { Type = GeminiType.String, Nullable = true } },
                    { "apellidos", new Schema { Type = GeminiType.String, Nullable = true } },
                    { "numeroDocumento", new Schema { Type = GeminiType.String, Nullable = true } },
                    { "fechaNacimiento", new Schema { Type = GeminiType.String, Nullable = true, Description = "Formato YYYY-MM-DD" } },
                    { "confianza", new Schema { Type = GeminiType.Number } },
                    { "documentoLegible", new Schema { Type = GeminiType.Boolean } }
                },
                Required = new List<string> { "confianza", "documentoLegible" }
            };

            var contents = new List<Content>
            {
                new Content
                {
                    Role = "user",
                    Parts = new List<Part>
                    {
                        new Part
                        {
                            InlineData = new Blob
                            {
                                MimeType = mimeType,
                                Data = imageBytes
                            }
                        },
                        new Part
                        {
                            Text = """
                                   Extrae la siguiente información de esta imagen de cédula de identidad.

                                   IMPORTANTE: La fecha de nacimiento en el documento puede estar en formato DD/MM/AAAA.
                                   Debes convertirla y devolverla ÚNICAMENTE en formato ISO: AAAA-MM-DD (ejemplo: 2007-06-04).
                                   No expedición ni se la fecha de a, solo la fecha de nacimiento.

                                   Si algún campo no es legible, devuélvelo como null.
                                   La confianza debe reflejar qué tan seguro estás de la extracción, entre 0 y 1.
                                   """
                        }
                    }
                }
            };

            var config = new GenerateContentConfig
            {
                ResponseMimeType = "application/json",
                ResponseSchema = schema
            };

            var response = await _geminiClient.Models.GenerateContentAsync(
                model: "gemini-2.5-flash",
                contents: contents,
                config: config
            );

            var jsonText = response.Candidates[0].Content.Parts[0].Text;

            Console.WriteLine("===== RESPUESTA CRUDA DE GEMINI =====");
            Console.WriteLine(jsonText);
            Console.WriteLine("======================================");

            var extracted = JsonSerializer.Deserialize<ExtractedDocumentDataDto>(jsonText, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (extracted == null)
            {
                return new SystemResponse<ExtractedDocumentDataDto> { Success = false, Message = "Could not parse extraction result" };
            }

            Console.WriteLine("===== DESERIALIZADO =====");
            Console.WriteLine($"Nombres: {extracted.Nombres}");
            Console.WriteLine($"Apellidos: {extracted.Apellidos}");
            Console.WriteLine($"NumeroDocumento: {extracted.NumeroDocumento}");
            Console.WriteLine($"FechaNacimiento: {extracted.FechaNacimiento}");
            Console.WriteLine($"Confianza: {extracted.Confianza}");
            Console.WriteLine($"DocumentoLegible: {extracted.DocumentoLegible}");
            Console.WriteLine("=========================");

            return new SystemResponse<ExtractedDocumentDataDto> { Success = true, Data = extracted };
        }
        catch (Exception e)
        {
            return new SystemResponse<ExtractedDocumentDataDto> { Success = false, Message = e.Message };
        }
    }
}