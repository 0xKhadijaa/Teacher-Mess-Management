using System.ComponentModel.DataAnnotations;

namespace MessManagementSystem.Models.Shared
{
    public class AppSetting
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Key { get; set; }

        [Required]
        public string Value { get; set; }
    }
}
