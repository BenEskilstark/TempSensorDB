using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TempSensorDB.Models;

public class Reading
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int ReadingID { get; set; }

    public double TempF { get; set; }

    public double Humidity { get; set; }

    public DateTime TimeStamp { get; set; }

    public int SensorID { get; set; }
    [ForeignKey("SensorID")]
    public virtual Sensor Sensor { get; set; }
}