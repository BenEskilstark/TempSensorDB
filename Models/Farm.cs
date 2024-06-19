using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TempSensorDB.Models;

public class Farm
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int FarmID { get; set; }

    [Required]
    public required string Name { get; set; }

    [Required]
    public required string Password { get; set; }


    public virtual ICollection<Sensor> Sensors { get; set; }
    public virtual ICollection<Location> Locations { get; set; }

}