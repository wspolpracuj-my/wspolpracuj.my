using System;
using System.Collections.Generic;

namespace wspolpracujmy.Models
{
    /// <summary>
    /// DTO zawierające komentarz wraz z powiązanymi odpowiedziami.
    /// </summary>
    public class CommentWithResponsesDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string? UserName { get; set; }
        public int? GroupId { get; set; }
        public string Content { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public List<ResponseDto> Responses { get; set; } = new List<ResponseDto>();
    }
}
