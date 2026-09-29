using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace NESOOP.Compiler
{
    public static class Compiler
    {
        public static void Compile(
            string sourcePath,
            string outputPath
        )
        {
            // ----------------------------------------------------------
            // Read source
            // ----------------------------------------------------------

            string source =
                File.ReadAllText(sourcePath);


            // ----------------------------------------------------------
            // Lexer
            // ----------------------------------------------------------

            Lexer lexer = new(source);

            List<Token> tokens =
                lexer.Lex();


            // ----------------------------------------------------------
            // Parser
            // ----------------------------------------------------------

            Parser parser = new(tokens);

            CompilationUnitSyntax syntaxTree =
                parser.Parse();


            // ----------------------------------------------------------
            // Semantic analysis
            // ----------------------------------------------------------

            SemanticAnalyzer analyzer = new();

            SemanticProgram program =
                analyzer.Analyze(syntaxTree);


            // ----------------------------------------------------------
            // NES / 6502 code generation
            // ----------------------------------------------------------

            Nes6502CodeGenerator generator = new();

            string assembly =
                generator.Generate(program);


            // ----------------------------------------------------------
            // Write generated assembly
            // ----------------------------------------------------------

            string? directory =
                Path.GetDirectoryName(outputPath);

            if (directory != null)
            {
                Directory.CreateDirectory(directory);
            }


            File.WriteAllText(
                outputPath,
                assembly
            );


            Console.WriteLine(
                $"Compiled: {Path.GetFileName(sourcePath)}"
            );

            Console.WriteLine(
                $"Generated: {outputPath}"
            );
        }
    }
}
