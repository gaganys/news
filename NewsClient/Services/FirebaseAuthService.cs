using Firebase.Auth;
using Newtonsoft.Json;
using System.Net.Http;
using System.Text;
using System;
using System.Threading.Tasks;

namespace NewsClient.Services
{
    public class FirebaseAuthService
    {
        private FirebaseAuthProvider _authProvider;
        private const string FirebaseApiKey = "AIzaSyAfH40qeU-MWLVn4iooHiH4SgJH_qwtARU";

        public FirebaseAuthService()
        {
            _authProvider = new FirebaseAuthProvider(new FirebaseConfig(FirebaseApiKey));
        }

        public async Task<(bool IsSuccess, string ErrorMessage, FirebaseAuthLink Auth)> Login(string email, string password)
        {
            try
            {
                var auth = await _authProvider.SignInWithEmailAndPasswordAsync(email, password);
                return (true, null, auth);
            }
            catch (FirebaseAuthException ex)
            {
                return (false, ex.Reason.ToString(), null);
            }
        }

        public async Task<(bool IsSuccess, string ErrorMessage, FirebaseAuthLink Auth)> Register(string email, string password)
        {
            try
            {
                var auth = await _authProvider.CreateUserWithEmailAndPasswordAsync(email, password);
                return (true, null, auth);
            }
            catch (FirebaseAuthException ex)
            {
                return (false, ex.Reason.ToString(), null);
            }
        }

        public async Task<(bool IsSuccess, string ErrorMessage)> DeleteAccountAsync(string idToken)
        {
            try
            {
                using (var client = new HttpClient())
                {
                    var request = new
                    {
                        idToken = idToken
                    };

                    var json = JsonConvert.SerializeObject(request);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");

                    var response = await client.PostAsync(
                        $"https://identitytoolkit.googleapis.com/v1/accounts:delete?key={FirebaseApiKey}",
                        content);

                    if (response.IsSuccessStatusCode)
                        return (true, null);

                    var error = await response.Content.ReadAsStringAsync();
                    return (false, error);
                }
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }
    }
}
