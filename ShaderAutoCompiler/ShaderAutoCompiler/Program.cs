using ConsoleAppFramework;
using Microsoft.Extensions.Hosting;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ShaderAutoCompiler
{
    class Program : ConsoleAppBase
    {
        static async Task Main(string[] args)
        {
            await Host.CreateDefaultBuilder().RunConsoleAppFrameworkAsync<Program>(args);
        }

        string CompilerPath = null!;
        public void Run(string compilerPath, string path)
        {
            CompilerPath = Path.GetFullPath(compilerPath);

            Console.WriteLine(
                "Q: ShutDown\n" +
                "A: AllForceCompile");

            AllForceCompile(path);


            using (var watcher = new FileSystemWatcher(".", "*.hlsl"))
            {
                watcher.Changed += (_, p)=>CompileAsync(CompilerPath, p.FullPath).Wait();
                watcher.IncludeSubdirectories = true;
                watcher.NotifyFilter = NotifyFilters.LastWrite;
                watcher.EnableRaisingEvents = true;


                while(true)
                {
                    var Input = Console.ReadLine();
                    if (Input == null)
                        continue;
                    var q = Input.Trim().ToLower();
                    if (q == "a")
                    {
                        AllForceCompile(path);
                    }
                    if (q == "q")
                    {
                        Console.WriteLine("Quit");
                        return;
                    }
                }
            }
        }

        void AllForceCompile(string path)
        {
            Console.WriteLine("Start AllForceCompile");
            foreach(var file in Directory.EnumerateFiles(path, "*.hlsl", SearchOption.AllDirectories))
            {
                CompileAsync(CompilerPath, file).Wait();
            }
            Console.WriteLine("End AllForceCompile");
        }

        async Task CompileAsync(string compilerPath, string path)
        {
            var directoryName = Path.GetDirectoryName(path);
            var fileName = Path.GetFileName(path);
            var fileNameWithoutEx = Path.GetFileNameWithoutExtension(fileName);
            string[] psArgs = new[]
            {
                "-T ps_6_0",
                "-E BasicPS",
                $"-Fo {directoryName}/{fileNameWithoutEx}-ps.bin",
            };
            string[] vsArgs = new[]
            {
                "-T vs_6_0",
                "-E BasicVS",
                $"-Fo {directoryName}/{fileNameWithoutEx}-vs.bin",
            };
            string[] args = new[]
            {
                "-Zi",
                "-force-rootsig-ver rootsig_1_0",
                path
            };
            Console.WriteLine($"Start compile: {fileName}");
            var psTask = Process.Start(compilerPath, string.Join(" ", psArgs.Concat(args))).WaitForExitAsync();
            var vsTask = Process.Start(compilerPath, string.Join(" ", vsArgs.Concat(args))).WaitForExitAsync();

            await psTask;
            await vsTask;
            Console.WriteLine($"End compile: {fileName}");
        } 
    }
}
