namespace mvc.Models {
    public class User
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        public required string Email { get; set; }
        public required string Password { get; set; }
        public int RoleId { get; set; }        // Foreign key
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        
        // Navigation property to Role
        public required Role Role { get; set; }
    }

    public class Role
    {
        public int Id { get; set; }
        public required string RoleName { get; set; }
        public string? Description { get; set; }
        
        // No navigation property back to Users
    }
}

