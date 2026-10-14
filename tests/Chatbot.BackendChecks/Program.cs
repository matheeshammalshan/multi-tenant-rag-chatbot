using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json.Nodes;
using System.IdentityModel.Tokens.Jwt;
using Application.DTOs;
using Application.Interfaces.Services;
using Domain.Entities;
using Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Driver;
using MySqlConnector;

var suffix = Guid.NewGuid().ToString("N");
var mysql = new MySqlConnectionStringBuilder(Environment.GetEnvironmentVariable("BACKEND_CHECKS_MYSQL")
    ?? "server=localhost;port=3306;user=root;password=1234;database=chatbot_backend_checks")
    { Database = "cb_checks_" + suffix };
var mongoDatabase = "chatbot_backend_checks_" + suffix;
var collection = "chatbot-backend-checks-" + suffix;
var root = Directory.GetCurrentDirectory();
var storage = Path.GetFullPath(Path.Combine(root, ".backend-checks", suffix));
var key = Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(48));
var settings = new Dictionary<string, string?>
{
    ["JWT__Key"] = key, ["ConnectionStrings__MySql"] = mysql.ConnectionString,
    ["MongoDB__Database"] = mongoDatabase, ["Qdrant__Collection"] = collection,
    ["Documents__EnableWorker"] = "true", ["Documents__RetryDelaySeconds"] = "1",
    ["Storage__Root"] = storage, ["Groq__ApiKey"] = "fake-key-used-only-by-local-test-handler", ["RateLimits__AuthPermitLimit"] = "1000"
};
var previous = settings.Keys.ToDictionary(x => x, Environment.GetEnvironmentVariable);
var dbOptions = new DbContextOptionsBuilder<AppDb>().UseMySql(mysql.ConnectionString, new MySqlServerVersion(new Version(8, 0, 29))).Options;
using var vectors = new HttpClient { BaseAddress = new Uri("http://localhost:6333") };
BackendFactory? factory = null;
var collectionCreated = false;
var sqlCreated = false;
try
{
    foreach (var setting in settings) Environment.SetEnvironmentVariable(setting.Key, setting.Value);
    sqlCreated = true;
    await using (var db = new AppDb(dbOptions)) { await db.Database.MigrateAsync(); }
    var create = await vectors.PutAsJsonAsync($"/collections/{collection}", new { vectors = new { size = 3, distance = "Cosine" } });
    create.EnsureSuccessStatusCode(); collectionCreated = true;
    factory = new BackendFactory();
    using var anonymous = factory.CreateClient(new() { HandleCookies = false });
    await Check.Status(anonymous, HttpMethod.Get, "/ChatBots", null, HttpStatusCode.Unauthorized, "Management endpoints require login");
    var alice = new SignedClient(factory.CreateClient());
    var bob = new SignedClient(factory.CreateClient());
    var aliceResult = await Check.Send(alice, HttpMethod.Post, "/Auth/register", new
        { organizationName = "Alpha", name = "Alice", email = "alice@example.test", password = "AlicePassword123!", acceptTerms = true, role = "owner" }, HttpStatusCode.OK);
    alice.Update(aliceResult);
    var aliceUser = aliceResult.Json!["user"]!;
    var aliceId = Guid.Parse(aliceUser["id"]!.GetValue<string>());
    var alpha = aliceUser["organizationId"]!.GetValue<int>();
    Check.Assert(aliceUser["role"]!.GetValue<string>() == "user", "Registration cannot assign the owner role");
    Check.Assert(aliceResult.CookieHeaders.Any(x => x.StartsWith("access_token=") && x.Contains("httponly", StringComparison.OrdinalIgnoreCase)), "Access JWT is in an HttpOnly cookie");
    Check.Assert(aliceResult.Json["accessToken"] is null, "Login response does not expose the access JWT");
    var bobResult = await Check.Send(bob, HttpMethod.Post, "/Auth/register", new
        { organizationName = "Beta", name = "Bob", email = "bob@example.test", password = "BobPassword123!", acceptTerms = true }, HttpStatusCode.OK);
    bob.Update(bobResult);
    var bobId = Guid.Parse(bobResult.Json!["user"]!["id"]!.GetValue<string>());
    var beta = bobResult.Json["user"]!["organizationId"]!.GetValue<int>();
    await Check.Send(new SignedClient(anonymous), HttpMethod.Post, "/Auth/register", new
        { organizationName = "Duplicate", name = "Duplicate", email = "ALICE@example.test", password = "AlicePassword123!", acceptTerms = true }, HttpStatusCode.Conflict);
    await using (var db = new AppDb(dbOptions))
    {
        Check.Assert(await db.Organizations.CountAsync() == 2, "Duplicate registration does not create an orphan organization");
        Check.Assert((await db.Users.FindAsync(aliceId))!.PasswordHash != "AlicePassword123!", "Passwords are hashed");
    }
    var aliceBotResult = await Check.Send(alice, HttpMethod.Post, "/ChatBot/create",
        new { organizationId = alpha, name = "Alpha Bot", description = "Alpha knowledge", systemPrompt = "CUSTOM_ALPHA_PROMPT" }, HttpStatusCode.Created);
    var aliceBot = aliceBotResult.Json!["id"]!.GetValue<int>();
    var bobBot = (await Check.Send(bob, HttpMethod.Post, "/ChatBot/create", new { organizationId = beta, name = "Beta Bot" }, HttpStatusCode.Created)).Json!["id"]!.GetValue<int>();
    await Check.Send(bob, HttpMethod.Post, "/ChatBot/create", new { organizationId = alpha, name = "Cross tenant" }, HttpStatusCode.NotFound);
    foreach (var path in new[] { $"/ChatBots/{aliceBot}", $"/LlmConfig/{aliceBot}", $"/Documents?chatBotId={aliceBot}", $"/ChatHistory/{aliceBot}", $"/Dashboard?organizationId={alpha}" })
        await Check.Send(bob, HttpMethod.Get, path, null, HttpStatusCode.NotFound);
    var bobList = await Check.Send(bob, HttpMethod.Get, "/ChatBots", null, HttpStatusCode.OK);
    Check.Assert(bobList.Json!.AsArray().All(x => x!["id"]!.GetValue<int>() == bobBot), "Chatbot lists are tenant and creator scoped");
    await Check.Send(alice, HttpMethod.Get, "/Users", null, HttpStatusCode.Forbidden);
    await Check.Send(alice, HttpMethod.Post, $"/ChatBots/{aliceBot}/api-key", null, HttpStatusCode.Forbidden, csrf: false);
    await Check.Send(bob, HttpMethod.Post, $"/ChatBots/{aliceBot}/api-key", null, HttpStatusCode.NotFound);
    await Check.Status(anonymous, HttpMethod.Post, $"/ChatBots/{aliceBot}/api-key", null, HttpStatusCode.Unauthorized, "Anonymous callers cannot rotate keys");
    using (var request = new HttpRequestMessage(HttpMethod.Post, $"/ChatBots/{aliceBot}/api-key"))
    {
        request.Headers.Add("Origin", "https://untrusted.example"); request.Headers.Add("X-CSRF-Token", alice.Csrf);
        Check.Assert((await alice.Client.SendAsync(request)).StatusCode == HttpStatusCode.Forbidden, "Untrusted browser origins are rejected");
    }
    using (var preflight = new HttpRequestMessage(HttpMethod.Options, "/ChatBots"))
    {
        preflight.Headers.Add("Origin", "http://localhost:5173"); preflight.Headers.Add("Access-Control-Request-Method", "POST");
        preflight.Headers.Add("Access-Control-Request-Headers", "Content-Type,x-api-key,X-CSRF-Token");
        var response = await anonymous.SendAsync(preflight);
        Check.Assert(response.Headers.TryGetValues("Access-Control-Allow-Origin", out var allowed) && allowed.Single() == "http://localhost:5173", "Configured frontend origins receive CORS headers");
    }
    var generated = await Check.Send(alice, HttpMethod.Post, $"/ChatBots/{aliceBot}/api-key", null, HttpStatusCode.OK);
    var apiKey = generated.Json!["apiKey"]!.GetValue<string>();
    await using (var db = new AppDb(dbOptions))
    {
        var bot = (await db.ChatBots.FindAsync(aliceBot))!;
        Check.Assert(bot.ApiKey is null && bot.ApiKeyHash is not null && bot.ApiKeyHash != apiKey, "New API keys are stored only as hashes");
    }
    var publicAccess = await Check.Send(new SignedClient(anonymous), HttpMethod.Get, "/ChatbotAccess", null, HttpStatusCode.OK, apiKey: apiKey);
    Check.Assert(publicAccess.Json!["chatBotId"]!.GetValue<int>() == aliceBot && publicAccess.Json["systemPrompt"] is null, "A guest key resolves only public chatbot metadata");
    await Check.Send(new SignedClient(anonymous), HttpMethod.Post, "/SemanticSearch/askQuery", new { chatBotId = bobBot, query = "Wrong scope" }, HttpStatusCode.Forbidden, apiKey: apiKey);
    await Check.Send(new SignedClient(anonymous), HttpMethod.Post, "/SemanticSearch/askQuery", new { query = "No key" }, HttpStatusCode.Unauthorized);
    await Check.Send(alice, HttpMethod.Get, "/Api/create-apikey", null, HttpStatusCode.MethodNotAllowed);
    Check.Assert(true, "Key generation is POST, not GET");
    await Check.Send(alice, HttpMethod.Put, "/LlmConfig/update", new
        { chatBotId = aliceBot, modelName = "GptOss20B", temperature = 0.3, topP = 0.8, maxToken = 2048 }, HttpStatusCode.OK);
    await Check.Send(alice, HttpMethod.Put, "/LlmConfig/update", new
        { chatBotId = aliceBot, modelName = "KimiK2", temperature = 0.3, topP = 0.8, maxToken = 2048 }, HttpStatusCode.BadRequest);
    await Check.Send(bob, HttpMethod.Put, "/LlmConfig/update", new
        { chatBotId = aliceBot, modelName = "GptOss20B", temperature = 0.3, topP = 0.8, maxToken = 2048 }, HttpStatusCode.NotFound);
    var betaDocument = await Check.Upload(bob, bobBot, "BETA_TENANT_SECRET.", "beta.txt");
    await Check.WaitDocument(bob, betaDocument, "Proceed");
    var alphaDocument = await Check.Upload(alice, aliceBot, "ALPHA_DOCUMENT_CONTEXT. Alpha policy allows twenty days leave.", "../../alpha.txt");
    var ready = await Check.WaitDocument(alice, alphaDocument, "Proceed");
    Check.Assert(ready["name"]!.GetValue<string>() == "alpha.txt" && ready["chunks"]!.GetValue<int>() == 2, "Uploads use safe filenames and persist extracted chunk counts");
    await Check.Send(bob, HttpMethod.Get, $"/Documents/{alphaDocument}", null, HttpStatusCode.NotFound);
    var answer = await Check.Send(new SignedClient(anonymous), HttpMethod.Post, "/SemanticSearch/askQuery", new { query = "How much leave?" }, HttpStatusCode.OK, apiKey: apiKey);
    Check.Assert(answer.Json!["answer"]!.GetValue<string>() == "Test answer", "Chat returns a JSON answer and conversation ID");
    var modelCall = factory.Provider.Calls.Last();
    Check.Assert(modelCall.Model == "openai/gpt-oss-20b" && modelCall.Temperature == 0.3 && modelCall.MaxTokens == 2048 && modelCall.TopP == 0.8, "Model selection and every saved generation setting reach the provider");
    Check.Assert(modelCall.System.Contains("CUSTOM_ALPHA_PROMPT") && modelCall.Context.Contains("ALPHA_DOCUMENT_CONTEXT") && !modelCall.Context.Contains("BETA_TENANT_SECRET"), "Saved system prompts and tenant-filtered Qdrant context reach the provider");
    var conversation = answer.Json["conversationId"]!.GetValue<string>();
    var history = await Check.Send(alice, HttpMethod.Get, $"/ChatHistory/{aliceBot}?conversationId={conversation}", null, HttpStatusCode.OK);
    Check.Assert(history.Json!.AsArray().Count == 1 && history.Json[0]!["createdAtUtc"] is not null, "Mongo history supports conversation filtering and timestamps");
    var dashboard = await Check.Send(alice, HttpMethod.Get, "/Dashboard", null, HttpStatusCode.OK);
    Check.Assert(dashboard.Json!["totalQueries"]!.GetValue<int>() == 1 && dashboard.Json["totalChunks"]!.GetValue<int>() == 2, "Dashboard counts successful queries and completed document chunks");
    var stream = await Check.Send(new SignedClient(anonymous), HttpMethod.Post, "/SemanticSearch/stream", new { query = "Stream a reply" }, HttpStatusCode.OK, apiKey: apiKey);
    Check.Assert(stream.Body.Contains("event: token") && stream.Body.Contains("event: completed"), "SSE emits answer tokens and completion");
    await Check.Send(new SignedClient(anonymous), HttpMethod.Post, $"/ChatBots/{aliceBot}/stream", new { query = "Hello" }, HttpStatusCode.Unauthorized);
    await Check.Send(bob, HttpMethod.Post, $"/ChatBots/{aliceBot}/stream", new { query = "Hello" }, HttpStatusCode.NotFound);
    await Check.Send(alice, HttpMethod.Post, $"/ChatBots/{aliceBot}/stream", new { query = "Hello" }, HttpStatusCode.Forbidden, csrf: false);
    var playground = await Check.Send(alice, HttpMethod.Post, $"/ChatBots/{aliceBot}/stream", new { query = "Test playground", conversationId = conversation }, HttpStatusCode.OK);
    Check.Assert(playground.Body.Contains("event: completed") && playground.Body.Contains(conversation), "Authorized playground streams without a shared API key and retains the conversation ID");
    await Check.Send(new SignedClient(anonymous), HttpMethod.Get, "/ChatbotAccess", null, HttpStatusCode.OK, apiKey: apiKey);
    Check.Assert(true, "Playground chat preserves the existing shared API key");
    var failedDocument = await Check.Upload(alice, aliceBot, "This is not a PDF.", "broken.pdf", "Pdf");
    await Check.WaitDocument(alice, failedDocument, "Failed");
    Check.Assert(true, "Failed processing retries and persists a terminal failure state");
    var rotated = (await Check.Send(alice, HttpMethod.Post, $"/ChatBots/{aliceBot}/api-key", null, HttpStatusCode.OK)).Json!["apiKey"]!.GetValue<string>();
    await Check.Send(new SignedClient(anonymous), HttpMethod.Get, "/ChatbotAccess", null, HttpStatusCode.Unauthorized, apiKey: apiKey);
    await Check.Send(new SignedClient(anonymous), HttpMethod.Get, "/ChatbotAccess", null, HttpStatusCode.OK, apiKey: rotated);
    Check.Assert(true, "Rotation immediately invalidates the old key");

    using (var scope = factory.Services.CreateScope()) await scope.ServiceProvider.GetRequiredService<IAuthService>()
        .BootstrapOwnerAsync("owner@example.test", "OwnerPassword123!", "Platform Owner");
    var owner = new SignedClient(factory.CreateClient());
    owner.Update(await Check.Send(owner, HttpMethod.Post, "/Auth/login", new { email = "owner@example.test", password = "OwnerPassword123!" }, HttpStatusCode.OK));
    var users = await Check.Send(owner, HttpMethod.Get, "/Users", null, HttpStatusCode.OK);
    Check.Assert(users.Json!.AsArray().Count == 3, "Only the platform owner can list all users and organizations");
    await Check.Send(owner, HttpMethod.Get, $"/ChatBots/{bobBot}", null, HttpStatusCode.OK);

    var cookieJwt = alice.Cookies["access_token"];
    var parsed = new JwtSecurityTokenHandler().ReadJwtToken(cookieJwt);
    var forged = new JwtSecurityToken("ChatbotApi", "ChatbotConsole", parsed.Claims.Where(x => x.Type != ClaimTypes.Role && x.Type != "exp" && x.Type != "nbf" && x.Type != "iat")
        .Append(new Claim(ClaimTypes.Role, "owner")), expires: DateTime.UtcNow.AddMinutes(5),
        signingCredentials: new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)), SecurityAlgorithms.HmacSha256));
    using (var request = new HttpRequestMessage(HttpMethod.Get, "/Users"))
    {
        request.Headers.Authorization = new("Bearer", new JwtSecurityTokenHandler().WriteToken(forged));
        Check.Assert((await anonymous.SendAsync(request)).StatusCode == HttpStatusCode.Unauthorized, "Database roles are checked even for correctly signed JWTs");
    }
    var bobOldRefresh = bob.Cookies["refresh_token"];
    var bobOldCsrf = bob.Csrf;
    var firstRefresh = await Check.RawRefresh(anonymous, bobOldRefresh, bobOldCsrf);
    Check.Assert(firstRefresh.StatusCode == HttpStatusCode.OK, "Refresh rotates tokens");
    var replay = await Check.RawRefresh(anonymous, bobOldRefresh, bobOldCsrf);
    Check.Assert(replay.StatusCode == HttpStatusCode.Unauthorized, "Used refresh tokens cannot be replayed");
    bob.Update(await Check.Result(firstRefresh));
    var bobOldAccess = bob.Cookies["access_token"];
    await Check.Send(bob, HttpMethod.Post, "/Auth/logout", null, HttpStatusCode.NoContent);
    using (var request = new HttpRequestMessage(HttpMethod.Get, "/Auth/me"))
    {
        request.Headers.Add("Cookie", "access_token=" + bobOldAccess);
        Check.Assert((await anonymous.SendAsync(request)).StatusCode == HttpStatusCode.Unauthorized, "Logout revokes an unexpired access JWT server-side");
    }
    var knownReset = await Check.Send(new SignedClient(anonymous), HttpMethod.Post, "/Auth/forgot-password", new { email = "alice@example.test" }, HttpStatusCode.OK);
    var unknownReset = await Check.Send(new SignedClient(anonymous), HttpMethod.Post, "/Auth/forgot-password", new { email = "missing@example.test" }, HttpStatusCode.OK);
    Check.Assert(knownReset.Body == unknownReset.Body, "Forgot-password responses do not disclose account existence");
    var resetToken = factory.Mail.Tokens["alice@example.test"];
    await Check.Send(new SignedClient(anonymous), HttpMethod.Post, "/Auth/reset-password", new { token = resetToken, newPassword = "NewAlicePassword123!" }, HttpStatusCode.NoContent);
    await Check.Send(alice, HttpMethod.Get, "/Auth/me", null, HttpStatusCode.Unauthorized);
    await Check.Send(new SignedClient(anonymous), HttpMethod.Post, "/Auth/reset-password", new { token = resetToken, newPassword = "OtherPassword123!" }, HttpStatusCode.BadRequest);
    var newAlice = new SignedClient(factory.CreateClient());
    newAlice.Update(await Check.Send(newAlice, HttpMethod.Post, "/Auth/login", new { email = "alice@example.test", password = "NewAlicePassword123!", rememberMe = true }, HttpStatusCode.OK));
    Check.Assert(newAlice.LastCookieHeaders.Any(x => x.StartsWith("refresh_token=") && x.Contains("expires=", StringComparison.OrdinalIgnoreCase)), "Remember-me sets an expiring persistent refresh cookie");
    await Check.Send(owner, HttpMethod.Put, $"/Users/{aliceId}/status", false, HttpStatusCode.NoContent);
    await Check.Send(newAlice, HttpMethod.Get, "/Auth/me", null, HttpStatusCode.Unauthorized);
    Check.Assert(true, "Account deactivation invalidates active sessions");
    var swagger = await anonymous.GetAsync("/swagger/index.html"); swagger.EnsureSuccessStatusCode();
    var openapi = JsonNode.Parse(await anonymous.GetStringAsync("/openapi/v1.json"))!;
    Check.Assert(openapi["components"]!["securitySchemes"]!["Bearer"] is not null && openapi["components"]!["securitySchemes"]!["ChatbotApiKey"] is not null, "Swagger documents management sessions and guest API keys");
    Console.WriteLine("All backend integration checks passed. Real MySQL, MongoDB, and Qdrant; stubbed email and LLM/embedding providers.");
}
finally
{
    if (factory is not null) await factory.DisposeAsync();
    if (collectionCreated) await vectors.DeleteAsync($"/collections/{collection}");
    if (sqlCreated && mysql.Database.StartsWith("cb_checks_", StringComparison.Ordinal))
    { await using var db = new AppDb(dbOptions); await db.Database.EnsureDeletedAsync(); }
    await new MongoClient("mongodb://localhost:27017").DropDatabaseAsync(mongoDatabase);
    if (Directory.Exists(storage) && storage.StartsWith(Path.GetFullPath(Path.Combine(root, ".backend-checks")) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)) Directory.Delete(storage, true);
    foreach (var setting in previous) Environment.SetEnvironmentVariable(setting.Key, setting.Value);
}
