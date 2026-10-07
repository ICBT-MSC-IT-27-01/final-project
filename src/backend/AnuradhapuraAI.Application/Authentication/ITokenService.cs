using AnuradhapuraAI.Domain.Entities;

namespace AnuradhapuraAI.Application.Authentication;

public interface ITokenService
{
    TokenResult CreateToken(User user, string roleName);
}
