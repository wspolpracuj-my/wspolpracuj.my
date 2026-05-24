using System.Collections.Generic;

// DTOs dla odpowiedzi Dashboard API.
// Zawierają proste struktury używane przez endpoints: full-projects oraz listy top firm.
namespace wspolpracujmy.DTOs
{
    /// <summary>
    /// DTO reprezentujące projekt, który jest "pełny" czyli osiągnął limit grup.
    /// Zawiera tylko pola wymagane przez endpoint: nazwa projektu, nazwa firmy,
    /// aktualna liczba grup oraz ustawiony limit `MaxGroups`.
    /// </summary>
    public class FullProjectDto
    {
        /// <summary>Temat / nazwa projektu.</summary>
        public string ProjectName { get; set; } = null!;

        /// <summary>Nazwa firmy odpowiedzialnej za projekt.</summary>
        public string CompanyName { get; set; } = null!;

        /// <summary>Aktualna liczba przypisanych grup do projektu.</summary>
        public int CurrentGroupsCount { get; set; }

        /// <summary>Wartość maksymalnej liczby grup dopuszczalnych dla projektu.</summary>
        public int MaxGroups { get; set; }
    }

    /// <summary>
    /// Odpowiedź endpointa `/api/dashboard/full-projects`.
    /// `TotalCount` to liczba projektów spełniających warunek, a `Projects` to lista obiektów <see cref="FullProjectDto"/>.
    /// </summary>
    public class FullProjectsResponseDto
    {
        /// <summary>Ilość projektów, które osiągnęły limit grup.</summary>
        public int TotalCount { get; set; }

        /// <summary>Lista projektów spełniających warunek.</summary>
        public List<FullProjectDto> Projects { get; set; } = new List<FullProjectDto>();
    }
}
