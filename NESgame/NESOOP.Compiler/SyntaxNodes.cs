using System;
using System.Collections.Generic;
using System.Text;

namespace NESOOP.Compiler
{
    public sealed record CompilationUnitSyntax(
        IReadOnlyList<ClassDeclarationSyntax> Classes
    );


    public sealed record ClassDeclarationSyntax(
        string Name,
        IReadOnlyList<MethodDeclarationSyntax> Methods
    );


    public sealed record MethodDeclarationSyntax(
        bool IsStatic,
        string Name,
        BlockSyntax Body
    );


    public sealed record BlockSyntax(
        IReadOnlyList<StatementSyntax> Statements
    );


    // =========================================================
    // Statements
    // =========================================================

    public abstract record StatementSyntax;


    public sealed record VariableDeclarationStatementSyntax(
        string Name,
        Token NameToken,
        ExpressionSyntax Initializer
    ) : StatementSyntax;


    public sealed record AssignmentStatementSyntax(
        MemberAccessExpressionSyntax Target,
        ExpressionSyntax Value
    ) : StatementSyntax;

    public sealed record MethodCallStatementSyntax(
    MemberAccessExpressionSyntax Target
    ) : StatementSyntax;


    // =========================================================
    // Expressions
    // =========================================================

    public abstract record ExpressionSyntax;


    public sealed record NumberExpressionSyntax(
        int Value,
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


    public sealed record MemberAccessExpressionSyntax(
        IReadOnlyList<string> Parts
    );


}
