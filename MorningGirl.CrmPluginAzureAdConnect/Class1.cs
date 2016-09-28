using Microsoft.Xrm.Sdk;
using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Threading.Tasks;

namespace MorningGirl.CrmPluginAzureAdConnect
{
    public class CrmPluginAzureAdConnect :IPlugin
    {
        public void Execute(IServiceProvider serviceProvider)
        {
            try
            {
                var context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));

                var entity = context.InputParameters["Target"] as Entity;

                var createFolderName = entity["name"];
                var clientId = "";
                var userName = "";
                var password = "";
                var sharePointUrl = "";
                var targetDocumentFolder = "";

                var azureAdConnect = new AzureAdConnect(
                    clientId,
                    userName,
                    password,
                    sharePointUrl
                    );

                var token = azureAdConnect.GetAccessToken();

                token.Wait();

                using (var httpClient = new HttpClient())
                {

                    httpClient.DefaultRequestHeaders.Add("Accept", "application/json; odata=verbose");
                    httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.Result.access_token);

                    var createReq = new HttpRequestMessage(HttpMethod.Post, sharePointUrl + "_api/web/folders");

                    // JSONコンバート
                    createReq.Content = new StringContent("{ '__metadata': { 'type': 'SP.Folder' }, 'ServerRelativeUrl': '/" + targetDocumentFolder + "/" + createFolderName + "'}");

                    createReq.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json;odata=verbose");

                    var result = httpClient.SendAsync(createReq);
                    result.Wait();

                    Console.WriteLine(result.Result.IsSuccessStatusCode);

                }
            }
            catch(Exception ex)
            {
                throw new InvalidPluginExecutionException(ex.Message);
            }
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public class AzureAdConnect
    {
        private string _clientId { get; set; }
        private string _userName { get; set; }
        private string _password { get; set; }
        private string _resourceId { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public AzureAdConnect(string clientId, string userName,string password, string recourceId)
        {
            _clientId = clientId;
            _userName = userName;
            _password = password;
            _resourceId = recourceId;
        }

        /// <summary>
        /// 
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
                Uri.EscapeDataString(this._resourceId));

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
    /// ACCESStoken格納クラス
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
