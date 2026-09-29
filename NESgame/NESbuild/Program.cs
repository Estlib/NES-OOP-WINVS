using System;
using System.Diagnostics;
using System.IO;
using NESOOP.Compiler;

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
            string sourceFolder = Path.Combine(root, "Game", "src");
            string configFolder = Path.Combine(root, "config");
            string buildFolder = Path.Combine(root, "build");
            string generatedFolder = Path.Combine(buildFolder, "generated");

            string runtimeSource = Path.Combine(runtimeFolder, "main.s");
            string oopSource = Path.Combine(sourceFolder, "Main.nesoo");

            string generatedSource = Path.Combine(
                generatedFolder,
                "game.s"
            );

            string configFile = Path.Combine(
                configFolder,
                "nes.cfg"
            );

            string runtimeObject = Path.Combine(
                buildFolder,
                "runtime.o"
            );

            string generatedObject = Path.Combine(
                buildFolder,
                "game.o"
            );

            string romFile = Path.Combine(
                buildFolder,
                "game.nes"
            );

            string mapFile = Path.Combine(
                buildFolder,
                "game.map"
            );

            string debugFile = Path.Combine(
                buildFolder,
                "game.dbg"
            );

            string mesenPath = @"C:\nestools\mesen\Mesen.exe";


            // ----------------------------------------------------------
            // Setup
            // ----------------------------------------------------------

            Directory.CreateDirectory(buildFolder);
            Directory.CreateDirectory(generatedFolder);

            Console.WriteLine("NES build");
            Console.WriteLine("---------");
            Console.WriteLine($"Root: {root}");
            Console.WriteLine();

            // ----------------------------------------------------------
            // Compile NES OOP
            // ----------------------------------------------------------

            Console.WriteLine("Compiling NES OOP...");

            try
            {
                Compiler.Compile(
                    oopSource,
                    generatedSource
                );
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine();
                Console.Error.WriteLine(ex.Message);

                return 1;
            }
            // ----------------------------------------------------------
            // Assemble runtime
            // ----------------------------------------------------------

            Console.WriteLine("Assembling runtime...");

            int result = RunProcess(
                "ca65",
                runtimeSource,
                "-g",
                "-o",
                runtimeObject
            );

            if (result != 0)
            {
                Console.Error.WriteLine("Runtime assembly failed.");
                return result;
            }


            // ----------------------------------------------------------
            // Assemble generated game code
            // ----------------------------------------------------------

            Console.WriteLine("Assembling generated code...");

            result = RunProcess(
                "ca65",
                generatedSource,
                "-g",
                "-o",
                generatedObject
            );

            if (result != 0)
            {
                Console.Error.WriteLine("Generated assembly failed.");
                return result;
            }


            // ----------------------------------------------------------
            // Link
            // ----------------------------------------------------------

            Console.WriteLine("Linking...");

            result = RunProcess(
                "ld65",
                "-C", configFile,

                runtimeObject,
                generatedObject,

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