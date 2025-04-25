using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Threading;
using System.IO;
using System.Text.Json;

public class ReminderService : IDisposable
{
    private DispatcherTimer _scheduledReminderTimer;
    // Removed: private DispatcherTimer _initialReminderTimer;

    private List<TimeSpan> _reminderTimes;
    private bool _reminderEnabled;
    // Removed: private int _initialIntervalMinutes; // No longer needed for timer logic
    private bool _hasTakenFirstReading;

    private DateTime? _lastNotificationTime;

    private const bool DefaultReminderEnabled = true;
    private static readonly List<TimeSpan> DefaultReminderTimes = [new TimeSpan(7, 0, 0), new TimeSpan(15, 0, 0)];
    // Removed: DefaultInitialIntervalMinutes constant

    public bool IsEnabled => _reminderEnabled;
    public IReadOnlyList<TimeSpan> ReminderTimes => _reminderTimes?.AsReadOnly() ?? DefaultReminderTimes.AsReadOnly();
    // Removed: InitialIntervalMinutes property
    public bool HasTakenFirstReading => _hasTakenFirstReading;

    public ReminderService()
    {
        _reminderEnabled = LoadSetting(nameof(_reminderEnabled), DefaultReminderEnabled);
        _reminderTimes = LoadSetting(nameof(_reminderTimes), new List<TimeSpan>(DefaultReminderTimes));
        // Removed loading for _initialIntervalMinutes
        _hasTakenFirstReading = LoadSetting(nameof(_hasTakenFirstReading), false);

        if (_reminderTimes == null || !_reminderTimes.Any())
        {
            _reminderTimes = new List<TimeSpan>(DefaultReminderTimes);
        }

        _scheduledReminderTimer = new DispatcherTimer
        {
            // Check every minute for scheduled times
            Interval = TimeSpan.FromMinutes(1)
        };
        _scheduledReminderTimer.Tick += ScheduledReminderTimer_Tick;

        // Removed _initialReminderTimer initialization and related logic
        Console.WriteLine("Reminder Service Initialized.");
    }

    public void Start()
    {
        Stop(); // Stop any existing timer first

        if (!_reminderEnabled)
        {
            Console.WriteLine("Reminder Service: Disabled, not starting timer.");
            return;
        }

        // Determine the next reminder time based on the current time
        DateTime now = DateTime.Now;
        var nextReminder = _reminderTimes
            .Select(time => new DateTime(now.Year, now.Month, now.Day, time.Hours, time.Minutes, 0))
            .FirstOrDefault(dateTime => dateTime > now); // Find the next time today

        if (nextReminder == default) // No reminders left today, pick the earliest time tomorrow
        {
            nextReminder = _reminderTimes
                .Select(time => new DateTime(now.Year, now.Month, now.Day, time.Hours, time.Minutes, 0).AddDays(1))
                .First();
        }

        TimeSpan intervalUntilNextReminder = nextReminder - now;

        // Update the timer to start at the next reminder time
        _scheduledReminderTimer.Interval = intervalUntilNextReminder;
        _scheduledReminderTimer.Start();

        Console.WriteLine($"Reminder Service: Initial timer set for {nextReminder}.");
        
        // Check immediately if reminders are relevant
        CheckScheduledReminders(DateTime.Now);
    }

    public void Stop()
    {
        _scheduledReminderTimer?.Stop();
        _lastNotificationTime = null;
        Console.WriteLine("Reminder Service: Stopped timer.");
    }

    public void NotifyFirstReadingTaken()
    {
        if (!_hasTakenFirstReading)
        {
            Console.WriteLine("Reminder Service: First reading detected.");
            _hasTakenFirstReading = true;
            SaveSetting(nameof(_hasTakenFirstReading), _hasTakenFirstReading);

            // No timer switching needed anymore. The existing _scheduledReminderTimer
            // will now show the regular notifications instead of the initial ones.
            Console.WriteLine("Reminder Service: Subsequent reminders will be standard notifications.");
        }
        else
        {
            Console.WriteLine("Reminder Service: NotifyFirstReadingTaken called, but first reading was already recorded.");
        }
    }

