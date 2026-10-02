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
            // =========================================================
            // Build a table of every method first.
            //
            // This allows:
            //
            // Start() calling a method declared later.
            // =========================================================

            Dictionary<string, MethodDeclarationSyntax> methods =
                new();


            foreach (ClassDeclarationSyntax classDeclaration in program.Classes)
            {
                foreach (MethodDeclarationSyntax method in classDeclaration.Methods)
                {
                    string key =
                        $"{classDeclaration.Name}.{method.Name}";


                    if (methods.ContainsKey(key))
                    {
                        throw new Exception(
                            $"Semantic error: method '{key}' " +
                            $"is already declared."
                        );
                    }


                    methods.Add(
                        key,
                        method
                    );
                }
            }


            // =========================================================
            // Game.Start must still exist.
            // =========================================================

            if (
                !methods.TryGetValue(
                    "Game.Start",
                    out MethodDeclarationSyntax? entryPoint
                )
            )
            {
                throw new Exception(
                    "Semantic error: static void Game.Start() was not found."
                );
            }


            if (!entryPoint.IsStatic)
            {
                throw new Exception(
                    "Semantic error: Game.Start() must be static."
                );
            }


            // =========================================================
            // Analyze all methods.
            // =========================================================

            List<SemanticMethod> semanticMethods =
                new();


            foreach (ClassDeclarationSyntax classDeclaration in program.Classes)
            {
                foreach (MethodDeclarationSyntax method in classDeclaration.Methods)
                {
                    // Instance methods come later when we actually
                    // implement objects and "this".
                    if (!method.IsStatic)
                    {
                        throw new Exception(
                            $"Semantic error: instance method " +
                            $"'{classDeclaration.Name}.{method.Name}' " +
                            $"is not supported yet."
                        );
                    }


                    SemanticMethod semanticMethod =
                        AnalyzeMethod(
                            classDeclaration.Name,
                            method,
                            methods
                        );


                    semanticMethods.Add(
                        semanticMethod
                    );
                }
            }


            return new SemanticProgram(
                semanticMethods
            );
        }



        private static SemanticMethod AnalyzeMethod(
            string className,
            MethodDeclarationSyntax method,
            Dictionary<string, MethodDeclarationSyntax> methods
        )
        {
            List<SemanticStatement> output =
                new();


            // Every method has its own local variable scope.
            Dictionary<string, SemanticByteDeclaration> variables =
                new();


            foreach (
                StatementSyntax statement
                in method.Body.Statements
            )
            {
                // =====================================================
                // byte variable = expression;
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


                    // Include class and method name so two methods
                    // can both have a local variable called "color".
                    string storageName =
                        $"{className}_{method.Name}_{declaration.Name}";


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
                // Method call
                // =====================================================

                if (
                    statement
                    is MethodCallStatementSyntax call
                )
                {
                    string targetClass;
                    string targetMethod;


                    // -------------------------------------------------
                    // SetColor();
                    //
                    // Means:
                    //
                    // CurrentClass.SetColor();
                    // -------------------------------------------------

                    if (call.Target.Parts.Count == 1)
                    {
                        targetClass =
                            className;

                        targetMethod =
                            call.Target.Parts[0];
                    }


                    // -------------------------------------------------
                    // Palette.SetColor();
                    // -------------------------------------------------

                    else if (call.Target.Parts.Count == 2)
                    {
                        targetClass =
                            call.Target.Parts[0];

                        targetMethod =
                            call.Target.Parts[1];
                    }


                    else
                    {
                        throw new Exception(
                            $"Semantic error: invalid method call " +
                            $"'{string.Join(".", call.Target.Parts)}'."
                        );
                    }


                    string methodKey =
                        $"{targetClass}.{targetMethod}";


                    if (
                        !methods.TryGetValue(
                            methodKey,
                            out MethodDeclarationSyntax? target
                        )
                    )
                    {
                        throw new Exception(
                            $"Semantic error: method " +
                            $"'{methodKey}' does not exist."
                        );
                    }


                    if (!target.IsStatic)
                    {
                        throw new Exception(
                            $"Semantic error: method " +
                            $"'{methodKey}' is not static."
                        );
                    }


                    output.Add(
                        new SemanticMethodCall(
                            MakeMethodLabel(
                                targetClass,
                                targetMethod
                            )
                        )
                    );


                    continue;
                }


                // =====================================================
                // Assignment
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


            return new SemanticMethod(
                className,
                method.Name,
                MakeMethodLabel(
                    className,
                    method.Name
                ),
                output
            );
        }



        private static SemanticValue ResolveValue(
            ExpressionSyntax expression,
            Dictionary<string, SemanticByteDeclaration> variables
        )
        {
            // =========================================================
            // Number
            // =========================================================

            if (
                expression
                is NumberExpressionSyntax number
            )
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


            // =========================================================
            // Variable
            // =========================================================

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


            // =========================================================
            // Addition
            // =========================================================

            if (
                expression
                is BinaryExpressionSyntax binary
            )
            {
                if (
                    binary.OperatorToken.Kind
                    != TokenKind.Plus
                )
                {
                    throw new Exception(
                        $"Semantic error: unsupported operator " +
                        $"'{binary.OperatorToken.Text}'."
                    );
                }


                return new SemanticBinaryAdd(
                    ResolveValue(
                        binary.Left,
                        variables
                    ),

                    ResolveValue(
                        binary.Right,
                        variables
                    )
                );
            }


            throw new Exception(
                "Semantic error: unsupported expression."
            );
        }



        private static string MakeMethodLabel(
            string className,
            string methodName
        )
        {
            return $"{className}_{methodName}";
        }
    }
}
