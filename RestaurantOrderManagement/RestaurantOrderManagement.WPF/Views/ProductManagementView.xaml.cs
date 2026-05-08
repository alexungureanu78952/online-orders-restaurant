using System.Windows.Controls;
using RestaurantOrderManagement.WPF.ViewModels;

namespace RestaurantOrderManagement.WPF.Views
{
    /// <summary>
    /// Interaction logic for ProductManagementView.xaml
    /// </summary>
    public partial class ProductManagementView : UserControl
    {
        public ProductManagementView()
        {
            InitializeComponent();
        }

        private void CancelProductForm_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (DataContext is ProductManagementViewModel vm)
            {
                vm.ShowProductForm = false;
            }
        }

        private void CancelCategoryForm_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (DataContext is ProductManagementViewModel vm)
            {
                vm.ShowCategoryForm = false;
            }
        }
    }
}
