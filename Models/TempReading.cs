using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TempSensorDB.Models;

public class TempReading
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int TempReadingID { get; set; }

    public double TempF { get; set; }

    public DateTime TimeStamp { get; set; }

    public int SensorID { get; set; }
    [ForeignKey("SensorID")]
    public virtual Sensor Sensor { get; set; }
}