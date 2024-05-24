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

    public int LocationID { get; set; }
    [ForeignKey("LocationID")]
    public virtual Location Location { get; set; }

    public virtual ICollection<TempReading> TempReadings { get; set; }
}