using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Data.Sqlite;
using Newtonsoft.Json;
// Add this for DataGrid
// Add this for MessageBox if not already present
// Add this for MouseButtonEventArgs if needed for Edit logic
// Assuming your ViewModel namespace is BloodPressureApp
// Assuming your HealthRecord and other models are here

// Make sure you have this using directive

namespace BPLog // Assuming this is your namespace
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
        private DateTime? _readingDateTime = DateTime.Now;
        private bool _standing;
        private HealthRecord _selectedReading;
        private DateTime? _lastExportDateTime;
        private DateTime? _exportStartDateTime;

        public ObservableCollection<HealthRecord> Readings { get; set; } = new ObservableCollection<HealthRecord>();

        // Add to MainViewModel.cs
        public ICommand AddRecordCommand { get; }
        public ICommand SetCurrentDateTimeCommand { get; }
        public ICommand ExportToTextCommand { get; }
        public ICommand ExportToHtmlCommand { get; }
        public ICommand EditReadingCommand { get; }
        public ICommand DeleteReadingCommand { get; }
        public ICommand SetLastExportedCommand { get; }

        public MainViewModel()
        {
            // Initialize commands
            AddRecordCommand = new RelayCommand(AddRecord);
            SetCurrentDateTimeCommand = new RelayCommand(SetCurrentDateTime);
            ExportToTextCommand = new RelayCommand(ExportToText);
            ExportToHtmlCommand = new RelayCommand(ExportToHtml);
            EditReadingCommand = new RelayCommand(EditReading, CanEditReading);
            DeleteReadingCommand = new RelayCommand(DeleteReading, CanEditReading);
            SetLastExportedCommand = new RelayCommand(SetLastExported, CanSetLastExported);
            
            ExportStartDateTime = LastExportDateTime;
        }

        private bool CanEditReading(object parameter) => SelectedReading != null;
        private bool CanSetLastExported(object parameter) => SelectedReading != null;

        // Implement command methods here

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

        public DateTime? ReadingDateTime
        {
            get => _readingDateTime;
            set
            {
                if (_readingDateTime != value)
                {
                    _readingDateTime = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(ReadingDate));
                    OnPropertyChanged(nameof(ReadingTime));
                }
            }
        }

        public DateTime? ReadingDate
        {
            get => _readingDateTime?.Date;
            set
            {
                if (value.HasValue)
                {
                    ReadingDateTime = value.Value.Date.Add(_readingDateTime?.TimeOfDay ?? TimeSpan.Zero);
                }
                else
                {
                    ReadingDateTime = null;
                }
            }
        }

        public TimeSpan? ReadingTime
        {
            get => _readingDateTime?.TimeOfDay;
            set
            {
                if (value.HasValue && _readingDateTime.HasValue)
                {
                    ReadingDateTime = _readingDateTime.Value.Date.Add(value.Value);
                }
                OnPropertyChanged();
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

        public DateTime? LastExportDateTime
        {
            get => _lastExportDateTime;
            set
            {
                _lastExportDateTime = value;
                OnPropertyChanged();
                
                if (ExportStartDateTime == null || ExportStartDateTime < value)
                {
                    ExportStartDateTime = value;
                }
            }
        }

        public DateTime? ExportStartDateTime
        {
            get => _exportStartDateTime;
            set
            {
                _exportStartDateTime = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ExportStartDate));
                OnPropertyChanged(nameof(ExportStartTime));
            }
        }

        public DateTime? ExportStartDate
        {
            get => _exportStartDateTime?.Date;
            set
            {
                if (value.HasValue && _exportStartDateTime.HasValue)
                {
                    ExportStartDateTime = value.Value.Date.Add(_exportStartDateTime.Value.TimeOfDay);
                }
                else if (value.HasValue)
                {
                    ExportStartDateTime = value.Value;
                }
                OnPropertyChanged();
            }
        }

        public TimeSpan? ExportStartTime
        {
            get => _exportStartDateTime?.TimeOfDay;
            set
            {
                if (value.HasValue && _exportStartDateTime.HasValue)
                {
                    ExportStartDateTime = _exportStartDateTime.Value.Date.Add(value.Value);
                }
                OnPropertyChanged();
            }
        }

        public void LoadSettings()
        {
            // Load user settings from storage
            // Example implementation: Load last export date, user preferences, etc.
            var settings = SettingsManager.LoadSettings();
            // Apply loaded settings to this instance
            LastExportDateTime = settings.LastExportDateTime;
            // Add other properties as needed
        }

        public void SaveSettings()
        {
            // Save current settings to storage
            // Example implementation: Save last export date, user preferences, etc.
            SettingsManager.SaveSettings(this);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        // Add these methods to MainViewModel class

        private void AddRecord(object parameter)
        {
            // Validate input
            if (!Systolic.HasValue || !Diastolic.HasValue || !Pulse.HasValue || !ReadingDateTime.HasValue)
            {
                // You would typically show a message to the user
                // In a full implementation, use a service or message broker
                return;
            }

            // Create new health record
            var newReading = new HealthRecord
            {
                Name = UserName,
                BirthDate = BirthDate ?? DateTime.Now,
                Systolic = Systolic.Value,
                Diastolic = Diastolic.Value,
                Pulse = Pulse.Value,
                ReadingDate = ReadingDateTime.Value,
                Standing = Standing,
                Id = Readings.Count > 0 ? Readings.Max(r => r.Id) + 1 : 1
            };

            // Add to collection
            Readings.Add(newReading);

            // Clear input fields
            Systolic = null;
            Diastolic = null;
            Pulse = null;
            ReadingDateTime = DateTime.Now;
            Standing = false;

            // Save settings
            SaveSettings();
        }

        private void SetCurrentDateTime(object parameter)
        {
            ReadingDateTime = DateTime.Now;
        }

        private void ExportToText(object parameter)
        {
            // In a real implementation, use a service for file operations
            try
            {
                var dialog = new Microsoft.Win32.SaveFileDialog
                {
                    DefaultExt = ".txt",
                    Filter = "Text documents (.txt)|*.txt"
                };

                bool? result = dialog.ShowDialog();

                if (result == true)
                {
                    string filePath = dialog.FileName;
                    
                    // Get records after last export
                    var recordsToExport = ExportStartDateTime.HasValue
                        ? Readings.Where(r => r.ReadingDate >= ExportStartDateTime.Value).ToList()
                        : Readings.ToList();

                    if (recordsToExport.Count == 0)
                    {
                        // Show message: "No new records to export"
                        return;
                    }

                    using (StreamWriter writer = new StreamWriter(filePath))
                    {
                        writer.WriteLine($"Blood Pressure Readings for {UserName}");
                        writer.WriteLine($"Exported on {DateTime.Now}");
                        writer.WriteLine(new string('-', 50));

                        foreach (var record in recordsToExport)
                        {
                            writer.WriteLine(record.ToString());
                        }
                    }

                    // Update last export date
                    LastExportDateTime = DateTime.Now;
                    SaveSettings();
                }
            }
            catch (Exception ex)
            {
                // Log error and show message to user
                Console.WriteLine($"Error exporting to text: {ex.Message}");
            }
        }

        private void ExportToHtml(object parameter)
        {
            try
            {
                var dialog = new Microsoft.Win32.SaveFileDialog
                {
                    DefaultExt = ".html",
                    Filter = "HTML documents (.html)|*.html"
                };

                bool? result = dialog.ShowDialog();

                if (result == true)
                {
                    string filePath = dialog.FileName;
                    
                    // Get records after last export
                    var recordsToExport = ExportStartDateTime.HasValue
                        ? Readings.Where(r => r.ReadingDate >= ExportStartDateTime.Value).ToList()
                        : Readings.ToList();

                    if (recordsToExport.Count == 0)
                    {
                        // Show message: "No new records to export"
                        return;
                    }

                    using (StreamWriter writer = new StreamWriter(filePath))
                    {
                        writer.WriteLine("<!DOCTYPE html>");
                        writer.WriteLine("<html>");
                        writer.WriteLine("<head>");
                        writer.WriteLine("<title>Blood Pressure Readings</title>");
                        writer.WriteLine("<style>");
                        writer.WriteLine("body { font-family: Arial, sans-serif; margin: 20px; }");
                        writer.WriteLine("table { border-collapse: collapse; width: 100%; }");
                        writer.WriteLine("th, td { border: 1px solid #ddd; padding: 8px; text-align: center; }");
                        writer.WriteLine("th { background-color: #f2f2f2; }");
                        writer.WriteLine("</style>");
                        writer.WriteLine("</head>");
                        writer.WriteLine("<body>");
                        writer.WriteLine($"<h1>Blood Pressure Readings for {UserName}</h1>");
                        writer.WriteLine($"<p>Exported on {DateTime.Now}</p>");
                        writer.WriteLine("<table>");
                        writer.WriteLine("<tr><th>Date</th><th>Time</th><th>Systolic</th><th>Diastolic</th><th>Pulse</th><th>Standing</th></tr>");

                        foreach (var record in recordsToExport)
                        {
                            writer.WriteLine("<tr>");
                            writer.WriteLine($"<td>{record.ReadingDate:MM/dd/yyyy}</td>");
                            writer.WriteLine($"<td>{record.ReadingTime:hh\\:mm tt}</td>");
                            writer.WriteLine($"<td>{record.Systolic}</td>");
                            writer.WriteLine($"<td>{record.Diastolic}</td>");
                            writer.WriteLine($"<td>{record.Pulse}</td>");
                            writer.WriteLine($"<td>{(record.Standing ? "Yes" : "No")}</td>");
                            writer.WriteLine("</tr>");
                        }

                        writer.WriteLine("</table>");
                        writer.WriteLine("</body>");
                        writer.WriteLine("</html>");
                    }

                    // Update last export date
                    LastExportDateTime = DateTime.Now;
                    SaveSettings();
                }
            }
            catch (Exception ex)
            {
                // Log error and show message to user
                Console.WriteLine($"Error exporting to HTML: {ex.Message}");
            }
        }

        private void EditReading(object parameter)
        {
            if (SelectedReading == null)
                return;

            // In a real app, you might open a dialog or navigate to another view
            // For now, we'll just copy the values to the input fields
            UserName = SelectedReading.Name;
            Systolic = SelectedReading.Systolic;
            Diastolic = SelectedReading.Diastolic;
            Pulse = SelectedReading.Pulse;
            ReadingDateTime = SelectedReading.ReadingDate;
            Standing = SelectedReading.Standing;
            
            // Remove the original record
            Readings.Remove(SelectedReading);
            
            // Note: In a real app, you would typically use a dialog
            // and only remove after confirmation of save
        }

        private void DeleteReading(object parameter)
        {
            if (SelectedReading == null)
                return;

            // In a real app, show confirmation dialog
            Readings.Remove(SelectedReading);
            SaveSettings();
        }

        private void SetLastExported(object parameter)
        {
            if (SelectedReading == null)
                return;

            // Set last export timestamp to the selected reading's date/time
            LastExportDateTime = SelectedReading.ReadingDate;
            SaveSettings();
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

            // *** Modified Query: Added r.Id as the first selected column ***
            string query = @"SELECT r.Id, u.Name, u.Birthdate, r.Systolic, r.Diastolic, r.Pulse, r.ReadingTime, r.Position
                             FROM Readings r
                             JOIN Users u ON r.UserId = u.Id
                             ORDER BY r.ReadingTime DESC";

            using var cmd = new SqliteCommand(query, connection);
            using var reader = cmd.ExecuteReader();

            // Use the ViewModel's collection directly if possible, or clear and add
            _viewModel.Readings.Clear(); // Clear existing items

            while (reader.Read())
            {
                // Ensure parsing handles potential DBNull or formatting issues if necessary
                var readingDateTime = DateTime.Parse(reader.GetString(6)); // Index 6 is ReadingTime

                var record = new HealthRecord()
                {
                    // *** Added Id assignment: Reads the first column (r.Id) ***
                    // Use GetInt64 for AUTOINCREMENT (typically maps to long/Int64)
                    // Cast to int if your HealthRecord.Id is int and you are sure IDs won't exceed int.MaxValue
                    Id = (int)reader.GetInt64(0), // Index 0 is r.Id
                    Name = reader.GetString(1),           // Index 1 is u.Name
                    BirthDate = DateTime.Parse(reader.GetString(2)), // Index 2 is u.Birthdate
                    Systolic = reader.GetInt32(3),        // Index 3 is r.Systolic
                    Diastolic = reader.GetInt32(4),       // Index 4 is r.Diastolic
                    Pulse = reader.GetInt32(5),           // Index 5 is r.Pulse
                    ReadingDate = readingDateTime,        // Index 6 is r.ReadingTime (already parsed)
                    Standing = reader.GetString(7) == "Standing" // Index 7 is r.Position
                };
                 _viewModel.Readings.Add(record); // Add directly to the ViewModel's collection
            }
             // No need to set ReadingsGrid.ItemsSource if it's bound to _viewModel.Readings
             // ReadingsGrid.ItemsSource = _viewModel.Readings;
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
        
    // ... existing fields and methods ...

    // --- New Menu Item Event Handlers ---

    private void ExitMenuItem_Click(object sender, RoutedEventArgs e)
    {
        this.Close(); // Close the main window
    }

    private void EditReadingMenuItem_Click(object sender, RoutedEventArgs e)
    {
        var viewModel = DataContext as MainViewModel;
        if (viewModel?.SelectedReading != null)
        {
            // Reuse the double-click logic to load data into input fields for editing
            // You might pass null for MouseButtonEventArgs if the handler doesn't strictly need it,
            // or create a dummy one if required. Let's assume it can handle null or doesn't use 'e'.
             ReadingsGrid_MouseDoubleClick(ReadingsGrid, null); // Pass null or adjust as needed
             // Consider focusing the first input field after loading
             SystolicTextBox.Focus();
        }
        else
        {
            MessageBox.Show("Please select a reading from the grid to edit.", "No Reading Selected", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    
    private void ReadingsGrid_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Delete)
        {
            // Check if an item is actually selected in the DataGrid
            if (ReadingsGrid.SelectedItem != null)
            {
                // Call the existing menu item click handler to perform the delete action
                DeleteReadingMenuItem_Click(sender, null); 
                
                // Mark the event as handled so it doesn't bubble up further
                e.Handled = true; 
            }
        }
    }

    // ... rest of the methods ...
        private void DeleteReadingMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel.SelectedReading != null)
            {
                var recordToDelete = _viewModel.SelectedReading;
                // *** Store the index BEFORE deleting ***
                int deletedIndex = _viewModel.Readings.IndexOf(recordToDelete);

                var result = MessageBox.Show($"Are you sure you want to delete the reading from {recordToDelete.ReadingDate:g}?",
                                             "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    using (var connection = new SqliteConnection(_connectionString))
                    {
                        connection.Open();
                        string deleteQuery = "DELETE FROM Readings WHERE Id = @id";
                        using var cmd = new SqliteCommand(deleteQuery, connection);
                        cmd.Parameters.AddWithValue("@id", recordToDelete.Id);

                        int rowsAffected = cmd.ExecuteNonQuery();

                        if (rowsAffected > 0)
                        {
                            // Remove from the UI collection
                            _viewModel.Readings.Remove(recordToDelete);

                            // *** Select the next item logic ***
                            if (_viewModel.Readings.Count > 0) // Check if the list is not empty
                            {
                                // Determine the index to select
                                int indexToSelect = Math.Min(deletedIndex, _viewModel.Readings.Count - 1);
                                // Ensure index is not negative (shouldn't happen with Count > 0 check, but safe)
                                indexToSelect = Math.Max(0, indexToSelect);

                                _viewModel.SelectedReading = _viewModel.Readings[indexToSelect];

                                // Optional: Scroll the selected item into view
                                ReadingsGrid.ScrollIntoView(_viewModel.SelectedReading);
                            }
                            else
                            {
                                // List is empty, clear selection
                                _viewModel.SelectedReading = null;
                            }

                            // Don't show this message box if you want the selection change to be the main feedback
                            // MessageBox.Show("Record deleted successfully.", "Delete Record", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                        else
                        {
                            MessageBox.Show("Could not find the record in the database to delete (ID mismatch).", "Delete Failed", MessageBoxButton.OK, MessageBoxImage.Warning);
                        }
                    }
                }
            }
            else
            {
                 MessageBox.Show("Please select a record to delete.", "Delete Record", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

    private void AboutMenuItem_Click(object sender, RoutedEventArgs e)
    {
        // Replace with your actual application name/version/copyright
        MessageBox.Show("Blood Pressure Log\n\nVersion v1.0\n\nDeveloped by Leland Green\n\nCopyright © 2025",
                        "About Blood Pressure Log",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
    }

    // Ensure ReadingsGrid_MouseDoubleClick is suitable for being called by Edit menu
    // If it relies heavily on MouseButtonEventArgs 'e', you might need to refactor
    // the core logic into a separate private method called by both handlers.
    private void ReadingsGrid_MouseDoubleClick(object sender, MouseButtonEventArgs? e) // Allow nullable e
    {
        var viewModel = DataContext as MainViewModel;
        if (viewModel?.SelectedReading != null)
        {
            var selectedRecord = viewModel.SelectedReading;
            viewModel.Systolic = selectedRecord.Systolic;
            viewModel.Diastolic = selectedRecord.Diastolic;
            viewModel.Pulse = selectedRecord.Pulse;
            viewModel.Standing = selectedRecord.Standing;

            // Combine Date and Time for the DateTimePicker/TextBox bindings
            viewModel.ReadingDate = selectedRecord.ReadingDate.Date;
            viewModel.ReadingTime = selectedRecord.ReadingDate.TimeOfDay;

            // Maybe scroll the selected item into view if called programmatically
             if (sender is DataGrid grid && grid.SelectedItem != null)
             {
                 grid.ScrollIntoView(grid.SelectedItem);
             }
        }
    }

    // ... SetLastExported_Click, SetCurrentDateTime_Click etc. ...
}
}