using System;
using System.Linq;

namespace RestaurantOrderManagement.ViewModels
{
    internal static class ViewModelErrorMessages
    {
        private static readonly string[] DatabaseMarkers =
        {
            "transient failure",
            "network-related",
            "instance-specific",
            "error occurred while establishing",
            "server was not found",
            "could not open a connection",
            "actively refused",
            "no such host is known",
            "cannot open database",
            "login failed for user",
            "timeout expired"
        };

        private static readonly string[] QueryMarkers =
        {
            "fromsqlraw",
            "fromsqlinterpolated",
            "non-composable sql"
        };

        public static string FromException(string action, Exception exception)
        {
            return Format(action, Flatten(exception));
        }

        public static string FromServiceMessage(string action, string? message)
        {
            if (string.IsNullOrWhiteSpace(message))
                return action;

            return Format(action, message);
        }

        private static string Format(string action, string details)
        {
            if (ContainsAny(details, QueryMarkers))
            {
                return $"{action}: a database query was wired incorrectly. Restart the app with the latest build and try again.";
            }

            if (ContainsAny(details, DatabaseMarkers))
            {
                return $"{action}: could not reach the restaurant database. Start SQL Server, wait until it is ready, and verify localhost:1433 uses the RestaurantOrderManagement database.";
            }

            return details.StartsWith(action, StringComparison.OrdinalIgnoreCase)
                ? details
                : $"{action}: {details}";
        }

        private static string Flatten(Exception exception)
        {
            var messages = Enumerable.Empty<string>();
            var current = exception;

            while (current != null)
            {
                if (!string.IsNullOrWhiteSpace(current.Message))
                    messages = messages.Append(current.Message);

                current = current.InnerException;
            }

            return string.Join(" ", messages);
        }

        private static bool ContainsAny(string text, string[] markers)
        {
            return markers.Any(marker => text.Contains(marker, StringComparison.OrdinalIgnoreCase));
        }
    }
}
