namespace HomeMaids.Services;

public class BookingSettings
{
    public decimal TaxPercent { get; set; } = 15;
    public int MinHoursPerBooking { get; set; } = 2;
    public int MaxHoursPerBooking { get; set; } = 12;
    public int CancellationWindowHours { get; set; } = 6;
}
