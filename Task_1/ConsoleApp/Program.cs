using System;
using ModelLib;
using System.Collections.Concurrent;

namespace ConsoleApp
{
    class Program 
    {
        private static EmotionDef emotionDef = new EmotionDef();
        private static string tokenType = "shared";
        private static bool cancellation = false;
        private static bool imgTransformFlag = false;
        private static CancellationTokenSource? cts;
        private static CancellationToken ctn;
        private static List<CancellationTokenSource>? ctsList;
        private static List<CancellationToken>? ctnList;
        private static readonly object consoleLock = new object();

        static void Main(string[] args)
        {
            var source = CreateSourceArr(args);
            InitTokenOptionsString(args);
            if (source is not null) { Runner(source, tokenType, cancellation); }
            else Console.WriteLine("ERROR: Problem with source creating.");
        }
        
        static string[]? CreateSourceArr(string[] args)
        {
            var argLength = args.Length;
            string[]? source = null;
            if (args[0] == "all") {
               source = new string[] {
                    "ImagesSource/anger.jpg",
                    "ImagesSource/contempt.jpg",
                    "ImagesSource/disgust.jpg",
                    "ImagesSource/fear.jpg",
                    "ImagesSource/happiness.jpg",
                    "ImagesSource/neutral.jpg",
                    "ImagesSource/sadness.jpg",
                    "ImagesSource/sadness2.jpg",
                    "ImagesSource/surprise.jpg",
                    "ImagesSource/surprise2.jpg"
                };
                return source;
            }
            else if (argLength > 0 && (args.Any(str => str.IndexOf("--token=") != -1) || args.Any(str => str.IndexOf("--cancellation=") != -1))) 
            {
                if (args.Any(str => str.IndexOf("--token=") != -1) && args.Any(str => str.IndexOf("--cancellation=") != -1))
                {
                    source = new string[argLength - 2];
                    for (int i = 0; i < source.Length; i++) 
                    {
                        source[i] = args[i];
                    }
                }
                else {
                    source = new string[argLength - 1];
                    for (int i = 0; i < source.Length; i++) 
                    {
                        source[i] = args[i];
                    }
                }
                return source;
            }
            else if (argLength > 0)
            {
                source = args;
                return source;
            }
            return source;
        }

        static void InitTokenOptionsString(string[] args)
        {
            if (args.Any(str => str == "--cancellation=true")) 
            {
                cancellation = true; 
            }
            if (args.Any(str => str == "--token=individual"))
            {
                tokenType = "individual";
            }
        } 

        static async void Runner(string[] source, string tokenType, bool cancellation) 
        {
            try {
                if (string.Equals(tokenType, "individual"))
                {
                    ctsList = new List<CancellationTokenSource>(source.Length);
                    ctnList = new List<CancellationToken>(source.Length);

                    for (int i = 0; i < source.Length; i++) ctnList[i] = ctsList[i].Token;
                }
                else
                {
                    cts = new CancellationTokenSource();
                    ctn = cts.Token;
                }

                var byteSourceList = ImageToBytes(source);
                if (imgTransformFlag)
                {
                    var taskArr = new Task[source.Length];
                    var tskIndex = 0;

                    foreach (var byteSource in byteSourceList) 
                    {
                        if (string.Equals(tokenType, "individual") && ctnList is not null)
                        {
                            taskArr[tskIndex] = ProcessData(byteSource, ctnList[tskIndex]);
                        }
                        else taskArr[tskIndex] = ProcessData(byteSource, ctn);
                        
                        tskIndex += 1;
                    }

                    if (cancellation) 
                    {
                        Thread.Sleep(3000);
                        if (string.Equals(tokenType, "individual"))
                        {
                            if (ctsList is not null) 
                            {
                                for (int i = 0; i < source.Length; i++) ctsList[i].Cancel();
                            }
                        }
                        else
                        {
                            if (cts is not null) cts.Cancel(); 
                        }
                    }

                    await Task.WhenAll(taskArr);
                }
                else { return; }
            }
            catch(Exception excp)
            {
                Console.WriteLine(excp.Message);
            }
        }

        static async Task ProcessData(byte[] byteSource, CancellationToken ctn) 
        {
            var resultDict = await emotionDef.ProcessAnImage(byteSource, ctn);

            lock (consoleLock) 
            {
                Console.WriteLine();
                PrintDict(resultDict);
                Console.WriteLine();
                Console.WriteLine($"-------------------------Another_Task-------------------------");
            }
        }

        static void PrintDict(ConcurrentDictionary<string,double>  dict)
        {
            foreach (var emotion in dict)
            {
                Console.WriteLine($"{emotion.Key}: {emotion.Value}");
            }
        }

        static List<byte[]> ImageToBytes(string[] source)
        {
            var byteList = new List<byte[]>();
            foreach (string image in source)
            {
                string imagePath;
                if (CheckISPath(image)){
                    if (File.Exists(image)) {
                        byteList.Add(File.ReadAllBytes(image)); 
                        imgTransformFlag = true;
                    }
                    else {
                        Console.WriteLine($"ERROR: Specified image source file doesn't exist - {image}!");
                        return byteList;
                    }
                }
                else {
                    imagePath = string.Concat("ImagesSource/", image);
                    if (File.Exists(imagePath)) {
                        byteList.Add(File.ReadAllBytes(imagePath)); 
                        imgTransformFlag = true;
                    }
                    else {
                        Console.WriteLine($"ERROR: Specified image source file doesn't exist - {imagePath}!");
                        return byteList;
                    }
                }
                
            }
            return byteList;
        }
        
        static bool CheckISPath(string str)
        {
            if (str.StartsWith("ImagesSource/")) { return true; }
            return false;
        }
    }
}
