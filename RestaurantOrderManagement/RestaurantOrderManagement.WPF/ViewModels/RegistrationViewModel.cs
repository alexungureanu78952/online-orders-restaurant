using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RestaurantOrderManagement.Services;
using System;
using System.Threading.Tasks;

namespace RestaurantOrderManagement.ViewModels
{
    public partial class RegistrationViewModel : BaseViewModel
    {
        private readonly IAuthenticationService _authenticationService;

        [ObservableProperty]
        private string email = string.Empty;

        [ObservableProperty]
        private string password = string.Empty;

        [ObservableProperty]
        private string confirmPassword = string.Empty;

        [ObservableProperty]
        private string firstName = string.Empty;

        [ObservableProperty]
        private string lastName = string.Empty;

        [ObservableProperty]
        private string phoneNumber = string.Empty;

        [ObservableProperty]
        private string deliveryAddress = string.Empty;

        [ObservableProperty]
        private string errorMessage = string.Empty;

        [ObservableProperty]
        private string successMessage = string.Empty;

        [ObservableProperty]
        private bool isLoading = false;

        [ObservableProperty]
        private bool isRegistrationComplete = false;

        public RegistrationViewModel(IAuthenticationService authenticationService)
        {
            _authenticationService = authenticationService;
        }

        private bool ValidateEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        private bool ValidatePassword(string password)
        {
            return !string.IsNullOrWhiteSpace(password) && password.Length >= 6;
        }

        private (bool IsValid, string Message) ValidateAllFields()
        {
            if (!ValidateEmail(Email))
                return (false, "Please enter a valid email address");

            if (!ValidatePassword(Password))
                return (false, "Password must be at least 6 characters");

            if (Password != ConfirmPassword)
                return (false, "Passwords do not match");

            if (string.IsNullOrWhiteSpace(FirstName))
                return (false, "First name is required");

            if (string.IsNullOrWhiteSpace(LastName))
                return (false, "Last name is required");

            if (!string.IsNullOrWhiteSpace(PhoneNumber) && PhoneNumber.Length < 10)
                return (false, "Phone number must be at least 10 digits");

            return (true, string.Empty);
        }

        [RelayCommand]
        public async Task RegisterAsync()
        {
            ErrorMessage = string.Empty;
            SuccessMessage = string.Empty;

            var (isValid, validationError) = ValidateAllFields();
            if (!isValid)
            {
                ErrorMessage = validationError;
                return;
            }

            try
            {
                IsLoading = true;

                var (success, message, userId) = await _authenticationService.RegisterAsync(
                    Email,
                    Password,
                    FirstName,
                    LastName,
                    string.IsNullOrWhiteSpace(PhoneNumber) ? null : PhoneNumber,
                    string.IsNullOrWhiteSpace(DeliveryAddress) ? null : DeliveryAddress
                );

                if (success)
                {
                    SuccessMessage = $"Registration successful! Welcome, {FirstName}";
                    IsRegistrationComplete = true;

                    await Task.Delay(1500);
                    ClearForm();
                }
                else
                {
                    ErrorMessage = ViewModelErrorMessages.FromServiceMessage("Registration failed", message);
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = ViewModelErrorMessages.FromException("Registration failed", ex);
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        public void ClearForm()
        {
            Email = string.Empty;
            Password = string.Empty;
            ConfirmPassword = string.Empty;
            FirstName = string.Empty;
            LastName = string.Empty;
            PhoneNumber = string.Empty;
            DeliveryAddress = string.Empty;
            ErrorMessage = string.Empty;
            SuccessMessage = string.Empty;
            IsRegistrationComplete = false;
        }

        public bool IsFormValid =>
            !string.IsNullOrWhiteSpace(Email) &&
            !string.IsNullOrWhiteSpace(Password) &&
            !string.IsNullOrWhiteSpace(ConfirmPassword) &&
            !string.IsNullOrWhiteSpace(FirstName) &&
            !string.IsNullOrWhiteSpace(LastName) &&
            Password == ConfirmPassword &&
            !IsLoading;
    }
}
