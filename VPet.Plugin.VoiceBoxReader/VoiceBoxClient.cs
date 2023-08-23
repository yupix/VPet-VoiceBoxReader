using LinePutScript;
using MediaToolkit;
using MediaToolkit.Model;
using MediaToolkit.Options;
using Newtonsoft.Json;
using Panuon.WPF.UI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using VPet_Simulator.Core;


namespace VPet.Plugin.VPet_VoiceBox


{
    public class SupportedFeatures
    {
        [JsonProperty("permitted_synthesis_morphing")]
        public string PermittedSynthesisMorphing { get; set; }
    }

    public class Style
    {
        [JsonProperty("name")]
        public string Name { get; set; }
        [JsonProperty("id")]
        public int Id { get; set; }
    }

    public class Speaker
    {
        [JsonProperty("supported_features")]
        public SupportedFeatures SupportedFeatures { get; set; }
        [JsonProperty("name")]
        public string Name { get; set; }
        [JsonProperty("speaker_uuid")]
        public string SpeakerUuid { get; set; }
        [JsonProperty("styles")]
        public List<Style> Styles { get; set; }
        [JsonProperty("version")]
        public string Version { get; set; }
    }

    public class Mora
    {
        [JsonProperty("text")]
        public string Text { get; set; }

        [JsonProperty("consonant")]
        public string Consonant { get; set; } = null;

        [JsonProperty("consonant_length")]
        public Nullable<double> ConsonantLength { get; set; } = null;

        [JsonProperty("vowel")]
        public string Vowel { get; set; }

        [JsonProperty("vowel_length")]
        public double VowelLength { get; set; }

        [JsonProperty("pitch")]
        public double Pitch { get; set; }
    }

    public class AccentPhrase
    {
        [JsonProperty("moras")]
        public List<Mora> Moras { get; set; }

        [JsonProperty("accent")]
        public int Accent { get; set; }

        [JsonProperty("pause_mora")]
        public object PauseMora { get; set; }

        [JsonProperty("is_interrogative")]
        public bool IsInterrogative { get; set; }
    }

    public class AudioQuery
    {
        [JsonProperty("accent_phrases")]
        public List<AccentPhrase> AccentPhrases { get; set; }

        [JsonProperty("speedScale")]
        public double SpeedScale { get; set; }

        [JsonProperty("pitchScale")]
        public double PitchScale { get; set; }

        [JsonProperty("intonationScale")]
        public double IntonationScale { get; set; }

        [JsonProperty("volumeScale")]
        public double VolumeScale { get; set; }

        [JsonProperty("prePhonemeLength")]
        public double PrePhonemeLength { get; set; }

        [JsonProperty("postPhonemeLength")]
        public double PostPhonemeLength { get; set; }

        [JsonProperty("outputSamplingRate")]
        public int OutputSamplingRate { get; set; }

        [JsonProperty("outputStereo")]
        public bool OutputStereo { get; set; }

        [JsonProperty("kana")]
        public string Kana { get; set; }
    }


    public class VoiceBoxClient
    {
        readonly HttpClient HttpClient = new HttpClient() { Timeout = TimeSpan.FromSeconds(5) };
        private VoiceBoxReader vboxr;

        public string BaseUri
        {
            get; set;
        }

        public VoiceBoxClient(VoiceBoxReader vboxr, string BaseUri)
        {
            this.vboxr = vboxr;
            this.BaseUri = BaseUri;
        }


        public void ConvertToMp3(string wavFilePath, string mp3FilePath)
        {
            MediaFile inputFile = new MediaFile { Filename = wavFilePath };
            MediaFile outputFile = new MediaFile { Filename = mp3FilePath };

            using (var engine = new Engine())
            {
                engine.GetMetadata(inputFile);
                var options = new ConversionOptions { AudioSampleRate = AudioSampleRate.Hz44100 };
                engine.Convert(inputFile, outputFile, options);
            }
        }

        public HttpRequestException RequestErrorHandler()
        {
            vboxr.Set.Enable = false;
            vboxr.SaveSetting();
            MessageBoxX.Show("VoiceBox Engineにアクセスする際何らかの問題が発生しました。自動的にModを使用しないようにしました。使用するには再度VoiceBox Readerから「使用する」をオンにしてください");
            return new HttpRequestException($"HTTP request failed");
        }

        public async Task<HttpResponseMessage> Request(HttpRequestMessage requestMessage)
        {
            try
            {
                var res = await HttpClient.SendAsync(requestMessage);
                if (res.IsSuccessStatusCode)
                {
                    return res;
                }
                else
                {
                    throw RequestErrorHandler();
                }
            } catch
            {
                throw RequestErrorHandler();
            }
        }

        public async Task<List<Speaker>> GetSpeakers()
        {
            string requestUrl = $"{BaseUri}/speakers";
            var request = new HttpRequestMessage(new HttpMethod("GET"), requestUrl);
            var response = await Request(request);
            var data = JsonConvert.DeserializeObject<List<Speaker>>(await response.Content.ReadAsStringAsync());
            return data;
        }

        public async Task<AudioQuery> Make_Query(string text, int speakerId)
        {
            string requestUrl = $"{BaseUri}/audio_query?text={text}&speaker={speakerId}";

            var request = new HttpRequestMessage(new HttpMethod("POST"), requestUrl);
            request.Headers.TryAddWithoutValidation("accept", "application/json");
            request.Content = new StringContent("");
            request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/x-www-form-urlencoded");

            try
            {
                var response = await Request(request);
                AudioQuery jsonQuery = JsonConvert.DeserializeObject<AudioQuery>(await response.Content.ReadAsStringAsync());
                return jsonQuery;
            } catch (HttpRequestException e)
            {
                throw e;
            }
        }

        public async Task<bool> Make_Sound(string text, bool upspeak, int speakerId, double volumeScale)
        {

            try
            {
                // queryを取得
                var query = await Make_Query(text, speakerId);
                query.VolumeScale = volumeScale;

                // wavファイルを作成
                string requestUrl = $"{BaseUri}/synthesis?speaker={speakerId}&enable_interrogative_upspeak={upspeak}";
                var request = new HttpRequestMessage(new HttpMethod("POST"), requestUrl);
                request.Headers.TryAddWithoutValidation("accept", "audio/wav");
                request.Content = new StringContent(JsonConvert.SerializeObject(query));
                request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json");
                var response = await Request(request);

                // wavファイルをmp3に変換する
                string waveFilePath = $"{GraphCore.CachePath}\\voice\\{Sub.GetHashCode(text):X}.wav";
                string mp3FileName = $"{GraphCore.CachePath}\\voice\\{Sub.GetHashCode(text):X}.mp3";
                using (var fileStream = File.Create(waveFilePath))
                {
                    using (var httpStream = await response.Content.ReadAsStreamAsync())
                    {
                        httpStream.CopyTo(fileStream);
                        fileStream.Flush();
                    }
                }
                ConvertToMp3(waveFilePath, mp3FileName);
                return true;
            }
            catch
            {
                return false;
            }
        }


    }
}
