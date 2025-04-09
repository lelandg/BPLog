using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Newtonsoft.Json;
using Formatting = System.Xml.Formatting;

namespace BloodPressureApp
{
    public static class SettingsManager
    {
        private static readonly string SettingsFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "BPLog",
            "settings.json");
        
        public static void SaveSettings(MainViewModel viewModel)
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

            // Return new instance with default settings if file not found or error
            return new MainViewModel();
        }
    }

    public class MainViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private string _userName;
        private DateTime? _birthDate;
        private int? _systolic;
        private int? _diastolic;
        private int? _pulse;
        private DateTime? _readingDate;
        private string _readingTime;
        private bool _standing;

        // Example Properties
        public string UserName
        {
            get => _userName;
            set
            {
                if (_userName != value)
                {
                    _userName = value;
                    OnPropertyChanged(nameof(UserName));
                }
            }
        }

        public DateTime? BirthDate
        {
            get => _birthDate;
            set
            {
                if (_birthDate != value)
                {
                    _birthDate = value;
                    OnPropertyChanged(nameof(BirthDate));
                }
            }
        }

        public int? Systolic
        {
            get => _systolic;
            set
            {
                if (_systolic != value)
                {
                    _systolic = value;
                    OnPropertyChanged(nameof(Systolic));
                }
            }
        }

        public int? Diastolic
        {
            get => _diastolic;
            set
            {
                if (_diastolic != value)
                {
                    _diastolic = value;
                    OnPropertyChanged(nameof(Diastolic));
                }
            }
        }

        public int? Pulse
        {
            get => _pulse;
            set
            {
                if (_pulse != value)
                {
                    _pulse = value;
                    OnPropertyChanged(nameof(Pulse));
                }
            }
        }

        public DateTime? ReadingDate
        {
            get => _readingDate;
            set
            {
                if (_readingDate != value)
                {
                    _readingDate = value;
                    OnPropertyChanged(nameof(ReadingDate));
                }
            }
        }

        public string ReadingTime
        {
            get => _readingTime;
            set
            {
                if (_readingTime != value)
                {
                    _readingTime = value;
                    OnPropertyChanged(nameof(ReadingTime));
                }
            }
        }

        public bool Standing
        {
            get => _standing;
            set
            {
                if (_standing != value)
                {
                    _standing = value;
                    OnPropertyChanged(nameof(Standing));
                }
            }
        }

        // Handle Saving and Loading of Settings with SettingsManager.
        public void SaveSettings() => SettingsManager.SaveSettings(this);
        public void LoadSettings()
        {
            var loadedSettings = SettingsManager.LoadSettings();
            if (loadedSettings == null) return;

            // Assign loaded settings to current properties
            UserName = loadedSettings.UserName;
            BirthDate = loadedSettings.BirthDate;
            Systolic = loadedSettings.Systolic;
            Diastolic = loadedSettings.Diastolic;
            Pulse = loadedSettings.Pulse;
            ReadingDate = loadedSettings.ReadingDate;
            ReadingTime = loadedSettings.ReadingTime;
            Standing = loadedSettings.Standing;
        }

        protected void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
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
            this.Closing += (sender, args) => _viewModel.SaveSettings();
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
            var date = _viewModel.ReadingDate ??= DateTime.Now;
            date = date.Date.Add(TimeSpan.Parse(_viewModel.ReadingTime));
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
                             JOIN Users u ON r.UserId = u.Id";

            using var cmd = new SqliteCommand(query, connection);
            using var reader = cmd.ExecuteReader();

            var readings = new List<dynamic>();
            while (reader.Read())
            {
                readings.Add(new HealthRecord()
                {
                    Name = reader.GetString(0),
                    BirthDate = DateTime.Parse(reader.GetString(1)),
                    Systolic = reader.GetInt32(2),
                    Diastolic = reader.GetInt32(3),
                    Pulse = reader.GetInt32(4),
                    ReadingDate = DateTime.Parse(reader.GetString(5)),
                    ReadingTime = DateTime.Parse(reader.GetString(5)).TimeOfDay,
                    Standing = reader.GetString(6) == "Standing"
                });
            }

            ReadingsGrid.ItemsSource = readings;
        }

        private void ExportTextButton_Click(object sender, RoutedEventArgs e)
        {
            // Example file path (update this logic based on actual file-saving implementation)
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
                writer.WriteLine($"{hr.Name} - {hr.BirthDate:yyyy-MM-dd}");
            foreach (var item in ReadingsGrid.ItemsSource)
            {
                writer.WriteLine(item.ToString());
            }
            Clipboard.SetText(fullPath);
            MessageBox.Show($"Data exported and path copied to clipboard:\n{fullPath}", 
                "Export Successful", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ExportHtmlButton_Click(object sender, RoutedEventArgs e)
        {
            using var writer = new StreamWriter("readings.html");
            writer.WriteLine("<html><body><table border='1'>");
            writer.WriteLine("<tr><th>Name</th><th>Birthdate</th><th>Systolic</th><th>Diastolic</th><th>Pulse</th><th>Time</th><th>Position</th></tr>");
            foreach (var item in ReadingsGrid.ItemsSource)
            {
                writer.WriteLine($"<tr><td>{item}</td></tr>");
            }
            writer.WriteLine("</table></body></html>");
        }
        
        // Event handler for double-clicking on a DataGrid row
        private void ReadingsGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            // Check if a row is selected
            if (ReadingsGrid.SelectedItem is not null)
            {
                // Cast the selected item to the appropriate type
                var selectedRecord = (HealthRecord)ReadingsGrid.SelectedItem;

                // Populate the fields in the form
                _viewModel.UserName = selectedRecord.Name;
                _viewModel.BirthDate = selectedRecord.BirthDate;
                BirthDatePicker.SelectedDate = selectedRecord.BirthDate;
                // BirthDatePicker.Text = selectedRecord.BirthDate.ToString("yyyy-MM-dd");
                _viewModel.Systolic = selectedRecord.Systolic;
                _viewModel.Diastolic = selectedRecord.Diastolic;
                _viewModel.Pulse = selectedRecord.Pulse;
                _viewModel.ReadingDate = selectedRecord.ReadingDate;
                // _viewModel.ReadingTime = selectedRecord.ReadingTime.ToString();
                _viewModel.ReadingTime = selectedRecord.ReadingTime.ToString(@"hh\:mm");
                
                _viewModel.Standing = selectedRecord.Standing;
            }
        }
        
    }
}