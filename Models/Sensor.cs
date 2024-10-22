using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TempSensorDB.Models;

public class Sensor
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int SensorID { get; set; }

    [Required]
    public required string Name { get; set; }

    // How many degrees F to adjust readings when they are displayed
    public double CalibrationValueF { get; set; } = 0;

    // Optional minimum and maximum acceptable temperatures
    public double? MinTempF { get; set; }
    public double? MaxTempF { get; set; }

    // Sensors can provide a heartbeat that just says the sensor is connected
    // but doesn't provide temperatures -- in case they can't read from their 
    // temperature sensors but we still want to know whether they are connected
    public DateTime? LastHeartbeat { get; set; }

    // The most recent temperature reading. Stored here for faster lookup 
    // since the most recent reading is the most important by far. 
    public double? LastTempF { get; set; }
    public DateTime? LastTimeStamp { get; set; }

    public int FarmID { get; set; }
    [ForeignKey("FarmID")]
    public virtual Farm Farm { get; set; }

    public virtual ICollection<Reading> Readings { get; set; }
}