using System.Threading.Tasks;
using Firebase.Auth;

namespace NewsClient.Services.Auth
{
    public interface IAuthService
    {
        Task<(bool IsSuccess, string ErrorMessage, FirebaseAuthLink Auth)> LoginAsync(string email, string password);
        Task<(bool IsSuccess, string ErrorMessage)> RegisterAsync(string email, string password);
    }
}