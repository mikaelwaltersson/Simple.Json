using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FrameworkPerformanceTests.TypedJsonObjects
{
    public class ImageRequest : IEquatable<ImageRequest>
    {
        public Image[] Images { get; set; }

        
        public bool Equals(ImageRequest other)
        {
            return Images == other.Images || (Images != null && Images.SequenceEqual(other.Images));
        }
    }
}