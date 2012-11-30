using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Simple.Json.Tests.TypedJsonObjects
{
    public class Image
    {
        public int Width { get; set; }
        public int Height { get; set; }
        public string Title { get; set; }
        public Thumbnail Thumbnail { get; set; }
        public long[] Ids { get; set; }
        public Optional<bool?> Visible { get; set; }
        public Optional<bool?> Archived { get; set; }
        public string Creator { get; set; }
        public Optional<double?> Scale { get; set; }
        public Optional<double?> Rotation { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? ModifiedAt { get; set; }
    }
}