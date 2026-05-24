namespace wspolpracujmy.DTOs
{
    /// <summary>
    /// DTO z decyzją użytkownika dotyczącą żądania grupowego.
    /// </summary>
    public class RespondGroupRequestDto
    {
        public int RespondedByUserId { get; set; }
        // expected values: "accept" or "decline"
        public string Action { get; set; } = string.Empty;
    }
}