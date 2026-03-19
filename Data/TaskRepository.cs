using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using TaskFlow.Models;
using TaskStatus = TaskFlow.Models.TaskStatus;

namespace TaskFlow.Data
{
    public class TaskRepository
    {
        private readonly string connectionString = "Data Source=TaskFlow.db;Version=3;";

        public TaskRepository()
        {
            InitializeDatabase();
        }

        private void InitializeDatabase()
        {
            bool createData = !File.Exists("TaskFlow.db");
            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();
                string createTableQuery = @"
                    CREATE TABLE IF NOT EXISTS Tasks (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        Title TEXT NOT NULL,
                        Description TEXT,
                        DueDate DATETIME,
                        Priority INTEGER,
                        Status INTEGER,
                        Category TEXT,
                        UserId INTEGER
                    )";
                using (var command = new SQLiteCommand(createTableQuery, connection))
                {
                    command.ExecuteNonQuery();
                }
            }

            if (createData)
            {
                InsertSampleData();
            }
        }

        private void InsertSampleData()
        {
            var tasks = new List<TaskItem>
            {
                // User 1 (Sonali) - UserId 1: 4 tasks (2 Todo, 1 InProgress, 1 Done)
                new TaskItem { Title = "Complete WPF project", Description = "Implement complete requirements", DueDate = new DateTime(2026, 03, 20), Priority = PriorityLevel.High, Status = TaskStatus.Todo, Category = "Development", UserId = 1 },
                new TaskItem { Title = "Review MVVM", Description = "Study Data Binding", DueDate = new DateTime(2026, 03, 16), Priority = PriorityLevel.Medium, Status = TaskStatus.Todo, Category = "Study", UserId = 1 },
                new TaskItem { Title = "Setup Database", Description = "SQLite Database config", DueDate = new DateTime(2026, 03, 17), Priority = PriorityLevel.Low, Status = TaskStatus.InProgress, Category = "Development", UserId = 1 },
                new TaskItem { Title = "UI Dashboard", Description = "Design MainWindow", DueDate = new DateTime(2026, 03, 15), Priority = PriorityLevel.High, Status = TaskStatus.Done, Category = "Design", UserId = 1 },

                // User 2 (Amit) - UserId 2: 4 tasks (3 Todo, 1 InProgress)
                new TaskItem { Title = "Create ADO.NET Logic", Description = "DataTable and DataAdapter", DueDate = new DateTime(2026, 03, 18), Priority = PriorityLevel.Medium, Status = TaskStatus.Todo, Category = "Development", UserId = 2 },
                new TaskItem { Title = "Learn LINQ", Description = "Advanced C# queries", DueDate = new DateTime(2026, 03, 19), Priority = PriorityLevel.Low, Status = TaskStatus.Todo, Category = "Study", UserId = 2 },
                new TaskItem { Title = "Build Models", Description = "Class and ENUMS", DueDate = new DateTime(2026, 03, 16), Priority = PriorityLevel.Medium, Status = TaskStatus.Todo, Category = "Development", UserId = 2 },
                new TaskItem { Title = "Debugging", Description = "Fix issues on startup", DueDate = new DateTime(2026, 03, 17), Priority = PriorityLevel.High, Status = TaskStatus.InProgress, Category = "Testing", UserId = 2 },

                // User 3 (Priya) - UserId 3: 4 tasks (1 Todo, 2 InProgress, 1 Done)
                new TaskItem { Title = "Write README", Description = "Project documentation", DueDate = new DateTime(2026, 03, 22), Priority = PriorityLevel.Low, Status = TaskStatus.Todo, Category = "Documentation", UserId = 3 },
                new TaskItem { Title = "Styling and Animations", Description = "WPF animations", DueDate = new DateTime(2026, 03, 21), Priority = PriorityLevel.High, Status = TaskStatus.InProgress, Category = "Design", UserId = 3 },
                new TaskItem { Title = "Performance Testing", Description = "Check async/await", DueDate = new DateTime(2026, 03, 20), Priority = PriorityLevel.Medium, Status = TaskStatus.InProgress, Category = "Testing", UserId = 3 },
                new TaskItem { Title = "Submit Project", Description = "Final evaluation", DueDate = new DateTime(2026, 03, 25), Priority = PriorityLevel.High, Status = TaskStatus.Done, Category = "Management", UserId = 3 }
            };

            foreach (var task in tasks)
            {
                Insert(task);
            }
        }

