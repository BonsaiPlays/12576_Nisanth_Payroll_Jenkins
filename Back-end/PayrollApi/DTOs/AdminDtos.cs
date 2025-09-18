namespace PayrollApi.DTOs
{
    public class CreateSkeletalUserRequest
    {
        public string Email { get; set; } = default!;
        public string FullName { get; set; } = default!;
        public string Role { get; set; } = default!;
        public int? DepartmentId { get; set; }
    }

    public class UserListItem
    {
        public int Id { get; set; }
        public string Email { get; set; } = default!;
        public string FullName { get; set; } = default!;
        public string Role { get; set; } = default!;
        public bool IsActive { get; set; }
        public string? Department { get; set; }
    }

    public class AssignRoleRequest
    {
        public string Role { get; set; } = default!;
    }
}
