# Zadania i status wykonania Kornela Niewiadomskiego

Stan na: 6 maja 2026

## Podsumowanie - Zadania do wykonania do 6 maja 2026

Poniżej znajduje się zestawienie podzadań, które miały być zakończone **do dzisiaj (6 maja 2026)**:

**Status: ✓ Wykonane (11 podzadań):**
- Zatwierdzenie technologii pod kątem obsługi repozytorium plików i komunikacji (11.04.2026 - 13.04.2026)
- Utworzenie repozytorium i ustalenie struktury branchy (11.04.2026)
- Utworzenie szkieletu projektu backendowego (14.04.2026 - 16.04.2026)
- Konfiguracja dependency injection i middleware (16.04.2026 - 18.04.2026)
- Przygotowanie standardu odpowiedzi API (16.04.2026 - 18.04.2026)
- Zaprojektowanie modelu zgłoszenia (19.04.2026)
- Obsługa statusów początkowych zgłoszenia (22.04.2026 - 26.04.2026)
- Implementacja CRUD dla tematów (26.04.2026 - 28.04.2026)
- Definicja statusów workflow (26.04.2026 - 28.04.2026)
- Implementacja przejść między statusami (28.04.2026 - 01.05.2026)
- Implementacja komentarzy lub wiadomości do zgłoszeń (03.05.2026 - 09.05.2026)

**Status: ⊕ Częściowo wykonane (2 podzadania):**
- Walidacja modelu danych pod kątem relacji między tematami a składami zespołów (11.04.2026 - 13.04.2026)
- Zapis/pobieranie i edycja zgłoszenia do/z bazy danych (20.04.2026 - 22.04.2026)

**Status: ✗ Do zrobienia (1 podzadanie):**
- Historia zmian statusu (01.05.2026 - 03.05.2026 -> 09.05.2026 - 15.05.2026).

---
## Zadania zaplanowane do wykonania po 6 maja 2026

**Status: ✓ Wykonane (1 podzadanie):**
- Endpointy pod agregacje danych (09.05.2026 - 15.05.2026)

**Status: ⊕ Częściowo wykonane (3 podzadania):**
- Pobieranie i usuwanie załączników (03.05.2026 - 09.05.2026).
- Powiązanie plików ze zgłoszeniami i komentarzami (03.05.2026 - 09.05.2026).
- Uspójnienie modeli danych i nazw endpointów (14.05.2026 - 16.05.2026).

**Status: ✗ Do zrobienia (6 podzadań):**
- Implementacja uploadu plików (03.05.2026 - 09.05.2026)
- Walidacja typu i rozmiaru plików (03.05.2026 - 09.05.2026)
- Zaprojektowanie dashboardu (09.05.2026 - 15.05.2026)
- Filtrowanie po statusach, datach i kategoriach (09.05.2026 - 15.05.2026)
- Podstawowe raporty dla zespołu (09.05.2026 - 15.05.2026)
- Przygotowanie dokumentacji technicznej i wdrożeniowej (16.05.2026 - 20.05.2026)
---

# Zadania opisane szczegółowo

## 1. Opracowanie architektury i wybór technologii (Zadanie nr 2)

- Walidacja modelu danych pod kątem relacji między tematami a składami zespołów (11.04.2026 – 13.04.2026)
  - **Status:** częściowo wykonane
  - **Potwierdzenie:** Modele `Project.cs`, `Group.cs`, `Student.cs` z relacjami (One-to-Many, Many-to-One); `AppDbContext` z konfiguracjami `HasOne`/`HasMany`; `init.sql` i migracje EF Core zachowują strukturę.
  - **Brakuje:** CHECK constraints i dodatkowych ograniczeń biznesowych (np. `priority BETWEEN 1 AND 5`, `max_number_group_members > 0`), walidacji na poziomie bazy.

- Zatwierdzenie technologii pod kątem obsługi repozytorium plików i komunikacji (11.04.2026 – 13.04.2026)
  - **Status:** wykonane
  - **Potwierdzenie:** Wybrano Google Cloud Platform (GCS) do przechowywania plików; backend używa JWT, PostgreSQL i REST; odpowiednie pakiety i konfiguracje w `Program.cs`.

### 2. Konfiguracja środowiska (Docker, CI/CD, Repo) (Zadanie nr 4)

- Utworzenie repozytorium i ustalenie struktury branchy (11.04.2026)
  - **Status:** wykonane
  - **Potwierdzenie:** Repozytorium: https://github.com/wspolpracuj-my/wspolpracuj.my; branch strategy: `Main`, `Core-Api`, feature branches.

### 3. Implementacja podstawowej struktury aplikacji (Zadanie nr 5)

- Utworzenie szkieletu projektu backendowego (14.04.2026 – 16.04.2026)
  - **Status:** wykonane
  - **Potwierdzenie:** `Program.cs`, katalogi `Controllers/`, `Models/`, `Data/`, `Services/`, `appsettings.json`, projekt `.csproj`.

- Konfiguracja dependency injection i middleware (16.04.2026 – 18.04.2026)
  - **Status:** wykonane
  - **Potwierdzenie:** Rejestracja serwisów (`AddScoped`), `AddDbContext` z PostgreSQL, `AddSwaggerGen`, `AddAuthentication/AddJwtBearer`, `UseAuthentication`, `UseAuthorization`.

