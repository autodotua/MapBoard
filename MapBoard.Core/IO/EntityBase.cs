using Microsoft.EntityFrameworkCore;
using System;
using System.ComponentModel.DataAnnotations;

namespace MapBoard.IO
{
    [Index("X", new[] { "Y", "Z", "TemplateUrl" })]
    public class EntityBase
    {
        [Key]
        public int Id { get; set; }
    }
}