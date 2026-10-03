using System;
using System.Collections.Generic;
using System.Text;

namespace NESOOP.Compiler
{
    public sealed class SemanticAnalyzer
    {
        private sealed record MethodSymbol(
            string ClassName,
            MethodDeclarationSyntax Syntax,
            SemanticType ReturnType,
            string Label,
            IReadOnlyList<SemanticParameter> Parameters
        );


        public SemanticProgram Analyze(
            CompilationUnitSyntax program
        )
        {
            // =========================================================
            // First pass:
            //
            // Discover every method and its signature.
            // =========================================================

            Dictionary<string, MethodSymbol> methods =
                new();


            foreach (
                ClassDeclarationSyntax classDeclaration
                in program.Classes
            )
            {
                foreach (
                    MethodDeclarationSyntax method
                    in classDeclaration.Methods
                )
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


                    SemanticType returnType =
                        method.ReturnType switch
                        {
                            TypeSyntaxKind.Void =>
                                SemanticType.Void,

                            TypeSyntaxKind.Byte =>
                                SemanticType.Byte,

                            _ =>
                                throw new Exception(
                                    "Internal compiler error."
                                )
                        };


                    List<SemanticParameter> parameters =
                        new();


                    HashSet<string> parameterNames =
                        new();


                    foreach (
                        ParameterSyntax parameter
                        in method.Parameters
                    )
                    {
                        if (
                            !parameterNames.Add(
                                parameter.Name
                            )
                        )
                        {
                            throw new Exception(
                                $"Semantic error: parameter " +
                                $"'{parameter.Name}' is duplicated."
                            );
                        }


                        parameters.Add(
                            new SemanticParameter(
                                parameter.Name,

                                $"{classDeclaration.Name}_" +
                                $"{method.Name}_" +
                                $"param_{parameter.Name}"
                            )
                        );
                    }


                    methods.Add(
                        key,

                        new MethodSymbol(
                            classDeclaration.Name,
                            method,
                            returnType,
                            MakeMethodLabel(
                                classDeclaration.Name,
                                method.Name
                            ),
                            parameters
                        )
                    );
                }
            }


            // =========================================================
            // Entry point
            // =========================================================

            if (
                !methods.TryGetValue(
                    "Game.Start",
                    out MethodSymbol? entry
                )
            )
            {
                throw new Exception(
                    "Semantic error: Game.Start() was not found."
                );
            }


            if (!entry.Syntax.IsStatic)
            {
                throw new Exception(
                    "Semantic error: Game.Start() must be static."
                );
            }


            if (entry.ReturnType != SemanticType.Void)
            {
                throw new Exception(
                    "Semantic error: Game.Start() must return void."
                );
            }


            if (entry.Parameters.Count != 0)
            {
                throw new Exception(
                    "Semantic error: Game.Start() cannot have parameters."
                );
            }


            // =========================================================
            // Second pass:
            //
            // Analyze method bodies.
            // =========================================================

            List<SemanticMethod> output =
                new();


            foreach (
                MethodSymbol symbol
                in methods.Values
            )
            {
                if (!symbol.Syntax.IsStatic)
                {
                    throw new Exception(
                        $"Semantic error: instance method " +
                        $"'{symbol.ClassName}.{symbol.Syntax.Name}' " +
                        $"is not supported yet."
                    );
                }


                output.Add(
                    AnalyzeMethod(
                        symbol,
                        methods
                    )
                );
            }


            return new SemanticProgram(
                output
            );
        }


