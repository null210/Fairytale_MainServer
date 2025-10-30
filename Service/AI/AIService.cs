using System.Text;
using System.Net.Http;
using System.Text.Json;
using FairyTaleExplorer.DTOs;

namespace MainServer.Service.AI
{
    public class AIService : IAIService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AIService> _logger;

        public AIService(HttpClient httpClient, IConfiguration configuration, ILogger<AIService> logger)
        {
            _configuration = configuration;
            _logger = logger;
            _httpClient = httpClient;
            _httpClient.BaseAddress = new Uri(configuration["AIServer:BaseUrl"]);
            _httpClient.DefaultRequestHeaders.Add("X-API-Key", configuration["AIServer:ApiKey"]);
        }

        public async Task<string> GenerateStoryText(string prompt)
        {
            try
            {
                var request = new
                {
                    prompt,
                    max_length = 500,
                    temperature = 0.8
                };

                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("/api/generate/text", content);
                response.EnsureSuccessStatusCode();

                var responseContent = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<GenerateTextResponse>(responseContent);

                _logger.LogInformation("Story text generated successfully");
                return result.Text;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating story text");
                throw;
            }
        }

        public async Task<string> EnrichStory(string baseStory)
        {
            try
            {
                var request = new
                {
                    story = baseStory,
                    style = "creative",
                    add_dialogue = true
                };

                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("/api/llm/enrich", content);
                response.EnsureSuccessStatusCode();

                var responseContent = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<EnrichStoryResponse>(responseContent);

                _logger.LogInformation("Story enriched successfully");
                return result.EnrichedStory;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error enriching story");
                throw;
            }
        }
        
        //************************************************************************************************************************10-30 임강묵 수정
        public async Task<byte[]> GenerateAudio(string text, IFormFile audioFile)
        {
           try
           {
                // 핵심 변경점: JSON 대신 'MultipartFormDataContent'를 사용하여 텍스트와 파일을 함께 보낼 준비
                using var content = new MultipartFormDataContent();
        
                // 텍스트 데이터 추가 (API가 요구하는 필드명: "text")
                content.Add(new StringContent(text), "text");
        
                // 오디오 파일 데이터 추가 (API가 요구하는 필드명: "speaker_wav")
                var fileContent = new StreamContent(audioFile.OpenReadStream());
                fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(audioFile.ContentType);
                content.Add(fileContent, "speaker_wav", audioFile.FileName);

                // 허깅페이스 API 주소로 POST 요청 (이 부분은 기존 코드와 거의 동일)
                var huggingFaceApiUrl = "https://limkang-ttstest.hf.space/generate-speech-api/";
                var response = await _httpClient.PostAsync(huggingFaceApiUrl, content);

                response.EnsureSuccessStatusCode();

                var audioData = await response.Content.ReadAsByteArrayAsync();
                _logger.LogInformation("Audio with reference generated successfully, size: {Size} bytes", audioData.Length);
                return audioData;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating audio with reference file");
                throw;
            }
        }
        //************************************************************************************************************************10-30 임강묵 수정

        public async Task<string> TranslateText(string text, string targetLanguage = "en")
        {
            try
            {
                var request = new
                {
                    text,
                    source_lang = "ko",
                    target_lang = targetLanguage
                };

                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("/api/translate", content);
                response.EnsureSuccessStatusCode();

                var responseContent = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<TranslateResponse>(responseContent);

                _logger.LogInformation("Text translated successfully");
                return result.TranslatedText;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error translating text");
                throw;
            }
        }

        public async Task<string> CallCallbackHook(string data)
        {
            try
            {
                // Callback 후크 처리 (AI 서버와의 양방향 통신)
                var request = new
                {
                    callback_data = data,
                    timestamp = DateTime.UtcNow
                };

                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("/api/callback", content);
                response.EnsureSuccessStatusCode();

                var result = await response.Content.ReadAsStringAsync();
                _logger.LogInformation("Callback processed successfully");
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing callback");
                throw;
            }
        }
    }
}
