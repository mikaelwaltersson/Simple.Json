using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FrameworkPerformanceTests.TypedJsonObjects
{
    public class Thumbnail : IEquatable<Thumbnail>
    {
        public string Url { get; set; }        
        public double? Height { get; set; }
        public double? Width { get; set; }


        public bool Equals(Thumbnail other)
        {
            return 
                Url == other.Url && 
                Height == other.Height && 
                Width == other.Width;
        }
    }
}