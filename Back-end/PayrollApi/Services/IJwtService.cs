using PayrollApi.Models;

namespace PayrollApi.Services
{
    public interface IJwtService
    {
        (string token, DateTime expiresAtUtc) Generate(User user);
    }
}
