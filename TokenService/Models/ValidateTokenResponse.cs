namespace TokenService.Models
{
    using Newtonsoft.Json;

    [JsonObject("ValidateTokenResponse")]
    public class ValidateTokenResponse
    {
        [JsonProperty("isValid", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public bool isValid { get; set; }
    }
}
