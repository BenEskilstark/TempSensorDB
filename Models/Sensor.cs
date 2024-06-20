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

    public int FarmID { get; set; }
    [ForeignKey("FarmID")]
    public virtual Farm Farm { get; set; }

    public virtual ICollection<Reading> Readings { get; set; }
}