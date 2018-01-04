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
using Sesame.Web.Models;
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
            var userPrincipalName = _getUserUniqueId();

            string result = null;

            try
            {
                result = await _persistantStorageService.GetSpeakerProfileAsync(userPrincipalName, SpeakerProfileType.Verification);

                if (result.IsNullOrEmpty())
                {
                    result = await mSpeakerRecognitionClient.CreateVerificationProfileAsync();
                    await _persistantStorageService.CreateSpeakerProfileAsync(userPrincipalName, SpeakerProfileType.Verification, result);
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
            string userPrincipalName = _getUserUniqueId();

            try
            {
                var verificationProfileId = await _persistantStorageService.GetSpeakerProfileAsync(userPrincipalName, SpeakerProfileType.Verification);
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
        [HttpGet, HttpPost]
        [Route("assign")]
        public async Task<IActionResult> AssignSpeakerPinAsync()
        {
            string userPrincipalName = _getUserUniqueId();

            try
            {
                string existingPin = await _persistantStorageService.GetPinBySpeakerAsync(userPrincipalName);
                if (existingPin != null)
                {
                    return Ok(existingPin);
                }

                string result = await _persistantStorageService.EnrollSpeakerPin(userPrincipalName);

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
            string userPrincipalName = _getUserUniqueId();

            using (var memoryStream = new MemoryStream())
            {
                await Request.Body.CopyToAsync(memoryStream);
                byte[] waveBytes = memoryStream.ToArray();

                try
                {
                    var result = await mSpeakerRecognitionClient.CreateVerificationProfileEnrollmentAsync(verificationProfileId, waveBytes);

                    await _persistantStorageService.UpdateSpeakerVerificationPhraseAsync(userPrincipalName, result.Phrase);
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
                string userPrincipalName = await _persistantStorageService.GetSpeakerByPinAsync(pin);

                return Ok(new { Phrase = await _persistantStorageService.GetSpeakerVerificationPhraseAsync(userPrincipalName) });
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
            string userPrincipalName = _getUserUniqueId();
            string verificationPhrase = bodyObject.verificationPhrase;

            try
            {
                await _persistantStorageService.UpdateSpeakerVerificationPhraseAsync(userPrincipalName, verificationPhrase);
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
            return Ok(await _persistantStorageService.GetSpeakerByPinAsync(pin));
        }

        [HttpGet]
        [Route("{pin}/verifypin")]
        public async Task<IActionResult> VerifyPinAsync(string pin)
        {
            return Ok(new { Success = !(await _persistantStorageService.GetSpeakerByPinAsync(pin)).IsNullOrEmpty() });
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


                    string verificationProfileId;
                    try
                    {
                        verificationProfileId = await _persistantStorageService.GetSpeakerVerificationProfileByPinAsync(pin);
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
                        mSessionStateService.Set<string>("UserPrincipalName", await _persistantStorageService.GetSpeakerByPinAsync(pin));
                    }

                    return Ok(result);
                }
                catch (HttpException e)
                {
                    return StatusCode((int)e.StatusCode, e.Message);
                }
            }
        }

        string _getUserUniqueId()
        {
            var userPrincipalName = User.Claims.FirstOrDefault(x => x.Type == ClaimTypes.NameIdentifier)?.Value;

            return userPrincipalName;
        }
    }
}
