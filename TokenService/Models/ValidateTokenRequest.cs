namespace TokenService.Models
{
    using Newtonsoft.Json;

    [JsonObject("ValidateTokenRequest")]
    public class ValidateTokenRequest
    {
        [JsonProperty("AccessToken", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string? AccessToken { get; set; }

        [JsonProperty("RefreshToken", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string? RefreshToken { get; set; }
    }
}
