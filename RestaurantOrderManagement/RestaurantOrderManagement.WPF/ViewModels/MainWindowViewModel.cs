using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RestaurantOrderManagement.Services;
using RestaurantOrderManagement.WPF.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace RestaurantOrderManagement.ViewModels
{
    public partial class MainWindowViewModel : ObservableObject
    {
        [ObservableProperty]
        private ObservableCollection<NavigationItemViewModel> navigationItems = new();

        [ObservableProperty]
        private NavigationItemViewModel? selectedNavigationItem;

        [ObservableProperty]
        private string loginEmail = string.Empty;

        [ObservableProperty]
        private string loginPassword = string.Empty;

        [ObservableProperty]
        private string loginErrorMessage = string.Empty;

        [ObservableProperty]
        private string loginStatusMessage = string.Empty;

        [ObservableProperty]
        private bool isLoggingIn;

        [ObservableProperty]
        private bool isAuthenticated;

        [ObservableProperty]
        private bool isClient;

        [ObservableProperty]
        private bool isEmployee;

        [ObservableProperty]
        private string currentRole = "Guest";

        [ObservableProperty]
        private string currentUserDisplay = "Guest";

        [ObservableProperty]
        private int? currentUserId;

        private readonly IAuthenticationService _authenticationService;
        private readonly MenuBrowseViewModel _menuBrowseViewModel;
        private readonly SearchViewModel _searchViewModel;
        private readonly RegistrationViewModel _registrationViewModel;
        private readonly OrderCartViewModel _orderCartViewModel;
        private readonly OrderHistoryViewModel _orderHistoryViewModel;
        private readonly OrderManagementViewModel _orderManagementViewModel;
        private readonly ProductManagementViewModel _productManagementViewModel;
        private readonly InventoryViewModel _inventoryViewModel;
        private readonly ReportsViewModel _reportsViewModel;
        private readonly List<NavigationItemViewModel> _allNavigationItems;

        public MainWindowViewModel(
            IAuthenticationService authenticationService,
            MenuBrowseViewModel menuBrowseViewModel,
            SearchViewModel searchViewModel,
            RegistrationViewModel registrationViewModel,
            OrderCartViewModel orderCartViewModel,
            OrderHistoryViewModel orderHistoryViewModel,
            OrderManagementViewModel orderManagementViewModel,
            ProductManagementViewModel productManagementViewModel,
            InventoryViewModel inventoryViewModel,
            ReportsViewModel reportsViewModel)
        {
            _authenticationService = authenticationService;
            _menuBrowseViewModel = menuBrowseViewModel;
            _searchViewModel = searchViewModel;
            _registrationViewModel = registrationViewModel;
            _orderCartViewModel = orderCartViewModel;
            _orderHistoryViewModel = orderHistoryViewModel;
            _orderManagementViewModel = orderManagementViewModel;
            _productManagementViewModel = productManagementViewModel;
            _inventoryViewModel = inventoryViewModel;
            _reportsViewModel = reportsViewModel;
            _productManagementViewModel.CatalogChanged += OnCatalogChanged;

            _allNavigationItems = new List<NavigationItemViewModel>
            {
                new("Restaurant Menu", "Browse food, portions, prices.", NavigationAccess.Public, _menuBrowseViewModel, _menuBrowseViewModel.RefreshAsync),
                new("Search Menu", "Find dishes and allergens.", NavigationAccess.Public, _searchViewModel),
                new("Create Account", "Register as a client.", NavigationAccess.AnonymousOnly, _registrationViewModel),
                new("Place Order", "Checkout your cart.", NavigationAccess.Client, _orderCartViewModel),
                new("My Orders", "Track and cancel orders.", NavigationAccess.Client, _orderHistoryViewModel, _orderHistoryViewModel.LoadUserOrdersAsync),
                new("Manage Orders", "Update active orders.", NavigationAccess.Employee, _orderManagementViewModel, _orderManagementViewModel.LoadAllOrdersAsync),
                new("Products", "Manage catalog records.", NavigationAccess.Employee, _productManagementViewModel, _productManagementViewModel.LoadProductsAndCategoriesAsync),
                new("Inventory", "Review low-stock items.", NavigationAccess.Employee, _inventoryViewModel, _inventoryViewModel.LoadLowStockProductsAsync),
                new("Reports", "Revenue and operations.", NavigationAccess.Employee, _reportsViewModel, _reportsViewModel.LoadReportsAsync)
            };

            _menuBrowseViewModel.ConfigureCart(_orderCartViewModel, false);
            RebuildNavigation();
        }

        public bool CanShowLogin => !IsAuthenticated && !IsLoggingIn;

        private bool CanLogin()
        {
            return !IsLoggingIn;
        }

        [RelayCommand(CanExecute = nameof(CanLogin))]
        private async Task LoginAsync()
        {
            try
            {
                IsLoggingIn = true;
                LoginErrorMessage = string.Empty;
                LoginStatusMessage = string.Empty;

                if (IsGuestLogin(LoginEmail))
                {
                    ContinueAsGuest();
                    return;
                }

                if (string.IsNullOrWhiteSpace(LoginEmail) || string.IsNullOrWhiteSpace(LoginPassword))
                {
                    LoginErrorMessage = "Enter email and password, or choose Guest.";
                    return;
                }

                var login = await _authenticationService.LoginAsync(LoginEmail.Trim(), LoginPassword);
                if (!login.Success || !login.UserId.HasValue)
                {
                    LoginErrorMessage = string.IsNullOrWhiteSpace(login.Message)
                        ? "Sign in failed."
                        : ViewModelErrorMessages.FromServiceMessage("Sign in failed", login.Message);
                    return;
                }

                CurrentUserId = login.UserId.Value;
                CurrentRole = string.IsNullOrWhiteSpace(login.Role) ? "Client" : login.Role;
                CurrentUserDisplay = LoginEmail.Trim();
                IsAuthenticated = true;
                IsClient = string.Equals(CurrentRole, "Client", StringComparison.OrdinalIgnoreCase);
                IsEmployee = string.Equals(CurrentRole, "Employee", StringComparison.OrdinalIgnoreCase) ||
                             string.Equals(CurrentRole, "Admin", StringComparison.OrdinalIgnoreCase);
                LoginPassword = string.Empty;
                LoginStatusMessage = $"Signed in as {CurrentRole}.";

                InitializeAuthenticatedSections();
                RebuildNavigation();
            }
            catch (Exception ex)
            {
                LoginErrorMessage = ViewModelErrorMessages.FromException("Sign in failed", ex);
            }
            finally
            {
                IsLoggingIn = false;
            }
        }

        [RelayCommand]
        private void ContinueAsGuest()
        {
            CurrentUserId = null;
            CurrentRole = "Guest";
            CurrentUserDisplay = "Guest";
            IsAuthenticated = false;
            IsClient = false;
            IsEmployee = false;
            LoginEmail = string.Empty;
            LoginPassword = string.Empty;
            LoginErrorMessage = string.Empty;
            LoginStatusMessage = "Browsing as guest.";
            _menuBrowseViewModel.ConfigureCart(_orderCartViewModel, false);

            RebuildNavigation();
        }

        [RelayCommand]
        private void Logout()
        {
            CurrentUserId = null;
            CurrentRole = "Guest";
            CurrentUserDisplay = "Guest";
            IsAuthenticated = false;
            IsClient = false;
            IsEmployee = false;
            LoginPassword = string.Empty;
            LoginStatusMessage = "Signed out.";
            LoginErrorMessage = string.Empty;
            _menuBrowseViewModel.ConfigureCart(_orderCartViewModel, false);

            RebuildNavigation();
        }

        partial void OnSelectedNavigationItemChanged(NavigationItemViewModel? value)
        {
            if (value != null)
            {
                _ = ActivateNavigationItemAsync(value);
            }
        }

        partial void OnLoginEmailChanged(string value)
        {
            LoginCommand.NotifyCanExecuteChanged();
        }

        partial void OnLoginPasswordChanged(string value)
        {
            LoginCommand.NotifyCanExecuteChanged();
        }

        partial void OnIsLoggingInChanged(bool value)
        {
            OnPropertyChanged(nameof(CanShowLogin));
            LoginCommand.NotifyCanExecuteChanged();
        }

        partial void OnIsAuthenticatedChanged(bool value)
        {
            OnPropertyChanged(nameof(CanShowLogin));
        }

        private void RebuildNavigation()
        {
            var previousTitle = SelectedNavigationItem?.Title;
            NavigationItems.Clear();

            foreach (var item in _allNavigationItems.Where(CanAccess))
            {
                NavigationItems.Add(item);
            }

            SelectedNavigationItem = NavigationItems.FirstOrDefault(item => item.Title == previousTitle)
                                     ?? NavigationItems.FirstOrDefault();
        }

        private bool CanAccess(NavigationItemViewModel item)
        {
            return item.Access switch
            {
                NavigationAccess.Public => true,
                NavigationAccess.AnonymousOnly => !IsAuthenticated,
                NavigationAccess.Client => IsClient,
                NavigationAccess.Employee => IsEmployee,
                _ => false
            };
        }

        private void InitializeAuthenticatedSections()
        {
            if (CurrentUserId is not int userId)
                return;

            if (IsClient)
            {
                _orderCartViewModel.InitializeCart(userId, string.Empty);
                _menuBrowseViewModel.ConfigureCart(_orderCartViewModel, true);
                _orderHistoryViewModel.Initialize(userId);
            }
            else
            {
                _menuBrowseViewModel.ConfigureCart(_orderCartViewModel, false);
            }
        }

        private async Task ActivateNavigationItemAsync(NavigationItemViewModel item)
        {
            if (item.ActivateAsync == null)
                return;

            try
            {
                await item.ActivateAsync();
            }
            catch (Exception ex)
            {
                LoginErrorMessage = ViewModelErrorMessages.FromException($"Could not load {item.Title}", ex);
            }
        }

        private async void OnCatalogChanged(object? sender, EventArgs e)
        {
            try
            {
                await _menuBrowseViewModel.RefreshAsync();
            }
            catch (Exception ex)
            {
                LoginErrorMessage = ViewModelErrorMessages.FromException("Could not refresh restaurant menu", ex);
            }
        }

        private static bool IsGuestLogin(string email)
        {
            var normalized = email.Trim();
            return string.Equals(normalized, "guest", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(normalized, "guest@local", StringComparison.OrdinalIgnoreCase);
        }
    }

    public sealed class NavigationItemViewModel
    {
        public NavigationItemViewModel(
            string title,
            string description,
            NavigationAccess access,
            object viewModel,
            Func<Task>? activateAsync = null)
        {
            Title = title;
            Description = description;
            Access = access;
            ViewModel = viewModel;
            ActivateAsync = activateAsync;
        }

        public string Title { get; }
        public string Description { get; }
        public NavigationAccess Access { get; }
        public object ViewModel { get; }
        public Func<Task>? ActivateAsync { get; }
    }

    public enum NavigationAccess
    {
        Public,
        AnonymousOnly,
        Client,
        Employee
    }
}