- Przygotowanie standardu odpowiedzi API (16.04.2026 – 18.04.2026)
  - **Status:** wykonane
  - **Potwierdzenie:** DTOs (`ProjectSummaryDto`, `CommentWithResponsesDto`, `NotificationDto`), kontrolery zwracające `ActionResult<T>`.

### 4. Moduł zgłoszeń (formularze, walidacja, zapis) (Zadanie nr 6)

- Zaprojektowanie modelu zgłoszenia (19.04.2026)
  - **Status:** wykonane
  - **Potwierdzenie:** `Project.cs` zawiera `Topic`, `Description`, `Priority`, `LanguageDoc`, `CompanyId`; relacje do `Company`, `Groups`, `Comments`, `ProjectTags`.

- Zapis/pobieranie i edycja zgłoszenia do/z bazy danych (20.04.2026 – 22.04.2026)
  - **Status:** częściowo wykonane
  - **Potwierdzenie:** `ProjectsController` implementuje `Post`, `Put`, `Get`, `Delete`; zapisy przez `AppDbContext.SaveChangesAsync()`.
  - **Brakuje:** pełnej walidacji DTO (`[Required]`, zakresy), obsługi błędów i walidacji biznesowej.

- Obsługa statusów początkowych zgłoszenia (22.04.2026 – 26.04.2026)
  - **Status:** wykonane
  - **Potwierdzenie:** `GroupStatus` enum, `Group.IsAccepted` oraz migracje `HasConversion<string>()`.

### 5. Moduł zarządzania tematami i workflow statusów (Zadanie nr 7)

- Implementacja CRUD dla tematów (26.04.2026 – 28.04.2026)
  - **Status:** wykonane
  - **Potwierdzenie:** `ProjectsController.cs` (`Get`, `Get(id)`, `Post`, `Put`, `Delete`); `AppDbContext.Projects`.

- Definicja statusów workflow (26.04.2026 – 28.04.2026)
  - **Status:** wykonane
  - **Potwierdzenie:** `Enums.cs` zawiera `GroupStatus`, `NotificationStatus`.

- Implementacja przejść między statusami (28.04.2026 – 01.05.2026)
  - **Status:** wykonane
  - **Potwierdzenie:** `GroupRequestsController.Respond()` aktualizuje `GroupRequest.Status` i `Group.IsAccepted`.

- Historia zmian statusu (01.05.2026 – 03.05.2026)
  - **Status:** do zrobienia

### 6. Moduł komunikacji i zarządzania plikami (Zadanie nr 9)

- Implementacja komentarzy lub wiadomości do zgłoszeń (03.05.2026 – 09.05.2026)
  - **Status:** wykonane
  - **Potwierdzenie:** `CommentsController` (`GetByProject`, `Post`, `Delete`); `ResponsesController` (`Post`, `Delete`); `ProjectCommentService`.

- Implementacja uploadu plików (03.05.2026 – 09.05.2026)
  - **Status:** do zrobienia

- Walidacja typu i rozmiaru plików (03.05.2026 – 09.05.2026)
  - **Status:** do zrobienia

- Pobieranie i usuwanie załączników (03.05.2026 – 09.05.2026)
  - **Status:** częściowo wykonane
  - **Potwierdzenie:** `FilesController` zawiera `Get(id)`, `Delete(id)`, `Put(id, file)`; `GroupFiles` relacje; `FileEntity` model.
  - **Brakuje:** rzeczywisty transfer plików, mapowanie URL→GCS, kontrola dostępu i GC.

- Powiązanie plików ze zgłoszeniami i komentarzami (03.05.2026 – 09.05.2026)
  - **Status:** częściowo wykonane
  - **Potwierdzenie:** `GroupFile` composite key (`GroupId`, `FileId`); relacje w `AppDbContext`; `FileEntity.UserId`, `FileEntity.GroupId`.
  - **Brakuje:** pełna implementacja upload/download i metadanych.

### 7. Moduł monitorowania (Dashboard, Kanban, Statystyki) (Zadanie nr 10)

- Zaprojektowanie dashboardu (09.05.2026 – 15.05.2026)
  - **Status:** do zrobienia

- Endpointy pod agregacje danych (09.05.2026 – 15.05.2026)
  - **Status:** wykonane
  - **Potwierdzenie:** `ProjectService.GetProjectsForCompanyAsync()`, `GetAllProjectSummariesAsync()`; `GroupsController.GetByProjectSummary()`; `ProjectsController.GetSummary()`, `GetDetails()`.

- Filtrowanie po statusach, datach i kategoriach (09.05.2026 – 15.05.2026)
  - **Status:** do zrobienia

- Podstawowe raporty dla zespołu (09.05.2026 – 15.05.2026)
  - **Status:** do zrobienia

### 8. Integracja wszystkich modułów i koordynacja (Zadanie nr 11)

- Uspójnienie modeli danych i nazw endpointów (14.05.2026 – 16.05.2026)
  - **Status:** częściowo wykonane
  - **Potwierdzenie:** 13 kontrolerów zwraca `ActionResult<T>`; DTOs (`ProjectSummaryDto`, `GroupSummaryDto`, `CommentWithResponsesDto`, `NotificationDto`); routing `[Route("api/[controller]")]`.
  - **Brakuje:** zunifikowanego formatu błędów, konwencji nazewnictwa (camelCase vs PascalCase), atrybutów walidacyjnych.

- Przygotowanie dokumentacji technicznej i wdrożeniowej (16.05.2026 – 20.05.2026)
  - **Status:** do zrobienia