    private void ScheduledReminderTimer_Tick(object? sender, EventArgs e)
    {
        // This timer now handles both initial and scheduled reminders based on _hasTakenFirstReading flag
        if (!_reminderEnabled) return;

        CheckScheduledReminders(DateTime.Now);
    }

    private void CheckScheduledReminders(DateTime currentTime)
    {
        TimeSpan now = currentTime.TimeOfDay;

        foreach (var reminderTime in _reminderTimes)
        {
            // Check if the current time matches a scheduled time (ignoring seconds)
            if (now.Hours == reminderTime.Hours && now.Minutes == reminderTime.Minutes)
            {
                // Prevent duplicate notifications if the timer ticks again within the same minute
                if (!_lastNotificationTime.HasValue || (currentTime - _lastNotificationTime.Value) >= TimeSpan.FromSeconds(59))
                {
                    if (_hasTakenFirstReading)
                    {
                        Console.WriteLine($"Reminder Service: Triggering scheduled reminder for {reminderTime}.");
                        ShowScheduledReminderNotification(reminderTime);
                    }
                    else
                    {
                        Console.WriteLine($"Reminder Service: Triggering initial reminder nudge at scheduled time {reminderTime}.");
                        ShowInitialReminderNotification(); // Show the initial nag notification
                    }
                    _lastNotificationTime = currentTime;
                }
                // Found a matching time, no need to check further times for this tick
                break;
            }
        }
    }

    // Removed InitialReminderTimer_Tick method

