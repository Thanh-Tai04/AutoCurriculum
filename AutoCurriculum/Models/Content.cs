using System;
using System.Collections.Generic;

namespace AutoCurriculum.Models;

public partial class Content
{
    public int ContentId { get; set; }

    public int LessonId { get; set; }

    public string? ContentText { get; set; }

    public virtual Lesson Lesson { get; set; } = null!;
}
