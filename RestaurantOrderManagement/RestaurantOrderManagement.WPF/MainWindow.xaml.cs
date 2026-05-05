using System.Windows;
using RestaurantOrderManagement.ViewModels;

namespace RestaurantOrderManagement
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = new MainWindowViewModel();
        }
    }
}
