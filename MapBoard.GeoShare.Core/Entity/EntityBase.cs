using System.ComponentModel.DataAnnotations;

namespace MapBoard.GeoShare.Core.Entity
{
    public class EntityBase
    {
        [Key]
        public int Id { get; set; }
    }
}