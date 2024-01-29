using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;

namespace HA.Store;

public class MeasurementEntity
{
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Key, Column(Order = 0)]
    public int Id { get; set; }

    [NotNull]
    public DateTime CreatedOn { get; set; } = DateTime.Now;

    [NotNull]
    public string LineProtocol { get; set; } = string.Empty;

    public override string ToString()
    {
        return $"Id: {Id} | CreateOn: {CreatedOn.ToLongDateString} | Measurement: {LineProtocol}";
    }
}