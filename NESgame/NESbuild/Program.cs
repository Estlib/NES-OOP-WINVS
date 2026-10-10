using System;
using System.Diagnostics;
using System.IO;
using NESOOP.Compiler;
using System.Collections.Generic;
using System.Linq;

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

            string oopSource = Path.Combine(sourceFolder, "Main.nesoo");

            string runtimeObjectFolder =
            Path.Combine(
                buildFolder,
                "runtime"
            );

            string generatedSource = Path.Combine(
                generatedFolder,
                "game.s"
            );

            string configFile = Path.Combine(
                configFolder,
                "nes.cfg"
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
            Directory.CreateDirectory(runtimeObjectFolder);

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
            Console.WriteLine("Assembling NES runtime...");


            string[] runtimeSources =
                Directory.GetFiles(
                    runtimeFolder,
                    "*.s",
                    SearchOption.TopDirectoryOnly
                );


            List<string> runtimeObjects =
                new();


            foreach (
                string runtimeSource
                in runtimeSources.OrderBy(
                    x => x
                )
            )
            {
                string fileName =
                    Path.GetFileNameWithoutExtension(
                        runtimeSource
                    );


                string objectFile =
                    Path.Combine(
                        runtimeObjectFolder,
                        fileName + ".o"
                    );


                Console.WriteLine(
                    $"  {Path.GetFileName(runtimeSource)}"
                );


                int runtimeResult =
                    RunProcess(
                        "ca65",
                        runtimeSource,
                        "-g",
                        "-o",
                        objectFile
                    );


                if (runtimeResult != 0)
                {
                    Console.Error.WriteLine(
                        $"Assembly failed: {runtimeSource}"
                    );

                    return runtimeResult;
                }


                runtimeObjects.Add(
                    objectFile
                );
            }


            // ----------------------------------------------------------
            // Assemble generated game code
            // ----------------------------------------------------------

            Console.WriteLine("Assembling generated code...");

            int generatedResult = RunProcess(
                "ca65",
                generatedSource,
                "-g",
                "-o",
                generatedObject
            );

            if (generatedResult != 0)
            {
                Console.Error.WriteLine("Generated assembly failed.");
                return generatedResult;
            }


            // ----------------------------------------------------------
            // Link
            // ----------------------------------------------------------

            Console.WriteLine("Linking...");


            List<string> linkerArguments = new(){"-C",configFile};


            linkerArguments.AddRange(runtimeObjects);

            linkerArguments.Add(generatedObject);

            linkerArguments.AddRange(new[] 
                {
                    "-o",
                    romFile,

                    "-m",
                    mapFile,

                    "--dbgfile",
                    debugFile
                }
            );


            int linkResult = RunProcess(
                "ld65",
                linkerArguments.ToArray()
            );

            if (linkResult != 0)
            {
                Console.Error.WriteLine();
                Console.Error.WriteLine(
                    "ld65 failed."
                );

                return linkResult;
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