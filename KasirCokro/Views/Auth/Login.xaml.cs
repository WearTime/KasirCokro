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
using BCrypt.Net;

namespace KasirCokro.Views.Auth
{
    public partial class LoginWindow1 : Window
    {
        public LoginWindow1()
        {
            InitializeComponent();
        }

        private void TxtUsername_GotFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(txtUsername.Text))
            {
                UsernamePlaceholder.Visibility = Visibility.Collapsed;
            }
        }

        private void TxtUsername_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(txtUsername.Text))
            {
                UsernamePlaceholder.Visibility = Visibility.Visible;
            }
        }

        private void TxtUsername_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!string.IsNullOrEmpty(txtUsername.Text))
            {
                UsernamePlaceholder.Visibility = Visibility.Collapsed;
            }
            else
            {
                UsernamePlaceholder.Visibility = Visibility.Visible;
            }
        }

        private void TxtPassword_GotFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(txtPassword.Password))
            {
                PasswordPlaceholder.Visibility = Visibility.Collapsed;
            }
        }

        private void TxtPassword_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(txtPassword.Password))
            {
                PasswordPlaceholder.Visibility = Visibility.Visible;
            }
        }

        private void TxtPassword_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(txtPassword.Password))
            {
                PasswordPlaceholder.Visibility = Visibility.Collapsed;
            }
            else
            {
                PasswordPlaceholder.Visibility = Visibility.Visible;
            }
        }

        private async void LogInButton_Click(object sender, RoutedEventArgs e)
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
                    conn.Close();
                    await conn.OpenAsync();

                    string query = "SELECT id, username, password, role FROM users WHERE username = @username";
                    MySqlCommand cmd = new MySqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@username", username);

                    using (MySqlDataReader reader = (MySqlDataReader)cmd.ExecuteReader())
                    {
                        if (await reader.ReadAsync())
                        {
                            string hashedPassword = reader["password"].ToString();

                            if (Helpers.PasswordHelper.VerifyPassword(password, hashedPassword))
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