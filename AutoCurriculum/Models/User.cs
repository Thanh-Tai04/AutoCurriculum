using System;
using System.Collections.Generic;

namespace AutoCurriculum.Models;

public partial class User
{
    public int UserId { get; set; }

    public string Username { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public string Role { get; set; } = null!;

    public DateTime? CreatedAt { get; set; }

    public virtual ICollection<CurriculumHistory> CurriculumHistories { get; set; } = new List<CurriculumHistory>();

    public virtual ICollection<Topic> Topics { get; set; } = new List<Topic>();
}
