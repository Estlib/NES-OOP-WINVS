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

        //datatypes
        Byte,

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

        // End of source
        EndOfFile

    }
}
