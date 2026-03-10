using System;
using System.Collections.Generic;

namespace AutoCurriculum.Models;

public partial class Section
{
    public int SectionId { get; set; }

    public int ChapterId { get; set; }

    public string? SectionTitle { get; set; }

    public int? SectionOrder { get; set; }

    public int? TocLevel { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual Chapter Chapter { get; set; } = null!;

    public virtual ICollection<Lesson> Lessons { get; set; } = new List<Lesson>();
}
