namespace wspolpracujmy.DTOs
{
    /// <summary>
    /// DTO używany do tworzenia GroupRequest przez API.
    /// Pola:
    /// - GroupId: id grupy docelowej (wymagane).
    /// - ProjectId: id projektu (opcjonalne) — wymagane przy typie "ProjectRequest".
    /// - TargetStudentId: DO NOT PROVIDE — this field is set/used by server only.
    /// - TargetEmail: email studenta będącego celem zaproszenia (wymagane dla typu "Invitation").
    /// - Type: typ żądania: "Invitation", "ProjectRequest" lub "Application".
    /// Uwaga: `CreatedByUserId` nie jest przekazywane w DTO — pobierane jest z aktualnie zalogowanego użytkownika po stronie serwera.
    /// </summary>
    public class CreateGroupRequestDto
    {
        public int GroupId { get; set; }
        public int? ProjectId { get; set; }
        public string? TargetEmail { get; set; }
        public string? Type { get; set; }
    }
}
