using CommunityToolkit.Mvvm.ComponentModel;

namespace RestaurantOrderManagement.ViewModels
{
    public partial class MainWindowViewModel : ObservableObject
    {
        [ObservableProperty]
        private string welcomeText = "Welcome to Restaurant Order Management";
    }
}
