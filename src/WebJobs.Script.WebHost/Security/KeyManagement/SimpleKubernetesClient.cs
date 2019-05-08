// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.WebJobs.Script.WebHost
{
    public class SimpleKubernetesClient : IKubernetesClient
    {
        private IEnvironment _environment;
        private HttpClient _httpClient;

        public SimpleKubernetesClient(IEnvironment environment, HttpClient httpClient)
        {
            _environment = environment;
            _httpClient = httpClient;
        }

        public Task<IDictionary<string, string>> GetSecret(string secretName)
        {
            throw new System.NotImplementedException();
        }
    }
}