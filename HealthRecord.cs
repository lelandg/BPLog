public class HealthRecord
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime BirthDate { get; set; }
    public int Systolic { get; set; }
    public int Diastolic { get; set; }
    public int Pulse { get; set; }
    public DateTime ReadingDate { get; set; } = DateTime.Now;
    public bool Standing { get; set; }

    public TimeSpan ReadingTime
    {
        get => ReadingDate.TimeOfDay;
        set => ReadingDate = ReadingDate.Date.Add(value);
    }

    public string Header => $"Reading on {ReadingDate:d}";

    public override string ToString()
    {
        return $"{ReadingDate:MM-dd-yyyy} {ReadingTime:hh\\:mm} " +
               $"{Systolic}/{Diastolic} " +
               $"Pulse: {Pulse} " +
               $"{(Standing ? "Standing" : "")}";
    }
}