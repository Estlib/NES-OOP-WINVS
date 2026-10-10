using System;
using System.Collections.Generic;
using System.Linq;

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

        private sealed record VariableSymbol(
            string StorageName,
            SemanticType Type
        );


        public SemanticProgram Analyze(
            CompilationUnitSyntax program
        )
        {
            // =========================================================
            // First pass: discover every method signature.
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
                        ToSemanticType(
                            method.ReturnType
                        );


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


                        SemanticType parameterType =
                            ToSemanticType(
                                parameter.Type
                            );


                        if (parameterType == SemanticType.Void)
                        {
                            throw new Exception(
                                $"Semantic error: parameter " +
                                $"'{parameter.Name}' cannot have type void."
                            );
                        }


                        string storageName =
                            $"{classDeclaration.Name}_" +
                            $"{method.Name}_" +
                            $"param_{parameter.Name}";


                        parameters.Add(
                            new SemanticParameter(
                                parameter.Name,
                                storageName,
                                parameterType
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
            // Entry point.
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
            // Second pass: analyze method bodies.
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
            Dictionary<string, VariableSymbol> variables =
                new();


            // Parameters are visible throughout the method.
            foreach (
                SemanticParameter parameter
                in method.Parameters
            )
            {
                variables.Add(
                    parameter.Name,

                    new VariableSymbol(
                        parameter.StorageName,
                        parameter.Type
                    )
                );
            }


            // Used to ensure locals in different nested blocks get
            // different assembler labels even when source names repeat.
            int localCounter = 0;


            List<SemanticStatement> statements =
                AnalyzeBlock(
                    method.Syntax.Body,
                    method,
                    variables,
                    methods,
                    ref localCounter
                );


            if (
                method.ReturnType != SemanticType.Void &&
                !AlwaysReturns(statements)
            )
            {
                throw new Exception(
                    $"Semantic error: {method.ReturnType.ToString().ToLowerInvariant()} method " +
                    $"'{method.ClassName}.{method.Syntax.Name}' " +
                    $"must return a value on every path."
                );
            }


            return new SemanticMethod(
                method.ClassName,
                method.Syntax.Name,
                method.Label,
                method.ReturnType,
                method.Parameters,
                statements
            );
        }


        // =============================================================
        // Blocks / statements
        // =============================================================

        private static List<SemanticStatement> AnalyzeBlock(
            BlockSyntax block,
            MethodSymbol method,
            Dictionary<string, VariableSymbol> variables,
            Dictionary<string, MethodSymbol> methods,
            ref int localCounter
        )
        {
            List<SemanticStatement> output =
                new();


            foreach (
                StatementSyntax statement
                in block.Statements
            )
            {
                // =====================================================
                // variable declaration
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


                    SemanticType variableType =
                        ToSemanticType(
                            declaration.Type
                        );


                    if (variableType == SemanticType.Void)
                    {
                        throw new Exception(
                            $"Semantic error: local variable " +
                            $"'{declaration.Name}' cannot have type void."
                        );
                    }


                    SemanticValue initializer =
                        ResolveValue(
                            declaration.Initializer,
                            method.ClassName,
                            variables,
                            methods
                        );


                    SemanticType initializerType =
                        GetValueType(
                            initializer
                        );


                    if (initializerType != variableType)
                    {
                        throw new Exception(
                            $"Semantic error: cannot initialize " +
                            $"'{declaration.Name}' of type {variableType} " +
                            $"with value of type {initializerType}."
                        );
                    }


                    string storageName =
                        $"{method.ClassName}_" +
                        $"{method.Syntax.Name}_" +
                        $"local_{localCounter++}_" +
                        $"{declaration.Name}";


                    variables.Add(
                        declaration.Name,

                        new VariableSymbol(
                            storageName,
                            variableType
                        )
                    );


                    output.Add(
                        new SemanticVariableDeclaration(
                            declaration.Name,
                            storageName,
                            variableType,
                            initializer
                        )
                    );


                    continue;
                }


                // =====================================================
                // method-call statement
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
                // return
                // =====================================================

                if (
                    statement
                    is ReturnStatementSyntax returnStatement
                )
                {
                    if (method.ReturnType == SemanticType.Void)
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


                    if (returnStatement.Value == null)
                    {
                        throw new Exception(
                            $"Semantic error: {method.ReturnType.ToString().ToLowerInvariant()} method " +
                            $"'{method.ClassName}.{method.Syntax.Name}' " +
                            $"must return a value."
                        );
                    }


                    SemanticValue returnValue =
                        ResolveValue(
                            returnStatement.Value,
                            method.ClassName,
                            variables,
                            methods
                        );


                    SemanticType returnValueType =
                        GetValueType(
                            returnValue
                        );


                    if (returnValueType != method.ReturnType)
                    {
                        throw new Exception(
                            $"Semantic error: method " +
                            $"'{method.ClassName}.{method.Syntax.Name}' " +
                            $"returns {method.ReturnType}, but return expression is {returnValueType}."
                        );
                    }


                    output.Add(
                        new SemanticReturnStatement(
                            returnValue
                        )
                    );


                    continue;
                }


                // =====================================================
                // if / else
                // =====================================================

                if (
                    statement
                    is IfStatementSyntax ifStatement
                )
                {
                    SemanticValue condition =
                        ResolveValue(
                            ifStatement.Condition,
                            method.ClassName,
                            variables,
                            methods
                        );


                    if (
                        GetValueType(condition)
                        != SemanticType.Bool
                    )
                    {
                        throw new Exception(
                            "Semantic error: if condition must be bool."
                        );
                    }


                    // Each branch receives a copy of the current scope.
                    // Variables declared inside a branch do not escape it.
                    Dictionary<string, VariableSymbol> thenVariables =
                        new(variables);


                    Dictionary<string, VariableSymbol> elseVariables =
                        new(variables);


                    List<SemanticStatement> thenStatements =
                        AnalyzeBlock(
                            ifStatement.ThenBlock,
                            method,
                            thenVariables,
                            methods,
                            ref localCounter
                        );


                    List<SemanticStatement> elseStatements =
                        ifStatement.ElseBlock == null
                            ? new List<SemanticStatement>()
                            : AnalyzeBlock(
                                ifStatement.ElseBlock,
                                method,
                                elseVariables,
                                methods,
                                ref localCounter
                            );


                    output.Add(
                        new SemanticIfStatement(
                            condition,
                            thenStatements,
                            elseStatements
                        )
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
                                out VariableSymbol? variable
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
                                method.ClassName,
                                variables,
                                methods
                            );


                        SemanticType valueType =
                            GetValueType(
                                value
                            );


                        if (valueType != variable.Type)
                        {
                            throw new Exception(
                                $"Semantic error: cannot assign " +
                                $"{valueType} to {variable.Type} " +
                                $"variable '{variableName}'."
                            );
                        }


                        output.Add(
                            new SemanticVariableAssignment(
                                variable.StorageName,
                                variable.Type,
                                value
                            )
                        );


                        continue;
                    }


                    // -------------------------------------------------
                    // NES built-in member assignment
                    // -------------------------------------------------

                    if (
                        NesBuiltIns.TryResolveMember(
                            assignment.Target.Parts,
                            out NesBuiltInDefinition builtIn
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


                        SemanticType valueType =
                            GetValueType(
                                value
                            );


                        if (valueType != builtIn.ValueType)
                        {
                            throw new Exception(
                                $"Semantic error: NES built-in member " +
                                $"'{builtIn.TypeName}.{builtIn.MemberName}' " +
                                $"requires {builtIn.ValueType}, but received {valueType}."
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


            return output;
        }


        // =============================================================
        // Expressions
        // =============================================================

        private static SemanticValue ResolveValue(
            ExpressionSyntax expression,
            string currentClass,
            Dictionary<string, VariableSymbol> variables,
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
                is BooleanExpressionSyntax boolean
            )
            {
                return new SemanticBoolLiteral(
                    boolean.Value
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
                        out VariableSymbol? variable
                    )
                )
                {
                    throw new Exception(
                        $"Semantic error: variable " +
                        $"'{identifier.Name}' does not exist."
                    );
                }


                return new SemanticVariableReference(
                    variable.StorageName,
                    variable.Type
                );
            }


            if (
                expression
                is BinaryExpressionSyntax binary
            )
            {
                SemanticValue left =
                    ResolveValue(
                        binary.Left,
                        currentClass,
                        variables,
                        methods
                    );


                SemanticValue right =
                    ResolveValue(
                        binary.Right,
                        currentClass,
                        variables,
                        methods
                    );


                SemanticType leftType =
                    GetValueType(
                        left
                    );


                SemanticType rightType =
                    GetValueType(
                        right
                    );


                // -----------------------------------------------------
                // Addition
                // -----------------------------------------------------

                if (
                    binary.OperatorToken.Kind
                    == TokenKind.Plus
                )
                {
                    if (
                        leftType != SemanticType.Byte ||
                        rightType != SemanticType.Byte
                    )
                    {
                        throw new Exception(
                            "Semantic error: '+' requires byte operands."
                        );
                    }


                    return new SemanticBinaryAdd(
                        left,
                        right
                    );
                }
                if (
                    binary.OperatorToken.Kind
                    == TokenKind.Minus
                )
                {
                    if (
                        leftType != SemanticType.Byte ||
                        rightType != SemanticType.Byte
                    )
                    {
                        throw new Exception(
                            "Semantic error: '-' requires byte operands."
                        );
                    }


                    return new SemanticBinarySubtract(
                        left,
                        right
                    );
                }
                if (
                    binary.OperatorToken.Kind == TokenKind.Ampersand ||
                    binary.OperatorToken.Kind == TokenKind.Pipe ||
                    binary.OperatorToken.Kind == TokenKind.Caret
                )
                {
                    if (
                        leftType != SemanticType.Byte ||
                        rightType != SemanticType.Byte
                    )
                    {
                        throw new Exception(
                            "Semantic error: bitwise operators require byte operands."
                        );
                    }


                    SemanticBitwiseOperator operation =
                        binary.OperatorToken.Kind switch
                        {
                            TokenKind.Ampersand =>
                                SemanticBitwiseOperator.And,

                            TokenKind.Caret =>
                                SemanticBitwiseOperator.Xor,

                            TokenKind.Pipe =>
                                SemanticBitwiseOperator.Or,

                            _ =>
                                throw new Exception(
                                    "Internal compiler error."
                                )
                        };


                    return new SemanticBinaryBitwise(
                        left,
                        operation,
                        right
                    );
                }
                if (
                    binary.OperatorToken.Kind == TokenKind.ShiftLeft ||
                    binary.OperatorToken.Kind == TokenKind.ShiftRight
                )
                {
                    if (
                        leftType != SemanticType.Byte ||
                        rightType != SemanticType.Byte
                    )
                    {
                        throw new Exception(
                            "Semantic error: shift operators require byte operands."
                        );
                    }


                    return new SemanticShift(
                        left,

                        binary.OperatorToken.Kind
                            == TokenKind.ShiftLeft
                                ? SemanticShiftOperator.Left
                                : SemanticShiftOperator.Right,

                        right
                    );
                }

                // -----------------------------------------------------
                // Equality
                // -----------------------------------------------------

                if (
                    binary.OperatorToken.Kind == TokenKind.EqualsEquals ||
                    binary.OperatorToken.Kind == TokenKind.BangEquals
                )
                {
                    if (leftType != rightType)
                    {
                        throw new Exception(
                            "Semantic error: equality operands " +
                            "must have the same type."
                        );
                    }


                    return new SemanticComparison(
                        left,

                        binary.OperatorToken.Kind
                            == TokenKind.EqualsEquals
                                ? SemanticComparisonOperator.Equal
                                : SemanticComparisonOperator.NotEqual,

                        right
                    );
                }


                // -----------------------------------------------------
                // Unsigned byte ordering comparisons
                // -----------------------------------------------------

                if (
                    binary.OperatorToken.Kind == TokenKind.Less ||
                    binary.OperatorToken.Kind == TokenKind.LessEquals ||
                    binary.OperatorToken.Kind == TokenKind.Greater ||
                    binary.OperatorToken.Kind == TokenKind.GreaterEquals
                )
                {
                    if (
                        leftType != SemanticType.Byte ||
                        rightType != SemanticType.Byte
                    )
                    {
                        throw new Exception(
                            "Semantic error: ordering comparisons " +
                            "require byte operands."
                        );
                    }


                    SemanticComparisonOperator operation =
                        binary.OperatorToken.Kind switch
                        {
                            TokenKind.Less =>
                                SemanticComparisonOperator.Less,

                            TokenKind.LessEquals =>
                                SemanticComparisonOperator.LessOrEqual,

                            TokenKind.Greater =>
                                SemanticComparisonOperator.Greater,

                            TokenKind.GreaterEquals =>
                                SemanticComparisonOperator.GreaterOrEqual,

                            _ =>
                                throw new Exception(
                                    "Internal compiler error."
                                )
                        };


                    return new SemanticComparison(
                        left,
                        operation,
                        right
                    );
                }


                throw new Exception(
                    $"Semantic error: unsupported operator " +
                    $"'{binary.OperatorToken.Text}'."
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
            Dictionary<string, VariableSymbol> variables,
            Dictionary<string, MethodSymbol> methods
        )
        {
            // ---------------------------------------------------------
            // NES built-in method
            // ---------------------------------------------------------

            if (
                NesBuiltIns.TryResolveMethod(
                    target.Parts,
                    out NesBuiltInMethodDefinition builtIn
                )
            )
            {
                if (
                    arguments.Count
                    != builtIn.Parameters.Count
                )
                {
                    throw new Exception(
                        $"Semantic error: NES built-in method " +
                        $"'{builtIn.TypeName}.{builtIn.MethodName}' expects " +
                        $"{builtIn.Parameters.Count} argument(s), " +
                        $"but {arguments.Count} were supplied."
                    );
                }


                List<SemanticValue> builtInArguments =
                    new();


                for (
                    int i = 0;
                    i < arguments.Count;
                    i++
                )
                {
                    SemanticValue argumentValue =
                        ResolveValue(
                            arguments[i],
                            currentClass,
                            variables,
                            methods
                        );


                    SemanticType argumentType =
                        GetValueType(
                            argumentValue
                        );


                    SemanticType expectedType =
                        builtIn.Parameters[i].Type;


                    if (argumentType != expectedType)
                    {
                        throw new Exception(
                            $"Semantic error: argument {i + 1} of " +
                            $"'{builtIn.TypeName}.{builtIn.MethodName}' " +
                            $"expects {expectedType}, but received {argumentType}."
                        );
                    }


                    builtInArguments.Add(
                        argumentValue
                    );
                }


                return new SemanticCall(
                    builtIn.RuntimeLabel,

                    builtIn.Parameters
                        .Select(
                            parameter =>
                                parameter.StorageName
                        )
                        .ToList(),

                    builtInArguments,

                    builtIn.ReturnType
                );
            }


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
                    out MethodSymbol? calledMethod
                )
            )
            {
                throw new Exception(
                    $"Semantic error: method " +
                    $"'{key}' does not exist."
                );
            }


            if (!calledMethod.Syntax.IsStatic)
            {
                throw new Exception(
                    $"Semantic error: method " +
                    $"'{key}' is not static."
                );
            }


            if (
                arguments.Count
                != calledMethod.Parameters.Count
            )
            {
                throw new Exception(
                    $"Semantic error: method '{key}' expects " +
                    $"{calledMethod.Parameters.Count} argument(s), " +
                    $"but {arguments.Count} were supplied."
                );
            }


            List<SemanticValue> methodArguments =
                new();


            for (
                int i = 0;
                i < arguments.Count;
                i++
            )
            {
                SemanticValue argumentValue =
                    ResolveValue(
                        arguments[i],
                        currentClass,
                        variables,
                        methods
                    );


                SemanticType argumentType =
                    GetValueType(
                        argumentValue
                    );


                SemanticType expectedType =
                    calledMethod.Parameters[i].Type;


                if (argumentType != expectedType)
                {
                    throw new Exception(
                        $"Semantic error: argument {i + 1} of " +
                        $"'{key}' expects {expectedType}, " +
                        $"but received {argumentType}."
                    );
                }


                methodArguments.Add(
                    argumentValue
                );
            }


            return new SemanticCall(
                calledMethod.Label,

                calledMethod.Parameters
                    .Select(
                        parameter =>
                            parameter.StorageName
                    )
                    .ToList(),

                methodArguments,

                calledMethod.ReturnType
            );
        }


        // =============================================================
        // Type helpers
        // =============================================================

        private static SemanticType GetValueType(
            SemanticValue value
        )
        {
            return value switch
            {
                SemanticByteLiteral =>
                    SemanticType.Byte,

                SemanticBoolLiteral =>
                    SemanticType.Bool,

                SemanticVariableReference variable =>
                    variable.Type,

                SemanticBinaryAdd =>
                    SemanticType.Byte,

                SemanticBinarySubtract =>
                    SemanticType.Byte,

                SemanticBinaryBitwise =>
                    SemanticType.Byte,

                SemanticShift =>
                    SemanticType.Byte,

                SemanticComparison =>
                    SemanticType.Bool,

                SemanticMethodCallValue call =>
                    call.Call.ReturnType,

                _ =>
                    throw new Exception(
                        "Internal compiler error: unknown value type."
                    )
            };
        }


        private static SemanticType ToSemanticType(
            TypeSyntaxKind type
        )
        {
            return type switch
            {
                TypeSyntaxKind.Void =>
                    SemanticType.Void,

                TypeSyntaxKind.Byte =>
                    SemanticType.Byte,

                TypeSyntaxKind.Bool =>
                    SemanticType.Bool,

                _ =>
                    throw new Exception(
                        "Internal compiler error: unknown type."
                    )
            };
        }


        private static bool AlwaysReturns(
            IReadOnlyList<SemanticStatement> statements
        )
        {
            if (statements.Count == 0)
            {
                return false;
            }


            SemanticStatement last =
                statements[^1];


            if (last is SemanticReturnStatement)
            {
                return true;
            }


            if (
                last is SemanticIfStatement ifStatement &&
                ifStatement.ElseStatements.Count > 0
            )
            {
                return
                    AlwaysReturns(
                        ifStatement.ThenStatements
                    ) &&
                    AlwaysReturns(
                        ifStatement.ElseStatements
                    );
            }


            return false;
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
