using System;
using System.Collections.Generic;

namespace AutoCurriculum.Models;

public partial class Lesson
{
    public int LessonId { get; set; }

    public int? ChapterId { get; set; }

    public int? SectionId { get; set; }

    public string? LessonTitle { get; set; }

    public int? LessonOrder { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual Chapter? Chapter { get; set; }

    public virtual ICollection<Content> Contents { get; set; } = new List<Content>();

    public virtual Section? Section { get; set; }
}
