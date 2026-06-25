using System;
using System.Linq;
using FluentAssertions;
using Microsoft.IdentityModel.JsonWebTokens;
using QtBank.Api.Infrastructure.Security;
using Xunit;

namespace QtBank.Api.Tests.Infrastructure.Security;

public class TokenGeneratorTests
{
    [Fact]
    public void GenerateToken_ShouldReturnValidJwtTokenStructure()
    {
        // Arrange
        var username = "john.doe";

        // Act
        var tokenString = TokenGenerator.GenerateToken(username);

        // Assert
        tokenString.Should().NotBeNullOrWhiteSpace();
        
        var jwt = new JsonWebToken(tokenString);
        jwt.Issuer.Should().Be(TokenGenerator.Issuer);
        jwt.Audiences.Should().ContainSingle().Which.Should().Be(TokenGenerator.Audience);
    }

    [Fact]
    public void GenerateToken_ShouldContainExpectedClaimsAndSettings_WithDefaultRole()
    {
        // Arrange
        var username = "jane.smith";

        // Act
        var tokenString = TokenGenerator.GenerateToken(username);
        var jwt = new JsonWebToken(tokenString);

        // Assert
        jwt.Issuer.Should().Be(TokenGenerator.Issuer);
        jwt.Audiences.Should().ContainSingle().Which.Should().Be(TokenGenerator.Audience);

        // Verify Name claim
        var nameClaim = jwt.Claims.FirstOrDefault(c => c.Type == "unique_name");
        nameClaim.Should().NotBeNull();
        nameClaim!.Value.Should().Be(username);

        // Verify Role claim
        var roleClaim = jwt.Claims.FirstOrDefault(c => c.Type == "role");
        roleClaim.Should().NotBeNull();
        roleClaim!.Value.Should().Be("User"); // Default role

        // Verify Expiration is set and close to 2 hours from now
        jwt.ValidTo.Should().BeCloseTo(DateTime.UtcNow.AddHours(2), TimeSpan.FromSeconds(10));
    }

    [Fact]
    public void GenerateToken_ShouldContainSpecifiedRole_WhenRoleIsProvided()
    {
        // Arrange
        var username = "admin.user";
        var customRole = "Admin";

        // Act
        var tokenString = TokenGenerator.GenerateToken(username, customRole);
        var jwt = new JsonWebToken(tokenString);

        // Assert
        var roleClaim = jwt.Claims.FirstOrDefault(c => c.Type == "role");
        roleClaim.Should().NotBeNull();
        roleClaim!.Value.Should().Be(customRole);
    }
}
