using Microsoft.EntityFrameworkCore;
using Contracts;
using System.Net;
using System.Collections.ObjectModel;
using ModelLib;
using System.Security.Cryptography;

namespace Server.Database
{
    public class DBContext : DbContext
    {
        public DbSet<ImageInfo> imgInfo { get; set; }
        public DbSet<ImageBLOB> imgData { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder o) =>
            o.UseSqlite("Data Source=Processed_Images.db");
    }

    public class DatabaseManager : IDatabase
    {
        private static EmotionDef emotionDef = new EmotionDef();
        private HashAlgorithm hashAlg = MD5.Create();

        private bool CorrectEmotionString(string emotionString)
        {
            if (string.Equals(emotionString, "anger") || string.Equals(emotionString, "contempt") || string.Equals(emotionString, "disgust")
                || string.Equals(emotionString, "fear") || string.Equals(emotionString, "happiness") || string.Equals(emotionString, "neutral")
                    || string.Equals(emotionString, "sadness") || string.Equals(emotionString, "surprise"))
                return true;

            return false;
        }

        private bool ContainsAllKeys(Dictionary<string, double> dict)
        {
            if (dict.ContainsKey("anger") && dict.ContainsKey("contempt") && dict.ContainsKey("disgust") && dict.ContainsKey("fear")
                && dict.ContainsKey("happiness") && dict.ContainsKey("neutral") && dict.ContainsKey("sadness") && dict.ContainsKey("surprise"))
                return true;

            return false;
        }

        private bool CheckImgNotExistDB(byte[] hash, byte[] source)
        {
            using (var db = new DBContext())
            {
                if (db.imgInfo.Any(x => Equals(x.hashCode, hash)))
                {
                    var query = db.imgInfo.Where(x => Equals(x.hashCode, hash)).Include(item => item.image);

                    if (query.Any(x => Equals(x.image.blob, source)))
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        private byte[] GetHash(byte[] source) => hashAlg.ComputeHash(source);

        public async Task<ImageInfo> PostImage(byte[] byteSource, string path, CancellationToken ctn)
        {
            try
            {
                var hash = GetHash(byteSource);

                if (CheckImgNotExistDB(hash, byteSource))
                {
                    var resultDict = await emotionDef.ProcessAnImage(byteSource, ctn);

                    if (resultDict != null && ContainsAllKeys(resultDict))
                    {
                        var imgInstance = new ImageInfo(Path.GetFileName(path), path, hash, resultDict);
                        imgInstance.SetBlob(byteSource);

                        if (imgInstance != null)
                        {
                            using (var db = new DBContext())
                            {
                                db.imgInfo.Add(imgInstance);
                                db.SaveChanges();
                                var imgPost = db.imgInfo.Include(item => item.image).Last();

                                return imgPost;
                            }
                        }
                    }
                }

                return null;
            }
            catch (OperationCanceledException ex)
            {
                throw new OperationCanceledException(DateTime.Now + "\n" + "Data processing has been canceled.");
            }
            catch (Exception ex)
            {
                throw new Error("Error happened while writing image information to DB.", HttpStatusCode.InternalServerError);
            }
        }

        public ObservableCollection<ImageInfo> GetAllImages()
        {
            try
            {
                using (var db = new DBContext())
                {
                    var images = db.imgInfo.Include(item => item.image).ToList();

                    return new ObservableCollection<ImageInfo>(images);
                }
            }
            catch (Exception ex)
            {
                throw new Error("Error happened while getting images information from DB.", HttpStatusCode.InternalServerError);
            }
        }

        public ImageInfo GetImageById(int id)
        {
            try
            {
                using (var db = new DBContext())
                {
                    var image = db.imgInfo.Where(item => Equals(item.imageId, id)).Include(item => item.image).FirstOrDefault();

                    if (image == null)
                    {
                        throw new Error("There is no image with such id in DB", HttpStatusCode.BadRequest);
                    }

                    return image;
                }
            }
            catch (Error err)
            {
                throw err;
            }
            catch (Exception ex)
            {
                throw new Error("Error happened while getting image information by idfrom DB.", HttpStatusCode.InternalServerError);
            }
        }

        public ObservableCollection<ImageInfo> GetImagesByEmotion(string emotion)
        {
            try
            {
                if (CorrectEmotionString(emotion))
                {
                    using (var db = new DBContext())
                    {
                        var images = db.imgInfo
                            .Include(item => item.image)
                            .ToList()
                            .Where(collection => Equals(collection.emotionsDict.ElementAt(0).Key, emotion));

                        return new ObservableCollection<ImageInfo>(images);
                    }
                }

                throw new Error("There is no such emotion in image data", HttpStatusCode.BadRequest);
            }
            catch (Error err)
            {
                throw err;
            }
            catch (Exception ex)
            {
                throw new Error("Error happened while getting images information by emotion from DB.", HttpStatusCode.InternalServerError);
            }
        }

        public void DeleteAllImages()
        {
            try
            {
                using (var db = new DBContext())
                {
                    var images = db.imgInfo.Include(x => x.image);

                    db.Database.ExecuteSqlRaw("DELETE FROM [imgInfo]");
                    db.Database.ExecuteSqlRaw("DELETE FROM [imgData]");
                }
            }
            catch (Exception ex)
            {
                throw new Error("Error happened while deleting images from DB.", HttpStatusCode.InternalServerError);
            }
        }

        public void DeleteImageById(int id)
        {
            try
            {
                using (var db = new DBContext())
                {
                    var img = db.imgInfo
                        .Where(item => Equals(item.imageId, id))
                        .Include(item => item.image)
                        .FirstOrDefault();

                    if (img == null)
                    {
                        throw new Error("There is no image with such id in DB", HttpStatusCode.BadRequest);
                    }

                    db.imgData.Remove(img.image);
                    db.imgInfo.Remove(img);
                    db.SaveChanges();
                }
            }
            catch (Error err)
            {
                throw err;
            }
            catch
            {
                throw new Error("Error happened while deleting image by id from DB.", HttpStatusCode.InternalServerError);
            }
        }
    }
}
