using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Http;
using System.IO;
using System.Text;
using Universal.Microsoft.CognitiveServices.SpeakerRecognition;
using Newtonsoft.Json;
using Universal.Common;
using Universal.Common.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Sesame.Web.Contracts;
using Sesame.Web.Services;


namespace Sesame.Web.Controllers
{
    [Route("api/[controller]")]
    public class VerificationProfilesController : Controller
    {
      
        private SpeakerRecognitionClient mSpeakerRecognitionClient;
        private ISessionStateService mSessionStateService;

        private static List<string> mVerificationPhrases = null;
        private IPersistentStorageService _persistantStorageService;

        public VerificationProfilesController(IConfiguration configuration, 
            ISessionStateService sessionStateService,
            SpeakerRecognitionClient mSpeakerRecognitionClient,
            IPersistentStorageService persistantStorageService)
        {
            _persistantStorageService = persistantStorageService;
            this.mSpeakerRecognitionClient = mSpeakerRecognitionClient;
            mSessionStateService = sessionStateService;
        }

        
        /// <summary>
        /// Gets a list of phrases that the user can choose from the verify their voice. 
        /// </summary>
        /// <param name="locale"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("/api/verificationPhrases")]
        public async Task<IActionResult> GetVerificationPhrasesAsync([FromQuery]string locale)
        {
            // These rarely, if ever change, so should be cached and reused. Workaround for lack of static support.
            if (mVerificationPhrases == null)
            {
                try
                {
                    mVerificationPhrases = await mSpeakerRecognitionClient.ListAllSupportedVerificationPhrasesAsync(locale.IsNullOrEmpty() ? "en-us" : locale);
                }
                catch (HttpException e)
                {
                    return StatusCode((int)e.StatusCode, e.Message);
                }
            }

            return Ok(mVerificationPhrases);
        }
        
        /// <summary>
        /// Checks to see if they user has a verification profile already and returns it if found
        /// If not - it creates it
        /// </summary>
        /// <returns></returns>
        [Authorize]
        [HttpPost]
        public async Task<IActionResult> GetOrCreateVerificationProfileAsync()
        {
            string userPrincipalName = User.Claims.Where(x => x.Type == ClaimTypes.Upn).FirstOrDefault().Value;

            string result = null;

            try
            {
                result = await PersistentStorage.GetSpeakerProfileAsync(userPrincipalName, SpeakerProfileType.Verification);

                if (result.IsNullOrEmpty())
                {
                    result = await mSpeakerRecognitionClient.CreateVerificationProfileAsync();
                    await PersistentStorage.CreateSpeakerProfileAsync(userPrincipalName, SpeakerProfileType.Verification, result);
                }
                return Ok(result);
            }
            catch (HttpException e)
            {
                return StatusCode((int)e.StatusCode, e.Message);
            }
        }

        // api/verificationProfiles/{userPrincipalName}
        [Authorize]
        [HttpGet]
        [Route("profile")]
        public async Task<IActionResult> GetProfile()
        {
            string userPrincipalName = User.Claims.Where(x => x.Type == ClaimTypes.Upn).FirstOrDefault().Value;

            try
            {
                var verificationProfileId = await PersistentStorage.GetSpeakerProfileAsync(userPrincipalName, SpeakerProfileType.Verification);
                if (verificationProfileId == null)
                {
                    return StatusCode(400, "new user");
                }

                return Ok(await mSpeakerRecognitionClient.GetVerificationProfileAsync(verificationProfileId));
            }
            catch (HttpException e)
            {
                return StatusCode((int)e.StatusCode, e.Message);
            }
        }

        // api/verificationProfiles/{verificationProfileId}
        [Authorize]
        [HttpDelete]
        [Route("{verificationProfileId}")]
        public async Task<IActionResult> DeleteByIdAsync(string verificationProfileId)
        {
            try
            {
                await mSpeakerRecognitionClient.DeleteVerificationProfileAsync(verificationProfileId);
                return Ok();
            }
            catch (HttpException e)
            {
                return StatusCode((int)e.StatusCode, e.Message);
            }
        }

        // TODO This really should be split into two - one to get and one to assign
        // api/verificationProfiles/{userPrincipalName}/assign
        [Authorize]
        [HttpGet,HttpPost]
        [Route("assign")]
        public async Task<IActionResult> AssignSpeakerPinAsync()
        {
            string userPrincipalName = User.Claims.Where(x => x.Type == ClaimTypes.Upn).FirstOrDefault().Value;

            try
            {
                int? existingPin = await PersistentStorage.GetPinBySpeakerAsync(userPrincipalName);
                if (existingPin != null)
                {
                    return Ok(existingPin.Value);
                }

                int result = await PersistentStorage.EnrollSpeakerPin(userPrincipalName);

                return Ok(result);
            }
            catch (Exception e)
            {
                return StatusCode(400, e.ToString());
            }
        }

