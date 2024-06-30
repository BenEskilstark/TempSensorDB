
namespace TempSensorDB.Models.DataTransfer;

public class FarmDTO
{
    public int FarmID { get; set; }
    public string Name { get; set; }
}


public class SensorDTO
{
    public int SensorID { get; set; }
    public required string Name { get; set; }
    public double CalibrationValueF { get; set; } = 0;
    public double? LastTempF { get; set; }
    public DateTime? LastTimeStamp { get; set; }
}


public class ReadingDTO
{
    public int SensorID { get; set; }
    public string Password { get; set; }
    public double TempF { get; set; }
    public double Humidity { get; set; }
    public DateTime TimeStamp { get; set; }
}

