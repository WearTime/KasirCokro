using System;
using System.ComponentModel.DataAnnotations;

namespace KasirCokro.Models
{
    public class Supplier
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string NamaSupplier { get; set; }

        [MaxLength(50)]
        public string Kontak { get; set; }

        public string Alamat { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        public string DisplayText => $"{NamaSupplier} - {Kontak}";

        public Supplier()
        {
            CreatedAt = DateTime.Now;
            UpdatedAt = DateTime.Now;
        }
    }
}