﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookApp.Models
{
    public class Chapter
    {
        public string Title { get; set; }
        public string Content { get; set; }
        public int WordCount
        {
            get
            {
                return string.IsNullOrWhiteSpace(Content)
                    ? 0
                    : Content.Split(new char[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).Length;
            }
        }
    }
}
