using KasirCokro.Models;
using KasirCokro.Views.Auth;
using MySql.Data.MySqlClient;
using System;
using System.Windows;

namespace KasirCokro.Views.Admin
{
    public partial class SupplierForm : Window
    {
        public bool IsSaved { get; private set; }
        private readonly Models.Supplier _supplier;

        public SupplierForm(Models.Supplier supplier)
        {
            InitializeComponent();
            _supplier = supplier;
            txtNama.Text = supplier.NamaSupplier;
            txtKontak.Text = supplier.Kontak;
            txtAlamat.Text = supplier.Alamat;
        }

        private void BtnSimpan_Click(object sender, RoutedEventArgs e)
        {
            string nama = txtNama.Text.Trim();
            string kontak = txtKontak.Text.Trim();
            string alamat = txtAlamat.Text.Trim();

            if (string.IsNullOrEmpty(nama))
            {
                MessageBox.Show("Nama supplier harus diisi!");
                return;
            }

            try
            {
                using (MySqlConnection conn = Helpers.DatabaseHelper.GetConnection())
                {
                    if (_supplier.Id == 0)
                    {
                        // Insert
                        string query = "INSERT INTO suppliers (nama_supplier, kontak, alamat) VALUES (@nama, @kontak, @alamat)";
                        MySqlCommand cmd = new MySqlCommand(query, conn);
                        cmd.Parameters.AddWithValue("@nama", nama);
                        cmd.Parameters.AddWithValue("@kontak", kontak);
                        cmd.Parameters.AddWithValue("@alamat", alamat);
                        cmd.ExecuteNonQuery();
                    }
                    else
                    {
                        // Update
                        string query = "UPDATE suppliers SET nama_supplier=@nama, kontak=@kontak, alamat=@alamat WHERE id=@id";
                        MySqlCommand cmd = new MySqlCommand(query, conn);
                        cmd.Parameters.AddWithValue("@id", _supplier.Id);
                        cmd.Parameters.AddWithValue("@nama", nama);
                        cmd.Parameters.AddWithValue("@kontak", kontak);
                        cmd.Parameters.AddWithValue("@alamat", alamat);
                        cmd.ExecuteNonQuery();
                    }
                }

                IsSaved = true;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Gagal menyimpan: " + ex.Message);
            }
        }

        private void BtnKembali_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void BtnBatal_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }

}