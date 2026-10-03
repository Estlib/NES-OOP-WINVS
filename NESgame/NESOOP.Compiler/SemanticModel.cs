using System;
using System.Collections.Generic;
using System.Text;

namespace NESOOP.Compiler
{
    public enum SemanticType
    {
        Void,
        Byte
    }


    public sealed record SemanticProgram(
        IReadOnlyList<SemanticMethod> Methods
    );


    public sealed record SemanticMethod(
        string ClassName,
        string Name,
        string Label,
        SemanticType ReturnType,
        IReadOnlyList<SemanticParameter> Parameters,
        IReadOnlyList<SemanticStatement> Statements
    );


    public sealed record SemanticParameter(
        string Name,
        string StorageName
    );


    // =========================================================
    // Calls
    // =========================================================

    public sealed record SemanticCall(
        string TargetLabel,
        IReadOnlyList<string> ParameterStorageNames,
        IReadOnlyList<SemanticValue> Arguments,
        SemanticType ReturnType
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


    public sealed record SemanticMethodCallStatement(
        SemanticCall Call
    ) : SemanticStatement;


    public sealed record SemanticReturnStatement(
        SemanticValue? Value
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


    public sealed record SemanticMethodCallValue(
        SemanticCall Call
    ) : SemanticValue;
    /*Later this is where things such as classes, fields, methods, object layouts, RAM locations, banks and tasks will live.*/
}
