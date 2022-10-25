using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Concurrent;

namespace WPFApp
{
    public class ImageData
    {
        public string fileName { get; set; }
        public string imgPath { get; set; }
        public Dictionary<string, double> emotionsDict { get; set; } 

        public ImageData(string name, string path, Dictionary<string, double> dict)
        {
            fileName = name;
            imgPath = path;
            emotionsDict = new Dictionary<string, double>(dict);
        }
    }
}
