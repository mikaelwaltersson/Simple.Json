using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FrameworkPerformanceTests.TypedJsonObjects
{
    public class Image : IEquatable<Image>
    {
        public int Width { get; set; }
        public int Height { get; set; }
        public string Title { get; set; }
        public Thumbnail Thumbnail { get; set; }
        public double?[] Ids { get; set; }
        public bool? Visible { get; set; }
        public bool? Archived { get; set; }
        public string Creator { get; set; }
        public double? Scale { get; set; }
        public double? Rotation { get; set; }
        public string CreatedAt { get; set; }
        public string ModifiedAt { get; set; }
        

        public bool Equals(Image other)
        {
            return
                Width == other.Width &&
                Height == other.Height &&
                Title == other.Title &&
                EqualityComparer<Thumbnail>.Default.Equals(Thumbnail, other.Thumbnail) &&
                (Ids == other.Ids || (Ids != null && Ids.SequenceEqual(other.Ids))) &&
                Visible == other.Visible &&
                Archived == other.Archived &&
                Creator == other.Creator &&
                Scale == other.Scale &&
                Rotation == other.Rotation &&
                CreatedAt == other.CreatedAt &&
                ModifiedAt == other.ModifiedAt;
        }
    }
}