using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Webhttp.Models;

namespace SelfApiproj.Services
{
    public interface IBaserowService
    {
        Task<BaserowUser?> GetUserByEmailAsync(string email);
        Task<BaserowUser> CreateUserAsync(string email, string password);
    }



    public class BaserowService : IBaserowService
    {
        private readonly HttpClient _httpClient;
        private readonly string _baserowApiUrl = "https://api.baserow.io/api/database/rows/table/";
        private readonly string _baserowAuthUrl = "https://api.baserow.io/api/user/token-auth/";
        private readonly string _tableId;
        private readonly string _username;
        private readonly string _password;
        private string? _authToken;

        public BaserowService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _tableId = configuration["Baserow:TableId"] ?? throw new ArgumentException("Baserow TableId manquant");
            _username = configuration["Baserow:Username"] ?? throw new ArgumentException("Baserow Username manquant");
            _password = configuration["Baserow:Password"] ?? throw new ArgumentException("Baserow Password manquant");
        }

        private async Task<string> GetAuthTokenAsync()
        {
            if (!string.IsNullOrEmpty(_authToken))
                return _authToken;

            var loginData = new
            {
                username = _username,
                password = _password
            };

            var content = JsonContent.Create(loginData);
            var response = await _httpClient.PostAsync(_baserowAuthUrl, content);

            if (response.IsSuccessStatusCode)
            {
                var tokenResponse = await response.Content.ReadFromJsonAsync<BaserowTokenResponse>();
                _authToken = tokenResponse?.Token ?? throw new Exception("Token d'authentification non reçu");
                return _authToken;
            }

            throw new Exception($"Erreur d'authentification Baserow: {response.StatusCode}");
        }

        public async Task<BaserowUser?> GetUserByEmailAsync(string email)
        {
            try
            {

                // 1. Assurez-vous d'avoir un token valide
                var token = await GetAuthTokenAsync();

                // 2. Ajoutez l'en-tête d'autorisation pour cette requête
                _httpClient.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("JWT", token); // Baserow utilise "Token"

                var filtersJson = $"{{\"field_email\":{{\"equal\":\"{email}\"}}}}";
                var filtersEncoded = HttpUtility.UrlEncode(filtersJson);

                //    var url = $"{_baserowApiUrl}/api/database/rows/table/{_tableId}/?user_field_names=true&filters={filtersEncoded}";

              //  var url = $"{_baserowApiUrl}{_tableId}/?filters={filtersEncoded}";

               var url = $"{_baserowApiUrl}{_tableId}/?filters={{\"field_email\":{{\"equal\":\"{email}\"}}}}";



                                var filters = new
                                {
                                    filter_type = "AND",
                                    filters = new[]
                     {
                        new {
                            field =4948137, //"email", // si tu as activé user_field_names=true
                            type = "equal",
                            value = email
                        }
                    }
                                };

                // // 2. Sérialisez l'objet filtre en JSON
                 var jsonFilter = JsonConvert.SerializeObject(filters);



                // // 3. Encodez la chaîne JSON en URL
                // // Option 1: System.Web.HttpUtility.UrlEncode (nécessite le package System.Web.HttpUtility)
                //// var encodedFilter = HttpUtility.UrlEncode(jsonFilter);

                // // Option 2: System.Uri.EscapeDataString (fait partie du framework, pas besoin de package supplémentaire)
                // var encodedFilter = Uri.EscapeDataString(jsonFilter);


                // // 4. Construisez l'URL finale
                // // Assurez-vous que _httpClient.BaseAddress est configuré sur "https://api.baserow.io/"
                // // Si ce n'est pas le cas, utilisez l'URL complète ici :
                // // string requestUri = $"{_baserowApiUrl}api/database/rows/table/{tableId}/?filters={encodedFilter}";
                 string requestUri = $"{_baserowApiUrl}{_tableId}/?filters={jsonFilter}"; // Utilise BaseAddress si configuré



                 var response = await _httpClient.GetAsync(requestUri);

              //  var response = await _httpClient.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    var createdUser = await response.Content.ReadFromJsonAsync<BaserowUserRow>();

                    if (createdUser!=null)
                    {
                       
                        return new BaserowUser
                        {
                            Id = createdUser.Id,
                            Email = createdUser.Email,
                            Password = createdUser.Password,
                            CreatedAt = createdUser.CreatedAt
                        };
                    }
                }
                else
                {

                    var errorContent = await response.Content.ReadAsStringAsync();

                    string mess = errorContent;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur lors de la récupération de l'utilisateur: {ex.Message}");
            }

            return null;
        }

        public async Task<BaserowUser> CreateUserAsync(string email, string password)
        {
            try
            {
                var hashedPassword = HashPassword(password);
                var newUser = new
                {
                    field_email = email,
                    field_password = hashedPassword
                };


                var content = JsonContent.Create(newUser);


                var response = await _httpClient.PostAsync($"{_baserowApiUrl}{_tableId}/", content);

                if (response.IsSuccessStatusCode)
                {
                    var createdUser = await response.Content.ReadFromJsonAsync<BaserowUserRow>();
                    if (createdUser != null)
                    {

                        return new BaserowUser
                        {
                            Id = createdUser.Id,
                            Email = createdUser.Email,
                            Password = createdUser.Password,
                            CreatedAt = createdUser.CreatedAt
                        };
                    }


                  
                }

                throw new Exception("Erreur lors de la création de l'utilisateur");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur lors de la création de l'utilisateur: {ex.Message}");
                throw;
            }
        }

        private string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(hashedBytes);
        }
    }

}
