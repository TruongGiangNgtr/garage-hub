using GarageHub.Common.Enums;

namespace GarageHub.Common.Dtos.User;

public record UserDto(int Id, string FullName, string Email, UserRole Role, DateTime CreatedAt);
