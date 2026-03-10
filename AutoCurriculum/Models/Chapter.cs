using System;
using System.Collections.Generic;

namespace AutoCurriculum.Models;

public partial class Chapter
{
    public int ChapterId { get; set; }

    public int TopicId { get; set; }

    public string? ChapterTitle { get; set; }

    public int? ChapterOrder { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual ICollection<Lesson> Lessons { get; set; } = new List<Lesson>();

    public virtual ICollection<Section> Sections { get; set; } = new List<Section>();

    public virtual Topic Topic { get; set; } = null!;
}
