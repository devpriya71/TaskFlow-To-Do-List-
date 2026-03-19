using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace TaskFlow.Models
{
    public enum PriorityLevel { Low, Medium, High }
    public enum TaskStatus { Todo, InProgress, Done }

    public class TaskItem : INotifyPropertyChanged
    {
        private int _id;
        private string _title;
        private string _description;
        private DateTime _dueDate;
        private PriorityLevel _priority;
        private TaskStatus _status;
        private string _category;
        private int _userId;

        public int Id { get => _id; set { _id = value; OnPropertyChanged(); } }
        public string Title 
        { 
            get => _title; 
            set 
            { 
                if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Title cannot be empty");
                _title = value; 
                OnPropertyChanged(); 
            } 
        }
        public string Description { get => _description; set { _description = value; OnPropertyChanged(); } }
        public DateTime DueDate { get => _dueDate; set { _dueDate = value; OnPropertyChanged(); } }
        public PriorityLevel Priority { get => _priority; set { _priority = value; OnPropertyChanged(); } }
        public TaskStatus Status { get => _status; set { _status = value; OnPropertyChanged(); } }
        public string Category { get => _category; set { _category = value; OnPropertyChanged(); } }
        public int UserId { get => _userId; set { _userId = value; OnPropertyChanged(); } }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
