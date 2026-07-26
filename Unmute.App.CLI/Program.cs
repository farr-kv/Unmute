using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;
using Unmute.Core.Extensions;
using Unmute.Core.Services;
using Unmute.OCR.Extensions;
using Unmute.TTS.Extensions;

namespace Unmute.App.CLI
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var serviceProvider = new ServiceCollection()
                .AddUnmute()
                .UseOCR()
                .UseTTS()
                .BuildServiceProvider();

            var tts = serviceProvider.GetRequiredService<ITtsService>();
            var appMonitor = serviceProvider.GetRequiredService<IApplicationMonitor>();

            Task.Run(async () =>
            {
                try
                {
                    await tts.StartAsync();
                    var proc = Process.GetProcessesByName("photos").FirstOrDefault()!;
                    var text = await appMonitor.MonitorProcessAsync(proc);
                    await tts.Narrate(text);
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