    private void ShowScheduledReminderNotification(TimeSpan reminderTime)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
             if (Application.Current.MainWindow != null)
                MessageBox.Show(Application.Current.MainWindow,
                            $"Time to take your blood pressure reading (Scheduled: {reminderTime:hh\\:mm})!",
                            "Blood Pressure Reminder", MessageBoxButton.OK, MessageBoxImage.Information);
        });
    }

    private void ShowInitialReminderNotification()
    {
        // Updated message to remove the interval concept
         var scheduledTimesString = string.Join(", ", _reminderTimes.Select(t => t.ToString("hh\\:mm")));
        Application.Current.Dispatcher.Invoke(() =>
        {
            if (Application.Current.MainWindow != null)
                MessageBox.Show(Application.Current.MainWindow,
                    $"Please take and record your first blood pressure reading. Reminders will occur at scheduled times ({scheduledTimesString}) until recorded.",
                    "Blood Pressure Reminder", MessageBoxButton.OK, MessageBoxImage.Warning);
        });
    }

    // Updated method signature: removed initialInterval parameter
    public void UpdateReminderSettings(bool enabled, List<TimeSpan> times)
    {
        Console.WriteLine("Reminder Service: Updating settings...");

        var newTimes = (times != null && times.Any())
                       ? new List<TimeSpan>(times)
                       : new List<TimeSpan>(DefaultReminderTimes);

        bool needsRestart = _reminderEnabled != enabled;
        bool settingsChanged = _reminderEnabled != enabled ||
                               !_reminderTimes.SequenceEqual(newTimes);

        _reminderEnabled = enabled;
        _reminderTimes = newTimes;
        // Removed _initialIntervalMinutes update

        SaveSetting(nameof(_reminderEnabled), _reminderEnabled);
        SaveSetting(nameof(_reminderTimes), _reminderTimes);
        // Removed saving _initialIntervalMinutes

        // Removed UpdateInitialTimerInterval call

        if (settingsChanged || needsRestart)
        {
            Console.WriteLine("Reminder Service: Settings changed or state requires restart, restarting service logic.");
            // Restart will re-evaluate enabled status and start/stop the single timer
            Start();
        }
        else
        {
            Console.WriteLine("Reminder Service: Settings updated, no restart needed.");
        }
    }

    // Removed UpdateInitialTimerInterval method

    private T LoadSetting<T>(string key, T defaultValue)
    {
        try
        {
            string configFilePath = Path.Combine(AppContext.BaseDirectory, "settings.json");
            if (!File.Exists(configFilePath)) return defaultValue;

            var json = File.ReadAllText(configFilePath);
            // Consider using a specific settings class for robustness instead of Dictionary<string, object>
            var settings = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);

            if (settings != null && settings.TryGetValue(key, out var jsonElement))
            {
                // Attempt to deserialize the specific element
                 // Need System.Text.Json options potentially if defaults aren't working
                return JsonSerializer.Deserialize<T>(jsonElement.GetRawText()) ?? defaultValue;
            }
        }
        catch (Exception ex)
        {
            // More specific exception handling could be useful (FileNotFound, JsonException, etc.)
            Console.WriteLine($"Error loading setting '{key}': {ex.Message}. Using default value.");
            // Optionally: Log the exception details (ex.ToString()) for debugging
        }
        return defaultValue;
    }


    private void SaveSetting<T>(string key, T value)
    {
        try
        {
            string configFilePath = Path.Combine(AppContext.BaseDirectory, "settings.json");
            Dictionary<string, object> settings = new();

            if (File.Exists(configFilePath))
            {
                try
                {
                     var json = File.ReadAllText(configFilePath);
                     // Handle potential empty or invalid JSON file
                     if(!string.IsNullOrWhiteSpace(json))
                     {
                        settings = JsonSerializer.Deserialize<Dictionary<string, object>>(json) ?? new Dictionary<string, object>();
                     }
                }
                catch (JsonException jsonEx)
                {
                     Console.WriteLine($"Error deserializing existing settings file: {jsonEx.Message}. Starting with empty settings.");
                     settings = new Dictionary<string, object>(); // Reset settings if file is corrupt
                }
                catch (IOException ioEx) // Catch file access issues
                {
                     Console.WriteLine($"Error reading existing settings file: {ioEx.Message}. Cannot save setting '{key}'.");
                    return; // Exit if we can't read the file
                }
            }

            settings[key] = value; // Update or add the setting

             try
             {
                 File.WriteAllText(configFilePath, JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true }));
             }
             catch (IOException ioEx) // Catch file writing issues
             {
                 Console.WriteLine($"Error writing settings file: {ioEx.Message}. Setting '{key}' may not be saved.");
             }
        }
        catch (Exception ex) // Catch-all for unexpected errors during the save process
        {
            // Log the generic exception, less specific but catches broader issues
            Console.WriteLine($"Unexpected error saving setting '{key}': {ex.Message}");
             // Optionally: Log ex.ToString() for full details
        }
    }


    private string FormatValueForLog(object? value) // Keep for potential future logging needs
    {
        if (value is null) return "null";
        if (value is IEnumerable<TimeSpan> times) return $"[{string.Join(", ", times)}]";
        return value.ToString() ?? "null";
    }

    private bool _disposed = false;

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                Console.WriteLine("Reminder Service: Disposing managed resources.");
                Stop(); // Stops the scheduled timer
                if (_scheduledReminderTimer != null)
                {
                    _scheduledReminderTimer.Tick -= ScheduledReminderTimer_Tick;
                    // No need to explicitly set timer to null if the class instance is being disposed
                    // GC will handle it, but setting to null is harmless.
                     _scheduledReminderTimer = null!;
                }
                // Removed disposal for _initialReminderTimer
            }

            // Clear potentially large list
            _reminderTimes = null!;

            _disposed = true;
            Console.WriteLine("Reminder Service: Disposal complete.");
        }
    }

     // Destructor (Finalizer) - only necessary if you have unmanaged resources
     // ~ReminderService()
     // {
     //     Dispose(false);
     // }
}