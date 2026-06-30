using System.Net.Http;
using System.Net.Http.Json;
using System.IO;
using System.Text;
using System.Text.Json;
using OllamaDesktopChat.src.Models;

namespace OllamaDesktopChat.src.Services;

public class OllamaService
{
    private const string DefaultEndpoint = "http://localhost:11434/";
    private readonly HttpClient _httpClient;

    public OllamaService(HttpClient? httpClient = null)
    {
        _httpClient = httpClient ?? new HttpClient();
        _httpClient.BaseAddress = new Uri(DefaultEndpoint);
    }

    public async Task<IReadOnlyList<OllamaModel>> GetModelsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using var response = await _httpClient.GetAsync("api/tags", cancellationToken);
            response.EnsureSuccessStatusCode();

            var payload = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);
            if (!payload.TryGetProperty("models", out var modelsElement) || modelsElement.ValueKind != JsonValueKind.Array)
            {
                return Array.Empty<OllamaModel>();
            }

            var models = new List<OllamaModel>();
            foreach (var model in modelsElement.EnumerateArray())
            {
                var name = model.TryGetProperty("name", out var nameEl) ? nameEl.GetString() ?? string.Empty : string.Empty;
                var size = model.TryGetProperty("size", out var sizeEl) && sizeEl.ValueKind == JsonValueKind.Number
                    ? sizeEl.GetInt64()
                    : 0;

                var modifiedAt = DateTime.MinValue;
                if (model.TryGetProperty("modified_at", out var modifiedAtEl))
                {
                    _ = DateTime.TryParse(modifiedAtEl.GetString(), out modifiedAt);
                }

                models.Add(new OllamaModel
                {
                    Name = name,
                    Size = size,
                    ModifiedAt = modifiedAt
                });
            }

            return models;
        }
        catch (HttpRequestException ex)
        {
            throw new InvalidOperationException("Could not connect to Ollama at http://localhost:11434. Confirm Ollama is installed and running.", ex);
        }
    }

    public async Task<string> SendMessageAsync(string model, IEnumerable<ChatMessage> messages, CancellationToken cancellationToken = default)
    {
        var builder = new StringBuilder();

        await StreamMessageAsync(
            model,
            messages,
            token => builder.Append(token),
            cancellationToken);

        return builder.ToString();
    }

    public async Task StreamMessageAsync(
        string model,
        IEnumerable<ChatMessage> messages,
        Action<string> onToken,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(model))
        {
            throw new ArgumentException("A model must be selected before sending a message.", nameof(model));
        }

        var requestBody = new
        {
            model,
            stream = true,
            messages = messages
                .Where(m => m.Role is "user" or "assistant")
                .Select(m => new
                {
                    role = m.Role,
                    content = m.Content
                })
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, "api/chat")
        {
            Content = JsonContent.Create(requestBody)
        };

        try
        {
            using var response = await _httpClient.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);

            response.EnsureSuccessStatusCode();

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var reader = new StreamReader(stream);

            while (!reader.EndOfStream)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var line = await reader.ReadLineAsync(cancellationToken);
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                using var json = JsonDocument.Parse(line);
                var root = json.RootElement;

                if (root.TryGetProperty("error", out var errorEl))
                {
                    throw new InvalidOperationException(errorEl.GetString() ?? "Ollama returned an unknown error.");
                }

                if (root.TryGetProperty("message", out var messageEl)
                    && messageEl.TryGetProperty("content", out var contentEl)
                    && contentEl.ValueKind == JsonValueKind.String)
                {
                    var token = contentEl.GetString();
                    if (!string.IsNullOrEmpty(token))
                    {
                        onToken(token);
                    }
                }

                if (root.TryGetProperty("done", out var doneEl)
                    && doneEl.ValueKind == JsonValueKind.True)
                {
                    break;
                }
            }
        }
        catch (HttpRequestException ex)
        {
            throw new InvalidOperationException("Unable to reach Ollama. Verify that the local Ollama server is running.", ex);
        }
    }

    /// <summary>
    /// Analyzes an image using a vision model (e.g., llava).
    /// Sends base64-encoded image to model for analysis.
    /// </summary>
    /// <param name="model">Vision model name (e.g., "llava")</param>
    /// <param name="imagePath">Full path to image file</param>
    /// <param name="prompt">Optional prompt/question about the image</param>
    /// <param name="onToken">Callback for streaming response tokens</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task AnalyzeImageAsync(
        string model,
        string imagePath,
        string prompt,
        Action<string> onToken,
        CancellationToken cancellationToken = default)
    {
        if (!File.Exists(imagePath))
        {
            throw new FileNotFoundException($"Image file not found: {imagePath}");
        }

        // Read and encode image as base64
        var imageData = await Task.Run(() => File.ReadAllBytes(imagePath));
        var base64Image = Convert.ToBase64String(imageData);

        var requestBody = new
        {
            model,
            stream = true,
            messages = new[]
            {
                new
                {
                    role = "user",
                    content = prompt ?? "Analyze this image.",
                    images = new[] { base64Image }
                }
            }
        };

        await StreamImageResponseAsync(requestBody, onToken, cancellationToken);
    }

    /// <summary>
    /// Analyzes text file content by sending it to a model.
    /// </summary>
    /// <param name="model">Model name</param>
    /// <param name="filePath">Full path to text file</param>
    /// <param name="prompt">Optional prompt/question about the file</param>
    /// <param name="onToken">Callback for streaming response tokens</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task AnalyzeTextFileAsync(
        string model,
        string filePath,
        string prompt,
        Action<string> onToken,
        CancellationToken cancellationToken = default)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"Text file not found: {filePath}");
        }

        // Read file content
        var fileContent = await File.ReadAllTextAsync(filePath, cancellationToken);

        // Truncate if too large (Ollama has limits on message size)
        if (fileContent.Length > 10000)
        {
            fileContent = fileContent.Substring(0, 10000) + "\n... (content truncated)";
        }

        var requestBody = new
        {
            model,
            stream = true,
            messages = new[]
            {
                new
                {
                    role = "user",
                    content = $"{prompt ?? "Analyze this file content."}\n\n```\n{fileContent}\n```"
                }
            }
        };

        await StreamMessageFromObjectAsync(requestBody, onToken, cancellationToken);
    }

    /// <summary>
    /// Internal helper to stream response from an image analysis request.
    /// </summary>
    private async Task StreamImageResponseAsync(
        object requestBody,
        Action<string> onToken,
        CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "api/chat")
        {
            Content = JsonContent.Create(requestBody)
        };

        try
        {
            using var response = await _httpClient.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);

            response.EnsureSuccessStatusCode();

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var reader = new StreamReader(stream);

            while (!reader.EndOfStream)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var line = await reader.ReadLineAsync(cancellationToken);
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                using var json = JsonDocument.Parse(line);
                var root = json.RootElement;

                if (root.TryGetProperty("error", out var errorEl))
                {
                    throw new InvalidOperationException(errorEl.GetString() ?? "Ollama returned an unknown error.");
                }

                if (root.TryGetProperty("message", out var messageEl)
                    && messageEl.TryGetProperty("content", out var contentEl)
                    && contentEl.ValueKind == JsonValueKind.String)
                {
                    var token = contentEl.GetString();
                    if (!string.IsNullOrEmpty(token))
                    {
                        onToken(token);
                    }
                }

                if (root.TryGetProperty("done", out var doneEl)
                    && doneEl.ValueKind == JsonValueKind.True)
                {
                    break;
                }
            }
        }
        catch (HttpRequestException ex)
        {
            throw new InvalidOperationException("Unable to reach Ollama. Verify that the local Ollama server is running.", ex);
        }
    }

    /// <summary>
    /// Internal helper to stream response from a request body object.
    /// </summary>
    private async Task StreamMessageFromObjectAsync(
        object requestBody,
        Action<string> onToken,
        CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "api/chat")
        {
            Content = JsonContent.Create(requestBody)
        };

        try
        {
            using var response = await _httpClient.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);

            response.EnsureSuccessStatusCode();

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var reader = new StreamReader(stream);

            while (!reader.EndOfStream)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var line = await reader.ReadLineAsync(cancellationToken);
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                using var json = JsonDocument.Parse(line);
                var root = json.RootElement;

                if (root.TryGetProperty("error", out var errorEl))
                {
                    throw new InvalidOperationException(errorEl.GetString() ?? "Ollama returned an unknown error.");
                }

                if (root.TryGetProperty("message", out var messageEl)
                    && messageEl.TryGetProperty("content", out var contentEl)
                    && contentEl.ValueKind == JsonValueKind.String)
                {
                    var token = contentEl.GetString();
                    if (!string.IsNullOrEmpty(token))
                    {
                        onToken(token);
                    }
                }

                if (root.TryGetProperty("done", out var doneEl)
                    && doneEl.ValueKind == JsonValueKind.True)
                {
                    break;
                }
            }
        }
        catch (HttpRequestException ex)
        {
            throw new InvalidOperationException("Unable to reach Ollama. Verify that the local Ollama server is running.", ex);
        }
    }
}