using System;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Threading.Tasks;
using AspNet.Security.OpenIdConnect.Primitives;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using NAudio.Wave;
using Newtonsoft.Json;
using TrustedConsoleApp.helpers;
using TrustedConsoleApp.models;

namespace TrustedConsoleApp
{
    class Program
    {
        private static SesameConfiguration _sesameConfiguration;
        private static WaveFileWriter _waveFile;

        static void Main(string[] args)
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", true, true)
                .AddEnvironmentVariables()
                .AddCommandLine(args)
                .Build();

            _sesameConfiguration = new SesameConfiguration();

            configuration.GetSection("Sesame").Bind(_sesameConfiguration);

            Console.WriteLine("Hello! Please enter your pin.");
            var pin = Console.ReadLine();

            var phraseResponse = Phrase(pin).Result;

            Console.WriteLine(phraseResponse.Phrase);

            if (phraseResponse.Phrase == null)
            {

                Console.WriteLine("error! pin is invalid");
            }
            else
            {
                Console.WriteLine("Now recording...");
                var waveSource = new WaveInEvent {WaveFormat = new WaveFormat(44100, 1)};
                waveSource.DataAvailable += new EventHandler<WaveInEventArgs>(waveSource_DataAvailable);
                var tempFile = $@"{Directory.GetCurrentDirectory()}\test1.wav";
                _waveFile = new WaveFileWriter(tempFile, waveSource.WaveFormat);
                waveSource.StartRecording();
                Console.WriteLine("Press enter to stop");
                Console.ReadLine();
                waveSource.StopRecording();
                _waveFile.Dispose();


                var verifyResult = Verify(pin, tempFile).Result;

                Console.WriteLine(verifyResult);

                var verifyResponse = JsonConvert.DeserializeObject<VerifyResponse>(verifyResult);
                if (verifyResponse.JwtToken != null)
                {
                    try
                    {
                        var token = VerifyToken(verifyResponse.JwtToken.Token);

                        if (token.Claims.Any())
                        {
                            var upn = token.Claims.SingleOrDefault(x => x.Type == OpenIdConnectConstants.Claims.Name)
                                ?.Value;
                            Console.WriteLine($"OpenIdConnectConstants.Claims.Name: {upn}");
                        }

                        Console.WriteLine(token);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                    }
                }
                
            }


            Console.WriteLine("Press any key to exit");
            Console.ReadKey();

        }

        public static JwtSecurityToken VerifyToken(string token)
        {
            var publicRsa = RSA.Create();

            //var currDir = Assembly.GetExecutingAssembly().Location;
            //var currDir = Directory.GetCurrentDirectory();
            var publicKeyXml = File.ReadAllText($@"{Directory.GetCurrentDirectory()}/Keys/{_sesameConfiguration.RsaPublicKeyXml}");
            XmlHelper.FromXmlString(publicRsa, publicKeyXml);
            var publicRsaSecurityKey = new RsaSecurityKey(publicRsa);

            var validationParameters = new TokenValidationParameters
            {
                RequireExpirationTime = true,
                RequireSignedTokens = true,
                ValidateAudience = false,
                ValidateIssuer = false,
                ValidateLifetime = false,
                IssuerSigningKey = publicRsaSecurityKey
            };

            var handler = new JwtSecurityTokenHandler();
            var result = handler.ValidateToken(token, validationParameters, out var validatedSecurityToken);
            var validatedJwt = validatedSecurityToken as JwtSecurityToken;
            return validatedJwt;
        }

        static void waveSource_DataAvailable(object sender, WaveInEventArgs e)
        {
            _waveFile.Write(e.Buffer, 0, e.BytesRecorded);
        }



        public static async Task<PhraseResponse> Phrase(string pin)
        {
            var url = $"{_sesameConfiguration.Authority}/api/verificationProfiles/{pin}/phrase";


            PhraseResponse responseContent = null;

            using (var client = new HttpClient())
            {
                var response = await client.GetAsync(url);
                var responseContentString = await response.Content.ReadAsStringAsync();
                responseContent = JsonConvert.DeserializeObject<PhraseResponse>(responseContentString);
            }

            return responseContent;

        }

        public static async Task<string> Verify(string pin, string voicepath)
        {
            var url = $"{_sesameConfiguration.Authority}/api/verificationProfiles/authenticationtoken/{pin}/verifyvoice";



            var client = new HttpClient();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/octet-stream"));

            var outputFile = $@"{Directory.GetCurrentDirectory()}\converted.wav";
            using (var waveFileReader = new WaveFileReader(voicepath))
            {
                var outFormat = new WaveFormat(16000, 1);
                using (var resampler = new MediaFoundationResampler(waveFileReader, outFormat))
                {
                    WaveFileWriter.CreateWaveFile(outputFile, resampler);
                }
            }

            var raw = File.ReadAllBytes(outputFile);
            var audioContent = new ByteArrayContent(raw);

            var response = await client.PostAsync(url, audioContent);
            var responseContentString = await response.Content.ReadAsStringAsync();

            return responseContentString;
        }



    }
}