        private static SemanticMethod AnalyzeMethod(
            MethodSymbol method,
            Dictionary<string, MethodSymbol> methods
        )
        {
            List<SemanticStatement> output =
                new();


            // Maps source names to actual RAM labels.
            Dictionary<string, string> variables =
                new();


            // Parameters are variables visible throughout
            // the method.
            foreach (
                SemanticParameter parameter
                in method.Parameters
            )
            {
                variables.Add(
                    parameter.Name,
                    parameter.StorageName
                );
            }


            foreach (
                StatementSyntax statement
                in method.Syntax.Body.Statements
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
                    if (
                        variables.ContainsKey(
                            declaration.Name
                        )
                    )
                    {
                        throw new Exception(
                            $"Semantic error: variable " +
                            $"'{declaration.Name}' is already declared."
                        );
                    }


                    SemanticValue initializer =
                        ResolveValue(
                            declaration.Initializer,
                            method.ClassName,
                            variables,
                            methods
                        );


                    string storageName =
                        $"{method.ClassName}_" +
                        $"{method.Syntax.Name}_" +
                        $"local_{declaration.Name}";


                    variables.Add(
                        declaration.Name,
                        storageName
                    );


                    output.Add(
                        new SemanticByteDeclaration(
                            declaration.Name,
                            storageName,
                            initializer
                        )
                    );


                    continue;
                }


                // =====================================================
                // Method call statement
                // =====================================================

                if (
                    statement
                    is MethodCallStatementSyntax call
                )
                {
                    SemanticCall semanticCall =
                        ResolveCall(
                            call.Target,
                            call.Arguments,
                            method.ClassName,
                            variables,
                            methods
                        );


                    output.Add(
                        new SemanticMethodCallStatement(
                            semanticCall
                        )
                    );


                    continue;
                }


                // =====================================================
                // Return
                // =====================================================

                if (
                    statement
                    is ReturnStatementSyntax returnStatement
                )
                {
                    if (
                        method.ReturnType
                        == SemanticType.Void
                    )
                    {
                        if (returnStatement.Value != null)
                        {
                            throw new Exception(
                                $"Semantic error: void method " +
                                $"'{method.ClassName}.{method.Syntax.Name}' " +
                                $"cannot return a value."
                            );
                        }


                        output.Add(
                            new SemanticReturnStatement(
                                null
                            )
                        );


                        continue;
                    }


                    // byte method

                    if (returnStatement.Value == null)
                    {
                        throw new Exception(
                            $"Semantic error: byte method " +
                            $"'{method.ClassName}.{method.Syntax.Name}' " +
                            $"must return a value."
                        );
                    }


                    output.Add(
                        new SemanticReturnStatement(
                            ResolveValue(
                                returnStatement.Value,
                                method.ClassName,
                                variables,
                                methods
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

                    if (
                        assignment.Target.Parts.Count
                        == 1
                    )
                    {
                        string variableName =
                            assignment.Target.Parts[0];


                        if (
                            !variables.TryGetValue(
                                variableName,
                                out string? storageName
                            )
                        )
                        {
                            throw new Exception(
                                $"Semantic error: variable " +
                                $"'{variableName}' does not exist."
                            );
                        }


                        output.Add(
                            new SemanticVariableAssignment(
                                storageName,

                                ResolveValue(
                                    assignment.Value,
                                    method.ClassName,
                                    variables,
                                    methods
                                )
                            )
                        );


                        continue;
                    }


                    // -------------------------------------------------
                    // Screen.BackgroundColor = ...
                    // -------------------------------------------------

                    // -------------------------------------------------
                    // NES built-in member
                    //
                    // Example:
                    //
                    // Screen.BackgroundColor = value;
                    // -------------------------------------------------

                    if (
                        NesBuiltIns.TryResolveMember(
                            assignment.Target.Parts,
                            out NesBuiltInDefinition? builtIn
                        )
                    )
                    {
                        if (!builtIn.CanWrite)
                        {
                            throw new Exception(
                                $"Semantic error: NES built-in member " +
                                $"'{builtIn.TypeName}.{builtIn.MemberName}' " +
                                $"cannot be written to."
                            );
                        }


                        SemanticValue value =
                            ResolveValue(
                                assignment.Value,
                                method.ClassName,
                                variables,
                                methods
                            );


                        // Everything in the language is byte-sized
                        // at the moment, so this is simple for now.
                        if (builtIn.ValueType != SemanticType.Byte)
                        {
                            throw new Exception(
                                "Internal compiler error: " +
                                "unsupported NES built-in type."
                            );
                        }


                        output.Add(
                            new SemanticBuiltInAssignment(
                                builtIn.Member,
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


            // =========================================================
            // A byte-returning method must currently end in return.
            //
            // We don't have if/else yet, so this simple rule is enough.
            // =========================================================

            if (
                method.ReturnType == SemanticType.Byte &&
                (
                    output.Count == 0 ||
                    output[^1]
                    is not SemanticReturnStatement
                )
            )
            {
                throw new Exception(
                    $"Semantic error: byte method " +
                    $"'{method.ClassName}.{method.Syntax.Name}' " +
                    $"must end with return."
                );
            }


            return new SemanticMethod(
                method.ClassName,
                method.Syntax.Name,
                method.Label,
                method.ReturnType,
                method.Parameters,
                output
            );
        }


        // =============================================================
        // Expressions
        // =============================================================

        private static SemanticValue ResolveValue(
            ExpressionSyntax expression,
            string currentClass,
            Dictionary<string, string> variables,
            Dictionary<string, MethodSymbol> methods
        )
        {
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
                        $"between 0 and 255 at line " +
                        $"{number.Token.Line}, " +
                        $"column {number.Token.Column}."
                    );
                }


                return new SemanticByteLiteral(
                    (byte)number.Value
                );
            }


            if (
                expression
                is IdentifierExpressionSyntax identifier
            )
            {
                if (
                    !variables.TryGetValue(
                        identifier.Name,
                        out string? storageName
                    )
                )
                {
                    throw new Exception(
                        $"Semantic error: variable " +
                        $"'{identifier.Name}' does not exist."
                    );
                }


                return new SemanticVariableReference(
                    storageName
                );
            }


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
                        currentClass,
                        variables,
                        methods
                    ),

                    ResolveValue(
                        binary.Right,
                        currentClass,
                        variables,
                        methods
                    )
                );
            }


            if (
                expression
                is MethodCallExpressionSyntax call
            )
            {
                SemanticCall semanticCall =
                    ResolveCall(
                        call.Target,
                        call.Arguments,
                        currentClass,
                        variables,
                        methods
                    );


                if (
                    semanticCall.ReturnType
                    == SemanticType.Void
                )
                {
                    throw new Exception(
                        $"Semantic error: void method " +
                        $"'{string.Join(".", call.Target.Parts)}' " +
                        $"cannot be used as a value."
                    );
                }


                return new SemanticMethodCallValue(
                    semanticCall
                );
            }


            throw new Exception(
                "Semantic error: unsupported expression."
            );
        }


        // =============================================================
        // Method-call resolution
        // =============================================================

        private static SemanticCall ResolveCall(
            MemberAccessExpressionSyntax target,
            IReadOnlyList<ExpressionSyntax> arguments,
            string currentClass,
            Dictionary<string, string> variables,
            Dictionary<string, MethodSymbol> methods
        )
        {
            string targetClass;
            string targetMethod;


            if (target.Parts.Count == 1)
            {
                targetClass =
                    currentClass;

                targetMethod =
                    target.Parts[0];
            }
            else if (target.Parts.Count == 2)
            {
                targetClass =
                    target.Parts[0];

                targetMethod =
                    target.Parts[1];
            }
            else
            {
                throw new Exception(
                    $"Semantic error: invalid method call " +
                    $"'{string.Join(".", target.Parts)}'."
                );
            }


            string key =
                $"{targetClass}.{targetMethod}";


            if (
                !methods.TryGetValue(
                    key,
                    out MethodSymbol? method
                )
            )
            {
                throw new Exception(
                    $"Semantic error: method " +
                    $"'{key}' does not exist."
                );
            }


            if (!method.Syntax.IsStatic)
            {
                throw new Exception(
                    $"Semantic error: method " +
                    $"'{key}' is not static."
                );
            }


            if (
                arguments.Count
                != method.Parameters.Count
            )
            {
                throw new Exception(
                    $"Semantic error: method '{key}' expects " +
                    $"{method.Parameters.Count} argument(s), " +
                    $"but {arguments.Count} were supplied."
                );
            }


            List<SemanticValue> semanticArguments =
                new();


            foreach (
                ExpressionSyntax argument
                in arguments
            )
            {
                semanticArguments.Add(
                    ResolveValue(
                        argument,
                        currentClass,
                        variables,
                        methods
                    )
                );
            }


            return new SemanticCall(
                method.Label,

                method.Parameters
                    .Select(
                        parameter =>
                            parameter.StorageName
                    )
                    .ToList(),

                semanticArguments,

                method.ReturnType
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
