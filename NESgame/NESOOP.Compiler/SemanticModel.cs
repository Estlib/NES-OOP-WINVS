using System;
using System.Collections.Generic;
using System.Text;

namespace NESOOP.Compiler
{
    public enum SemanticType
    {
        Void,
        Byte,
        Bool
    }

    public enum SemanticBitwiseOperator
    {
        And,
        Xor,
        Or
    }


    public enum SemanticShiftOperator
    {
        Left,
        Right
    }
    public enum SemanticComparisonOperator
    {
        Equal,
        NotEqual,
        Less,
        LessOrEqual,
        Greater,
        GreaterOrEqual
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
        string StorageName,
        SemanticType Type
    );


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


    public sealed record SemanticVariableDeclaration(
        string Name,
        string StorageName,
        SemanticType Type,
        SemanticValue Initializer
    ) : SemanticStatement;


    public sealed record SemanticVariableAssignment(
        string StorageName,
        SemanticType Type,
        SemanticValue Value
    ) : SemanticStatement;


    public sealed record SemanticBuiltInAssignment(
        NesBuiltInMember Member,
        SemanticValue Value
    ) : SemanticStatement;


    public sealed record SemanticMethodCallStatement(
        SemanticCall Call
    ) : SemanticStatement;


    public sealed record SemanticReturnStatement(
        SemanticValue? Value
    ) : SemanticStatement;


    public sealed record SemanticIfStatement(
        SemanticValue Condition,
        IReadOnlyList<SemanticStatement> ThenStatements,
        IReadOnlyList<SemanticStatement> ElseStatements
    ) : SemanticStatement;


    // =========================================================
    // Values
    // =========================================================

    public abstract record SemanticValue;


    public sealed record SemanticByteLiteral(
        byte Value
    ) : SemanticValue;


    public sealed record SemanticBoolLiteral(
        bool Value
    ) : SemanticValue;


    public sealed record SemanticVariableReference(
        string StorageName,
        SemanticType Type
    ) : SemanticValue;


    public sealed record SemanticBinaryAdd(
        SemanticValue Left,
        SemanticValue Right
    ) : SemanticValue;
    public sealed record SemanticBinarySubtract(
    SemanticValue Left,
    SemanticValue Right
) : SemanticValue;


    public sealed record SemanticBinaryBitwise(
        SemanticValue Left,
        SemanticBitwiseOperator Operator,
        SemanticValue Right
    ) : SemanticValue;


    public sealed record SemanticShift(
        SemanticValue Left,
        SemanticShiftOperator Operator,
        SemanticValue Right
    ) : SemanticValue;

    public sealed record SemanticComparison(
        SemanticValue Left,
        SemanticComparisonOperator Operator,
        SemanticValue Right
    ) : SemanticValue;


    public sealed record SemanticMethodCallValue(
        SemanticCall Call
    ) : SemanticValue;
    /*Later this is where things such as classes, fields, methods, object layouts, RAM locations, banks and tasks will live.*/
}
