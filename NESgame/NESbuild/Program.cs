using System;
using System.Diagnostics;
using System.IO;

namespace NESbuild
{
    internal class Program
    {
        static int RunProcess(string fileName, params string[] arguments)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = fileName,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            foreach (string argument in arguments)
            {
                startInfo.ArgumentList.Add(argument);
            }

            using Process process = new();

            process.StartInfo = startInfo;

            process.OutputDataReceived += (_, e) =>
            {
                if (e.Data != null)
                    Console.WriteLine(e.Data);
            };

            process.ErrorDataReceived += (_, e) =>
            {
                if (e.Data != null)
                    Console.Error.WriteLine(e.Data);
            };

            process.Start();

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            process.WaitForExit();

            return process.ExitCode;
        }


        static string FindSolutionRoot()
        {
            DirectoryInfo? directory = new(AppContext.BaseDirectory);

            while (directory != null)
            {
                bool hasSolution =
                    directory.GetFiles("*.sln").Length > 0 ||
                    directory.GetFiles("*.slnx").Length > 0;

                if (hasSolution)
                    return directory.FullName;

                directory = directory.Parent;
            }

            throw new Exception("Could not find the solution root.");
        }


        static int Main(string[] args)
        {
            // ----------------------------------------------------------
            // Paths
            // ----------------------------------------------------------

            string root = FindSolutionRoot();

            string runtimeFolder = Path.Combine(root, "Game", "runtime");
            string configFolder = Path.Combine(root, "config");
            string buildFolder = Path.Combine(root, "build");

            string sourceFile = Path.Combine(runtimeFolder, "main.s");
            string configFile = Path.Combine(configFolder, "nes.cfg");

            string objectFile = Path.Combine(buildFolder, "main.o");
            string romFile = Path.Combine(buildFolder, "game.nes");
            string mapFile = Path.Combine(buildFolder, "game.map");
            string debugFile = Path.Combine(buildFolder, "game.dbg");

            string mesenPath = @"C:\nestools\mesen\Mesen.exe";


            // ----------------------------------------------------------
            // Setup
            // ----------------------------------------------------------

            Directory.CreateDirectory(buildFolder);

            Console.WriteLine("NES build");
            Console.WriteLine("---------");
            Console.WriteLine($"Root: {root}");
            Console.WriteLine();


            // ----------------------------------------------------------
            // Assemble
            // ----------------------------------------------------------

            Console.WriteLine("Assembling...");

            int result = RunProcess(
                "ca65",
                sourceFile,
                "-g",
                "-o",
                objectFile
            );

            if (result != 0)
            {
                Console.Error.WriteLine();
                Console.Error.WriteLine("ca65 failed.");

                return result;
            }


            // ----------------------------------------------------------
            // Link
            // ----------------------------------------------------------

            Console.WriteLine("Linking...");

            result = RunProcess(
                "ld65",
                "-C", configFile,
                objectFile,
                "-o", romFile,
                "-m", mapFile,
                "--dbgfile", debugFile
            );

            if (result != 0)
            {
                Console.Error.WriteLine();
                Console.Error.WriteLine("ld65 failed.");

                return result;
            }


            // ----------------------------------------------------------
            // Success
            // ----------------------------------------------------------

            Console.WriteLine();
            Console.WriteLine("Build successful.");
            Console.WriteLine($"ROM: {romFile}");


            // ----------------------------------------------------------
            // Launch MesenCE
            // ----------------------------------------------------------

            if (!File.Exists(mesenPath))
            {
                Console.Error.WriteLine();
                Console.Error.WriteLine("MesenCE not found:");
                Console.Error.WriteLine(mesenPath);

                return 1;
            }

            Console.WriteLine("Launching MesenCE...");

            Process.Start(new ProcessStartInfo
            {
                FileName = mesenPath,
                UseShellExecute = false,
                ArgumentList =
                {
                    romFile
                }
            });

            return 0;
        }
    }
}