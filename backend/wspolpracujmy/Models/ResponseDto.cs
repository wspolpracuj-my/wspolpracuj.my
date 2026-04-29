using System;

namespace wspolpracujmy.Models
{
    /// <summary>
    /// DTO przedstawiające odpowiedź w formacie do serializacji/odpowiedzi API.
    /// </summary>
    public class ResponseDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string? UserName { get; set; }
        public string Content { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}
