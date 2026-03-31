using System;
using System.ComponentModel.DataAnnotations;

namespace AutoCurriculum.Models
{
    public class PromptConfig
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string PromptCode { get; set; }
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } 

        public string Description { get; set; }

        [Required]
        public string PromptText { get; set; } 

        public DateTime UpdatedAt { get; set; } = DateTime.Now; 
    }
}