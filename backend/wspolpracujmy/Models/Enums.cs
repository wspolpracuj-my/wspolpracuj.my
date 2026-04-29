using System;

namespace wspolpracujmy.Models
{
    /// <summary>
    /// Role użytkowników w aplikacji.
    /// </summary>
    public enum Role
    {
        Admin,
        Student,
        Company
    }

    /// <summary>
    /// Status grupy w procesie akceptacji.
    /// </summary>
    public enum GroupStatus
    {
        Pending,
        Accepted,
        Declined
    }

    /// <summary>
    /// Obsługiwane języki dokumentów/treści.
    /// </summary>
    public enum LanguageDoc
    {
        Polish,
        English
    }

    /// <summary>
    /// Status powiadomienia (przeczytane/nieprzeczytane).
    /// </summary>
    public enum NotificationStatus
    {
        NotRead,
        Read
    }

    /// <summary>
    /// Priorytety używane w zadaniach lub powiadomieniach.
    /// </summary>
    public enum Priority
    {
        P1 = 1,
        P2 = 2,
        P3 = 3,
        P4 = 4,
        P5 = 5
    }
}
