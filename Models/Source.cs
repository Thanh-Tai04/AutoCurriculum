using System;
using System.Collections.Generic;

namespace AutoCurriculum.Models;

public partial class Source
{
    public int SourceId { get; set; }

    public string? SourceName { get; set; }

    public string? SourceUrl { get; set; }

    public DateTime? RetrievedDate { get; set; }

    public virtual ICollection<Topic> Topics { get; set; } = new List<Topic>();
}
