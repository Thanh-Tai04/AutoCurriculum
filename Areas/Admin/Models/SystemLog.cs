using System;
using System.ComponentModel.DataAnnotations;

namespace AutoCurriculum.Models
{
    public class SystemLog
    {
        [Key]
        public int Id { get; set; }
        public string? UserEmail { get; set; }

        public string Action { get; set; }    
        
        public string Keyword { get; set; }   

        public string Status { get; set; }    

        public string Message { get; set; }   
        public long ExecutionTimeMs { get; set; } 

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}