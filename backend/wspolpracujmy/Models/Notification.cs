using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;

namespace wspolpracujmy.Models
{
    [Table("Notifications")]
    /// <summary>
    /// Reprezentuje powiadomienie wysyłane do użytkownika (tylko tekst do wyświetlenia).
    /// </summary>
    public class Notification
    {
        public const string StudentCommentReplyPrefix = "student:comment_reply:";
        public const string StudentProjectDecisionPrefix = "student:project_decision:";
        public const string CompanyTeamCommentPrefix = "company:team_comment:";
        public const string CompanyTeamApplicationPrefix = "company:team_application:";

        [Key]
        [Column("id")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [Column("user_id")]
        public int UserId { get; set; }

        [Required]
        [Column("content")]
        public required string Content { get; set; }

        [Required]
        [Column("status")]
        public required NotificationStatus Status { get; set; }

        [Required]
        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        [Column("link_target")]
        public string? LinkTarget { get; set; }

        [Column("group_request_id")]
        public int? GroupRequestId { get; set; }

        [JsonIgnore]
        public GroupRequest? GroupRequest { get; set; }

        [JsonIgnore]
        public User? User { get; set; }

        public static string FormatCompanyCommentReply(string companyName, string projectName)
            => $"FIRMA {companyName} odpowiedziała na twój komentarz pod projektem {projectName}.";

        public static string FormatCompanyProjectAccepted(string companyName, string projectName)
            => $"FIRMA {companyName} zaakceptowała twoje zgłoszenie się do projektu {projectName}.";

        public static string FormatCompanyProjectDeclined(string companyName, string projectName)
            => $"FIRMA {companyName} odrzuciła twoje zgłoszenie się do projektu {projectName}.";

        public static string LinkTargetCommentReply(int projectId)
            => $"{StudentCommentReplyPrefix}{projectId}";

        public static string LinkTargetProjectDecision(int projectId, bool accepted)
            => $"{StudentProjectDecisionPrefix}{projectId}:{(accepted ? "accept" : "decline")}";

        public static string FormatTeamCommentOnProject(string teamName, string projectName)
            => $"Zespół {teamName} napisał komentarz pod projektem {projectName}.";

        public static string FormatTeamProjectApplication(string teamName, string projectName)
            => $"Zespół {teamName} zgłosił się do projektu {projectName}.";

        public static string LinkTargetCompanyTeamComment(int projectId, int groupId = 0)
            => $"{CompanyTeamCommentPrefix}{projectId}:{groupId}";

        public static string LinkTargetCompanyTeamApplication(int projectId, int groupRequestId)
            => $"{CompanyTeamApplicationPrefix}{projectId}:{groupRequestId}";

        public static bool IsCompanyDisplayNotification(string? linkTarget, string content)
        {
            if (!string.IsNullOrEmpty(linkTarget)
                && (linkTarget.StartsWith(CompanyTeamCommentPrefix, StringComparison.OrdinalIgnoreCase)
                    || linkTarget.StartsWith(CompanyTeamApplicationPrefix, StringComparison.OrdinalIgnoreCase)))
            {
                return true;
            }

            if (content.StartsWith("Zespół ", StringComparison.OrdinalIgnoreCase)
                && (content.Contains("napisał komentarz", StringComparison.OrdinalIgnoreCase)
                    || content.Contains("zgłosił się do projektu", StringComparison.OrdinalIgnoreCase)))
            {
                return true;
            }

            if (linkTarget != null
                && linkTarget.StartsWith("project:", StringComparison.OrdinalIgnoreCase)
                && content.Contains("komentarz", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (content.Contains("wysłała prośbę o realizację", StringComparison.OrdinalIgnoreCase)
                || content.Contains("wysłał prośbę", StringComparison.OrdinalIgnoreCase)
                || content.Contains("zgłosił się do projektu", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return false;
        }

        public static bool IsStudentDisplayNotification(string? linkTarget, string content)
        {
            if (!string.IsNullOrEmpty(linkTarget)
                && (linkTarget.StartsWith(StudentCommentReplyPrefix, StringComparison.OrdinalIgnoreCase)
                    || linkTarget.StartsWith(StudentProjectDecisionPrefix, StringComparison.OrdinalIgnoreCase)))
            {
                return true;
            }

            if (content.StartsWith("FIRMA ", StringComparison.OrdinalIgnoreCase))
                return true;

            if (linkTarget != null
                && linkTarget.StartsWith("project:", StringComparison.OrdinalIgnoreCase)
                && (content.Contains("został przyjęty", StringComparison.OrdinalIgnoreCase)
                    || content.Contains("nie został przyjęty", StringComparison.OrdinalIgnoreCase)
                    || content.Contains("zaakceptowała", StringComparison.OrdinalIgnoreCase)
                    || content.Contains("odrzuciła", StringComparison.OrdinalIgnoreCase)))
            {
                return true;
            }

            return false;
        }
    }
}
