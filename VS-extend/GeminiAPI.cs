using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using EnvDTE;
using Microsoft.VisualStudio.RpcContracts.Commands;

// Gemini API의 최종 응답을 나타내는 모델
public class GeminiResponse
{
    [JsonPropertyName("problem_found")]
    public bool ProblemFound { get; set; }

    [JsonPropertyName("message")]
    public string Message { get; set; }
}
public class GeminiFeedbackService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly string _model = "gemini-2.5-flash"; // 사용할 모델 지정

    public GeminiFeedbackService(string apiKey)
    {
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        _httpClient.DefaultRequestHeaders.Add("x-goog-api-key", _apiKey);
    }

    public async Task<GeminiResponse> GetFeedbackAsync(
        string promptContent)
    {
        string systemPrompt = $"아래 코드에서 os 종속적인 부분이 있는지 확인해줘. os 종속적인 내용이 있다면 문제가 있는거야. 만약 문자게 있다면 problem_found를 true로, 없다면 false로 반환해줘. message에는 어느 부분이 문제인지 아주 간단하게 설명해줘. 문제가 없다면 그냥 \"아무 문제도 발견되지 않았습니다.\"라고 써줘.";

        var requestBody = new
        {
            contents = new
            {
                text = systemPrompt + "\n---실제 코드---\n" + promptContent
            },
            generationConfig = new
            {
                responseMimeType = "application/json",
                responseSchema = new {
                    type = "OBJECT",
                    properties = new {
                        problem_found = new { type = "BOOLEAN", description = "os 종속적인 부분이 있는지 여부" },
                        message = new { type = "STRING", description = "문제가 있는 부분에 대한 간단한 설명" }
                    }
                }
            }
        };

        var jsonPayload = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

        string url = $"https://generativelanguage.googleapis.com/v1/models/{_model}/generateContent";

        var response = await _httpClient.PostAsync(url, content);
        response.EnsureSuccessStatusCode();

        var rawResponse = await response.Content.ReadAsStringAsync();

        var parsedResponse = JsonSerializer.Deserialize<GeminiResponse>(rawResponse);

        return parsedResponse;
    }
}
