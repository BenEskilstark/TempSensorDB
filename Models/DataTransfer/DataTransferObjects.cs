
namespace TempSensorDB.Models.DataTransfer;

using TempSensorDB.WebApplication;

public class FarmDTO
{
    public static FarmDTO FromFarm(Farm f)
    {
        return new FarmDTO() { FarmID = f.FarmID, Name = f.Name };
    }

    public int FarmID { get; set; }
    public string Name { get; set; }
}


public class SensorDTO
{
    public static SensorDTO FromSensor(Sensor s)
    {
        Reading? lastReading = s.Readings.OrderByDescending(r => r.TimeStamp).FirstOrDefault();
        return new SensorDTO()
        {
            SensorID = s.SensorID,
            Name = s.Name,
            CalibrationValueF = s.CalibrationValueF,
            LastTempF = lastReading?.TempF,
            LastTimeStamp = lastReading != null
                ? DateTime.SpecifyKind(lastReading.TimeStamp, DateTimeKind.Utc)
                : null,
            MinTempF = s.MinTempF,
            MaxTempF = s.MaxTempF,
        };
    }

    public int SensorID { get; set; }
    public required string Name { get; set; }
    public double CalibrationValueF { get; set; } = 0;
    public double? LastTempF { get; set; }
    public DateTime? LastTimeStamp { get; set; }
    public double? MinTempF { get; set; }
    public double? MaxTempF { get; set; }

}


public class ReadingDTO
{
    public Reading ToReading()
    {
        return new Reading()
        {
            TempF = this.TempF,
            Humidity = this.Humidity,
            SensorID = this.SensorID,
            TimeStamp = this.TimeStamp,
        };
    }

    public int SensorID { get; set; }
    public string Password { get; set; }
    public double TempF { get; set; }
    public double Humidity { get; set; }
    public DateTime TimeStamp { get; set; }
}

