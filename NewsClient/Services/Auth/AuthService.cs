using System.Threading.Tasks;
using Firebase.Auth;
using NewsClient.Utils;

namespace NewsClient.Services.Auth
{
    public class AuthService: IAuthService
    {
        public async Task<(bool IsSuccess, string ErrorMessage, FirebaseAuthLink Auth)> LoginAsync(string email, string password)
        {
            try
            {
                var authProvider = new FirebaseAuthProvider(new FirebaseConfig(AppConstants.FirebaseApiKey));
                var auth = await authProvider.SignInWithEmailAndPasswordAsync(email, password);
                return (true, null, auth);
            }
            catch (FirebaseAuthException ex)
            {
                return (false, ex.Reason.ToString(), null);
            }
        }

        public async Task<(bool IsSuccess, string ErrorMessage)> RegisterAsync(string email, string password)
        {
            try
            {
                var authProvider = new FirebaseAuthProvider(new FirebaseConfig(AppConstants.FirebaseApiKey));
                await authProvider.CreateUserWithEmailAndPasswordAsync(email, password);
                return (true, null);
            }
            catch (FirebaseAuthException ex)
            {
                return (false, ex.Reason.ToString());
            }
        }
    }
}