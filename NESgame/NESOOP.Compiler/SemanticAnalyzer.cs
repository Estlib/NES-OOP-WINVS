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


            List<SemanticStatement> output = new();


            // Variables currently visible inside Game.Start()
            Dictionary<string, SemanticByteDeclaration> variables =
                new();


            foreach (
                StatementSyntax statement
                in startMethod.Body.Statements
            )
            {
                // =====================================================
                // byte name = value;
                // =====================================================

                if (
                    statement
                    is VariableDeclarationStatementSyntax declaration
                )
                {
                    if (variables.ContainsKey(declaration.Name))
                    {
                        throw new Exception(
                            $"Semantic error: variable " +
                            $"'{declaration.Name}' is already declared " +
                            $"at line {declaration.NameToken.Line}, " +
                            $"column {declaration.NameToken.Column}."
                        );
                    }


                    if (
                        declaration.Initializer
                        is not NumberExpressionSyntax number
                    )
                    {
                        throw new Exception(
                            $"Semantic error: byte variables currently " +
                            $"must be initialized with a number."
                        );
                    }


                    if (
                        number.Value < 0 ||
                        number.Value > 255
                    )
                    {
                        throw new Exception(
                            $"Semantic error: byte value must be " +
                            $"between 0 and 255 at " +
                            $"line {number.Token.Line}, " +
                            $"column {number.Token.Column}."
                        );
                    }


                    string storageName =
                        $"Game_Start_{declaration.Name}";


                    SemanticByteDeclaration semanticDeclaration =
                        new(
                            declaration.Name,
                            storageName,
                            (byte)number.Value
                        );


                    variables.Add(
                        declaration.Name,
                        semanticDeclaration
                    );


                    output.Add(
                        semanticDeclaration
                    );


                    continue;
                }


                // =====================================================
                // assignment
                // =====================================================

                if (
                    statement
                    is AssignmentStatementSyntax assignment
                )
                {
                    bool isBackgroundColor =
                        assignment.Target.Parts.Count == 2 &&
                        assignment.Target.Parts[0] == "Screen" &&
                        assignment.Target.Parts[1] == "BackgroundColor";


                    if (!isBackgroundColor)
                    {
                        throw new Exception(
                            $"Semantic error: unknown assignment target " +
                            $"'{string.Join(".", assignment.Target.Parts)}'."
                        );
                    }


                    SemanticValue value =
                        ResolveValue(
                            assignment.Value,
                            variables
                        );


                    // For our current language, variables cannot
                    // be reassigned yet, so we can still prove that
                    // their initial value is a valid NES palette value.

                    int knownValue =
                        value switch
                        {
                            SemanticByteLiteral literal =>
                                literal.Value,

                            SemanticVariableReference reference =>
                                variables.Values
                                    .First(
                                        x =>
                                            x.StorageName ==
                                            reference.StorageName
                                    )
                                    .InitialValue,

                            _ =>
                                throw new Exception(
                                    "Internal compiler error."
                                )
                        };


                    if (knownValue > 0x3F)
                    {
                        throw new Exception(
                            "Semantic error: background color " +
                            "must be between 0x00 and 0x3F."
                        );
                    }


                    output.Add(
                        new SemanticBackgroundColorAssignment(
                            value
                        )
                    );


                    continue;
                }


                throw new Exception(
                    "Semantic error: unsupported statement."
                );
            }


            return new SemanticProgram(output);
        }


        private static SemanticValue ResolveValue(
            ExpressionSyntax expression,
            Dictionary<string, SemanticByteDeclaration> variables
        )
        {
            // ---------------------------------------------------------
            // Numeric literal
            // ---------------------------------------------------------

            if (expression is NumberExpressionSyntax number)
            {
                if (
                    number.Value < 0 ||
                    number.Value > 255
                )
                {
                    throw new Exception(
                        $"Semantic error: byte value must be " +
                        $"between 0 and 255 at " +
                        $"line {number.Token.Line}, " +
                        $"column {number.Token.Column}."
                    );
                }


                return new SemanticByteLiteral(
                    (byte)number.Value
                );
            }


            // ---------------------------------------------------------
            // Variable
            // ---------------------------------------------------------

            if (
                expression
                is IdentifierExpressionSyntax identifier
            )
            {
                if (
                    !variables.TryGetValue(
                        identifier.Name,
                        out SemanticByteDeclaration? variable
                    )
                )
                {
                    throw new Exception(
                        $"Semantic error: variable " +
                        $"'{identifier.Name}' does not exist at " +
                        $"line {identifier.Token.Line}, " +
                        $"column {identifier.Token.Column}."
                    );
                }


                return new SemanticVariableReference(
                    variable.StorageName
                );
            }


            throw new Exception(
                "Semantic error: unsupported expression."
            );
        }
    }
}
