using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO; // Required for file operations
using System.Linq;
using System.Text.Json; // Required for JSON deserialization
using System.Windows;
using System.Windows.Controls;

namespace BPLog
{
    // Define a class to hold the application settings
    public class AppSettings
    {
        public bool EnableReminders { get; set; }
        public int InitialInterval { get; set; }
        public List<string> ReminderTimes { get; set; } = new List<string>();
    }

    public partial class SettingsWindow : Window
    {
        private const string SettingsFilePath = "settings.json"; // Define the path to your settings file

        public SettingsWindow()
        {
            InitializeComponent();
        }

        // Event: When the window is loaded
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            LoadSettings();
        }

        // Loads settings from settings.json into the UI elements
        private void LoadSettings()
        {
            AppSettings settings = LoadSettingsFromFile();

            EnableRemindersCheckBox.IsChecked = settings.EnableReminders;
            InitialIntervalTextBox.Text = settings.InitialInterval.ToString();
            // Use a modifiable collection like ObservableCollection or simply reset ItemsSource
            ReminderTimesListBox.ItemsSource = null; // Clear previous items if any
            ReminderTimesListBox.ItemsSource = new System.Collections.ObjectModel.ObservableCollection<string>(settings.ReminderTimes); // Use ObservableCollection for dynamic updates if needed, or just List<string>
        }

        // Helper method to load settings from the file
        private AppSettings LoadSettingsFromFile()
        {
            if (File.Exists(SettingsFilePath))
            {
                try
                {
                    string json = File.ReadAllText(SettingsFilePath);
                    var settings = JsonSerializer.Deserialize<AppSettings>(json);
                    // Ensure ReminderTimes is not null after deserialization
                    if (settings.ReminderTimes == null)
                    {
                        settings.ReminderTimes = new List<string>();
                    }
                    return settings ?? CreateDefaultSettings();
                }
                catch (Exception ex)
                {
                    // Log the error or inform the user
                    StatusTextBlock.Text = $"Error loading settings: {ex.Message}. Using defaults.";
                    return CreateDefaultSettings(); // Return default settings on error
                }
            }
            else
            {
                // Settings file doesn't exist, return default settings
                StatusTextBlock.Text = "Settings file not found. Using default settings.";
                return CreateDefaultSettings();
            }
        }

        // Helper method to create default settings
        private AppSettings CreateDefaultSettings()
        {
            return new AppSettings
            {
                EnableReminders = false,
                InitialInterval = 30, // Default interval
                ReminderTimes = new List<string>() // Default empty list
            };
        }


        // Event: Adding a new time to the reminder list
        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            string newTime = NewTimeTextBox.Text.Trim();

