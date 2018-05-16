using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using NAudio.Wave;
using Newtonsoft.Json;
using TrustedConsoleApp.models;

namespace TrustedConsoleApp
{
    class Program
    {
        private static SesameConfiguration _sesameConfiguration;
        private static  WaveFileWriter _waveFile;

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

            var response = Phrase(pin).Result;

            Console.WriteLine(response.Phrase);


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


            var token = Verify(pin, tempFile).Result;

            Console.WriteLine(token);
            Console.ReadLine();

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

        public static async Task<string> Verify(string pin,string voicepath)
        {
            var url = $"{_sesameConfiguration.Authority}/api/verificationProfiles/authenticationtoken/{pin}/verifyvoice";



            HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/octet-stream"));

            string outputFile = $@"{Directory.GetCurrentDirectory()}\converted.wav";
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
