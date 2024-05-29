using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TempSensorDB.Models;

public class Location
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int LocationID { get; set; }

    // E.g. Essex vs Echo
    [Required]
    public required string Farm { get; set; }

    [Required]
    public required string Name { get; set; }

    public double? MinTempF { get; set; }
    public double? MaxTempF { get; set; }

    public virtual ICollection<Sensor> Sensors { get; set; }
}