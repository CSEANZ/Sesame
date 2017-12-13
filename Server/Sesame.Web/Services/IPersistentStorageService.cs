using System.Threading.Tasks;

namespace Sesame.Web.Services
{
    public interface IPersistentStorageService
    {
        Task CreateSpeakerProfileAsync(string userPrincipalName,
            SpeakerProfileType speakerProfileType,
            string profileId);

        Task<string> GetSpeakerProfileAsync(string userPrincipalName,
            SpeakerProfileType speakerProfileType);

        Task<string> EnrollSpeakerPin(string userPrincipalName);
        Task<string> GetPinBySpeakerAsync(string userPrincipalName);
        Task<string> GetSpeakerByPinAsync(string pin);
        Task<string> GetSpeakerVerificationProfileByPinAsync(string pin);
        Task<string> GetSpeakerVerificationPhraseAsync(string userPrincipalName);

        Task UpdateSpeakerVerificationPhraseAsync(string userPrincipalName,
            string speakerVerificationPhrase);

        Task<SimpleClaim> GetSimpleClaimAsync(string userPrincipalName);
        Task UpdateClaim(SimpleClaim simpleClaim);
    }
}