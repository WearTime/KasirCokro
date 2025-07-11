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
using MySql.Data.MySqlClient;
using LiveCharts;
using LiveCharts.Wpf;
using KasirCokro.Views.Auth;
using System.Globalization;

namespace KasirCokro.Views.Admin
{
    public partial class DashboardAdmin : Window
    {
        public DashboardAdmin()
        {
            InitializeComponent();

            // Load all dashboard data
            LoadDashboardCards();
            LoadPenjualanChart();
            LoadKeuntunganChart();
            LoadStokKurang();
        }

        // Method untuk memuat data ke 3 cards
        private void LoadDashboardCards()
        {
            LoadPenjualanHariIni();
            LoadPendapatanHariIni();
            LoadTotalHPP();
        }

        private void LoadPenjualanHariIni()
        {
            try
            {
                using (MySqlConnection conn = Helpers.DatabaseHelper.GetConnection())
                {
                    
                    // Query disesuaikan dengan struktur tabel transactions
                    string query = @"
                        SELECT COALESCE(SUM(subtotal), 0) AS total_penjualan 
                        FROM transactions 
                        WHERE DATE(tanggal_transaksi) = CURDATE()";

                    MySqlCommand cmd = new MySqlCommand(query, conn);
                    object result = cmd.ExecuteScalar();

                    decimal totalPenjualan = Convert.ToDecimal(result ?? 0);
                    txtPenjualanHariIni.Text = totalPenjualan.ToString("C", new CultureInfo("id-ID"));
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error memuat data penjualan hari ini: " + ex.Message);
                txtPenjualanHariIni.Text = "Rp 0";
            }
        }

        private void LoadPendapatanHariIni()
        {
            try
            {
                using (MySqlConnection conn = Helpers.DatabaseHelper.GetConnection())
                {
                    
                    // Query disesuaikan dengan struktur tabel transactions
                    string query = @"
                        SELECT COALESCE(SUM(laba), 0) AS total_laba 
                        FROM transactions 
                        WHERE DATE(tanggal_transaksi) = CURDATE()";

                    MySqlCommand cmd = new MySqlCommand(query, conn);
                    object result = cmd.ExecuteScalar();

                    decimal totalLaba = Convert.ToDecimal(result ?? 0);
                    txtPendapatanHariIni.Text = totalLaba.ToString("C", new CultureInfo("id-ID"));
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error memuat data pendapatan hari ini: " + ex.Message);
                txtPendapatanHariIni.Text = "Rp 0";
            }
        }

        private void LoadTotalHPP()
        {
            try
            {
                using (MySqlConnection conn = Helpers.DatabaseHelper.GetConnection())
                {
                    
                    // Query untuk menghitung total HPP hari ini
                    string query = @"
                        SELECT COALESCE(SUM(t.qty * p.harga_beli), 0) AS total_hpp
                        FROM transactions t
                        INNER JOIN products p ON t.kode_product = p.barcode
                        WHERE DATE(t.tanggal_transaksi) = CURDATE()";

                    MySqlCommand cmd = new MySqlCommand(query, conn);
                    object result = cmd.ExecuteScalar();

                    decimal totalHpp = Convert.ToDecimal(result ?? 0);
                    txtStokRendah.Text = totalHpp.ToString("C", new CultureInfo("id-ID"));
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error memuat data HPP: " + ex.Message);
                txtStokRendah.Text = "Rp 0";
            }
        }

        private void LoadPenjualanChart()
        {
            var series = new SeriesCollection();
            var labels = new List<string>();
            var values = new ChartValues<int>();

            try
            {
                using (MySqlConnection conn = Helpers.DatabaseHelper.GetConnection())
                {
                    
                    string query = @"
                        SELECT HOUR(tanggal_transaksi) AS jam, COUNT(*) AS jumlah 
                        FROM transactions 
                        WHERE DATE(tanggal_transaksi) = CURDATE()
                        GROUP BY HOUR(tanggal_transaksi)
                        ORDER BY jam";

                    MySqlCommand cmd = new MySqlCommand(query, conn);
                    var reader = cmd.ExecuteReader();

                    // Initialize dengan jam 0-23
                    for (int i = 0; i < 24; i++)
                    {
                        labels.Add($"{i:00}:00");
                        values.Add(0);
                    }

                    while (reader.Read())
                    {
                        int jam = Convert.ToInt32(reader["jam"]);
                        int jumlah = Convert.ToInt32(reader["jumlah"]);

                        if (jam >= 0 && jam < 24)
                        {
                            values[jam] = jumlah;
                        }
                    }

                    series.Add(new ColumnSeries
                    {
                        Title = "Penjualan per Jam",
                        Values = values,
                        Fill = new SolidColorBrush(Color.FromRgb(59, 130, 246))
                    });

                    chartPenjualanHarian.Series = series;
                    chartPenjualanHarian.AxisX.Clear();
                    chartPenjualanHarian.AxisX.Add(new Axis
                    {
                        Title = "Jam",
                        Labels = labels
                    });

                    chartPenjualanHarian.AxisY.Clear();
                    chartPenjualanHarian.AxisY.Add(new Axis
                    {
                        Title = "Jumlah Transaksi",
                        LabelFormatter = value => value.ToString("N0")
                    });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error memuat chart penjualan: " + ex.Message);
            }
        }

        private void LoadKeuntunganChart()
        {
            var series = new SeriesCollection();
            var labels = new List<string>();
            var profitValues = new ChartValues<decimal>();

            try
            {
                using (MySqlConnection conn = Helpers.DatabaseHelper.GetConnection())
                {
                    
                    string query = @"
                        SELECT DATE(tanggal_transaksi) AS tanggal, 
                               COALESCE(SUM(laba), 0) AS total_laba
                        FROM transactions
                        WHERE tanggal_transaksi >= DATE_SUB(CURDATE(), INTERVAL 7 DAY)
                        GROUP BY DATE(tanggal_transaksi)
                        ORDER BY tanggal";

                    MySqlCommand cmd = new MySqlCommand(query, conn);
                    var reader = cmd.ExecuteReader();

                    while (reader.Read())
                    {
                        DateTime tanggal = reader.GetDateTime("tanggal");
                        decimal laba = reader.GetDecimal("total_laba");

                        labels.Add(tanggal.ToString("dd/MM"));
                        profitValues.Add(laba);
                    }

                    series.Add(new LineSeries
                    {
                        Title = "Keuntungan Harian",
                        Values = profitValues,
                        PointGeometrySize = 8,
                        Fill = Brushes.Transparent,
                        Stroke = new SolidColorBrush(Color.FromRgb(34, 197, 94)),
                        StrokeThickness = 3
                    });

                    chartKeuntungan.Series = series;
                    chartKeuntungan.AxisX.Clear();
                    chartKeuntungan.AxisX.Add(new Axis
                    {
                        Title = "Tanggal",
                        Labels = labels
                    });

                    chartKeuntungan.AxisY.Clear();
                    chartKeuntungan.AxisY.Add(new Axis
                    {
                        Title = "Keuntungan (Rp)",
                        LabelFormatter = value => value.ToString("C0", new CultureInfo("id-ID"))
                    });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error memuat chart keuntungan: " + ex.Message);
            }
        }

        private void LoadStokKurang()
        {
            var list = new List<ProdukStok>();

            try
            {
                using (MySqlConnection conn = Helpers.DatabaseHelper.GetConnection())
                {
                    
                    string query = @"
                        SELECT barcode, nama_produk, stok 
                        FROM products 
                        WHERE stok < 10 
                        ORDER BY stok ASC 
                        LIMIT 10";

                    MySqlCommand cmd = new MySqlCommand(query, conn);
                    var reader = cmd.ExecuteReader();

                    while (reader.Read())
                    {
                        list.Add(new ProdukStok
                        {
                            Barcode = reader.GetString("barcode"),
                            NamaProduk = reader.GetString("nama_produk"),
                            Stok = reader.GetInt32("stok")
                        });
                    }

                    dgStokKurang.ItemsSource = list;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error memuat produk stok rendah: " + ex.Message);
            }
        }

        // Method untuk refresh data
        public void RefreshDashboard()
        {
            LoadDashboardCards();
            LoadPenjualanChart();
            LoadKeuntunganChart();
            LoadStokKurang();
        }

        // Event handler untuk refresh data button
        private void RefreshData_Click(object sender, RoutedEventArgs e)
        {
            RefreshDashboard();
            MessageBox.Show("Data berhasil diperbarui!", "Refresh Data",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        // Event handler untuk logout button
        private void Logout_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult result = MessageBox.Show("Apakah Anda yakin ingin logout?",
                "Konfirmasi Logout", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                var login = new LoginWindow();
                login.Show();
                this.Close();
            }
        }

        // Navigation event handlers
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

        // Class untuk binding data grid
        public class ProdukStok
        {
            public string Barcode { get; set; }
            public string NamaProduk { get; set; }
            public int Stok { get; set; }
        }
    }
}