using System;
using System.Collections.Generic;
using System.Text;

namespace Elton.CommentConvert
{
    /// <summary>
    /// 
    /// </summary>
    public class Node
    {
        public string Tag { get; set; }
        public string Name { get; set; }
        readonly List<string> contents = new List<string>();

        public Node(string tag, string name)
        {
            this.Tag = tag;
            this.Name = name;
        }
        public List<string> Contents => contents;

        public string FullName => $"{Tag}:{Name}";
    }
}
