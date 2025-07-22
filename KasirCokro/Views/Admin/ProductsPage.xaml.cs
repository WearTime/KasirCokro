using KasirCokro.Models;
using KasirCokro.Views.Auth;
using MySql.Data.MySqlClient;
using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace KasirCokro.Views.Admin
{
    public partial class ProductsPage : Window
    {
        public ProductsPage()
        {
            InitializeComponent();
            LoadDataProduk();
            TampilkanStatistikProduk();
        }

        private void TampilkanStatistikProduk()
        {
            int totalProduk = 0;
            int stokHampirHabis = 0;
            int stokHabis = 0;

            try
            {
                using (MySqlConnection conn = Helpers.DatabaseHelper.GetConnection())
                {
                    string query = "SELECT COUNT(*) as total, " +
                                   "SUM(CASE WHEN stok < 10 AND stok > 0 THEN 1 ELSE 0 END) AS hampir_habis, " +
                                   "SUM(CASE WHEN stok = 0 THEN 1 ELSE 0 END) AS habis " +
                                   "FROM products";

                    MySqlCommand cmd = new MySqlCommand(query, conn);
                    var reader = cmd.ExecuteReader();

                    if (reader.Read())
                    {
                        totalProduk = reader.GetInt32("total");
                        stokHampirHabis = reader.GetInt32("hampir_habis");
                        stokHabis = reader.GetInt32("habis");
                    }
                }

                txtTotalProduk.Text = $"{totalProduk}";
                txtStokRendah.Text = $"{stokHampirHabis}";
                txtHabisStok.Text = $"{stokHabis}";

                txtJumlahData.Text = $"Menampilkan {totalProduk} dari {totalProduk} data";
            }
            catch (Exception ex)
            {
                MessageBox.Show("Gagal memuat statistik produk: " + ex.Message);
            }
        }

        private void LoadDataProduk(string search = "")
        {
            var list = new List<Product>();

            try
            {
                using (MySqlConnection conn = Helpers.DatabaseHelper.GetConnection())
                {
                    string query = @"SELECT p.id, p.barcode, p.nama_produk, p.harga_beli, p.harga_jual, p.mark_up, p.stok, p.stok_awal, p.pendapatan, 
                           p.laba, p.harta, p.persentase, p.supplier_id, p.created_at, p.updated_at,
                           s.nama_supplier
                           FROM products p 
                           LEFT JOIN suppliers s ON p.supplier_id = s.id";

                    if (!string.IsNullOrEmpty(search))
                    {
                        query += " WHERE p.nama_produk LIKE @search OR p.barcode LIKE @search";
                    }

                    query += " ORDER BY p.nama_produk";

                    MySqlCommand cmd = new MySqlCommand(query, conn);
                    if (!string.IsNullOrEmpty(search))
                    {
                        cmd.Parameters.AddWithValue("@search", $"%{search}%");
                    }

                    var reader = cmd.ExecuteReader();

                    while (reader.Read())
                    {
                        var product = new Product
                        {
                            Id = reader.GetInt32("id"),
                            Barcode = reader.GetString("barcode"),
                            NamaProduk = reader.GetString("nama_produk"),
                            HargaBeli = reader.IsDBNull(reader.GetOrdinal("harga_beli")) ? 0 : reader.GetDecimal("harga_beli"),
                            HargaJual = reader.GetDecimal("harga_jual"),
                            MarkUp = reader.IsDBNull(reader.GetOrdinal("mark_up")) ? 0 : reader.GetDecimal("mark_up"),
                            Stok = reader.GetInt32("stok"),
                            StokAwal = reader.IsDBNull(reader.GetOrdinal("stok_awal")) ? 0 : reader.GetInt32("stok_awal"),
                            Pendapatan = reader.GetDecimal("pendapatan"),
                            Laba = reader.GetDecimal("laba"),
                            Harta = reader.IsDBNull(reader.GetOrdinal("harta")) ? 0 : reader.GetDecimal("harta"),
                            Persentase = reader.IsDBNull(reader.GetOrdinal("persentase")) ? "" : reader.GetString("persentase"),
                            SupplierId = reader.GetInt32("supplier_id"),
                            CreatedAt = reader.GetDateTime("created_at"),
                            UpdatedAt = reader.GetDateTime("updated_at"),
                            Supplier = new Models.Supplier
                            {
                                Id = reader.GetInt32("supplier_id"),
                                NamaSupplier = reader.IsDBNull(reader.GetOrdinal("nama_supplier")) ? "Unknown" : reader.GetString("nama_supplier")
                            }
                        };

                        list.Add(product);
                    }

                    dgProduk.ItemsSource = list;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error memuat data produk: " + ex.Message);
            }
        }
        private void BtnTambah_Click(object sender, RoutedEventArgs e)
        {
            OpenProductForm(new Product());
        }

        private void BtnEdit_Click(object sender, RoutedEventArgs e)
        {
            if (dgProduk.SelectedItem is Product selected)
            {
                OpenProductForm(selected);
            }
        }

        private void BtnDetail_Click(object sender, RoutedEventArgs e)
        {
            if (dgProduk.SelectedItem is Product selected)
            {
                string detail = $"Detail Produk:\n\n" +
                               $"Barcode: {selected.Barcode}\n" +
                               $"Nama: {selected.NamaProduk}\n" +
                               $"Harga Beli: Rp{selected.HargaBeli:N0}\n" +
                               $"Harga Jual: Rp{selected.HargaJual:N0}\n" +
                               $"Stok Awal: {selected.StokAwal}\n" +
                               $"Stok Saat Ini: {selected.Stok}\n" +
                               $"Supplier: {selected.SupplierName}\n" +
                               $"Status: {selected.StokStatus}";

                MessageBox.Show(detail, "Detail Produk", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void BtnHapus_Click(object sender, RoutedEventArgs e)
        {
            if (dgProduk.SelectedItem is Product selected)
            {
                if (MessageBox.Show($"Yakin ingin menghapus produk '{selected.NamaProduk}'?",
                    "Konfirmasi", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                {
                    HapusProduk(selected.Id);
                }
            }
        }

        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            LoadDataProduk();
            TampilkanStatistikProduk();
        }

        private void TxtCari_TextChanged(object sender, TextChangedEventArgs e)
        {
            LoadDataProduk(txtCari.Text);
        }

        private void OpenProductForm(Product product)
        {
            var form = new ProductForm(product);
            form.ShowDialog();

            if (form.IsSaved)
            {
                LoadDataProduk(txtCari.Text);
                TampilkanStatistikProduk();
            }
        }

        private void HapusProduk(int id)
        {
            try
            {
                using (MySqlConnection conn = Helpers.DatabaseHelper.GetConnection())
                {
                    string query = "DELETE FROM products WHERE id = @id";
                    MySqlCommand cmd = new MySqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@id", id);

                    int rowsAffected = cmd.ExecuteNonQuery();

                    if (rowsAffected > 0)
                    {
                        MessageBox.Show("Produk berhasil dihapus!", "Sukses", MessageBoxButton.OK, MessageBoxImage.Information);
                        LoadDataProduk(txtCari.Text);
                        TampilkanStatistikProduk();
                    }
                    else
                    {
                        MessageBox.Show("Produk tidak ditemukan!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Gagal menghapus produk: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnImportExcel_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.FileName = "Pilih File Excel";
            dlg.Filter = "Excel Files (*.xlsx)|*.xlsx";

            if (dlg.ShowDialog() == true)
            {
                ImportFromExcel(dlg.FileName);
            }
        }

        private void ImportFromExcel(string filePath)
        {
            try
            {
                int successCount = 0;
                FileInfo fileInfo = new FileInfo(filePath);
                using (var package = new ExcelPackage(fileInfo))
                {
                    var ws = package.Workbook.Worksheets[0];
                    int rowCount = ws.Dimension.Rows;

                    for (int i = 2; i <= rowCount; i++)
                    {
                        try
                        {
                            string barcode = ws.Cells[i, 1].Value?.ToString();
                            string nama = ws.Cells[i, 2].Value?.ToString();
                            decimal hargaBeli = Convert.ToDecimal(ws.Cells[i, 3].Value);
                            decimal hargaJual = Convert.ToDecimal(ws.Cells[i, 4].Value);
                            int stok = Convert.ToInt32(ws.Cells[i, 5].Value);
                            int supplierId = Convert.ToInt32(ws.Cells[i, 6].Value ?? 1);
                            if (!string.IsNullOrEmpty(barcode) && !string.IsNullOrEmpty(nama))
                            {
                                SimpanProduk(new Product
                                {
                                    Barcode = barcode,
                                    NamaProduk = nama,
                                    HargaBeli = hargaBeli,
                                    HargaJual = hargaJual,
                                    Stok = stok,
                                    StokAwal = stok,
                                    SupplierId = supplierId
                                });
                                successCount++;
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error importing row {i}: {ex.Message}");
                        }
                    }
                }

                MessageBox.Show($"Berhasil import {successCount} produk dari Excel!",
                    "Import Berhasil", MessageBoxButton.OK, MessageBoxImage.Information);
                LoadDataProduk();
                TampilkanStatistikProduk();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Gagal import dari Excel: " + ex.Message,
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SimpanProduk(Product product)
        {
            using (MySqlConnection conn = Helpers.DatabaseHelper.GetConnection())
            {
                string query = "";
                if (product.Id == 0)
                {
                    query = @"INSERT INTO products (barcode, nama_produk, harga_beli, harga_jual, stok, stok_awal, supplier_id) 
                             VALUES (@barcode, @nama, @hargabeli, @hargajual, @stok, @stokawal, @supplierid)";
                }
                else
                {
                    query = @"UPDATE products SET barcode=@barcode, nama_produk=@nama, harga_beli=@hargabeli, 
                             harga_jual=@hargajual, stok=@stok, stok_awal=@stokawal, supplier_id=@supplierid 
                             WHERE id=@id";
                }

                MySqlCommand cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@barcode", product.Barcode);
                cmd.Parameters.AddWithValue("@nama", product.NamaProduk);
                cmd.Parameters.AddWithValue("@hargabeli", product.HargaBeli ?? 0);
                cmd.Parameters.AddWithValue("@hargajual", product.HargaJual);
                cmd.Parameters.AddWithValue("@stok", product.Stok);
                cmd.Parameters.AddWithValue("@stokawal", product.StokAwal);
                cmd.Parameters.AddWithValue("@supplierid", product.SupplierId);

                if (product.Id != 0)
                {
                    cmd.Parameters.AddWithValue("@id", product.Id);
                }

                cmd.ExecuteNonQuery();
            }
        }

        private void BtnDashboard_Click(object sender, RoutedEventArgs e)
        {
            Views.Admin.DashboardAdmin dashboard = new Views.Admin.DashboardAdmin();
            dashboard.Show();
            this.Close();
        }

        private void Product_Click(object sender, RoutedEventArgs e)
        {
            Views.Admin.ProductsPage products = new Views.Admin.ProductsPage();
            products.Show();
            this.Close();
        }

        private void BrngMasuk_Click(object sender, RoutedEventArgs e)
        {
            Views.Admin.BarangMasukPage barangMasuk = new Views.Admin.BarangMasukPage();
            barangMasuk.Show();
            this.Close();
        }
        private void BrngKeluar_Click(object sender, RoutedEventArgs e)
        {
            Views.Admin.BarangKeluarPage barangMasuk = new Views.Admin.BarangKeluarPage();
            barangMasuk.Show();
            this.Close();
        }

        private void Supplier_Click(object sender, RoutedEventArgs e)
        {
            Views.Admin.SuppliersPage suppliers = new Views.Admin.SuppliersPage();
            suppliers.Show();
            this.Close();
        }

        private void Kas_Click(object sender, RoutedEventArgs e)
        {
            KasPage kasPage = new KasPage();
            kasPage.Show();
            this.Close();
        }

        private void TransactionMasuk_Click(object sender, RoutedEventArgs e)
        {
            TransactionMasukPage transactionMasuk = new TransactionMasukPage();
            transactionMasuk.Show();
            this.Close();
        }

        private void TransactionKeluar_Click(object sender, RoutedEventArgs e)
        {
            var transactionKeluarWindow = new TransactionKeluarPage();
            transactionKeluarWindow.Show();
            this.Close();
        }

        private void Pihutang_Click(object sender, RoutedEventArgs e)
        {
            PihutangPage pihutangPage = new PihutangPage();
            pihutangPage.Show();
            this.Close();
        }
        private void Logout_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Yakin ingin logout?", "Konfirmasi",
                                 MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                var login = new LoginWindow1();
                login.Show();
                this.Close();
            }
        }
    }
}