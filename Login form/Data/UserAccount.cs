namespace Login_form.Data;

public class UserAccount
{
    public string Id { get; set; } = string.Empty;

    public string? Email { get; set; }

    public string? NormalizedEmail { get; set; }

    public string? PasswordHash { get; set; }
}
