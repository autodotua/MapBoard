using MapBoard.IO;
using Microsoft.EntityFrameworkCore;
using System;
using System.ComponentModel.DataAnnotations;

namespace MapBoard.Mapping
{
    [Index(nameof(X), nameof(Y), nameof(Z), nameof(TemplateUrl))]
    public class TileCacheEntity : EntityBase
    {
        public string TileUrl { get; set; }

        public string TemplateUrl { get; set; }

        public int X { get; set; }

        public int Y { get; set; }

        public int Z { get; set; }

        public byte[] Data { get; set; }
    }

}
