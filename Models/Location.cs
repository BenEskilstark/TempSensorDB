using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TempSensorDB.Models;

public class Location
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int LocationID { get; set; }

    [Required]
    public required string Name { get; set; }

    // E.g. Essex vs Echo
    public int FarmID { get; set; }
    [ForeignKey("FarmID")]
    public virtual Farm Farm { get; set; }

    public double? MinTempF { get; set; }
    public double? MaxTempF { get; set; }

    public virtual ICollection<Sensor> Sensors { get; set; }
}