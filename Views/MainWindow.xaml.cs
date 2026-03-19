using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using TaskFlow.Models;
using TaskFlow.ViewModels;

namespace TaskFlow.Views
{
    public partial class MainWindow : Window
    {
        private MainViewModel _viewModel;
        private DispatcherTimer _timer;

        public MainWindow()
        {
            InitializeComponent();
            _viewModel = new MainViewModel();
            DataContext = _viewModel;
            
            Loaded += MainWindow_Loaded;
            
            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromSeconds(10);
            _timer.Tick += Timer_Tick;
            _timer.Start();
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            await _viewModel.LoadTasksAsync();
            CheckOverdueTasks();
            
            // Initial animation
            AnimateListView(LvTodo);
            AnimateListView(LvInProgress);
            AnimateListView(LvDone);
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            CheckOverdueTasks();
        }

        private void CheckOverdueTasks()
        {
            if (_viewModel.TodoTasks == null || _viewModel.InProgressTasks == null) return;
            
            var overdueCount = _viewModel.TodoTasks.Count(t => t.DueDate < DateTime.Now) +
                               _viewModel.InProgressTasks.Count(t => t.DueDate < DateTime.Now);
                               
            if (overdueCount > 0)
            {
                TxtTimerAlert.Text = $"Alert: {overdueCount} task(s) overdue!";
            }
            else
            {
                TxtTimerAlert.Text = "";
            }
        }

        private void BtnNew_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.AssignNewTaskForm();
        }

        // --- Drag and Drop Logic ---
        private void ListViewItem_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is ListViewItem item && item.Content is TaskItem task)
            {
                var listView = FindVisualParent<ListView>(item);
                if (listView != null)
                {
                    DragDrop.DoDragDrop(item, new DataObject("TaskFormat", task), DragDropEffects.Move);
                }
            }
        }

        private void ListView_DragOver(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent("TaskFormat"))
            {
                e.Effects = DragDropEffects.None;
                e.Handled = true;
            }
        }

        private void ListView_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent("TaskFormat") && sender is ListView targetListView)
            {
                var task = e.Data.GetData("TaskFormat") as TaskItem;
                if (task == null) return;

                string targetTag = targetListView.Tag.ToString();
                Models.TaskStatus targetStatus = Models.TaskStatus.Todo;
                if (targetTag == "InProgress") targetStatus = Models.TaskStatus.InProgress;
                else if (targetTag == "Done") targetStatus = Models.TaskStatus.Done;

                if (task.Status != targetStatus)
                {
                    _viewModel.SelectedTask = task;
                    _viewModel.TaskToEdit = new TaskItem
                    {
                        Id = task.Id, Title = task.Title, Description = task.Description, 
                        DueDate = task.DueDate, Priority = task.Priority, Status = targetStatus, 
                        Category = task.Category, UserId = task.UserId
                    };
                    _viewModel.EditCommand.Execute(null);

                    AnimateListView(targetListView);
                }
            }
        }
        
        private void ListViewItem_Drop(object sender, DragEventArgs e)
        {
            // Bubbles up to ListView_Drop
        }
        
        private void AnimateListView(ListView lv)
        {
            Storyboard sb = new Storyboard();
            ThicknessAnimation slide = new ThicknessAnimation
            {
                From = new Thickness(0, 50, 0, 0),
                To = new Thickness(0),
                Duration = new Duration(TimeSpan.FromSeconds(0.4)),
                EasingFunction = new QuarticEase { EasingMode = EasingMode.EaseOut }
            };
            DoubleAnimation fade = new DoubleAnimation
            {
                From = 0.5,
                To = 1,
                Duration = new Duration(TimeSpan.FromSeconds(0.4))
            };
            Storyboard.SetTarget(slide, lv);
            Storyboard.SetTargetProperty(slide, new PropertyPath("Margin"));
            Storyboard.SetTarget(fade, lv);
            Storyboard.SetTargetProperty(fade, new PropertyPath("Opacity"));
            sb.Children.Add(slide);
            sb.Children.Add(fade);
            sb.Begin();
        }

        private static T FindVisualParent<T>(DependencyObject child) where T : DependencyObject
        {
            DependencyObject parentObject = VisualTreeHelper.GetParent(child);
            if (parentObject == null) return null;
            if (parentObject is T parent) return parent;
            return FindVisualParent<T>(parentObject);
        }
    }
}
