using KasirCokro.Views.Auth;
using LiveCharts;
using LiveCharts.Wpf;
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

namespace KasirCokro.Views.Kasir
{
    public partial class DashboardKasir : Window
    {
        public SeriesCollection SeriesCollection { get; set; }
        public List<string> Labels { get; set; }

        public DashboardKasir()
        {
            InitializeComponent();
            LoadChartData();
        }

        private void LoadChartData()
        {
            SeriesCollection = new SeriesCollection();
            Labels = new List<string>();

            try
            {
                using (MySqlConnection conn = Helpers.DatabaseHelper.GetConnection())
                {
                    string query = @"
                        SELECT HOUR(tanggal_transaksi) AS jam, COUNT(*) AS jumlah 
                        FROM transactions 
                        WHERE DATE(tanggal_transaksi) = CURDATE() AND jenis = 'penjualan'
                        GROUP BY HOUR(tanggal_transaksi)";

                    MySqlCommand cmd = new MySqlCommand(query, conn);
                    MySqlDataReader reader = cmd.ExecuteReader();

                    var values = new ChartValues<int>();

                    while (reader.Read())
                    {
                        int jam = Convert.ToInt32(reader["jam"]);
                        int jumlah = Convert.ToInt32(reader["jumlah"]);

                        Labels.Add(jam + ":00");
                        values.Add(jumlah);
                    }

                    SeriesCollection.Add(new ColumnSeries
                    {
                        Title = "Penjualan",
                        Values = values
                    });

                    chartTransaksiHarian.Series = SeriesCollection;
                    chartTransaksiHarian.AxisX.Clear();
                    chartTransaksiHarian.AxisX.Add(new Axis
                    {
                        Title = "Jam",
                        Labels = Labels
                    });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error memuat data chart: " + ex.Message);
            }
        }

        private void BtnTransaksi_Click(object sender, RoutedEventArgs e)
        {
            TransaksiPenjualan transaksiWindow = new TransaksiPenjualan();
            transaksiWindow.Show();
            this.Close();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            TransaksiPenjualan transaksiWindow = new TransaksiPenjualan();
            transaksiWindow.Show();
            this.Close();
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            var riwayat = new RiwayatTransaksi();
            riwayat.Show();
            this.Close();
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            MessageBoxResult result = MessageBox.Show("Apakah Anda yakin ingin logout?",
                "Konfirmasi Logout", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                var login = new LoginWindow1();
                login.Show();
                this.Close();
            }
        }
    }
}