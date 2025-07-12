using System;
using System.ComponentModel.DataAnnotations;

namespace KasirCokro.Models
{
    public class Product
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string Barcode { get; set; }

        [Required]
        [MaxLength(100)]
        public string NamaProduk { get; set; }

        [Required]
        public decimal HargaJual { get; set; }

        [Required]
        public int Stok { get; set; }

        public int StokAwal { get; set; } = 0;

        [Required]
        public int SupplierId { get; set; }

        public decimal? HargaBeli { get; set; }

        public decimal Hpp
        {
            get => HargaBeli ?? 0;
            set => HargaBeli = value;
        }

        public decimal? MarkUp { get; set; }

        public decimal Pendapatan { get; set; } = 0;

        public decimal Laba { get; set; } = 0;

        public decimal? Harta { get; set; }

        [MaxLength(50)]
        public string Persentase { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        public virtual Supplier Supplier { get; set; }

        public string SupplierName => Supplier?.NamaSupplier ?? "Unknown";

        public decimal TotalValue => Stok * HargaJual;

        public decimal ProfitMargin => HargaBeli.HasValue && HargaBeli.Value > 0
            ? ((HargaJual - HargaBeli.Value) / HargaBeli.Value) * 100
            : 0;

        public bool IsLowStock => Stok < 10 && Stok > 0;

        public bool IsOutOfStock => Stok == 0;

        public string StokStatus
        {
            get
            {
                if (Stok == 0) return "Habis";
                if (Stok < 10) return "Rendah";
                return "Cukup";
            }
        }

        public string DisplayText => $"{NamaProduk} - {Barcode}";

        public Product()
        {
            CreatedAt = DateTime.Now;
            UpdatedAt = DateTime.Now;
        }
    }
}