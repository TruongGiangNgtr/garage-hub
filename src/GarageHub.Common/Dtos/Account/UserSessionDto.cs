using GarageHub.Common.Enums;

namespace GarageHub.Common.Dtos.Account;

public class UserSessionDto
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public UserRole Role { get; set; }
}
