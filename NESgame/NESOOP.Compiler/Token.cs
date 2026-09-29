using System;
using System.Collections.Generic;
using System.Text;

namespace NESOOP.Compiler
{
    public readonly record struct Token(
        TokenKind Kind,
        string Text,
        int Line,
        int Column
    )
    {
        public override string ToString()
        {
            return $"{Line}:{Column}  {Kind,-14} {Text}";
        }
    }
}
