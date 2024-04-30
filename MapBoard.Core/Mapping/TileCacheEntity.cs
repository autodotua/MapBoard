using Microsoft.EntityFrameworkCore;
using System;
using System.ComponentModel.DataAnnotations;

namespace MapBoard.Mapping
{
    [Index("X", "Y", "Z", "TemplateUrl")]
    public class TileCacheEntity
    {
        [Key]
        public int Id { get; set; }

        public string TileUrl { get; set; }

        public string TemplateUrl { get; set; }

        public int X { get; set; }

        public int Y { get; set; }

        public int Z { get; set; }

        public byte[] Data { get; set; }
    }

}
