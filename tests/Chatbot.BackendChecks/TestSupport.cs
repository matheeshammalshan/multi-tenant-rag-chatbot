using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json.Nodes;
using Application.Interfaces.Integrations;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.AspNetCore.DataProtection;

public class BackendFactory : WebApplicationFactory<Api.ApiAssemblyMarker>
{
    public TestMail Mail { get; } = new();
    public TestProvider Provider { get; } = new();
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseContentRoot(Path.Combine(Directory.GetCurrentDirectory(), "Api"));
        builder.ConfigureServices(services =>
        {
            services.AddDataProtection().UseEphemeralDataProtectionProvider();
            services.RemoveAll<IAccountEmailSender>(); services.AddSingleton<IAccountEmailSender>(Mail);
            services.RemoveAll<IEmbeddingsCreater>(); services.AddSingleton<IEmbeddingsCreater, TestEmbeddings>();
            services.AddHttpClient("Groq").ConfigurePrimaryHttpMessageHandler(() => Provider);
        });
    }
}
public class TestMail : IAccountEmailSender
{
    public ConcurrentDictionary<string, string> Tokens { get; } = new();
    public Task SendPasswordResetAsync(string email, string token) { Tokens[email] = token; return Task.CompletedTask; }
}
public class TestEmbeddings : IEmbeddingsCreater
{
    public Task<float[]?> CreateEmbeddings(string prompt) => Task.FromResult<float[]?>([1, 0, 0]);
}
public class ProviderCall
{
    public string Model { get; set; }
    public double Temperature { get; set; }
    public int MaxTokens { get; set; }
    public double TopP { get; set; }
    public string System { get; set; }
    public string Context { get; set; }

    public ProviderCall(string Model, double Temperature, int MaxTokens, double TopP, string System, string Context)
    {
        this.Model = Model;
        this.Temperature = Temperature;
        this.MaxTokens = MaxTokens;
        this.TopP = TopP;
        this.System = System;
        this.Context = Context;
    }
}
public class TestProvider : HttpMessageHandler
{
    public ConcurrentQueue<ProviderCall> Calls { get; } = new();
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var json = JsonNode.Parse(await request.Content!.ReadAsStringAsync(cancellationToken))!;
        Calls.Enqueue(new(json["model"]!.GetValue<string>(), json["temperature"]!.GetValue<double>(),
            json["max_completion_tokens"]!.GetValue<int>(), json["top_p"]!.GetValue<double>(),
            json["messages"]![0]!["content"]!.GetValue<string>(), json["messages"]![1]!["content"]!.GetValue<string>()));
        return new(HttpStatusCode.OK)
        {
            Content = new StringContent("data: {\"choices\":[{\"delta\":{\"content\":\"Test \"}}]}\n\ndata: {\"choices\":[{\"delta\":{\"content\":\"answer\"}}]}\n\ndata: [DONE]\n\n", Encoding.UTF8, "text/event-stream")
        };
    }
}
public class SignedClient
{
    public HttpClient Client { get; }

    public SignedClient(HttpClient client)
    {
        Client = client;
    }
    public string Csrf { get; private set; } = "";
    public Dictionary<string, string> Cookies { get; } = new();
    public string[] LastCookieHeaders { get; private set; } = [];
    public string CookieHeader => string.Join("; ", Cookies.Select(x => x.Key + "=" + x.Value));
    public void Update(Response result)
    {
        if (result.Json?["csrfToken"] is JsonNode csrf) Csrf = csrf.GetValue<string>();
        LastCookieHeaders = result.CookieHeaders;
        foreach (var header in result.CookieHeaders)
        {
            var value = header.Split(';')[0]; var separator = value.IndexOf('=');
            Cookies[value[..separator]] = value[(separator + 1)..];
        }
    }
}
public class Response
{
    public HttpStatusCode Status { get; set; }
    public string Body { get; set; }
    public JsonNode? Json { get; set; }
    public string[] CookieHeaders { get; set; }

    public Response(HttpStatusCode Status, string Body, JsonNode? Json, string[] CookieHeaders)
    {
        this.Status = Status;
        this.Body = Body;
        this.Json = Json;
        this.CookieHeaders = CookieHeaders;
    }
}
public static class Check
{
    public static void Assert(bool condition, string message)
    {
        if (!condition) throw new Exception("FAIL: " + message);
        Console.WriteLine("PASS: " + message);
    }
    public static async Task<Response> Send(SignedClient client, HttpMethod method, string path, object? body, HttpStatusCode expected,
        bool csrf = true, string? apiKey = null)
    {
        using var request = new HttpRequestMessage(method, path);
        if (body is not null) request.Content = JsonContent.Create(body);
        if (csrf && client.Csrf.Length > 0) request.Headers.Add("X-CSRF-Token", client.Csrf);
        if (apiKey is not null) request.Headers.Add("x-api-key", apiKey);
        var response = await client.Client.SendAsync(request);
        var result = await Result(response);
        if (result.Status != expected) throw new Exception($"{method} {path}: expected {(int)expected}, received {(int)result.Status}: {result.Body}");
        return result;
    }
    public static async Task Status(HttpClient client, HttpMethod method, string path, object? body, HttpStatusCode expected, string message)
    {
        await Send(new(client), method, path, body, expected); Assert(true, message);
    }
    public static async Task<Response> Result(HttpResponseMessage response)
    {
        var text = await response.Content.ReadAsStringAsync();
        JsonNode? json = null;
        if (text.Length > 0 && response.Content.Headers.ContentType?.MediaType != "text/event-stream") json = JsonNode.Parse(text);
        return new(response.StatusCode, text, json, response.Headers.TryGetValues("Set-Cookie", out var values) ? values.ToArray() : []);
    }
    public static async Task<int> Upload(SignedClient client, int chatbotId, string text, string filename, string reader = "Text")
    {
        using var form = new MultipartFormDataContent();
        form.Add(new StringContent(chatbotId.ToString()), "ChatBotId"); form.Add(new StringContent(reader), "ReaderType");
        form.Add(new ByteArrayContent(Encoding.UTF8.GetBytes(text)), "File", filename);
        using var request = new HttpRequestMessage(HttpMethod.Post, "/Document/create") { Content = form };
        request.Headers.Add("X-CSRF-Token", client.Csrf);
        var result = await Result(await client.Client.SendAsync(request));
        if (result.Status != HttpStatusCode.Accepted) throw new Exception("Upload failed: " + result.Body);
        return result.Json!["documentId"]!.GetValue<int>();
    }
    public static async Task<JsonNode> WaitDocument(SignedClient client, int id, string status)
    {
        for (var attempt = 0; attempt < 40; attempt++)
        {
            var result = await Send(client, HttpMethod.Get, $"/Documents/{id}", null, HttpStatusCode.OK);
            var state = result.Json!["status"]!.GetValue<string>();
            if (state == status) return result.Json;
            if (state == "Failed" && status != "Failed") throw new Exception("Document failed: " + result.Body);
            await Task.Delay(500);
        }
        throw new Exception("Document did not reach status " + status);
    }
    public static Task<HttpResponseMessage> RawRefresh(HttpClient client, string token, string csrf)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/Auth/refresh");
        request.Headers.Add("Cookie", "refresh_token=" + token); request.Headers.Add("X-CSRF-Token", csrf);
        return client.SendAsync(request);
    }
}
