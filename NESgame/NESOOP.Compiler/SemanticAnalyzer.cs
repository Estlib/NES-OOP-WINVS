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


            Dictionary<string, SemanticByteDeclaration> variables =
                new();


            foreach (
                StatementSyntax statement
                in startMethod.Body.Statements
            )
            {
                // =====================================================
                // byte name = expression;
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


                    SemanticValue initializer =
                        ResolveValue(
                            declaration.Initializer,
                            variables
                        );


                    string storageName =
                        $"Game_Start_{declaration.Name}";


                    SemanticByteDeclaration semanticDeclaration =
                        new(
                            declaration.Name,
                            storageName,
                            initializer
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
                // Assignments
                // =====================================================

                if (
                    statement
                    is AssignmentStatementSyntax assignment
                )
                {
                    // -------------------------------------------------
                    // variable = expression;
                    // -------------------------------------------------

                    if (assignment.Target.Parts.Count == 1)
                    {
                        string variableName =
                            assignment.Target.Parts[0];


                        if (
                            !variables.TryGetValue(
                                variableName,
                                out SemanticByteDeclaration? variable
                            )
                        )
                        {
                            throw new Exception(
                                $"Semantic error: variable " +
                                $"'{variableName}' does not exist."
                            );
                        }


                        SemanticValue value =
                            ResolveValue(
                                assignment.Value,
                                variables
                            );


                        output.Add(
                            new SemanticVariableAssignment(
                                variable.StorageName,
                                value
                            )
                        );


                        continue;
                    }


                    // -------------------------------------------------
                    // Screen.BackgroundColor = expression;
                    // -------------------------------------------------

                    bool isBackgroundColor =
                        assignment.Target.Parts.Count == 2 &&
                        assignment.Target.Parts[0] == "Screen" &&
                        assignment.Target.Parts[1] == "BackgroundColor";


                    if (isBackgroundColor)
                    {
                        SemanticValue value =
                            ResolveValue(
                                assignment.Value,
                                variables
                            );


                        output.Add(
                            new SemanticBackgroundColorAssignment(
                                value
                            )
                        );


                        continue;
                    }


                    throw new Exception(
                        $"Semantic error: unknown assignment target " +
                        $"'{string.Join(".", assignment.Target.Parts)}'."
                    );
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
            // Number
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


            // ---------------------------------------------------------
            // Addition
            // ---------------------------------------------------------

            if (
                expression
                is BinaryExpressionSyntax binary
            )
            {
                if (binary.OperatorToken.Kind != TokenKind.Plus)
                {
                    throw new Exception(
                        $"Semantic error: unsupported operator " +
                        $"'{binary.OperatorToken.Text}'."
                    );
                }


                SemanticValue left =
                    ResolveValue(
                        binary.Left,
                        variables
                    );


                SemanticValue right =
                    ResolveValue(
                        binary.Right,
                        variables
                    );


                return new SemanticBinaryAdd(
                    left,
                    right
                );
            }


            throw new Exception(
                "Semantic error: unsupported expression."
            );
        }
    }
}
