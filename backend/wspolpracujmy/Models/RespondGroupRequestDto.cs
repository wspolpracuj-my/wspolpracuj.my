using System;

namespace wspolpracujmy.Models
{
    public class RespondGroupRequestDto
    {
        public int RespondedByUserId { get; set; }
        // expected values: "accept" or "decline"
        public string Action { get; set; } = string.Empty;
    }
}