            if (ValidateTimeFormat(newTime))
            {
                // Assuming ReminderTimesListBox.ItemsSource is ObservableCollection<string>
                var times = ReminderTimesListBox.ItemsSource as System.Collections.ObjectModel.ObservableCollection<string>;
                if (times == null)
                {
                    // If it's not ObservableCollection, handle accordingly (e.g., create new list, add, reassign ItemsSource)
                    // For simplicity, let's assume it is for now, or handle the list case directly
                     var currentTimes = (ReminderTimesListBox.ItemsSource as IEnumerable<string> ?? Enumerable.Empty<string>()).ToList();
                     if (!currentTimes.Contains(newTime))
                     {
                         currentTimes.Add(newTime);
                         ReminderTimesListBox.ItemsSource = new System.Collections.ObjectModel.ObservableCollection<string>(currentTimes.OrderBy(t => t)); // Keep sorted
                         NewTimeTextBox.Clear();
                         StatusTextBlock.Text = "New time added.";
                     }
                     else
                     {
                         StatusTextBlock.Text = "The entered time already exists in the list.";
                     }
                     return;
                }


                if (!times.Contains(newTime))
                {
                    times.Add(newTime);
                    // Optionally sort the list after adding
                    var sortedTimes = new System.Collections.ObjectModel.ObservableCollection<string>(times.OrderBy(t => t));
                    ReminderTimesListBox.ItemsSource = sortedTimes;

                    NewTimeTextBox.Clear();
                    StatusTextBlock.Text = "New time added.";
                }
                else
                {
                    StatusTextBlock.Text = "The entered time already exists in the list.";
                }
            }
            else
            {
                StatusTextBlock.Text = "Invalid time format. Please use HH:mm (24-hour format).";
            }
        }

        // Event: Removing selected time from the reminder list
        private void RemoveButton_Click(object sender, RoutedEventArgs e)
        {
            if (ReminderTimesListBox.SelectedItem != null)
            {
                string selectedTime = ReminderTimesListBox.SelectedItem.ToString();
                 // Assuming ReminderTimesListBox.ItemsSource is ObservableCollection<string>
                var times = ReminderTimesListBox.ItemsSource as System.Collections.ObjectModel.ObservableCollection<string>;
                 if (times != null)
                 {
                    times.Remove(selectedTime);
                    StatusTextBlock.Text = $"Removed {selectedTime} from the list.";
                 }
                 else // Handle non-ObservableCollection case
                 {
                     var currentTimes = (ReminderTimesListBox.ItemsSource as IEnumerable<string> ?? Enumerable.Empty<string>()).ToList();
                     if(currentTimes.Remove(selectedTime))
                     {
                        ReminderTimesListBox.ItemsSource = new System.Collections.ObjectModel.ObservableCollection<string>(currentTimes);
                        StatusTextBlock.Text = $"Removed {selectedTime} from the list.";
                     }
                 }
                // Clear selection and text box if needed
                ReminderTimesListBox.SelectedItem = null;
                 NewTimeTextBox.Clear();
            }
            else
            {
                StatusTextBlock.Text = "No time selected to remove.";
            }
        }

        // Event: Handling changes in the selected item in ReminderTimesListBox
        private void ReminderTimesListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ReminderTimesListBox.SelectedItem != null)
            {
                NewTimeTextBox.Text = ReminderTimesListBox.SelectedItem.ToString();
            }
            // Consider clearing NewTimeTextBox if selection is removed? Depends on desired UX.
            // else { NewTimeTextBox.Clear(); }
        }

        // Event: Saving the settings
        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                bool enableReminders = EnableRemindersCheckBox.IsChecked ?? false;
                int initialInterval;
                if (!int.TryParse(InitialIntervalTextBox.Text, out initialInterval) || initialInterval <= 0)
                {
                    StatusTextBlock.Text = "Invalid initial interval. It should be a positive number.";
                    return;
                }

                // Get times from the ListBox's ItemsSource
                var reminderTimes = (ReminderTimesListBox.ItemsSource as IEnumerable<string> ?? Enumerable.Empty<string>()).ToList();


                // Create settings object to save
                 var settingsToSave = new AppSettings
                 {
                     EnableReminders = enableReminders,
                     InitialInterval = initialInterval,
                     ReminderTimes = reminderTimes.OrderBy(t => t).ToList() // Save sorted list
                 };


                SaveSettingsToFile(settingsToSave); // Save the actual settings object

                StatusTextBlock.Text = "Settings saved successfully.";
                Close(); // Optionally close the window after saving
            }
            catch (Exception ex)
            {
                StatusTextBlock.Text = $"Error saving settings: {ex.Message}";
                // Optionally log the full exception details
            }
        }

        // Modified method to save settings to settings.json
        private void SaveSettingsToFile(AppSettings settings)
        {
            try
            {
                // Configure JsonSerializer options for pretty printing (optional)
                var options = new JsonSerializerOptions { WriteIndented = true };
                string json = JsonSerializer.Serialize(settings, options);
                File.WriteAllText(SettingsFilePath, json);
            }
            catch (Exception ex)
            {
                 // Rethrow or handle appropriately (e.g., show message box)
                throw new InvalidOperationException($"Failed to save settings to {SettingsFilePath}", ex);
            }
        }


        // Event: Cancel button behavior
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close(); // Simply close the window without saving
        }

        // Validates HH:mm format for the reminder time
        private bool ValidateTimeFormat(string time)
        {
            // Use "HH\\:mm" for strict 24-hour format validation
            return TimeSpan.TryParseExact(time, "HH\\:mm", CultureInfo.InvariantCulture, TimeSpanStyles.None, out _);
        }
    }
}