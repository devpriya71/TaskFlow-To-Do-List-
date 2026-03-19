using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using TaskFlow.Models;

namespace TaskFlow.Data
{
    public class UserRepository
    {
        private readonly string connectionString = "Data Source=TaskFlow.db;Version=3;";

        public UserRepository()
        {
            InitializeDatabase();
        }

        private void InitializeDatabase()
        {
            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();
                string createTableQuery = @"
                    CREATE TABLE IF NOT EXISTS Users (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        Name TEXT NOT NULL
                    )";
                using (var command = new SQLiteCommand(createTableQuery, connection))
                {
                    command.ExecuteNonQuery();
                }
            }

            // Seed default users if table is empty (for existing DBs or first run)
            var existing = GetAll();
            if (existing.Count == 0)
            {
                SeedDefaultUsers();
            }
        }

        private void SeedDefaultUsers()
        {
            var defaults = new[] { "Sonali", "Amit", "Priya" };
            foreach (var name in defaults)
            {
                Insert(new User { Name = name });
            }
        }

        public List<User> GetAll()
        {
            var list = new List<User>();
            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();
                string query = "SELECT Id, Name FROM Users ORDER BY Id";
                using (var command = new SQLiteCommand(query, connection))
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add(new User
                        {
                            Id = reader.GetInt32(0),
                            Name = reader.GetString(1)
                        });
                    }
                }
            }
            return list;
        }

        public void Insert(User user)
        {
            if (string.IsNullOrWhiteSpace(user?.Name))
                throw new ArgumentException("User name cannot be empty.");

            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();
                string query = "INSERT INTO Users (Name) VALUES (@Name); SELECT last_insert_rowid();";
                using (var command = new SQLiteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Name", user.Name.Trim());
                    var newId = command.ExecuteScalar();
                    user.Id = Convert.ToInt32(newId);
                }
            }
        }
    }
}
