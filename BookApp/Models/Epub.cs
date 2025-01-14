using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookApp.Models
{
    public class Epub
    {
        public string Filepath { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public List<Chapter> Chapters { get; set; } = new List<Chapter>();
        public int StartNumber { get; set; } = 0;
        public int EndNumber { get; set; } = 0;
        public int WordCount
        {
            get
            {
                return Chapters?.Sum(chapter => chapter.WordCount) ?? 0;
            }
        }
        public bool NeedStartNumbers { get; set; }
    }
}
