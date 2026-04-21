using System;

namespace wspolpracujmy.Models
{
    public enum Role
    {
        Admin,
        Student,
        Company
    }

    public enum GroupStatus
    {
        Pending,
        Accepted,
        Declined
    }

    public enum LanguageDoc
    {
        Polish,
        English
    }

    public enum NotificationStatus
    {
        NotRead,
        Read
    }

    public enum Priority
    {
        P1 = 1,
        P2 = 2,
        P3 = 3,
        P4 = 4,
        P5 = 5
    }
}
