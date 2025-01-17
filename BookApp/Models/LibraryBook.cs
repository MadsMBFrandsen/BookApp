using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookApp.Models
{
    public class LibraryBook
    {
        public string Title { get; set; }
        public ObservableCollection<Chapter> Chapters { get; set; } = new();
        public string FirstChapter { get; set; }
        public string LastChapter { get; set; }
        public int ChaptersCount => Chapters.Count;
    }
}
