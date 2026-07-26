using System.Diagnostics;
using System.IO.Compression;
using System.Text.RegularExpressions;

namespace Unmute.TTS
{
    internal class PythonInstaller
    {
        private static readonly string WorkingDirectory = Path.Combine(AppContext.BaseDirectory, "py");
        private static readonly string PythonExe = Path.Combine(WorkingDirectory, "python.exe");
        private bool enableImports = false;
        private bool addPip = false;
        private bool addUv = false;
        private string version = "3.13.14";

        #region Builder        
        public PythonInstaller EnableImports(bool enabled = true)
        {
            this.enableImports = enabled;
            return this;
        }

        public PythonInstaller WithPip()
        {
            this.addPip = true;
            return this;
        }

        public PythonInstaller WithUV()
        {
            this.WithPip();
            this.addUv = true;
            return this;
        }

        public PythonInstaller Version(string version)
        {
            this.version = version;
            return this;
        }
        #endregion

        #region Installation
        public async Task<PythonClient> InstallAsync()
        {
            if (File.Exists(PythonExe))
            {
                var currentVersion = FileVersionInfo.GetVersionInfo(PythonExe);
                if (currentVersion.FileVersion != this.version)
                    Directory.Delete(WorkingDirectory, true);
            }

            if (!File.Exists(PythonExe))
            {
                Directory.CreateDirectory(WorkingDirectory);
                var downloadPythonPath = $"https://www.python.org/ftp/python/{version}/python-{version}-embed-amd64.zip";
                var pythonZip = await Download(downloadPythonPath);
                await ZipFile.ExtractToDirectoryAsync(pythonZip, WorkingDirectory);
                File.Delete(pythonZip);
            }

            await EnableImports();

            var client = new PythonClient(WorkingDirectory, PythonExe);

            if (addPip)
                await InstallPip(client);

            if (addUv)
                await InstallUV(client);

            return client;
        }

        private async Task EnableImports()
        {
            var configFile = GetFile(@"python(\w*)\._pth");
            if (configFile is null)
                return;

            var contents = await File.ReadAllTextAsync(configFile);
            var isCurrentlyEnabled = !contents.Contains("#import site");

            if (isCurrentlyEnabled == enableImports)
                return;

            if (enableImports)
                contents = contents.Replace("#import site", "import site");
            else
                contents = contents.Replace("import site", "#import site");

            await File.WriteAllTextAsync(configFile, contents);
        }

        private async Task InstallPip(PythonClient client)
        {
            var isPipInstalled = File.Exists(Path.Combine(WorkingDirectory, "Scripts", "pip.exe"));
            if (isPipInstalled)
                return;

            var downloadPipPath = @"https://bootstrap.pypa.io/get-pip.py";
            await Download(downloadPipPath);
            await client.ExecutePythonAsync("get-pip.py", Console.Out);
        }

        private async Task InstallUV(PythonClient client)
        {
            await client.ExecutePythonAsync("-m pip install uv", Console.Out); // TODO Get a logger from DI
        }
        #endregion

        private async Task<string> Download(string url)
        {
            var uri = new Uri(url);
            var outputPath = Path.Combine(WorkingDirectory, Path.GetFileName(uri.AbsolutePath));
            if (!File.Exists(outputPath))
            {
                using var client = new HttpClient();
                using var response = await client.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead);

                response.EnsureSuccessStatusCode();

                await using Stream input = await response.Content.ReadAsStreamAsync();
                await using FileStream output = File.Create(outputPath);

                await input.CopyToAsync(output);
            }
            return outputPath;
        }

        private string? GetFile(string regex)
        {
            return Directory
                .EnumerateFiles(WorkingDirectory)
                .FirstOrDefault(path => Regex.IsMatch(Path.GetFileName(path), regex));
        }
    }
}
