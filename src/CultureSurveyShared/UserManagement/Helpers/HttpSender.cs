using System;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace CultureSurveyShared.UserManagement.Helpers
{
    public class HttpSender
    {
        private HttpClient client;
        private HttpClient Client => client.SetLanguageHeader();
        public string UrlBase { get; }
        public string Username { get; }
        public string Password { get; }

        private HttpSender(string urlBase, string username, string password)
        {
            client = new HttpClient();
            UrlBase = urlBase;
            Username = username;
            Password = password;
        }

        public void SetBaseUrl()
        {
            client.BaseAddress = new Uri(UrlBase);
        }
        public void SetAuthorization()
        {
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Basic",
                                Convert.ToBase64String(Encoding.ASCII.GetBytes(Username + ":" + Password)));
        }
        public void SetAccept()
        {
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        public static HttpSender Generate(string urlBase, string username, string password)
                                    => new HttpSender(urlBase, username, password);

        public async Task<HttpResponseMessage> GetAsync(string requestUri)
        {
            return await Client.GetAsync(requestUri);
        }

        public async Task<HttpResponseMessage> PostAsync(string requestUri, HttpContent content)
        {
            return await Client.PostAsync(requestUri, content);
        }

        public async Task<HttpResponseMessage> PutAsync(string requestUri, HttpContent content)
        {
            return await Client.PutAsync(requestUri, content);
        }
    }

    public static class ClientExtensions
    {
        public static HttpClient SetLanguageHeader(this HttpClient client)
        {
            if (client.DefaultRequestHeaders.AcceptLanguage.Count < 1)
            {
                client.DefaultRequestHeaders.AcceptLanguage.Add(
                                                new StringWithQualityHeaderValue(Thread.CurrentThread.CurrentCulture.Name));
            }

            return client;
        }
    }
}
