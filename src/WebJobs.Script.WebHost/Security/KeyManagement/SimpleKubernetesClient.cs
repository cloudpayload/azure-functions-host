// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace Microsoft.Azure.WebJobs.Script.WebHost
{
    public class SimpleKubernetesClient : IKubernetesClient
    {
        private IEnvironment _environment;
        private HttpClient _httpClient;

        static SimpleKubernetesClient()
        {
            System.Net.ServicePointManager.ServerCertificateValidationCallback +=
                (sender, cert, chain, sslPolicyErrors) => true;
        }

        public SimpleKubernetesClient(IEnvironment environment, HttpClient httpClient)
        {
            _environment = environment;
            _httpClient = httpClient;
        }

        public async Task<IDictionary<string, string>> GetSecret(string secretName)
        {
            using (var request = GetRequest(secretName))
            {
                var res = await _httpClient.SendAsync(request);
                res.EnsureSuccessStatusCode();
                var obj = await res.Content.ReadAsAsync<JObject>();
                return obj["data"].ToObject<IDictionary<string, string>>();
            }
        }

        private HttpRequestMessage GetRequest(string secretName)
        {
            var url = $"https://{_environment.GetEnvironmentVariable("KUBERNETES_SERVICE_HOST")}:{_environment.GetEnvironmentVariable("KUBERNETES_SERVICE_PORT_HTTPS")}";
            url = url + $"/api/v1/namespaces/{FileUtility.ReadAllText("/run/secrets/kubernetes.io/serviceaccount/namespace")}/secrets/{secretName}";
            var request = new HttpRequestMessage(HttpMethod.Get, url);

            request.Headers.Add("Authorization", $"Bearer {FileUtility.ReadAllText("/run/secrets/kubernetes.io/serviceaccount/token")}");
            return request;
        }
    }
}