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

        private async void OnCategorySelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DataContext is MenuBrowseViewModel viewModel &&
                !viewModel.IsLoading &&
                e.AddedItems.Count > 0)
            {
                await viewModel.LoadMenuAsync();
            }
        }
    }
}
