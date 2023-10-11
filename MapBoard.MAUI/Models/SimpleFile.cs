using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MapBoard.Models
{
    public class SimpleFile
    {
        public SimpleFile()
        {

        }
        public SimpleFile(string path)
        {
            FileInfo file = new FileInfo(path); 
            FullName = file.FullName;
            Length = file.Length;
            Name = Path.GetFileNameWithoutExtension(FullName);
            Time = file.LastWriteTime;
        }

        public string FullName { get; set; }
        public string Name { get; set; }
        public long Length { get; set; }
        public DateTime Time { get; set; }
    }
}
