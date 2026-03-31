using System;

namespace AutoCurriculum.Models
{
    public class UserViewModel
    {
        public string Id { get; set; }
        public string Email { get; set; }
        public string Role { get; set; }
        public DateTimeOffset? LockoutEnd { get; set; }
        
        public bool IsLocked => LockoutEnd != null && LockoutEnd > DateTimeOffset.UtcNow;
    }
}
