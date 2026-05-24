using Microsoft.AspNetCore.Authorization;

namespace wspolpracujmy.Services.Authorization
{
    /// <summary>
    /// Wymaganie autoryzacji oznaczające, że użytkownik musi być liderem grupy.
    /// </summary>
    public class GroupOwnerRequirement : IAuthorizationRequirement
    {
        // marker requirement - no properties required
    }
}
