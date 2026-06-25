namespace QtBank.Api.Common.Security;

public interface ITokenGenerator
{
    string GenerateToken(string userId, string email, string role);
}
