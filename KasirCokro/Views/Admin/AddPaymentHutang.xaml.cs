using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace KasirCokro.Views.Admin
{
    /// <summary>
    /// Interaction logic for AddPaymentHutang.xaml
    /// </summary>
    public partial class AddPaymentHutang : Window
    {
        private string _kodeTransaksi;
        private decimal _sisaHutang;

        public AddPaymentHutang(string kodeTransaksi, decimal sisaHutang)
        {
            InitializeComponent();
            _kodeTransaksi = kodeTransaksi;
            _sisaHutang = sisaHutang;

            // Set maximum payment amount
            txtJumlahPembayaran.Text = sisaHutang.ToString("N0");
        }

        private async void BtnSimpan_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!decimal.TryParse(txtJumlahPembayaran.Text.Replace(",", ""), out decimal jumlahPembayaran))
                {
                    MessageBox.Show("Jumlah pembayaran tidak valid!", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if (jumlahPembayaran <= 0)
                {
                    MessageBox.Show("Jumlah pembayaran harus lebih dari 0!", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if (jumlahPembayaran > _sisaHutang)
                {
                    MessageBox.Show($"Jumlah pembayaran tidak boleh lebih dari sisa hutang (Rp {_sisaHutang:N0})!",
                        "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                await SavePembayaran(jumlahPembayaran);

                MessageBox.Show("Pembayaran berhasil disimpan!", "Sukses",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                this.DialogResult = true;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving payment: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task SavePembayaran(decimal jumlahPembayaran)
        {
            string query = @"
            INSERT INTO pembayaran (tgl_pembelian, tunai, tgl_pembayaran, kode_transaksi, jenis)
            VALUES (@tglPembelian, @tunai, @tglPembayaran, @kodeTransaksi, 'piutang')";

            using (MySqlConnection conn = Helpers.DatabaseHelper.GetConnection())
            {

                using (MySqlCommand cmd = new MySqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@tglPembelian", DateTime.Now.Date);
                    cmd.Parameters.AddWithValue("@tunai", jumlahPembayaran);
                    cmd.Parameters.AddWithValue("@tglPembayaran", DateTime.Now.Date);
                    cmd.Parameters.AddWithValue("@kodeTransaksi", _kodeTransaksi);

                    await cmd.ExecuteNonQueryAsync();
                }
            }
        }

        private void BtnBatal_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}
