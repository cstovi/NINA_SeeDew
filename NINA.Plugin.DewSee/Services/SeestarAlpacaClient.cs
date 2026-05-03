using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace NINA.Plugin.DewSee.Services {

    public class SeestarAlpacaClient {
        private readonly HttpClient _http;
        private readonly Func<string> _getBaseUrl;

        private const int ClientId = 1;
        private const int ClientTransactionId = 1;
        private const int SwitchId = 0;

        public SeestarAlpacaClient(Func<string> getBaseUrl) {
            _getBaseUrl = getBaseUrl;
            _http = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
        }

        private string SwitchBase => $"{_getBaseUrl()}/api/v1/switch/{SwitchId}";

        public async Task ConnectAsync() {
            var response = await PutAsync($"{SwitchBase}/connected", new Dictionary<string, string> {
                ["Connected"] = "true",
                ["ClientID"] = ClientId.ToString(),
                ["ClientTransactionID"] = ClientTransactionId.ToString()
            });
            CheckAlpacaError(response, "connect");
        }

        public async Task DisconnectAsync() {
            await PutAsync($"{SwitchBase}/connected", new Dictionary<string, string> {
                ["Connected"] = "false",
                ["ClientID"] = ClientId.ToString(),
                ["ClientTransactionID"] = ClientTransactionId.ToString()
            });
        }

        public async Task<bool> GetSwitchStateAsync() {
            var url = $"{SwitchBase}/getswitch?Id={SwitchId}&ClientID={ClientId}&ClientTransactionID={ClientTransactionId}";
            var responseText = await _http.GetStringAsync(url);
            var json = JObject.Parse(responseText);
            return json["Value"]?.Value<bool>() ?? false;
        }

        public async Task SetSwitchStateAsync(bool state) {
            await PutAsync($"{SwitchBase}/setswitch", new Dictionary<string, string> {
                ["Id"] = SwitchId.ToString(),
                ["State"] = state ? "true" : "false",
                ["ClientID"] = ClientId.ToString(),
                ["ClientTransactionID"] = ClientTransactionId.ToString()
            });
        }

        public async Task<bool> TestConnectionAsync() {
            try {
                await GetSwitchStateAsync();
                return true;
            } catch {
                return false;
            }
        }

        private async Task<JObject> PutAsync(string url, Dictionary<string, string> fields) {
            using var content = new FormUrlEncodedContent(fields);
            var response = await _http.PutAsync(url, content);
            response.EnsureSuccessStatusCode();
            var text = await response.Content.ReadAsStringAsync();
            return JObject.Parse(text);
        }

        private static void CheckAlpacaError(JObject response, string operation) {
            var errorNumber = response["ErrorNumber"]?.Value<int>() ?? 0;
            if (errorNumber != 0) {
                var msg = response["ErrorMessage"]?.Value<string>() ?? "unknown error";
                throw new InvalidOperationException($"Alpaca {operation} error {errorNumber}: {msg}");
            }
        }
    }
}
