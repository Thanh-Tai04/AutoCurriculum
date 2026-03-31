using System;
using System.ComponentModel.DataAnnotations;

namespace AutoCurriculum.Models
{
    public class ApiKey
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên dịch vụ (Provider)")]
        [StringLength(100)]
        public string Provider { get; set; } 

        [Required(ErrorMessage = "Vui lòng nhập mã API Key")]
        public string KeyValue { get; set; } 

        public bool IsActive { get; set; } = true; 

        public DateTime CreatedAt { get; set; } = DateTime.Now; 
    }
}