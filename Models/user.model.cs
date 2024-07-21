using System.Text.Json.Serialization;

namespace Digital_Wallet_System.Models
{
    public class User
    {
        public int Id { get; set; }
        public required string Username { get; set; }
        public required string Email { get; set; }
        public required string Password { get; set; }
        [JsonIgnore] // prevent object cycle in json serialization
        public Wallet? Wallet { get; set; }
    }
}