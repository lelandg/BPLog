using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace BloodPressureApp
{
    public partial class MainWindow : Window
    {
        private string _connectionString = "Data Source=bloodpressure.db";

        public MainWindow()
        {
            InitializeComponent();
            InitializeDatabase();
            LoadReadings();
        }

        private void InitializeDatabase()
        {
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

            if (string.IsNullOrWhiteSpace(SystolicInput.Text) || 
                string.IsNullOrWhiteSpace(DiastolicInput.Text) ||
                string.IsNullOrWhiteSpace(PulseInput.Text))
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
            string position = StandingCheckbox.IsChecked == true ? "Standing" : "Sitting";
            string insertReadingQuery = @"INSERT INTO Readings (UserId, Systolic, Diastolic, Pulse, ReadingTime, Position) 
                                         VALUES (@userId, @systolic, @diastolic, @pulse, @readingTime, @position)";
            using var insertReadingCmd = new SqliteCommand(insertReadingQuery, connection);
            insertReadingCmd.Parameters.AddWithValue("@userId", userId);
            insertReadingCmd.Parameters.AddWithValue("@systolic", SystolicInput.Text);
            insertReadingCmd.Parameters.AddWithValue("@diastolic", DiastolicInput.Text);
            insertReadingCmd.Parameters.AddWithValue("@pulse", PulseInput.Text);
            var date = ReadingDatePicker.SelectedDate ??= DateTime.Now;
            date = date.Date.Add(TimeSpan.Parse(ReadingTimeInput.Text));
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
                UserNameInput.Text = selectedRecord.Name;
                BirthDatePicker.SelectedDate = selectedRecord.BirthDate;
                // BirthDatePicker.Text = selectedRecord.BirthDate.ToString("yyyy-MM-dd");
                SystolicInput.Text = selectedRecord.Systolic.ToString();
                DiastolicInput.Text = selectedRecord.Diastolic.ToString();
                PulseInput.Text = selectedRecord.Pulse.ToString();
                ReadingDatePicker.SelectedDate = selectedRecord.ReadingDate;
                ReadingDatePicker.Text = selectedRecord.ReadingDate.ToString("yyyy-MM-dd");
                ReadingTimeInput.Text = selectedRecord.ReadingTime.ToString();
                ReadingTimeInput.Text = selectedRecord.ReadingTime.ToString(@"hh\:mm");
                StandingCheckbox.IsChecked = selectedRecord.Standing;
            }
        }
        
    }
}