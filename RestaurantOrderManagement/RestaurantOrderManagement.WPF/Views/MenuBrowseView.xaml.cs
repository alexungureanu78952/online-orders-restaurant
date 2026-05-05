using System.Windows.Controls;
using RestaurantOrderManagement.ViewModels;

namespace RestaurantOrderManagement.Views
{
    public partial class MenuBrowseView : UserControl
    {
        public MenuBrowseView()
        {
            InitializeComponent();
        }

        private void OnCategorySelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DataContext is MenuBrowseViewModel viewModel)
            {
                viewModel.LoadProductsByCategoryCommand.ExecuteAsync(null);
            }
        }
    }
}
