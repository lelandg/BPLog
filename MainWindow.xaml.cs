using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Newtonsoft.Json;
using Formatting = System.Xml.Formatting;
using System.Runtime.CompilerServices;

namespace BloodPressureApp // Assuming this is your namespace
{
    public static class SettingsManager
    {
        private static readonly string SettingsFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "BPLog",
            "settings.json");
        
        public static void SaveSettings(MainViewModel viewModel, MainWindow window = null)
        {
            Console.WriteLine("Saving setting to: " + SettingsFilePath);
            try
            {
                // Ensure the directory exists
                string? directory = Path.GetDirectoryName(SettingsFilePath);
                if (!Directory.Exists(directory))
                    if (directory != null)
                        Directory.CreateDirectory(directory);

                // Serialize and write to file
                string json = JsonConvert.SerializeObject(viewModel, Newtonsoft.Json.Formatting.Indented);
                File.WriteAllText(SettingsFilePath, json);
                
            }
            catch (Exception ex)
            {
                // Handle save errors
                Console.WriteLine($"Error saving settings:\n {ex}");
            }
        }

        public static MainViewModel LoadSettings()
        {
            Console.WriteLine("Loading settings from: " + SettingsFilePath);
            try
            {
                if (File.Exists(SettingsFilePath))
                {
                    string json = File.ReadAllText(SettingsFilePath);
                    var viewModel = JsonConvert.DeserializeObject<MainViewModel>(json) ?? new MainViewModel();
                    return viewModel;
                }
            }
            catch (Exception ex)
            {
                // Handle load errors
                Console.WriteLine($"Error loading settings:\n  {ex}");
            }

            // Return a new instance with default settings if a file not found or error
            return new MainViewModel();
        }
    }

    public class MainViewModel : INotifyPropertyChanged
    {
        private string _userName = "Default User";
        private DateTime? _birthDate;
        private int? _systolic;
        private int? _diastolic;
        private int? _pulse;
        private DateTime? _readingDateTime; // Combined Date and Time for reading
        private bool _standing;
        private HealthRecord _selectedReading;
        private DateTime? _lastExportDateTime;
        private DateTime? _exportStartDateTime;

        public ObservableCollection<HealthRecord> Readings { get; set; } = new ObservableCollection<HealthRecord>();

        // Constructor used by WPF DataContext if no specific user needed at start
        public MainViewModel()
        {
           // Consider loading settings here if appropriate
           // LoadSettings(); // Example - Careful with async/timing
        }

        // Maybe a constructor for specific user init
        public MainViewModel(string userName) : this()
        {
            UserName = userName;
        }

        public string UserName
        {
            get => _userName;
            set { _userName = value; OnPropertyChanged(); }
        }

        public DateTime? BirthDate
        {
            get => _birthDate;
            set { _birthDate = value; OnPropertyChanged(); }
        }

        public int? Systolic
        {
            get => _systolic;
            set { _systolic = value; OnPropertyChanged(); }
        }

        public int? Diastolic
        {
            get => _diastolic;
            set { _diastolic = value; OnPropertyChanged(); }
        }

        public int? Pulse
        {
            get => _pulse;
            set { _pulse = value; OnPropertyChanged(); }
        }

        // Combined Reading Date and Time property
        public DateTime? ReadingDateTime
        {
            get => _readingDateTime;
            set
            {
                _readingDateTime = value;
                OnPropertyChanged();
                // Optionally update separate Date/Time parts if needed for UI binding
                OnPropertyChanged(nameof(ReadingDate));
                OnPropertyChanged(nameof(ReadingTime));
            }
        }

        // Helper property for DatePicker binding (Reading Date)
        public DateTime? ReadingDate
        {
            get => _readingDateTime?.Date;
            set
            {
                var currentTime = _readingDateTime?.TimeOfDay ?? DateTime.Now.TimeOfDay;
                ReadingDateTime = value?.Date + currentTime;
                OnPropertyChanged();
            }
        }

        // Helper property for Time TextBox binding (Reading Time)
        // Consider using a MaskedTextBox or TimePicker control for better UX
        public TimeSpan? ReadingTime
        {
            get => _readingDateTime?.TimeOfDay;
            set
            {
                var currentDate = _readingDateTime?.Date ?? DateTime.Today;
        
                // Only update with value if it's provided, otherwise keep the existing time
                // or use default time only if there's no existing time
                if (value.HasValue)
                {
                    ReadingDateTime = currentDate + value.Value;
                    Console.WriteLine($"Set date/time to: {ReadingDateTime}");
                }
                else if (_readingDateTime == null) 
                {
                    // Only set current time if there's no existing time at all
                    ReadingDateTime = currentDate + DateTime.Now.TimeOfDay;
                    Console.WriteLine($"Set date/time to current time: {ReadingDateTime}");
                }
        
                OnPropertyChanged();
                OnPropertyChanged(nameof(ReadingDate));
            }
        }

        public bool Standing
        {
            get => _standing;
            set { _standing = value; OnPropertyChanged(); }
        }

        public HealthRecord SelectedReading
        {
            get => _selectedReading;
            set { _selectedReading = value; OnPropertyChanged(); }
        }

        // Stores the date/time of the last successful export
        public DateTime? LastExportDateTime
        {
            get => _lastExportDateTime;
            set
            {
                if (_lastExportDateTime != value)
                {
                    _lastExportDateTime = value;
                    OnPropertyChanged();
                    // When LastExportDateTime changes, potentially update the default ExportStartDateTime
                    // This logic might be better placed in LoadSettings or after a successful export
                     if (ExportStartDateTime == null && value != null) // Only default if not already set by user maybe?
                     {
                         ExportStartDateTime = value;
                     }
                }
            }
        }

        // Stores the start date/time for the next export, defaults to LastExportDateTime
        public DateTime? ExportStartDateTime
        {
            get => _exportStartDateTime;
            set
            {
                if (_exportStartDateTime != value)
                {
                    _exportStartDateTime = value;
                    OnPropertyChanged();
                    // Optionally update separate Date/Time parts if needed for UI binding
                    OnPropertyChanged(nameof(ExportStartDate));
                    OnPropertyChanged(nameof(ExportStartTime));
                }
            }
        }

         // Helper property for DatePicker binding (Export Start Date)
        public DateTime? ExportStartDate
        {
            get => _exportStartDateTime?.Date;
            set
            {
                var currentTime = _exportStartDateTime?.TimeOfDay ?? TimeSpan.Zero; // Default to midnight if no time set
                ExportStartDateTime = value?.Date + currentTime;
                OnPropertyChanged();
            }
        }

        // Helper property for Time TextBox binding (Export Start Time)
        // Consider using a MaskedTextBox or TimePicker control for better UX
        public TimeSpan? ExportStartTime
        {
            get => _exportStartDateTime?.TimeOfDay;
            set
            {
                 var currentDate = _exportStartDateTime?.Date ?? DateTime.Today; // Default to today if no date set
                ExportStartDateTime = currentDate + (value ?? TimeSpan.Zero); // Default to midnight if no time provided
                OnPropertyChanged();
            }
        }


        // --- Methods ---

        public void SaveSettings()
        {
            // Instance might be better injected or retrieved via service locator
            SettingsManager.SaveSettings(this); // Pass the ViewModel instance
        }

        public void LoadSettings()
        {
            var loadedViewModelData = SettingsManager.LoadSettings();

            // Apply loaded settings to this instance
            UserName = loadedViewModelData.UserName;
            BirthDate = loadedViewModelData.BirthDate;
            LastExportDateTime = loadedViewModelData.LastExportDateTime; // Load last export time
            // Default the Export Start time to the Last Export time when loading
            ExportStartDateTime = LastExportDateTime;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }


    public partial class MainWindow : Window
    { 
        private MainViewModel _viewModel;

        public MainWindow()
        {
            InitializeComponent();
            InitializeDatabase();
            
            _viewModel = new MainViewModel();
        
            // Load settings from SettingsManager
            _viewModel.LoadSettings();
            DataContext = _viewModel;

            // Handle closing to save settings
            Closing += (sender, args) => _viewModel.SaveSettings();
            
            LoadReadings();
        }

        private string _connectionString;

        private void InitializeDatabase()
        {
            string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\BPReadings";
            _connectionString = "Data Source=" + Path.Combine(documentsPath, "bloodpressure.db");

            using var connection = new SqliteConnection(_connectionString);
            connection.Open();
            Console.WriteLine($"Opened database: {_connectionString}");

            string userTable = @"CREATE TABLE IF NOT EXISTS Users (
                                 Id INTEGER PRIMARY KEY AUTOINCREMENT,
                                 Name TEXT NOT NULL,
                                 Birthdate TEXT NOT NULL)";
            string readingTable = @"CREATE TABLE IF NOT EXISTS Readings (
                                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                                    UserId INTEGER NOT NULL,
                                    Systolic INTEGER NOT NULL,
                                    Diastolic INTEGER NOT NULL,
                                    Pulse INTEGER NOT NULL,
                                    ReadingTime TEXT NOT NULL,
                                    Position TEXT NOT NULL,
                                    FOREIGN KEY(UserId) REFERENCES Users(Id))";

            using var userCmd = new SqliteCommand(userTable, connection);
            using var readingCmd = new SqliteCommand(readingTable, connection);
            userCmd.ExecuteNonQuery();
            readingCmd.ExecuteNonQuery();
        }

        private void AddRecordButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(UserNameInput.Text) || BirthDatePicker.SelectedDate == null)
            {
                MessageBox.Show("Please enter user name and birthdate.");
                return;
            }

            if (string.IsNullOrWhiteSpace(_viewModel.Systolic.ToString()) || 
                string.IsNullOrWhiteSpace(_viewModel.Diastolic.ToString()) ||
                string.IsNullOrWhiteSpace(_viewModel.Pulse.ToString()))
            {
                MessageBox.Show("Please enter valid blood pressure readings.");
                return;
            }

            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            // Check if user exists
            string selectUserQuery = "SELECT Id FROM Users WHERE Name = @name AND Birthdate = @birthdate";
            using var selectUserCmd = new SqliteCommand(selectUserQuery, connection);
            selectUserCmd.Parameters.AddWithValue("@name", UserNameInput.Text);
            selectUserCmd.Parameters.AddWithValue("@birthdate", BirthDatePicker.SelectedDate.Value.ToString("yyyy-MM-dd"));
            object result = selectUserCmd.ExecuteScalar();

            int userId;
            if (result == null)
            {
                // Insert user
                string insertUserQuery = "INSERT INTO Users (Name, Birthdate) VALUES (@name, @birthdate)";
                using var insertUserCmd = new SqliteCommand(insertUserQuery, connection);
                insertUserCmd.Parameters.AddWithValue("@name", UserNameInput.Text);
                insertUserCmd.Parameters.AddWithValue("@birthdate", BirthDatePicker.SelectedDate.Value.ToString("yyyy-MM-dd"));
                insertUserCmd.ExecuteNonQuery();

                userId = Convert.ToInt32(new SqliteCommand("SELECT last_insert_rowid();", connection).ExecuteScalar());
            }
            else
            {
                userId = Convert.ToInt32(result);
            }

            // Add reading
            string position = _viewModel.Standing == true ? "Standing" : "Sitting";
            string insertReadingQuery = @"INSERT INTO Readings (UserId, Systolic, Diastolic, Pulse, ReadingTime, Position) 
                            VALUES (@userId, @systolic, @diastolic, @pulse, @readingTime, @position)";
            using var insertReadingCmd = new SqliteCommand(insertReadingQuery, connection);
            insertReadingCmd.Parameters.AddWithValue("@userId", userId);
            insertReadingCmd.Parameters.AddWithValue("@systolic", _viewModel.Systolic);
            insertReadingCmd.Parameters.AddWithValue("@diastolic", _viewModel.Diastolic);
            insertReadingCmd.Parameters.AddWithValue("@pulse", _viewModel.Pulse);
            DateTime date;
            if (_viewModel.ReadingDate.HasValue)
            {
                // Get the date from the DatePicker
                date = _viewModel.ReadingDate.Value.Date;
                
                // Add the time component - either from the time field or current time
                if (_viewModel.ReadingTime.HasValue)
                {
                    date = date.Add(_viewModel.ReadingTime.Value);
                }
                else
                {
                    date = date.Add(DateTime.Now.TimeOfDay);
                }
            }
            else
            {
                // If no date was picked, use today with either provided time or current time
                date = DateTime.Today;
                if (_viewModel.ReadingTime.HasValue)
                {
                    date = date.Add(_viewModel.ReadingTime.Value);
                }
                else
                {
                    date = date.Add(DateTime.Now.TimeOfDay);
                }
            }
            insertReadingCmd.Parameters.AddWithValue("@readingTime", date);
            insertReadingCmd.Parameters.AddWithValue("@position", position);
            insertReadingCmd.ExecuteNonQuery();

            // Reload readings
            LoadReadings();
        }

        private void LoadReadings()
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            string query = @"SELECT u.Name, u.Birthdate, r.Systolic, r.Diastolic, r.Pulse, r.ReadingTime, r.Position
                             FROM Readings r
                             JOIN Users u ON r.UserId = u.Id
                             ORDER BY r.ReadingTime ASC";

            using var cmd = new SqliteCommand(query, connection);
            using var reader = cmd.ExecuteReader();

            var readings = new List<HealthRecord>();
            while (reader.Read())
            {
                var readingDateTime = DateTime.Parse(reader.GetString(5));
                var record = new HealthRecord()
                {
                    Name = reader.GetString(0),
                    BirthDate = DateTime.Parse(reader.GetString(1)),
                    Systolic = reader.GetInt32(2),
                    Diastolic = reader.GetInt32(3),
                    Pulse = reader.GetInt32(4),
                    ReadingDate = readingDateTime,
                    // We don't need to explicitly set ReadingTime as it's derived from ReadingDate
                    Standing = reader.GetString(6) == "Standing"
                };
                
                // Debugging - verify ReadingTime is actually populated
                Console.WriteLine($"Record time: {record.ReadingTime}");
                
                readings.Add(record);
            }

            ReadingsGrid.ItemsSource = readings;
        }

        private void ExportTextButton_Click(object sender, RoutedEventArgs e)
        {
            // Example file path (update this logic based on the actual file-saving implementation)
            string fileName = $"{DateTime.Now:yyyy-MM-dd}_bp_readings.txt";
            string folderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "BPReadings"); // Example folder
            string fullPath = Path.Combine(folderPath, fileName);
            
            // Ensure the directory exists
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            using var writer = new StreamWriter(fullPath);
            if ((ReadingsGrid.ItemsSource as IList<dynamic>)?[0] is HealthRecord hr) 
                writer.WriteLine($"Name: {hr.Name}  Birth Date: {hr.BirthDate:yyyy-MM-dd}");
            
            // Filter records based on LastExportDateTime
            var recordsToExport = ReadingsGrid.ItemsSource.Cast<HealthRecord>()
                .Where(r => _viewModel.LastExportDateTime == null || r.ReadingDate > _viewModel.LastExportDateTime)
                .ToList();
                
            foreach (var item in recordsToExport)
            {
                writer.WriteLine(item.ToString());
            }
            
            // Update the LastExportDateTime to the current date/time
            _viewModel.LastExportDateTime = DateTime.Now;
            _viewModel.SaveSettings();
            
            Clipboard.SetText(fullPath);
            MessageBox.Show($"Data exported and path copied to clipboard:\n{fullPath}", 
                "Export Successful", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ExportHtmlButton_Click(object sender, RoutedEventArgs e)
        {
            string fileName = $"{DateTime.Now:yyyy-MM-dd}_bp_readings.html";
            string folderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "BPReadings"); // Example folder
            string fullPath = Path.Combine(folderPath, fileName);

            // Ensure the directory exists
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }
        
            using var writer = new StreamWriter(fullPath);
            writer.WriteLine("<html><body><table border='2px' style='border-collapse:collapse; padding:5px;'>");
            writer.WriteLine("<style>th, td { padding: 5px; text-align: center}</style>"); // Added CSS for cell padding
        
            if ((ReadingsGrid.ItemsSource as IList<dynamic>)?[0] is HealthRecord hr) 
                writer.WriteLine($"<h1>{hr.Name} {hr.BirthDate:MM-dd-yyyy}</h1>");
            
            writer.WriteLine("<tr><th>Date</th><th>Time</th><th>Sys/Dia</th><th>Pulse</th><th>Standing</th></tr>");
        
            // Filter records based on LastExportDateTime
            var recordsToExport = ReadingsGrid.ItemsSource.Cast<HealthRecord>()
                .Where(r => _viewModel.LastExportDateTime == null || r.ReadingDate > _viewModel.LastExportDateTime)
                .ToList();
        
            foreach (HealthRecord item in recordsToExport)
            {
                writer.WriteLine($"<tr><td>{item.ReadingDate:MM-dd-yyyy}</td>" +
                                 $"<td>{item.ReadingTime.ToString()?[..5]}</td>" +
                                 $"<td>{item.Systolic} / {item.Diastolic}</td>" +
                                 $"<td>{item.Pulse}</td>" +
                                 $"<td>{(item.Standing == true ? "X" : "")}</td></tr>");
            }
        
            writer.WriteLine("</table></body></html>");
        
            // Update the LastExportDateTime to the current date/time
            _viewModel.LastExportDateTime = DateTime.Now;
            _viewModel.SaveSettings();

            Clipboard.SetText(fullPath);
            MessageBox.Show($"Data exported and path copied to clipboard:\n{fullPath}", 
                "Export Successful", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        
        private void ReadingsGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            // Check if a row is selected
            if (ReadingsGrid.SelectedItem is HealthRecord selectedRecord)
            {
                // Populate the fields in the form
                _viewModel.UserName = selectedRecord.Name;
                _viewModel.BirthDate = selectedRecord.BirthDate;
                _viewModel.Systolic = selectedRecord.Systolic;
                _viewModel.Diastolic = selectedRecord.Diastolic;
                _viewModel.Pulse = selectedRecord.Pulse;
                _viewModel.ReadingDate = selectedRecord.ReadingDate.Date;
                _viewModel.ReadingTime = selectedRecord.ReadingTime;
                _viewModel.Standing = selectedRecord.Standing;
            }
        }
        
        private void SetLastExportedRecord_Click(object sender, RoutedEventArgs e)
        {
            // Check if a row is selected
            if (ReadingsGrid.SelectedItem is HealthRecord selectedRecord)
            {
                // Set the LastExportDateTime to the selected record's date
                _viewModel.LastExportDateTime = selectedRecord.ReadingDate;
                
                _viewModel.SaveSettings();
        
                MessageBox.Show($"Last exported record set to: {selectedRecord.ReadingDate:yyyy-MM-dd HH:mm}",
                    "Last Export Date Updated", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show("Please select a record first.",
                    "No Record Selected", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }        

        private void SetCurrentDateTime_Click(object sender, RoutedEventArgs e)
        {
            // Set the current date and time to the view model
            _viewModel.ReadingDate = DateTime.Now;
            _viewModel.ReadingTime = TimeSpan.Parse(DateTime.Now.ToString("HH:mm"));
        }
        
    }
}