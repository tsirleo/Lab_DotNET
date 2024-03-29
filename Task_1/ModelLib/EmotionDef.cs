﻿using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace ModelLib;

public class EmotionDef
{
    private InferenceSession session;
    static Semaphore semaphore = new Semaphore(1, 1);
    public EmotionDef() 
    {
        using var modelStream = typeof(EmotionDef).Assembly.GetManifestResourceStream("emofps.onnx");
        using var memoryStream = new MemoryStream();
        if (modelStream is not null) { modelStream.CopyTo(memoryStream); }
        session = new InferenceSession(memoryStream.ToArray()); 
    }

    public async Task<Dictionary<string,double>> ProcessAnImage(byte[] source, CancellationToken ctn)
    {
            var resultDict = new Dictionary<string,double> ();
            var funcMemStream = new MemoryStream(source);
            
            if (ctn.IsCancellationRequested) return resultDict;

            var image = await Image.LoadAsync<Rgb24>(funcMemStream, ctn);
            image.Mutate(ctx => {
                ctx.Resize(new Size(64,64));
            });

            if (ctn.IsCancellationRequested) return resultDict;

            var emotions = new float[0];
            var inputs = new List<NamedOnnxValue> { NamedOnnxValue.CreateFromTensor("Input3", GrayscaleImageToTensor(image)) };

            await Task.Factory.StartNew(() =>
            {
                semaphore.WaitOne();   
                using IDisposableReadOnlyCollection<DisposableNamedOnnxValue> results = session.Run(inputs);
                semaphore.Release();
                emotions = Softmax(results.First(v => v.Name == "Plus692_Output_0").AsEnumerable<float>().ToArray());
            }, TaskCreationOptions.LongRunning);
            
            if (ctn.IsCancellationRequested) return resultDict;

            string[] keys = { "neutral", "happiness", "surprise", "sadness", "anger", "disgust", "fear", "contempt" };
            foreach(var emotionPair in keys.Zip(emotions))
                resultDict.TryAdd(emotionPair.First, emotionPair.Second);

            return resultDict;
    }

    private DenseTensor<float> GrayscaleImageToTensor(Image<Rgb24> img)
    {
        var w = img.Width;
        var h = img.Height;
        var t = new DenseTensor<float>(new[] { 1, 1, h, w });

        img.ProcessPixelRows(pa => 
        {
            for (int y = 0; y < h; y++)
            {           
                Span<Rgb24> pixelSpan = pa.GetRowSpan(y);
                for (int x = 0; x < w; x++)
                {
                    t[0, 0, y, x] = pixelSpan[x].R; // B and G are the same
                }
            }
        });
        
        return t;
    }

    private float[] Softmax(float[] z)
    {
        var exps = z.Select(x => Math.Exp(x)).ToArray();
        var sum = exps.Sum();
        return exps.Select(x => (float)(x / sum)).ToArray();
    }
}
