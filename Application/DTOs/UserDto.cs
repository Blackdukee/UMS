namespace Application.DTOs
{
    // Record for a lightweight user data transfer
    public record UserDto(
        int Id,
        string Email,
        string Role,
        bool IsActive = true
    );
}
