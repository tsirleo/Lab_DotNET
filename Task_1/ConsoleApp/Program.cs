using System;
using ModelLib;
using System.Collections.Concurrent;

namespace ConsoleApp
{
    class Program 
    {
        static EmotionDef emotionDef = new EmotionDef();
        static readonly object consoleLock = new object();

        static void Main(string[] args)
        {
            string tokenType;
            bool cancellation;
            var source = CreateSourceArr(args);
            InitTokenOptionsString(args, out tokenType, out cancellation);
            if (source is not null) { Runner(source, tokenType, cancellation); }
            else Console.WriteLine("ERROR: Problem with source creating.");
        }
        
        static string[]? CreateSourceArr(string[] args)
        {
            var argLength = args.Length;
            string[]? source = null;
            if (argLength >= 1 && args[0] == "all") {
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
            else if (argLength >=2 && (args.Any(str => str.IndexOf("--token=") != -1) || args.Any(str => str.IndexOf("--cancellation=") != -1))) 
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
            else if (argLength >= 1)
            {
                source = args;
                return source;
            }
            return source;
        }

        static void InitTokenOptionsString(string[] args, out string tokenType, out bool cancellation)
        {
            tokenType = "shared";
            cancellation = false;
            if (args.Any(str => str.IndexOf("cancellation=true") != -1)) 
            {
                cancellation = true; 
            }
            if (args.Any(str => str.IndexOf("token=individual") != -1))
            {
                tokenType = "individual";
            }
        } 

        static async void Runner(string[] source, string tokenType, bool cancellation) 
        {
            CancellationTokenSource cts = new CancellationTokenSource();
            CancellationToken ctn = cts.Token;
            List<CancellationTokenSource>? ctsList = new List<CancellationTokenSource>(source.Length);
            List<CancellationToken>? ctnList = new List<CancellationToken>(source.Length);

            try {
                if (string.Equals(tokenType, "individual"))
                {
                    for (int i = 0; i < source.Length; i++) ctnList[i] = ctsList[i].Token;
                }

                var sourceList = PreparePath(source);

                var taskArr = new Task[source.Length];
                var tskIndex = 0;
                foreach (var path in sourceList) 
                {
                    if (string.Equals(tokenType, "individual") && ctnList is not null)
                    {
                        taskArr[tskIndex] = ProcessData(path, ctnList[tskIndex]);
                    }
                    else taskArr[tskIndex] = ProcessData(path, ctn);
                    
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
            catch(Exception excp)
            {
                Console.WriteLine(excp.Message);
            }
        }

        static async Task ProcessData(string path, CancellationToken ctn) 
        {
            var byteSource = await File.ReadAllBytesAsync(path);
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

        static List<string> PreparePath(string[] source)
        {
            var pathList = new List<string>();
            foreach (string image in source)
            {
                string imagePath;
                if (CheckISPath(image)){
                    if (File.Exists(image)) {
                        pathList.Add(image);
                    }
                    else {
                        throw new Exception($"ERROR: Specified image source file doesn't exist - {image}!");
                    }
                }
                else {
                    imagePath = string.Concat("ImagesSource/", image);
                    if (File.Exists(imagePath)) {
                        pathList.Add(imagePath); 
                    }
                    else {
                        throw new Exception($"ERROR: Specified image source file doesn't exist - {imagePath}!");
                    }
                }
                
            }
            return pathList;
        }
        
        static bool CheckISPath(string str)
        {
            if (str.IndexOf("ImagesSource/") != -1) { return true; }
            return false;
        }
    }
}
