using System.Windows;
using System.Windows.Controls;
using RestaurantOrderManagement.ViewModels;

namespace RestaurantOrderManagement
{
    public partial class MainWindow : Window
    {
        public MainWindow(MainWindowViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;

            viewModel.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName == nameof(MainWindowViewModel.LoginPassword) &&
                    string.IsNullOrEmpty(viewModel.LoginPassword) &&
                    !string.IsNullOrEmpty(LoginPasswordBox.Password))
                {
                    LoginPasswordBox.Clear();
                }
            };
        }

        private void OnPasswordChanged(object sender, RoutedEventArgs e)
        {
            if (DataContext is MainWindowViewModel viewModel && sender is PasswordBox passwordBox)
            {
                viewModel.LoginPassword = passwordBox.Password;
            }
        }
    }
}
