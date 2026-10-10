using System;
using System.Collections.Generic;
using System.Text;

namespace NESOOP.Compiler
{
    public enum TokenKind
    {
        // Keywords
        Class,
        Static,
        Void,
        Return,
        True,
        False,
        If,
        Else,
        While,
        Do,
        For,
        Break,
        Continue,

        //datatypes
        Byte,
        Bool,


        // Values / names
        Identifier,
        Number,


        // Symbols
        LeftBrace,
        RightBrace,
        LeftParen,
        RightParen,
        Dot,
        Equals,
        Semicolon,
        Plus,
        Comma,
        Minus,
        Ampersand,
        Pipe,
        Caret,
        ShiftLeft,
        ShiftRight,

        //logic operators & comparison
        EqualsEquals,
        BangEquals,
        Less,
        LessEquals,
        Greater,
        GreaterEquals,

        // End of source
        EndOfFile

    }
}
