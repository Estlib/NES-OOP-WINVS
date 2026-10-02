using System;
using System.Collections.Generic;
using System.Text;

namespace NESOOP.Compiler
{
    public sealed record SemanticProgram(
        IReadOnlyList<SemanticMethod> Methods
    );


    public sealed record SemanticMethod(
        string ClassName,
        string Name,
        string Label,
        IReadOnlyList<SemanticStatement> Statements
    );


    // =========================================================
    // Statements
    // =========================================================

    public abstract record SemanticStatement;


    public sealed record SemanticByteDeclaration(
        string Name,
        string StorageName,
        SemanticValue Initializer
    ) : SemanticStatement;


    public sealed record SemanticVariableAssignment(
        string StorageName,
        SemanticValue Value
    ) : SemanticStatement;


    public sealed record SemanticBackgroundColorAssignment(
        SemanticValue Value
    ) : SemanticStatement;


    public sealed record SemanticMethodCall(
        string TargetLabel
    ) : SemanticStatement;


    // =========================================================
    // Values
    // =========================================================

    public abstract record SemanticValue;


    public sealed record SemanticByteLiteral(
        byte Value
    ) : SemanticValue;


    public sealed record SemanticVariableReference(
        string StorageName
    ) : SemanticValue;


    public sealed record SemanticBinaryAdd(
        SemanticValue Left,
        SemanticValue Right
    ) : SemanticValue;
    /*Later this is where things such as classes, fields, methods, object layouts, RAM locations, banks and tasks will live.*/
}
