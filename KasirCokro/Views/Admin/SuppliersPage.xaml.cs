using KasirCokro.Models;
using KasirCokro.Views.Admin;
using KasirCokro.Views.Auth;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using static KasirCokro.Views.Admin.SuppliersPage;

namespace KasirCokro.Views.Admin
{
    public partial class SuppliersPage : Window
    {
        public SuppliersPage()
        {
            InitializeComponent();
            LoadDataSupplier();
        }

        private void LoadDataSupplier()
        {
            var list = new List<Models.Supplier>();

            try
            {
                using (MySqlConnection conn = Helpers.DatabaseHelper.GetConnection())
                {
                    string query = "SELECT id, nama_supplier, kontak, alamat FROM suppliers";
                    MySqlCommand cmd = new MySqlCommand(query, conn);
                    var reader = cmd.ExecuteReader();

                    while (reader.Read())
                    {
                        list.Add(new Models.Supplier
                        {
                            Id = reader.GetInt32("id"),
                            NamaSupplier = reader.GetString("nama_supplier"),
                            Kontak = reader.GetString("kontak"),
                            Alamat = reader.GetString("alamat")
                        });
                    }

                    dgSupplier.ItemsSource = list;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error memuat data supplier: " + ex.Message);
            }
        }

        public class Supplier
        {
            public int Id { get; set; }
            public string NamaSupplier { get; set; }
            public string Kontak { get; set; }
            public string Alamat { get; set; }
        }

        private void BtnTambah_Click(object sender, RoutedEventArgs e)
        {
            OpenSupplierForm(new Models.Supplier());
        }

        private void BtnEdit_Click(object sender, RoutedEventArgs e)
        {
            if (dgSupplier.SelectedItem is Models.Supplier selected)
            {
                OpenSupplierForm(selected);
            }
        }

        private void BtnHapus_Click(object sender, RoutedEventArgs e)
        {
            if (dgSupplier.SelectedItem is Models.Supplier selected)
            {
                if (MessageBox.Show("Yakin ingin menghapus supplier ini?", "Konfirmasi", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    HapusSupplier(selected.Id);
                }
            }
        }

        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            LoadDataSupplier();
        }

        private void OpenSupplierForm(Models.Supplier supplier)
        {
            this.Close();
            var form = new SupplierForm(supplier);
            form.ShowDialog();

            if (form.IsSaved)
            {
                LoadDataSupplier();
            }
        }

        private void HapusSupplier(int id)
        {
            try
            {
                using (MySqlConnection conn = Helpers.DatabaseHelper.GetConnection())
                {
                    string query = "DELETE FROM suppliers WHERE id = @id";
                    MySqlCommand cmd = new MySqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@id", id);
                    cmd.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Gagal menghapus supplier: " + ex.Message);
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

        private void Supplier_Click(object sender, RoutedEventArgs e)
        {
            Views.Admin.SuppliersPage suppliers = new Views.Admin.SuppliersPage();
            suppliers.Show();
            this.Close();
        }
        private void Logout_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Yakin ingin logout?", "Konfirmasi",
                                 MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                var login = new LoginWindow();
                login.Show();
                this.Close();
            }
        }

        private void BtnImportExcel_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var openFileDialog = new Microsoft.Win32.OpenFileDialog
                {
                    Filter = "Excel Files (*.xlsx)|*.xlsx",
                    Title = "Pilih File Excel Supplier"
                };

                if (openFileDialog.ShowDialog() == true)
                {
                    var filePath = openFileDialog.FileName;

                    using (var package = new OfficeOpenXml.ExcelPackage(new FileInfo(filePath)))
                    {
                        var worksheet = package.Workbook.Worksheets.FirstOrDefault();
                        if (worksheet == null)
                        {
                            MessageBox.Show("Worksheet tidak ditemukan di file Excel.");
                            return;
                        }

                        int row = 2; // Mulai dari baris ke-2, baris ke-1 untuk header
                        while (worksheet.Cells[row, 1].Value != null)
                        {
                            string nama = worksheet.Cells[row, 1].Text;
                            string alamat = worksheet.Cells[row, 2].Text;
                            string telepon = worksheet.Cells[row, 3].Text;

                            // Simpan ke database
                            using (var conn = Helpers.DatabaseHelper.GetConnection())
                            {
                                string query = "INSERT INTO suppliers (nama, alamat, telepon) VALUES (@nama, @alamat, @telepon)";
                                using (var cmd = new MySqlCommand(query, conn))
                                {
                                    cmd.Parameters.AddWithValue("@nama", nama);
                                    cmd.Parameters.AddWithValue("@alamat", alamat);
                                    cmd.Parameters.AddWithValue("@telepon", telepon);
                                    cmd.ExecuteNonQuery();
                                }
                            }

                            row++;
                        }

                        MessageBox.Show("Import data supplier berhasil!");
                        LoadDataSupplier();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Gagal mengimpor data: " + ex.Message);
            }

        }

        private void TxtCari_TextChanged(object sender, TextChangedEventArgs e)
        {
            string keyword = (sender as TextBox)?.Text.ToLower() ?? "";

            try
            {
                var filteredList = new List<Models.Supplier>();

                using (MySqlConnection conn = Helpers.DatabaseHelper.GetConnection())
                {
                    string query = "SELECT id, nama_supplier, kontak, alamat FROM suppliers WHERE LOWER(nama_supplier) LIKE @keyword";
                    MySqlCommand cmd = new MySqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@keyword", $"%{keyword}%");

                    var reader = cmd.ExecuteReader();
                    while (reader.Read())
                    {
                        filteredList.Add(new Models.Supplier
                        {
                            Id = reader.GetInt32("id"),
                            NamaSupplier = reader.GetString("nama_supplier"),
                            Kontak = reader.GetString("kontak"),
                            Alamat = reader.GetString("alamat")
                        });
                    }
                }

                dgSupplier.ItemsSource = filteredList;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error saat pencarian supplier: " + ex.Message);
            }
        }
    }
}