        // api/verificationProfiles/{verificationProfileId}/enroll
        [Authorize]
        [HttpPost]
        [Route("{verificationProfileId}/enroll")]
        public async Task<IActionResult> CreateEnrollmentAsync(string verificationProfileId)
        {
            string userPrincipalName = User.Claims.Where(x => x.Type == ClaimTypes.Upn).FirstOrDefault().Value;

            using (var memoryStream = new MemoryStream())
            {
                await Request.Body.CopyToAsync(memoryStream);
                byte[] waveBytes = memoryStream.ToArray();

                try
                {
                    var result = await mSpeakerRecognitionClient.CreateVerificationProfileEnrollmentAsync(verificationProfileId, waveBytes);

                    await PersistentStorage.UpdateSpeakerVerificationPhraseAsync(userPrincipalName, result.Phrase);
                    return Ok(result);
                }
                catch (HttpException e)
                {
                    return StatusCode((int)e.StatusCode, e.Message);
                }
            }
        }

        // TODO This really should be split into two - one to get and one to assign
        // api/verificationProfiles/{userPrincipalName}/phrase
        [HttpGet]
        [Route("{pin}/phrase")]
        public async Task<IActionResult> GetVerificationPhrase(string pin)
        {
            try
            {
                int parsedPin = Int32.Parse(pin);
                string userPrincipalName = await PersistentStorage.GetSpeakerByPinAsync(parsedPin);

                return Ok(new { Phrase = await PersistentStorage.GetSpeakerVerificationPhraseAsync(userPrincipalName) });
            }
            catch (Exception e)
            {
                return StatusCode(400, e.ToString());
            }
        }

        // api/verificationProfiles/{userPrincipalName}/phrase
        [Authorize]
        [HttpPost]
        [Route("phrase")]
        public async Task<IActionResult> GetVerificationPhrase([FromBody]dynamic bodyObject)
        {
            string userPrincipalName = User.Claims.Where(x => x.Type == ClaimTypes.Upn).FirstOrDefault().Value;
            string verificationPhrase = bodyObject.verificationPhrase;

            try
            {
                await PersistentStorage.UpdateSpeakerVerificationPhraseAsync(userPrincipalName, verificationPhrase);
                return Ok();
            }
            catch (Exception e)
            {
                return StatusCode(400, e.ToString());
            }
        }

        // api/verificationProfiles/{verificationProfileId}/reset
        [Authorize]
        [HttpPost]
        [Route("{verificationProfileId}/reset")]
        public async Task<IActionResult> ResetEnrollmentsAsync(string verificationProfileId)
        {
            try
            {
                await mSpeakerRecognitionClient.ResetVerificationProfileEnrollmentsAsync(verificationProfileId);
                return Ok();
            }
            catch (HttpException e)
            {
                return StatusCode((int)e.StatusCode, e.Message);
            }
        }

        [HttpGet]
        [Route("{pin}/upn")]
        public async Task<IActionResult> GetSpeakerUserPrincipalNameAsync(string pin)
        {
            int parsedPin;
            try
            {
                parsedPin = Int32.Parse(pin);
            }
            catch
            {
                return StatusCode(400, "A valid PIN was not provided.");
            }

            return Ok(await PersistentStorage.GetSpeakerByPinAsync(parsedPin));
        }

        [HttpGet]
        [Route("{pin}/verifypin")]
        public async Task<IActionResult> VerifyPinAsync(string pin)
        {
            int parsedPin;
            try
            {
                parsedPin = Int32.Parse(pin);
            }
            catch
            {
                return Ok(new { Success = false });
            }

            return Ok(new { Success = !(await PersistentStorage.GetSpeakerByPinAsync(parsedPin)).IsNullOrEmpty() });
        }

        [HttpPost]
        [Route("{pin}/verifyvoice")]
        public async Task<IActionResult> VerifyVoiceAsync(string pin)
        {
            using (var memoryStream = new MemoryStream())
            {
                await Request.Body.CopyToAsync(memoryStream);
                byte[] waveBytes = memoryStream.ToArray();

                try
                {
                    //find it based on pin
                    int parsedPin;
                    try
                    {
                        parsedPin = Int32.Parse(pin);
                    }
                    catch
                    {
                        // throw new ArgumentException("A valid PIN was not provided.");
                        return StatusCode(400, "A valid PIN was not provided.");
                    }

                    string verificationProfileId;
                    try
                    {
                        verificationProfileId = await PersistentStorage.GetSpeakerVerificationProfileByPinAsync(parsedPin);
                    }
                    catch
                    {
                        //throw new InvalidOperationException("No valid speaker profile matching PIN.");
                        return StatusCode(400, "No valid speaker profile matching PIN.");
                    }

                    var result = await mSpeakerRecognitionClient.VerifyAsync(verificationProfileId, waveBytes);

                    if (result.Result == "Accept")
                    {
                        mSessionStateService.Set<bool>("VoiceAuthenticated", true);
                        mSessionStateService.Set<string>("UserPrincipalName", await PersistentStorage.GetSpeakerByPinAsync(parsedPin));
                    }

                    return Ok(result);
                }
                catch (HttpException e)
                {
                    return StatusCode((int)e.StatusCode, e.Message);
                }
            }
        }
    }
}
