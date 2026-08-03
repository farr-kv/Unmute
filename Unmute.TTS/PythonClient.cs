using System.Diagnostics;
using Unmute.TTS.Extensions;

namespace Unmute.TTS
{
    internal class PythonClient
    {
        private string workingDirectory;
        private string pythonExePath;

        public PythonClient(string workingDirectory, string pythonExePath)
        {
            this.workingDirectory = workingDirectory;
            this.pythonExePath = pythonExePath;
        }

        public Task ExecutePythonAsync(string command, TextWriter? output = null)
        {
            var process = this.ExecutePython(command, output);
            return process.WaitForExitAsync();
        }

        public Process ExecutePython(string command, TextWriter? output = null)
        {
            var process = new Process();
            process.OutputDataReceived += (_, e) =>
            {
                output?.WriteLine(e.Data);
            };
            process.ErrorDataReceived += (_, e) =>
            {
                output?.WriteLine(e.Data);
            };

            var info = new ProcessStartInfo
            {
                FileName = pythonExePath,
                Arguments = command,
                WorkingDirectory = workingDirectory,
                RedirectStandardInput = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            process.StartInfo = info;
            process.Start();
            process.AttachToParent();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            return process;
        }
    }
}
