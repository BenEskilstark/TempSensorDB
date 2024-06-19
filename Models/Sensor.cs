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

    public int LocationID { get; set; }
    [ForeignKey("LocationID")]
    public virtual Location Location { get; set; }

    public int FarmID { get; set; }
    [ForeignKey("FarmID")]
    public virtual Farm Farm { get; set; }

    public virtual ICollection<TempReading> TempReadings { get; set; }
}