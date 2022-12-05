using Contracts;
using System.Collections.ObjectModel;

namespace Server.Database
{
    public interface IDatabase
    {
        Task<ImageInfo> PostImage(byte[] byteSource, string path, CancellationToken ctn);

        ObservableCollection<ImageInfo> GetAllImages();

        ImageInfo GetImageById(int id);

        ObservableCollection<ImageInfo> GetImagesByEmotion(string emotion);

        void DeleteAllImages();

        public void DeleteImageById(int id);
    }
}
