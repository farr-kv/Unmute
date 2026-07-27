using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;
using Unmute.Core.Extensions;
using Unmute.Core.Models;
using Unmute.Core.Services;
using Unmute.OCR.PaddleOCR.Extensions;
using Unmute.TTS.PocketTTS.Extensions;

namespace Unmute.App.CLI
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var serviceProvider = new ServiceCollection()
                .AddUnmuteCore()
                .UsePaddleOCR()
                .UsePocketTTS()
                .BuildServiceProvider();

            var tts = serviceProvider.GetRequiredService<ITtsService>();
            var ocrEngine = serviceProvider.GetRequiredService<IOCREngine>();
            var appMonitor = serviceProvider.GetRequiredService<IApplicationMonitor>();

            Task.Run(async () =>
            {
                try
                {
                    await tts.InitializeAsync();
                    await ocrEngine.InitializeAsync();

                    await tts.StartAsync();
                    var proc = Process.GetProcessesByName("photos").FirstOrDefault()!;
                    var text = await appMonitor.MonitorProcessAsync(proc);
                    await tts.NarrateAsync(text);

                    //foreach (var voice in tts.AvailableVoices)
                    //{
                    //    var text = "hello world, my name is " + voice.Name;
                    //    await tts.NarrateAsync(text, voice);
                    //}
                }
                finally
                {
                    tts.StopAsync().Wait();
                    serviceProvider.Dispose();
                }
            });

            Console.ReadLine();
        }
    }
}
