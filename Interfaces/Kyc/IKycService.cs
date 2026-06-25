using assestment.Dto.Kyc;
using assestment.Models;
using assestment.Response;

namespace assestment.Interfaces.Kyc;

public interface IKycService
{
    Task<SystemResponse<KycVerification>> ValidateAndPersist(Guid userId, ExtractedDocumentDataDto extracted);
    Task<SystemResponse<KycVerification>> GetByUserId(Guid userId); 
}