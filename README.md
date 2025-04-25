# BPLog

### A simple C# WPF app to record blood pressure readings in SQLite database, then export as text or HTML.
### Features
- Add, edit, and delete blood pressure readings.
- View readings in a simple table format.
- Export readings to text or HTML format.
- Sort readings by date or value.

### Usage

1.  **User Information**:
    *   On first run, you will be prompted to enter your `Name` and `Birth Date`. This information may be used in exports.

2.  **Adding a Reading**:
    *   Navigate to the section for adding new readings.
    *   Enter the `Systolic` pressure, `Diastolic` pressure, and `Pulse`.
    *   Select the `Date` and `Time` of the reading.
    *   Indicate if the reading was taken while `Standing`.
    *   Save the reading.

3.  **Viewing Readings**:
    *   Readings are displayed in a table.
    *   You can sort the readings by clicking on column headers (e.g., Date, Systolic).

4.  **Editing/Deleting Readings**:
    *   Select a reading from the table.
    *   Use the appropriate buttons or menu options to `Edit` or `Delete` the selected reading.

5.  **Exporting Readings**:
    *   Choose the export option (Text or HTML).
    *   The application will generate a file containing your readings.
    *   The application keeps track of the last export date and time.

### Data Storage

*   Your blood pressure readings and user information are stored locally in a SQLite database file.
*   Application settings are stored in a `settings.json` file.

### To Do
1. **Data Visualization**: Add charts to visualize blood pressure trends over time
2. **Filtering/Sorting**: Implement more advanced filtering options for your readings
3. **Data Export**: Expand export capabilities to include CSV or PDF formats
4. **Reminder System**: Add a reminder system for taking blood pressure readings
5. **User Profiles**: Support multiple user profiles if needed for family members
6. **Statistical Analysis**: Add basic statistical analysis (averages, trends, etc.)

