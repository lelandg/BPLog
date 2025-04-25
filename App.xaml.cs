using System.Windows;

namespace BPLog;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    // Make the ReminderService accessible globally
    // For more complex applications, consider Dependency Injection
    public static ReminderService? ReminderServiceInstance { get; private set; }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Initialize and start the reminder service
        try
        {
            ReminderServiceInstance = new ReminderService();
        
            // Set the next reminder at startup
            ReminderServiceInstance.Start();
        
            Console.WriteLine("App: ReminderService started with the next reminder configured.");
        }
        catch (Exception ex)
        {
            // Log or display the error appropriately
            MessageBox.Show($"Failed to initialize the Reminder Service: {ex.Message}",
                "Reminder Error", MessageBoxButton.OK, MessageBoxImage.Error);
            ReminderServiceInstance = null; // Ensure it's null if init failed
        }

        // Initialize other parts of your application, like the main window if needed
    }

    protected override void OnExit(ExitEventArgs e)
    {
        // Dispose of the reminder service when the application closes
        ReminderServiceInstance?.Dispose();
        Console.WriteLine("App: ReminderService disposed.");

        base.OnExit(e);
    }
}