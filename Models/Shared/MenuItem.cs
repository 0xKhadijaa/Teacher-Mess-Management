using System.ComponentModel.DataAnnotations;

namespace MessManagementSystem.Models.Shared
{
    /// <summary>
    /// Represents the daily menu for the mess.
    /// One record per day (breakfast, lunch, dinner).
    /// </summary>
    public class MenuItem
    {
        public int Id { get; set; }

        /// <summary>
        /// The date for which this menu is valid.
        /// Must be unique (one menu per day).
        /// </summary>
        [Required]
        public DateOnly Date { get; set; }

        /// <summary>
        /// Breakfast menu description.
        /// </summary>
        [Required]
        [StringLength(200)]
        public string Breakfast { get; set; } = "Paratha, Egg, Tea";

        /// <summary>
        /// Lunch menu description.
        /// </summary>
        [Required]
        [StringLength(200)]
        public string Lunch { get; set; } = "Biryani, Raita, Salad";

        /// <summary>
        /// Dinner menu description.
        /// </summary>
        [Required]
        [StringLength(200)]
        public string Dinner { get; set; } = "Daal, Roti, Sabzi";
    }
}