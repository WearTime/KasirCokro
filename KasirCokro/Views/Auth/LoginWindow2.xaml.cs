using KasirCokro.Helpers;
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

namespace KasirCokro.Views.Auth
{
    /// <summary>
    /// Interaction logic for Login.xaml
    /// </summary>
    public partial class LoginWindow : Window
    {
        public LoginWindow()
        {
            InitializeComponent();
        }


        private async void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            string username = txtUsername.Text.Trim();
            string password = txtPassword.Password.Trim();

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                MessageBox.Show("Username dan Password harus diisi!", "Peringatan", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                using (MySqlConnection conn = Helpers.DatabaseHelper.GetConnection())
                {
                    conn.Close(); // Pastikan koneksi ditutup sebelum dibuka lagi
                    await conn.OpenAsync(); // Gunakan async

                    string query = "SELECT id, username, role FROM users WHERE username = @username AND password = @password";
                    MySqlCommand cmd = new MySqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@username", username);
                    cmd.Parameters.AddWithValue("@password", password);

                    using (MySqlDataReader reader = (MySqlDataReader)cmd.ExecuteReader())
                    {
                        if (await reader.ReadAsync())
                        {
                            string role = reader["role"].ToString();
                            int userId = Convert.ToInt32(reader["id"]);

                            this.Hide();

                            if (role == "admin")
                            {
                                var adminDash = new Views.Admin.DashboardAdmin();
                                adminDash.Show();
                                
                            }
                            else if (role == "kasir")
                            {
                                var kasirDash = new Views.Kasir.DashboardKasir();
                                kasirDash.Show();
                            }
                        }
                        else
                        {
                            MessageBox.Show("Username atau Password salah!", "Login Gagal", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Terjadi kesalahan: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}

