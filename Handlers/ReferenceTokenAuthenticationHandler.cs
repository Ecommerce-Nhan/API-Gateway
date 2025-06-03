using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using SharedLibrary.Response.Identity;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace APIGateway.Handlers;

public class ReferenceTokenAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;

    public ReferenceTokenAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration)
        : base(options, logger, encoder)
    {
        _httpClient = httpClientFactory.CreateClient();
        _configuration = configuration;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (IsOptionsHttpMethod(Request.HttpContext))
        {

        }

        if (!Request.Headers.ContainsKey("Authorization"))
            return AuthenticateResult.Fail("Missing Authorization Header");

        var authHeader = Request.Headers["Authorization"].ToString();
        if (!authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            return AuthenticateResult.Fail("Invalid Scheme");

        var token = authHeader.Substring("Bearer ".Length).Trim();
        var introspectionEndpoint = _configuration["OpenIddict:IntrospectionEndpoint"];

        var request = new HttpRequestMessage(HttpMethod.Post, introspectionEndpoint);
        request.Content = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("token", token)
        });

        var response = await _httpClient.SendAsync(request);
        if (!response.IsSuccessStatusCode)
            return AuthenticateResult.Fail($"Introspection endpoint returned {response.StatusCode}");

        var responseAsString = await response.Content.ReadAsStringAsync();
        var introspectionResult = JsonSerializer.Deserialize<TokenIntrospectionResponse>(responseAsString, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            ReferenceHandler = ReferenceHandler.Preserve
        });
        if (introspectionResult is null)
        {
            throw new InvalidOperationException("Unable to deserialize response content.");
        }

        if (!introspectionResult.Active)
        {
            return AuthenticateResult.Fail("Token is not active");

        }

        var claims = new List<Claim>
        {
            new Claim("iss", introspectionResult.Iss),
            new Claim("sub", introspectionResult.Sub),
            new Claim("iat", introspectionResult.Iat.ToString()),
            new Claim("nbf", introspectionResult.Nbf.ToString()),
            new Claim("exp", introspectionResult.Exp.ToString()),
            new Claim("payload_token", introspectionResult.Payload_token)
        };

        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);

        Request.Headers.Authorization = "Bearer " + introspectionResult.Payload_token;
        return AuthenticateResult.Success(ticket);
    }

    private static bool IsOptionsHttpMethod(HttpContext httpContext)
    {
        return httpContext.Request.Method.Equals("OPTIONS", StringComparison.CurrentCultureIgnoreCase);
    }
}