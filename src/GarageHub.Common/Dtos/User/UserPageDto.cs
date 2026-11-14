namespace GarageHub.Common.Dtos.User;

public record UserPageDto(IReadOnlyList<UserDto> Items, int TotalCount, int Page, int PageSize, string? Search)
{
    public int TotalPages => TotalCount == 0 ? 1 : (TotalCount + PageSize - 1) / PageSize;
}
