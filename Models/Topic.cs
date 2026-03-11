using System;
using System.Collections.Generic;

namespace AutoCurriculum.Models;

public partial class Topic
{
    public int TopicId { get; set; }

    public string TopicName { get; set; } = null!;

    public string? Description { get; set; }

    public int? SourceId { get; set; }

    public int? CreatedBy { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual ICollection<Chapter> Chapters { get; set; } = new List<Chapter>();

    public virtual User? CreatedByNavigation { get; set; }

    public virtual ICollection<CurriculumHistory> CurriculumHistories { get; set; } = new List<CurriculumHistory>();

    public virtual Source? Source { get; set; }
}
