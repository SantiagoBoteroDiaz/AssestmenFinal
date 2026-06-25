using assestment.Dto.Kyc;
using assestment.Response;

namespace assestment.Interfaces.Kyc;

public interface IAiExtractionService
{
    Task<SystemResponse<ExtractedDocumentDataDto>> ExtractDocumentData(byte[] imageBytes, string mimeType);
}