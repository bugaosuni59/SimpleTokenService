namespace TokenService.Models
{
    public class GenerateTokenRequest
    {
        public Dictionary<string, string> Claims { get; set; }
    }
}
