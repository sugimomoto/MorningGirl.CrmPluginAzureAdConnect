using Microsoft.Xrm.Sdk;
using System;
using System.Net.Http;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Threading.Tasks;

namespace MorningGirl.CrmPluginAzureAdConnect
{
    public class CrmPluginAzureAdConnect :IPlugin
    {
        public void Execute(IServiceProvider serviceProvider)
        {
            var context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));
            
            var clientId = "";
            var userName = "";
            var password = "";
            var resourceUrl = "";

            var azureAdConnect = new AzureAdConnect(
                clientId,
                userName,
                password,
                resourceUrl
                );

            var token = azureAdConnect.GetAccessToken();

            token.Wait();

            throw new InvalidPluginExecutionException(token.Result.access_token);
             
        }
    }

    /// <summary>
    /// AzureADからTokenを取得するためのClass
    /// </summary>
    public class AzureAdConnect
    {
        private string _clientId { get; set; }
        private string _userName { get; set; }
        private string _password { get; set; }
        private string _recourceUrl { get; set; }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public AzureAdConnect(string clientId, string userName,string password, string recourceUrl)
        {
            _clientId = clientId;
            _userName = userName;
            _password = password;
            _recourceUrl = recourceUrl;
        }

        /// <summary>
        /// grant_type=passwordでTokenを取得
        /// </summary>
        /// <returns></returns>
        public async Task<AzureAccessToken> GetAccessToken()
        {
            var token = new AzureAccessToken();

            string oauthUrl = string.Format("https://login.windows.net/common/oauth2/token");
            
            string reqBody = string.Format("grant_type=password&client_id={0}&username={1}&password={2}&resource={3}&scope=openid",
                Uri.EscapeDataString(_clientId),
                Uri.EscapeDataString(_userName),
                Uri.EscapeDataString(_password),
                Uri.EscapeDataString(this._recourceUrl));

            var client = new HttpClient();
            var content = new StringContent(reqBody);
            content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/x-www-form-urlencoded");
            using (HttpResponseMessage response = await client.PostAsync(oauthUrl, content))
            {
                if (response.IsSuccessStatusCode)
                {
                    var serializer = new DataContractJsonSerializer(typeof(AzureAccessToken));
                    var json = await response.Content.ReadAsStreamAsync();
                    token = (AzureAccessToken)serializer.ReadObject(json);
                }
            }

            return token;
        }
    }

    /// <summary>
    /// AccessToken等の格納クラス
    /// </summary>
    [DataContract]
    public class AzureAccessToken
    {
        [DataMember]
        public string access_token { get; set; }

        [DataMember]
        public string token_type { get; set; }

        [DataMember]
        public string expires_in { get; set; }

        [DataMember]
        public string expires_on { get; set; }

        [DataMember]
        public string resource { get; set; }
    }
}
