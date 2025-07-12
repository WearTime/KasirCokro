using KasirCokro.Views.Kasir;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Printing;
using System.Windows;

namespace KasirCokro.Helpers
{
    public static class RawPrinterHelper
    {
        public static void PrintStruk(int transaksiId, List<TransaksiPenjualan.KeranjangItem> items)
        {
            var pd = new PrintDocument();
            pd.DefaultPageSettings.PaperSize = new PaperSize("Thermal", 290, 800);
            pd.PrintPage += (sender, args) =>
            {
                int y = 20;
                Graphics g = args.Graphics;

                using (Font fontNormal = new Font(new FontFamily("Courier New"), 9))
                using (Font fontBold = new Font(new FontFamily("Courier New"), 9, System.Drawing.FontStyle.Bold))
                {
                    g.DrawString("TOKO ABC", fontBold, Brushes.Black, new System.Drawing.Point(90, y)); y += 20;
                    g.DrawString("Jl. Raya No. 123, Jakarta", fontNormal, Brushes.Black, new System.Drawing.Point(50, y)); y += 20;
                    g.DrawString("----------------------------------------", fontNormal, Brushes.Black, new System.Drawing.Point(0, y)); y += 20;

                    g.DrawString($"Kode: TRX-{transaksiId}", fontNormal, Brushes.Black, new System.Drawing.Point(0, y)); y += 20;
                    g.DrawString($"Tanggal: {DateTime.Now:dd/MM/yyyy HH:mm}", fontNormal, Brushes.Black, new System.Drawing.Point(0, y)); y += 20;
                    g.DrawString("----------------------------------------", fontNormal, Brushes.Black, new System.Drawing.Point(0, y)); y += 20;

                    foreach (var item in items)
                    {
                        string line1 = $"{item.NamaProduk.Substring(0, Math.Min(20, item.NamaProduk.Length))}";
                        g.DrawString(line1, fontNormal, Brushes.Black, new System.Drawing.Point(0, y)); y += 15;

                        string line2 = $"{item.Jumlah} x {item.HargaJual:N0} = {item.Subtotal:N0}";
                        g.DrawString(line2, fontNormal, Brushes.Black, new System.Drawing.Point(0, y)); y += 20;
                    }

                    g.DrawString("----------------------------------------", fontNormal, Brushes.Black, new System.Drawing.Point(0, y)); y += 20;

                    decimal totalHarga = 0;
                    foreach (var item in items)
                        totalHarga += item.Subtotal;

                    g.DrawString($"Total: Rp {totalHarga:N0}", fontBold, Brushes.Black, new System.Drawing.Point(0, y));
                }

                args.HasMorePages = false;
            };

            pd.Print();
        }
    }
}