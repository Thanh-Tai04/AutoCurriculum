using System;
using System.Collections.Generic;

namespace AutoCurriculum.Models;

public partial class CurriculumHistory
{
    public int HistoryId { get; set; }

    public int? TopicId { get; set; }

    public int? CreatedBy { get; set; }

    public DateTime? CreatedAt { get; set; }

    public string? Status { get; set; }

    public virtual User? CreatedByNavigation { get; set; }

    public virtual Topic? Topic { get; set; }
}
