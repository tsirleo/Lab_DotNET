using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;

namespace Contracts
{
    public class ImageInfo
    {
        [Key]
        public int imageId { get; set; }
        public string fileName { get; set; }
        public string imgPath { get; set; }
        public byte[] hashCode { get; set; }
        public ImageBLOB image { get; set; }

        public string emotionsJSON
        {
            get { return JsonConvert.SerializeObject(emotionsDict); }
            set { emotionsDict = new Dictionary<string, double>(JsonConvert.DeserializeObject<Dictionary<string, double>>(value)); }
        }

        [NotMapped]
        public Dictionary<string, double> emotionsDict { get; set; }

        public ImageInfo() { }

        public ImageInfo(string name, string path, byte[] hash, Dictionary<string, double> dict)
        {
            fileName = name;
            imgPath = path;
            hashCode = hash;
            emotionsDict = new Dictionary<string, double>(dict);
        }

        public void SetBlob(byte[] source)
        {
            image = new ImageBLOB(source);
        }
    }

    public class ImageBLOB
    {
        [Key]
        public int blobId { get; set; }
        [ForeignKey(nameof(ImageInfo))]
        public int imageInfoId;
        public byte[] blob { get; set; }

        public ImageBLOB() {}

        public ImageBLOB(byte[] source)
        {
            blob = source;
        }
    }
}