public class HealthRecord
{
    public string Name { get; set; }
    public DateTime BirthDate { get; set; }
    public int Systolic { get; set; }
    public int Diastolic { get; set; }
    public int Pulse { get; set; }
    public DateTime ReadingDate { get; set; }
    public TimeSpan? ReadingTime { get; set; }
    public bool Standing { get; set; }
    
    protected internal string Header
    {
        get
        {
            return $"{Name} {BirthDate:yyyy-MM-dd} ";
        }
    }
    
    public override string ToString()
    {
        return $"{ReadingDate:MM-dd-yyyy} {ReadingTime.ToString().Substring(0, 5)} " +
               // $"Name = {Name}, " +
               // $"BirthDate = {BirthDate:yyyy-MM-dd}, " +
               $"{Systolic}/{Diastolic} " +
               $"Pulse: {Pulse} " +
               $"{(Standing ? "Standing" : "")}";
    }    
}