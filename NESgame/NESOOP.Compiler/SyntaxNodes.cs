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

    public abstract record StatementSyntax;

    public sealed record AssignmentStatementSyntax(
        MemberAccessExpressionSyntax Target,
        NumberExpressionSyntax Value
    ) : StatementSyntax;

    public sealed record MemberAccessExpressionSyntax(
        IReadOnlyList<string> Parts
    );

    public sealed record NumberExpressionSyntax(
        int Value,
        Token Token
    );
}
