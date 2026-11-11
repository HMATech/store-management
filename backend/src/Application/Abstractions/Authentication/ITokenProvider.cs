using Domain.Users;

namespace Application.Abstractions.Authentication;

public interface ITokenProvider
{
    string CreateFor(User user);
}