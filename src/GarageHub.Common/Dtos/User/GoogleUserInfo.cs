namespace GarageHub.Common.Dtos.User;

/// <summary>Profile data read from the Google login (claims), before it is mapped to a Users row.</summary>
public record GoogleUserInfo(string GoogleId, string Email, string FullName);
