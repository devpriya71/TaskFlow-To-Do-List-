using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using TaskFlow.Data;
using TaskFlow.Models;

namespace TaskFlow.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private TaskRepository _repository;
        private UserRepository _userRepository;
        private ObservableCollection<TaskItem> _allTasks;
        
        public ObservableCollection<TaskItem> TodoTasks { get; set; }
        public ObservableCollection<TaskItem> InProgressTasks { get; set; }
        public ObservableCollection<TaskItem> DoneTasks { get; set; }

        public ObservableCollection<Models.User> Users { get; set; }
        public ObservableCollection<string> Categories { get; set; }
        
        private Models.User _selectedUser;
        public Models.User SelectedUser 
        { 
            get => _selectedUser; 
            set 
            { 
                _selectedUser = value; 
                OnPropertyChanged(); 
                FilterTasks(); 
            } 
        }

        private string _newUserName;
        public string NewUserName
        {
            get => _newUserName;
            set { _newUserName = value; OnPropertyChanged(); }
        }

        private string _selectedCategory;
        public string SelectedCategory
        {
            get => _selectedCategory;
            set
            {
                _selectedCategory = value;
                OnPropertyChanged();
                FilterTasks();
            }
        }

        private TaskItem _selectedTask;
        public TaskItem SelectedTask
        {
            get => _selectedTask;
            set
            {
                _selectedTask = value;
                OnPropertyChanged();
                if (_selectedTask != null)
                {
                    TaskToEdit = new TaskItem
                    {
                        Id = _selectedTask.Id,
                        Title = _selectedTask.Title,
                        Description = _selectedTask.Description,
                        DueDate = _selectedTask.DueDate,
                        Priority = _selectedTask.Priority,
                        Status = _selectedTask.Status,
                        UserId = _selectedTask.UserId,
                        Category = _selectedTask.Category
                    };
                }
            }
        }
        
        private TaskItem _taskToEdit;
        public TaskItem TaskToEdit
        {
            get => _taskToEdit;
            set { _taskToEdit = value; OnPropertyChanged(); }
        }

        private int _totalTasks;
        public int TotalTasks
        {
            get => _totalTasks;
            set { _totalTasks = value; OnPropertyChanged(); }
        }

        public RelayCommand FilterCommand { get; }
        public RelayCommand AddCommand { get; }
        public RelayCommand EditCommand { get; }
        public RelayCommand DeleteCommand { get; }
        public RelayCommand MarkCompleteCommand { get; }
        public RelayCommand ExportCommand { get; }
        public RelayCommand SortPriorityCommand { get; }
        public RelayCommand SortDueDateCommand { get; }
        public RelayCommand AddUserCommand { get; }

        public MainViewModel()
        {
            TodoTasks = new ObservableCollection<TaskItem>();
            InProgressTasks = new ObservableCollection<TaskItem>();
            DoneTasks = new ObservableCollection<TaskItem>();

            _repository = new TaskRepository();
            _userRepository = new UserRepository();
            Users = new ObservableCollection<Models.User>();
            foreach (var u in _userRepository.GetAll())
                Users.Add(u);
            Categories = new ObservableCollection<string> { "All", "Development", "Study", "Design", "Testing", "Documentation", "Management" };
            _selectedUser = Users.Count > 0 ? Users[0] : null;
            _selectedCategory = Categories[0];
            _taskToEdit = new TaskItem { DueDate = DateTime.Now, Status = Models.TaskStatus.Todo, Priority = PriorityLevel.Medium };

            FilterCommand = new RelayCommand(obj => FilterTasks());
            AddCommand = new RelayCommand(ExecuteAdd, CanExecuteAddEdit);
            EditCommand = new RelayCommand(ExecuteEdit, CanExecuteEdit);
            DeleteCommand = new RelayCommand(ExecuteDelete, CanExecuteDelete);
            MarkCompleteCommand = new RelayCommand(ExecuteMarkComplete, CanExecuteDelete);
            ExportCommand = new RelayCommand(ExecuteExport);
            SortPriorityCommand = new RelayCommand(ExecuteSortPriority);
            SortDueDateCommand = new RelayCommand(ExecuteSortDueDate);
            AddUserCommand = new RelayCommand(ExecuteAddUser, CanExecuteAddUser);
        }

        public async Task LoadTasksAsync()
        {
            try
            {
                var tasks = await _repository.GetAllTasksAsync();
                _allTasks = new ObservableCollection<TaskItem>(tasks);
                FilterTasks();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading tasks: " + ex.Message);
            }
        }

        public void FilterTasks()
        {
            if (_allTasks == null) return;

            // Only show tasks for the selected user (no "All Users" – each user sees only their own tasks)
            Func<TaskItem, bool> filterPredicate = t =>
                (SelectedUser != null && t.UserId == SelectedUser.Id) &&
                (string.IsNullOrEmpty(SelectedCategory) || SelectedCategory == "All" || t.Category == SelectedCategory);

            var filtered = _allTasks.Where(filterPredicate).ToList();

            TodoTasks.Clear();
            InProgressTasks.Clear();
            DoneTasks.Clear();

            foreach(var t in filtered)
            {
                if (t.Status == Models.TaskStatus.Todo) TodoTasks.Add(t);
                else if (t.Status == Models.TaskStatus.InProgress) InProgressTasks.Add(t);
                else if (t.Status == Models.TaskStatus.Done) DoneTasks.Add(t);
            }

            TotalTasks = filtered.Count;
        }

        private bool CanExecuteAddEdit(object obj)
        {
            return TaskToEdit != null && !string.IsNullOrWhiteSpace(TaskToEdit.Title);
        }
        
        private bool CanExecuteEdit(object obj)
        {
            return SelectedTask != null && TaskToEdit != null && !string.IsNullOrWhiteSpace(TaskToEdit.Title);
        }

        private bool CanExecuteDelete(object obj) => SelectedTask != null;

        private void ExecuteAdd(object obj)
        {
            try
            {
                if (SelectedUser == null)
                {
                    MessageBox.Show("Please select a user (or add one) before adding a task.");
                    return;
                }
                TaskToEdit.UserId = SelectedUser.Id;
                if(string.IsNullOrEmpty(TaskToEdit.Category)) TaskToEdit.Category = "General";
                
                _repository.Insert(TaskToEdit);
                _allTasks.Add(TaskToEdit);
                FilterTasks();
                TaskToEdit = new TaskItem { DueDate = DateTime.Now, Status = Models.TaskStatus.Todo, Priority = PriorityLevel.Medium };
            }
            catch (Exception ex)
            {
                MessageBox.Show("Validation/Error adding task: " + ex.Message);
            }
        }

        private void ExecuteEdit(object obj)
        {
            try
            {
                _repository.Update(TaskToEdit);
                var taskToUpdate = _allTasks.FirstOrDefault(t => t.Id == TaskToEdit.Id);
                if (taskToUpdate != null)
                {
                    taskToUpdate.Title = TaskToEdit.Title;
                    taskToUpdate.Description = TaskToEdit.Description;
                    taskToUpdate.DueDate = TaskToEdit.DueDate;
                    taskToUpdate.Priority = TaskToEdit.Priority;
                    taskToUpdate.Category = TaskToEdit.Category;
                    taskToUpdate.Status = TaskToEdit.Status;
                }
                FilterTasks();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error editing task: " + ex.Message);
            }
        }

        private void ExecuteDelete(object obj)
        {
            try
            {
                _repository.Delete(SelectedTask.Id);
                _allTasks.Remove(_allTasks.FirstOrDefault(t => t.Id == SelectedTask.Id));
                FilterTasks();
                SelectedTask = null;
                AssignNewTaskForm();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error deleting task: " + ex.Message);
            }
        }

        private void ExecuteMarkComplete(object obj)
        {
            try
            {
                if (SelectedTask == null) return;
                SelectedTask.Status = Models.TaskStatus.Done;
                _repository.Update(SelectedTask);
                FilterTasks();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error updating task: " + ex.Message);
            }
        }

        private void ExecuteExport(object obj)
        {
            try
            {
                var lines = _allTasks.Select(t => $"{t.Id},{t.Title},{t.Status},{t.Priority},{t.DueDate:yyyy-MM-dd}");
                File.WriteAllLines("TasksExport.csv", new[] { "Id,Title,Status,Priority,DueDate" }.Concat(lines));
                MessageBox.Show("Tasks exported successfully to TasksExport.csv!");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Export failed: " + ex.Message);
            }
        }

        private void ExecuteSortPriority(object obj)
        {
            Action sort = () =>
            {
                var sorted = _allTasks.OrderByDescending(t => t.Priority).ToList();
                _allTasks = new ObservableCollection<TaskItem>(sorted);
                FilterTasks();
            };
            sort();
        }

        private void ExecuteSortDueDate(object obj)
        {
            Action sort = () =>
            {
                var sorted = _allTasks.OrderBy(t => t.DueDate).ToList();
                _allTasks = new ObservableCollection<TaskItem>(sorted);
                FilterTasks();
            };
            sort();
        }

        private bool CanExecuteAddUser(object obj)
        {
            return !string.IsNullOrWhiteSpace(NewUserName);
        }

        private void ExecuteAddUser(object obj)
        {
            try
            {
                var user = new Models.User { Name = NewUserName.Trim() };
                _userRepository.Insert(user);
                Users.Add(user);
                NewUserName = string.Empty;
                // Switch to the new user so they can start adding tasks
                SelectedUser = user;
                MessageBox.Show($"User '{user.Name}' added successfully.");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error adding user: " + ex.Message);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public void AssignNewTaskForm()
        {
            TaskToEdit = new TaskItem { DueDate = DateTime.Now, Status = Models.TaskStatus.Todo, Priority = PriorityLevel.Medium };
            SelectedTask = null;
        }
    }
}
