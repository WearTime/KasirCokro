using BCrypt.Net;
using KasirCokro.Helpers;
using MySql.Data.MySqlClient;
using Org.BouncyCastle.Crypto.Generators;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace KasirCokro.Helpers
{
    public static class PasswordHelper
    {
        public static string HashPassword(string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password, 12);
        }

        public static bool VerifyPassword(string password, string hashedPassword)
        {
            return BCrypt.Net.BCrypt.Verify(password, hashedPassword);
        }
    }

    public static class DatabasePasswordUpdater
    {
        public static async Task UpdateExistingPasswordsToHash()
        {
            try
            {
                using (MySqlConnection conn = DatabaseHelper.GetConnection())
                {
                    conn.Close();
                    await conn.OpenAsync();

                    string selectQuery = "SELECT id, username, password FROM users";
                    MySqlCommand selectCmd = new MySqlCommand(selectQuery, conn);

                    using (MySqlDataReader reader = (MySqlDataReader)selectCmd.ExecuteReader())
                    {
                        var usersToUpdate = new List<(int id, string username, string plainPassword)>();

                        while (await reader.ReadAsync())
                        {
                            int id = Convert.ToInt32(reader["id"]);
                            string username = reader["username"].ToString();
                            string plainPassword = reader["password"].ToString();

                            usersToUpdate.Add((id, username, plainPassword));
                        }

                        reader.Close();

                        foreach (var user in usersToUpdate)
                        {
                            string hashedPassword = PasswordHelper.HashPassword(user.plainPassword);

                            string updateQuery = "UPDATE users SET password = @hashedPassword WHERE id = @id";
                            MySqlCommand updateCmd = new MySqlCommand(updateQuery, conn);
                            updateCmd.Parameters.AddWithValue("@hashedPassword", hashedPassword);
                            updateCmd.Parameters.AddWithValue("@id", user.id);

                            await updateCmd.ExecuteNonQueryAsync();

                            Console.WriteLine($"Password untuk user '{user.username}' berhasil di-hash");
                        }
                    }
                }

                Console.WriteLine("Semua password berhasil diupdate ke format hash!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saat update password: {ex.Message}");
                throw;
            }
        }

        public static async Task<bool> CreateNewUser(string username, string password, string role)
        {
            try
            {
                using (MySqlConnection conn = DatabaseHelper.GetConnection())
                {
                    conn.Close();
                    await conn.OpenAsync();

                    string checkQuery = "SELECT COUNT(*) FROM users WHERE username = @username";
                    MySqlCommand checkCmd = new MySqlCommand(checkQuery, conn);
                    checkCmd.Parameters.AddWithValue("@username", username);

                    int userCount = Convert.ToInt32(await checkCmd.ExecuteScalarAsync());

                    if (userCount > 0)
                    {
                        Console.WriteLine("Username sudah ada!");
                        return false;
                    }

                    string hashedPassword = PasswordHelper.HashPassword(password);

                    string insertQuery = "INSERT INTO users (username, password, role) VALUES (@username, @password, @role)";
                    MySqlCommand insertCmd = new MySqlCommand(insertQuery, conn);
                    insertCmd.Parameters.AddWithValue("@username", username);
                    insertCmd.Parameters.AddWithValue("@password", hashedPassword);
                    insertCmd.Parameters.AddWithValue("@role", role);

                    int result = await insertCmd.ExecuteNonQueryAsync();

                    if (result > 0)
                    {
                        Console.WriteLine($"User '{username}' berhasil dibuat dengan role '{role}'");
                        return true;
                    }

                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saat membuat user: {ex.Message}");
                return false;
            }
        }
    }
}

public class Program
{
    public static async Task Main(string[] args)
    {
        try
        {
            await DatabasePasswordUpdater.UpdateExistingPasswordsToHash();


            Console.WriteLine("Selesai!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }
}