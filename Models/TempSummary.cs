using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TempSensorDB.Models;

public class TempSummary
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int TempSummaryID { get; set; }

    public double TempF { get; set; }

    public DateTime TimeStamp { get; set; }

    public string TimeCategory { get; set; }

    public int SensorID { get; set; }
    [ForeignKey("SensorID")]
    public virtual Sensor Sensor { get; set; }
}