namespace PayrollApi.DTOs
{
    public class RegisterRequest
    {
        public string Email { get; set; } = default!;
        public string FullName { get; set; } = default!;
        public string Password { get; set; } = default!;
        public string Role { get; set; } = default!; // Admin/HR/HRManager/Employee
    }

    public class LoginRequest
    {
        public string Email { get; set; } = default!;
        public string Password { get; set; } = default!;
    }

    public class AuthResponse
    {
        public string Token { get; set; } = default!;
        public string Role { get; set; } = default!;
        public int UserId { get; set; }
        public string FullName { get; set; } = default!;
        public bool ProfileCompleted { get; set; }
        public DateTime ExpiresAtUtc { get; set; }
    }

    public class ResetPasswordRequest
    {
        public string Email { get; set; } = default!;
    }

    public class NewPasswordRequest
    {
        public string Email { get; set; } = default!;
        public string NewPassword { get; set; } = default!;
        public string Otp { get; set; } = default!;
    }
}
