﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using System.IO;
using Universal.Microsoft.CognitiveServices.SpeakerRecognition;
using Universal.Common;
using Universal.Common.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using AspNet.Security.OpenIdConnect.Primitives;
using Sesame.Web.Contracts;
using Sesame.Web.Helpers;
using Sesame.Web.Models;
using Sesame.Web.Services;


namespace Sesame.Web.Controllers
{
    [Route("api/[controller]")]
    public class VerificationProfilesController : Controller
    {

        private readonly SpeakerRecognitionClient _speakerRecognitionClient;
        private readonly ISessionStateService _sessionStateService;
        private static List<string> _verificationPhrases;
        private readonly IPersistentStorageService _persistantStorageService;
        private readonly IJwtHandler _jwtHandler;

        public VerificationProfilesController(IConfiguration configuration,
            ISessionStateService sessionStateService,
            SpeakerRecognitionClient speakerRecognitionClient,
            IPersistentStorageService persistantStorageService,
            IJwtHandler jwtHandler)
        {
            _persistantStorageService = persistantStorageService;
            _speakerRecognitionClient = speakerRecognitionClient;
            _sessionStateService = sessionStateService;
            _jwtHandler = jwtHandler;
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
            if (_verificationPhrases == null)
            {
                try
                {
                    _verificationPhrases = await _speakerRecognitionClient.ListAllSupportedVerificationPhrasesAsync(locale.IsNullOrEmpty() ? "en-us" : locale);
                }
                catch (HttpException e)
                {
                    return StatusCode(e.StatusCode, e.Message);
                }
            }

            return Ok(_verificationPhrases);
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

            try
            {
                var result = await _persistantStorageService.GetSpeakerProfileAsync(userPrincipalName, SpeakerProfileType.Verification);

                if (result.IsNullOrEmpty())
                {
                    result = await _speakerRecognitionClient.CreateVerificationProfileAsync();
                    await _persistantStorageService.CreateSpeakerProfileAsync(userPrincipalName, SpeakerProfileType.Verification, result);
                }
                return Ok(result);
            }
            catch (HttpException e)
            {
                return StatusCode(e.StatusCode, e.Message);
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

                return Ok(await _speakerRecognitionClient.GetVerificationProfileAsync(verificationProfileId));
            }
            catch (HttpException e)
            {
                return StatusCode(e.StatusCode, e.Message);
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
                await _speakerRecognitionClient.DeleteVerificationProfileAsync(verificationProfileId);
                return Ok();
            }
            catch (HttpException e)
            {
                return StatusCode(e.StatusCode, e.Message);
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
                    var result = await _speakerRecognitionClient.CreateVerificationProfileEnrollmentAsync(verificationProfileId, waveBytes);

                    await _persistantStorageService.UpdateSpeakerVerificationPhraseAsync(userPrincipalName, result.Phrase);
                    return Ok(result);
                }
                catch (HttpException e)
                {
                    return StatusCode(e.StatusCode, e.Message);
                }
            }
        }

        // TODO This really should be split into two - one to get and one to assign
        // api/verificationProfiles/{pin}/phrase
        [HttpGet]
        [Route("{pin}/phrase")]
        public async Task<IActionResult> GetVerificationPhrase(string pin)
        {
            try
            {
                var userPrincipalName = await _persistantStorageService.GetSpeakerByPinAsync(pin);

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
                await _speakerRecognitionClient.ResetVerificationProfileEnrollmentsAsync(verificationProfileId);
                return Ok();
            }
            catch (HttpException e)
            {
                return StatusCode(e.StatusCode, e.Message);
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

                    var result = await _speakerRecognitionClient.VerifyAsync(verificationProfileId, waveBytes);

                    if (result.Result == "Accept")
                    {
                        _sessionStateService.Set("VoiceAuthenticated", true);
                        _sessionStateService.Set("UserPrincipalName", await _persistantStorageService.GetSpeakerByPinAsync(pin));
                    }

                    return Ok(result);
                }
                catch (HttpException e)
                {
                    return StatusCode(e.StatusCode, e.Message);
                }
            }
        }


        //az
        //api/verificationProfiles/authenticationtoken/{pin}/verifyvoice
        [HttpPost]
        [Route("authenticationtoken/{pin}/verifyvoice")]
        public async Task<IActionResult> AuthenticationToken(string pin)
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


            using (var memoryStream = new MemoryStream())
            {

                try
                {
                    await Request.Body.CopyToAsync(memoryStream);
                    byte[] waveBytes = memoryStream.ToArray();

                    VerificationResult result = await _speakerRecognitionClient.VerifyAsync(verificationProfileId, waveBytes);

                    if (result.Result == "Accept")
                    {
                        var user = await _persistantStorageService.GetUserByVerificationProfileId(verificationProfileId);

                        if (user == null)
                        {
                            return StatusCode(500, "Invalid verification profile provided.");
                        }

                        var claims = new Dictionary<string, string>
                        {
                            {OpenIdConnectConstants.Claims.Subject, user.UserPrinipleName }
                        };

                        return Json(new { result = result.Result, jwt = _jwtHandler.Create(claims) });
                    }
                    else
                    {
                        return Json(new { result = result.Result });
                    }
                }
                catch (HttpException e)
                {
                    return StatusCode(e.StatusCode, e.Message);
                }
                catch (Exception e)
                {
                    return StatusCode(500, e.ToString());
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
