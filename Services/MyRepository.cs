
using Newtonsoft.Json;

using SelfApiproj.settings;

using System.Net.Http.Headers;

using System.Text;




namespace SelfApiproj.Repository
{
    public class MyRepository : IMyRepository
    {
        private readonly HttpClient _httpClient;
        private string _jwtToken;

        public string JwtToken
        {
            get => _jwtToken;
            set
            {
                _jwtToken = value;
                if (!string.IsNullOrEmpty(_jwtToken))
                {
                    // S'assure que l'en-tête est défini APRÈS que le token est connu
                    _httpClient.DefaultRequestHeaders.Authorization =
                        new AuthenticationHeaderValue("Bearer", _jwtToken);
                }
                else
                {
                    _httpClient.DefaultRequestHeaders.Authorization = null;
                }
            }
        }


        public MyRepository(ISettings settings)
        {

            _httpClient = new HttpClient { BaseAddress = new Uri("https://localhost:5001/") };

          

         

        }


        public async Task<TResponse> GetAsync<TResponse>(string requestUri)
        {
            try
            {
                HttpResponseMessage response = await _httpClient.GetAsync(requestUri);
                response.EnsureSuccessStatusCode(); // Lance une exception si le code de statut n'indique pas le succès (2xx)

                string jsonResponse = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<TResponse>(jsonResponse);
            }
            catch (HttpRequestException e)
            {
              //  Console.WriteLine($"Erreur de requête GET: {e.Message}");
                return default(TResponse);
            }
            catch (Exception e)
            {
                //Console.WriteLine($"Une erreur inattendue est survenue lors du GET: {e.Message}");
                return default(TResponse);
            }
        }


        public async Task<TResponse> PostAsync<TRequest, TResponse>(string requestUri, TRequest data)
        {
            try
            {
                string jsonContent = JsonConvert.SerializeObject(data);
                StringContent content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                HttpResponseMessage response = await _httpClient.PostAsync(requestUri, content);
                response.EnsureSuccessStatusCode(); // Lance une exception si le code de statut n'indique pas le succès (2xx)

                string jsonResponse = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<TResponse>(jsonResponse);
            }
            catch (HttpRequestException e)
            {
               // Console.WriteLine($"Erreur de requête POST: {e.Message}");
              
                return default(TResponse);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Une erreur inattendue est survenue lors du POST: {e.Message}");
                return default(TResponse);
            }
        }

        // Surcharge pour POST sans attente de réponse spécifique (par exemple, pour un 204 No Content)
        public async Task<bool> PostAsync<TRequest>(string requestUri, TRequest data)
        {
            try
            {
                string jsonContent = JsonConvert.SerializeObject(data);
                StringContent content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                HttpResponseMessage response = await _httpClient.PostAsync(requestUri, content);
                response.EnsureSuccessStatusCode();
                return response.IsSuccessStatusCode;
            }
            catch (HttpRequestException e)
            {
                Console.WriteLine($"Erreur de requête POST (sans réponse spécifique): {e.Message}");
                return false;
            }
            catch (Exception e)
            {
                Console.WriteLine($"Une erreur inattendue est survenue lors du POST (sans réponse spécifique): {e.Message}");
                return false;
            }
        }

    }
}

public interface IMyRepository
{
    Task<bool> PostAsync<TRequest>(string requestUri, TRequest data);

    Task<TResponse> PostAsync<TRequest, TResponse>(string requestUri, TRequest data);

    Task<TResponse> GetAsync<TResponse>(string requestUri);

    string JwtToken { get; set; } // Ajout du setter dans l'interface

  


}

  





