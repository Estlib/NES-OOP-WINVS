using System;
using System.Collections.Generic;
using System.Text;

namespace NESOOP.Compiler
{
    public sealed class SemanticAnalyzer
    {
        public SemanticProgram Analyze(
            CompilationUnitSyntax program
        )
        {
            // ----------------------------------------------------------
            // Find class Game
            // ----------------------------------------------------------

            ClassDeclarationSyntax? gameClass =
                program.Classes.FirstOrDefault(
                    x => x.Name == "Game"
                );

            if (gameClass == null)
            {
                throw new Exception(
                    "Semantic error: class Game was not found."
                );
            }


            // ----------------------------------------------------------
            // Find static void Game.Start()
            // ----------------------------------------------------------

            MethodDeclarationSyntax? startMethod =
                gameClass.Methods.FirstOrDefault(
                    x =>
                        x.Name == "Start" &&
                        x.IsStatic
                );

            if (startMethod == null)
            {
                throw new Exception(
                    "Semantic error: static void Game.Start() was not found."
                );
            }


            // ----------------------------------------------------------
            // Find:
            //
            // Screen.BackgroundColor = value;
            // ----------------------------------------------------------

            AssignmentStatementSyntax? colorAssignment =
                startMethod.Body.Statements
                    .OfType<AssignmentStatementSyntax>()
                    .FirstOrDefault(
                        x =>
                            x.Target.Parts.Count == 2 &&
                            x.Target.Parts[0] == "Screen" &&
                            x.Target.Parts[1] == "BackgroundColor"
                    );


            if (colorAssignment == null)
            {
                throw new Exception(
                    "Semantic error: " +
                    "Screen.BackgroundColor must be assigned in Game.Start()."
                );
            }


            int value =
                colorAssignment.Value.Value;


            // ----------------------------------------------------------
            // NES palette indexes are 0x00 - 0x3F
            // ----------------------------------------------------------

            if (value < 0 || value > 0x3F)
            {
                Token token =
                    colorAssignment.Value.Token;

                throw new Exception(
                    $"Semantic error: background color " +
                    $"must be between 0x00 and 0x3F " +
                    $"at line {token.Line}, " +
                    $"column {token.Column}."
                );
            }


            return new SemanticProgram(
                (byte)value
            );
        }
    }
}
