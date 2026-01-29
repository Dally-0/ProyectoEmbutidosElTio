using System.Text.Json.Serialization;

namespace ProyectoFinalEmbutidosElTio.Models.PayPal
{
    // Request Models
    public class CreateOrderRequest
    {
        [JsonPropertyName("intent")]
        public string Intent { get; set; } = "CAPTURE";

        [JsonPropertyName("purchase_units")]
        public List<PurchaseUnitRequest> PurchaseUnits { get; set; } = new List<PurchaseUnitRequest>();
    }

    public class PurchaseUnitRequest
    {
        [JsonPropertyName("amount")]
        public Amount Amount { get; set; } = new Amount();
    }

    public class Amount
    {
        [JsonPropertyName("currency_code")]
        public string CurrencyCode { get; set; } = "USD";

        [JsonPropertyName("value")]
        public string Value { get; set; } = string.Empty;
    }

    // Response Models
    public class OrderResponse
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;

        [JsonPropertyName("links")]
        public List<Link> Links { get; set; } = new List<Link>();
    }

    public class Link
    {
        [JsonPropertyName("href")]
        public string Href { get; set; } = string.Empty;

        [JsonPropertyName("rel")]
        public string Rel { get; set; } = string.Empty;
        
        [JsonPropertyName("method")]
        public string Method { get; set; } = string.Empty;
    }

    public class AccessTokenResponse
    {
        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; } = string.Empty;

        [JsonPropertyName("token_type")]
        public string TokenType { get; set; } = string.Empty;

        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; }
    }
}