        public Task<DataTable> SelectAllAsync()
        {
            return Task.Run(() =>
            {
                var dataTable = new DataTable();
                using (var connection = new SQLiteConnection(connectionString))
                {
                    string query = "SELECT * FROM Tasks";
                    using (var adapter = new SQLiteDataAdapter(query, connection))
                    {
                        adapter.Fill(dataTable);
                    }
                }
                return dataTable;
            });
        }

        public async Task<List<TaskItem>> GetAllTasksAsync()
        {
            DataTable table = await SelectAllAsync();

            var tasks = (from DataRow row in table.Rows
                         select new TaskItem
                         {
                             Id = Convert.ToInt32(row["Id"]),
                             Title = row["Title"].ToString(),
                             Description = row["Description"].ToString(),
                             DueDate = Convert.ToDateTime(row["DueDate"]),
                             Priority = (PriorityLevel)Convert.ToInt32(row["Priority"]),
                             Status = (TaskStatus)Convert.ToInt32(row["Status"]),
                             Category = row["Category"].ToString(),
                             UserId = Convert.ToInt32(row["UserId"])
                         }).ToList();

            return tasks;
        }

        public TaskItem SelectById(int id)
        {
            var dataTable = new DataTable();
            using (var connection = new SQLiteConnection(connectionString))
            {
                string query = "SELECT * FROM Tasks WHERE Id = @Id";
                using (var command = new SQLiteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Id", id);
                    using (var adapter = new SQLiteDataAdapter(command))
                    {
                        adapter.Fill(dataTable);
                    }
                }
            }
            if (dataTable.Rows.Count == 0) return null;
            DataRow row = dataTable.Rows[0];
            return new TaskItem
            {
                Id = Convert.ToInt32(row["Id"]),
                Title = row["Title"].ToString(),
                Description = row["Description"].ToString(),
                DueDate = Convert.ToDateTime(row["DueDate"]),
                Priority = (PriorityLevel)Convert.ToInt32(row["Priority"]),
                Status = (TaskStatus)Convert.ToInt32(row["Status"]),
                Category = row["Category"].ToString(),
                UserId = Convert.ToInt32(row["UserId"])
            };
        }

        public void Insert(TaskItem task)
        {
            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();
                string query = @"INSERT INTO Tasks (Title, Description, DueDate, Priority, Status, Category, UserId) 
                                 VALUES (@Title, @Description, @DueDate, @Priority, @Status, @Category, @UserId); 
                                 SELECT last_insert_rowid();";
                using (var command = new SQLiteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Title", task.Title);
                    command.Parameters.AddWithValue("@Description", task.Description ?? "");
                    command.Parameters.AddWithValue("@DueDate", task.DueDate);
                    command.Parameters.AddWithValue("@Priority", (int)task.Priority);
                    command.Parameters.AddWithValue("@Status", (int)task.Status);
                    command.Parameters.AddWithValue("@Category", task.Category ?? "");
                    command.Parameters.AddWithValue("@UserId", task.UserId);
                    
                    var newId = command.ExecuteScalar();
                    task.Id = Convert.ToInt32(newId);
                }
            }
        }

        public void Update(TaskItem task)
        {
            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();
                string query = @"UPDATE Tasks SET Title=@Title, Description=@Description, DueDate=@DueDate, 
                                 Priority=@Priority, Status=@Status, Category=@Category, UserId=@UserId 
                                 WHERE Id=@Id";
                using (var command = new SQLiteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Title", task.Title);
                    command.Parameters.AddWithValue("@Description", task.Description ?? "");
                    command.Parameters.AddWithValue("@DueDate", task.DueDate);
                    command.Parameters.AddWithValue("@Priority", (int)task.Priority);
                    command.Parameters.AddWithValue("@Status", (int)task.Status);
                    command.Parameters.AddWithValue("@Category", task.Category ?? "");
                    command.Parameters.AddWithValue("@UserId", task.UserId);
                    command.Parameters.AddWithValue("@Id", task.Id);
                    
                    command.ExecuteNonQuery();
                }
            }
        }

        public void Delete(int id)
        {
            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();
                string query = "DELETE FROM Tasks WHERE Id=@Id";
                using (var command = new SQLiteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Id", id);
                    command.ExecuteNonQuery();
                }
            }
        }
    }
}
