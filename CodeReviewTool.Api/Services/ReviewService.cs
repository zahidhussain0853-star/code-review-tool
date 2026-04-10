using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace CodeReviewTool.Api.Services;

public class ReviewService
{
    private readonly HttpClient _httpClient;
    private readonly string? _openAiKey;
    private readonly string? _geminiKey;
    private readonly string? _openRouterKey;

    public ReviewService(IConfiguration config)
    {
        _httpClient = new HttpClient();

        _openAiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        _geminiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY");
        _openRouterKey = Environment.GetEnvironmentVariable("OPENROUTER_API_KEY");
    }

    // ================= MAIN ENTRY =================
    public async Task<string> GenerateAiReview(string before, string after)
    {
        // 1. Gemini
        try
        {
            Console.WriteLine("\n--- Trying Gemini ---");
            var result = await CallGemini(before, after);

            if (!IsError(result))
            {
                Console.WriteLine("Gemini SUCCESS");
                return "[Gemini]\n\n" + result;
            }

            Console.WriteLine("Gemini FAILED: " + result);
        }
        catch (Exception ex)
        {
            Console.WriteLine("Gemini EXCEPTION: " + ex.Message);
        }

        // 2. OpenAI
        try
        {
            Console.WriteLine("\n--- Trying OpenAI ---");
            var result = await CallOpenAI(before, after);

            if (!IsError(result))
            {
                Console.WriteLine("OpenAI SUCCESS");
                return "[OpenAI]\n\n" + result;
            }

            Console.WriteLine("OpenAI FAILED: " + result);
        }
        catch (Exception ex)
        {
            Console.WriteLine("OpenAI EXCEPTION: " + ex.Message);
        }

        // 3. OpenRouter
        try
        {
            Console.WriteLine("\n--- Trying OpenRouter ---");
            var result = await CallOpenRouter(before, after);

            if (!IsError(result))
            {
                Console.WriteLine("OpenRouter SUCCESS");
                return "[OpenRouter]\n\n" + result;
            }

            Console.WriteLine("OpenRouter FAILED: " + result);
        }
        catch (Exception ex)
        {
            Console.WriteLine("OpenRouter EXCEPTION: " + ex.Message);
        }

        // 4. Mock fallback
        Console.WriteLine("\n--- Falling back to Mock ---");
        return "[Mock Fallback]\n\n" + MockReview(before, after);
    }

    // ================= ERROR CHECK (FIXED) =================
    private bool IsError(string result)
    {
        if (string.IsNullOrWhiteSpace(result))
            return true;

        var lower = result.ToLower();

        // Detect real API errors only
        return lower.Contains("\"error\"")
            || lower.Contains("invalid_api_key")
            || lower.Contains("insufficient_quota")
            || lower.Contains("quota exceeded")
            || lower.Contains("not found")
            || lower.StartsWith("openai error")
            || lower.StartsWith("gemini error")
            || lower.StartsWith("openrouter error");
    }

    // ================= MOCK =================
    private string MockReview(string before, string after)
    {
        var feedback = new List<string>();

        if (before == after)
            feedback.Add("No changes detected.");

        if (after.Contains("Console.WriteLine"))
            feedback.Add("Avoid Console.WriteLine in production code.");

        if (after.Length > before.Length)
            feedback.Add("Code size increased. Consider simplifying.");

        if (!after.Contains("try"))
            feedback.Add("Consider adding error handling.");

        return string.Join("\n", feedback);
    }

    // ================= OPENAI =================
    private async Task<string> CallOpenAI(string before, string after)
    {
        if (string.IsNullOrEmpty(_openAiKey))
            return "OpenAI key missing";

        var prompt = BuildPrompt(before, after);

        var body = new
        {
            model = "gpt-4o-mini",
            messages = new[] { new { role = "user", content = prompt } }
        };

        var request = new HttpRequestMessage(HttpMethod.Post,
            "https://api.openai.com/v1/chat/completions");

        request.Headers.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _openAiKey);

        request.Content = new StringContent(
            JsonSerializer.Serialize(body),
            Encoding.UTF8,
            "application/json");

        var response = await _httpClient.SendAsync(request);
        var json = await response.Content.ReadAsStringAsync();

        Console.WriteLine("OpenAI RAW RESPONSE: " + json);

        try
        {
            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString() ?? "";
        }
        catch
        {
            return "OpenAI error: " + json;
        }
    }

    // ================= GEMINI =================
    private async Task<string> CallGemini(string before, string after)
    {
        if (string.IsNullOrEmpty(_geminiKey))
            return "Gemini key missing";

        var prompt = BuildPrompt(before, after);

        var body = new
        {
            contents = new[]
            {
                new
                {
                    parts = new[] { new { text = prompt } }
                }
            }
        };

        var response = await _httpClient.PostAsync(
            $"https://generativelanguage.googleapis.com/v1/models/gemini-2.0-flash:generateContent?key={_geminiKey}",
            new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json"));

        var json = await response.Content.ReadAsStringAsync();

        Console.WriteLine("Gemini RAW RESPONSE: " + json);

        try
        {
            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.GetProperty("candidates")[0]
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("text")
                .GetString() ?? "";
        }
        catch
        {
            return "Gemini error: " + json;
        }
    }

    // ================= OPENROUTER =================
    private async Task<string> CallOpenRouter(string before, string after)
    {
        if (string.IsNullOrEmpty(_openRouterKey))
            return "OpenRouter key missing";

        var prompt = BuildPrompt(before, after);

        var body = new
        {
            model = "openai/gpt-3.5-turbo",
            messages = new[] { new { role = "user", content = prompt } }
        };

        var request = new HttpRequestMessage(HttpMethod.Post,
            "https://openrouter.ai/api/v1/chat/completions");

        request.Headers.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _openRouterKey);

        request.Headers.Add("HTTP-Referer", "http://localhost");
        request.Headers.Add("X-Title", "CodeReviewTool");

        request.Content = new StringContent(
            JsonSerializer.Serialize(body),
            Encoding.UTF8,
            "application/json");

        var response = await _httpClient.SendAsync(request);
        var json = await response.Content.ReadAsStringAsync();

        Console.WriteLine("OpenRouter RAW RESPONSE: " + json);

        try
        {
            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString() ?? "";
        }
        catch
        {
            return "OpenRouter error: " + json;
        }
    }

    // ================= PROMPT =================
    private string BuildPrompt(string before, string after)
    {
        return $@"
You are a senior C# code reviewer.

Compare BEFORE and AFTER code and provide:
- Improvements
- Potential issues
- Code quality suggestions

Use short bullet points.

BEFORE:
{before}

AFTER:
{after}";
    }
}