using System;
using System.Collections.Generic;
using System.Text;

namespace NESOOP.Compiler
{
    public enum TypeSyntaxKind
    {
        Void,
        Byte,
        Bool
    }


    public sealed record CompilationUnitSyntax(
        IReadOnlyList<ClassDeclarationSyntax> Classes
    );


    public sealed record ClassDeclarationSyntax(
        string Name,
        IReadOnlyList<MethodDeclarationSyntax> Methods
    );


    public sealed record MethodDeclarationSyntax(
        bool IsStatic,
        TypeSyntaxKind ReturnType,
        string Name,
        IReadOnlyList<ParameterSyntax> Parameters,
        BlockSyntax Body
    );


    public sealed record ParameterSyntax(
        TypeSyntaxKind Type,
        string Name,
        Token NameToken
    );


    public sealed record BlockSyntax(
        IReadOnlyList<StatementSyntax> Statements
    );


    // =========================================================
    // Statements
    // =========================================================

    public abstract record StatementSyntax;


    public sealed record VariableDeclarationStatementSyntax(
        TypeSyntaxKind Type,
        string Name,
        Token NameToken,
        ExpressionSyntax Initializer
    ) : StatementSyntax;


    public sealed record AssignmentStatementSyntax(
        MemberAccessExpressionSyntax Target,
        ExpressionSyntax Value
    ) : StatementSyntax;


    public sealed record MethodCallStatementSyntax(
        MemberAccessExpressionSyntax Target,
        IReadOnlyList<ExpressionSyntax> Arguments
    ) : StatementSyntax;


    public sealed record ReturnStatementSyntax(
        ExpressionSyntax? Value,
        Token ReturnToken
    ) : StatementSyntax;


    public sealed record IfStatementSyntax(
        ExpressionSyntax Condition,
        BlockSyntax ThenBlock,
        BlockSyntax? ElseBlock
    ) : StatementSyntax;
    public sealed record WhileStatementSyntax(
    ExpressionSyntax Condition,
    BlockSyntax Body,
    BlockSyntax? ElseBlock
) : StatementSyntax;


    public sealed record DoWhileStatementSyntax(
        BlockSyntax Body,
        ExpressionSyntax Condition
    ) : StatementSyntax;


    public sealed record ForStatementSyntax(
        StatementSyntax? Initializer,
        ExpressionSyntax? Condition,
        StatementSyntax? Iterator,
        BlockSyntax Body,
        BlockSyntax? ElseBlock
    ) : StatementSyntax;


    public sealed record BreakStatementSyntax(
        Token Token
    ) : StatementSyntax;


    public sealed record ContinueStatementSyntax(
        Token Token
    ) : StatementSyntax;


    // =========================================================
    // Expressions
    // =========================================================

    public abstract record ExpressionSyntax;


    public sealed record NumberExpressionSyntax(
        int Value,
        Token Token
    ) : ExpressionSyntax;


    public sealed record BooleanExpressionSyntax(
        bool Value,
        Token Token
    ) : ExpressionSyntax;


    public sealed record IdentifierExpressionSyntax(
        string Name,
        Token Token
    ) : ExpressionSyntax;


    public sealed record BinaryExpressionSyntax(
        ExpressionSyntax Left,
        Token OperatorToken,
        ExpressionSyntax Right
    ) : ExpressionSyntax;


    public sealed record MethodCallExpressionSyntax(
        MemberAccessExpressionSyntax Target,
        IReadOnlyList<ExpressionSyntax> Arguments
    ) : ExpressionSyntax;


    public sealed record MemberAccessExpressionSyntax(
        IReadOnlyList<string> Parts
    );

}
