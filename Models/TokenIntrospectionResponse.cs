namespace APIGateway.Models;

public class TokenIntrospectionResponse
{
    public bool Active { get; set; }
    public string Iss { get; set; } = string.Empty;
    public string Sub { get; set; } = string.Empty;
    public int Iat { get; set; }
    public int Nbf { get; set; }
    public int Exp { get; set; }
    public string Payload_token { get; set; } = string.Empty;
}
