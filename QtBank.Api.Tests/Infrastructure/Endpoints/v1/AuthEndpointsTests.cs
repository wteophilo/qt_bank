using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.IdentityModel.JsonWebTokens;
using QtBank.Api.Infrastructure.Endpoints.v1;
using QtBank.Api.Infrastructure.Security;
using Xunit;

namespace QtBank.Api.Tests.Infrastructure.Endpoints.v1;

[Collection("Sequential")]
public class AuthEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public AuthEndpointsTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public async Task GenerateToken_WithEmptyOrNullUsername_Returns400BadRequest(string? invalidUsername)
    {
        // Arrange
        var client = _factory.CreateClient();
        var request = new TokenRequest(invalidUsername!);

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/auth/token", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Be("\"Username is required.\"");
    }

    [Fact]
    public async Task GenerateToken_WithValidUsername_Returns200OK_AndValidJwtToken()
    {
        // Arrange
        var client = _factory.CreateClient();
        var username = "charlie.brown";
        var request = new TokenRequest(username);

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/auth/token", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var tokenResponse = await response.Content.ReadFromJsonAsync<TokenResponse>();
        tokenResponse.Should().NotBeNull();
        tokenResponse!.Token.Should().NotBeNullOrWhiteSpace();

        // Verify the token content
        var jwt = new JsonWebToken(tokenResponse.Token);
        jwt.Issuer.Should().Be(TokenGenerator.Issuer);
        jwt.Audiences.Should().ContainSingle().Which.Should().Be(TokenGenerator.Audience);
        
        var nameClaim = jwt.Claims.FirstOrDefault(c => c.Type == "unique_name");
        nameClaim.Should().NotBeNull();
        nameClaim!.Value.Should().Be(username);
    }
}
