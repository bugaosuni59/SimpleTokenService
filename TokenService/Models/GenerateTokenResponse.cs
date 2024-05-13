namespace TokenService.Models
{
    using Newtonsoft.Json;

    [JsonObject("GenerateTokenResponse")]
    public class GenerateTokenResponse
    {
        [JsonProperty("AccessToken")]
        public string AccessToken { get; set; }

        [JsonProperty("RefreshToken")]
        public string RefreshToken { get; set; }
    }
}
