using System;
using System.Collections.Generic;
using System.Text;

namespace NESOOP.Compiler
{
    public sealed class Lexer
    {
        private readonly string _source;

        private int _position = 0;
        private int _line = 1;
        private int _column = 1;


        public Lexer(string source)
        {
            _source = source;
        }


        public List<Token> Lex()
        {
            List<Token> tokens = new();

            while (!IsAtEnd())
            {
                char current = Current();

                // ---------------------------------------------
                // Whitespace
                // ---------------------------------------------

                if (char.IsWhiteSpace(current))
                {
                    SkipWhitespace();
                    continue;
                }


                // ---------------------------------------------
                // Comments
                //
                // // this is a comment
                // ---------------------------------------------

                if (current == '/' && Peek(1) == '/')
                {
                    SkipLineComment();
                    continue;
                }


                // ---------------------------------------------
                // Identifiers and keywords
                // ---------------------------------------------

                if (char.IsLetter(current) || current == '_')
                {
                    tokens.Add(ReadIdentifierOrKeyword());
                    continue;
                }


                // ---------------------------------------------
                // Numbers
                //
                // 123
                // 0x21
                // ---------------------------------------------

                if (char.IsDigit(current))
                {
                    tokens.Add(ReadNumber());
                    continue;
                }


                // ---------------------------------------------
                // Symbols
                // ---------------------------------------------

                int line = _line;
                int column = _column;

                switch (current)
                {
                    case '{':
                        tokens.Add(
                            new Token(
                                TokenKind.LeftBrace,
                                "{",
                                line,
                                column
                            )
                        );

                        Advance();
                        break;


                    case '}':
                        tokens.Add(
                            new Token(
                                TokenKind.RightBrace,
                                "}",
                                line,
                                column
                            )
                        );

                        Advance();
                        break;


                    case '(':
                        tokens.Add(
                            new Token(
                                TokenKind.LeftParen,
                                "(",
                                line,
                                column
                            )
                        );

                        Advance();
                        break;


                    case ')':
                        tokens.Add(
                            new Token(
                                TokenKind.RightParen,
                                ")",
                                line,
                                column
                            )
                        );

                        Advance();
                        break;


                    case '.':
                        tokens.Add(
                            new Token(
                                TokenKind.Dot,
                                ".",
                                line,
                                column
                            )
                        );

                        Advance();
                        break;


                    case '=':
                        tokens.Add(
                            new Token(
                                TokenKind.Equals,
                                "=",
                                line,
                                column
                            )
                        );

                        Advance();
                        break;


                    case ';':
                        tokens.Add(
                            new Token(
                                TokenKind.Semicolon,
                                ";",
                                line,
                                column
                            )
                        );

                        Advance();
                        break;

                    case '+':
                        tokens.Add(
                            new Token(
                                TokenKind.Plus,
                                "+",
                                line,
                                column
                            )
                        );

                        Advance();
                        break;


                    default:
                        throw new Exception(
                            $"Unexpected character '{current}' " +
                            $"at line {_line}, column {_column}."
                        );
                }
            }


            tokens.Add(
                new Token(
                    TokenKind.EndOfFile,
                    "",
                    _line,
                    _column
                )
            );

            return tokens;
        }


        private Token ReadIdentifierOrKeyword()
        {
            int start = _position;

            int line = _line;
            int column = _column;

            while (
                !IsAtEnd() &&
                (
                    char.IsLetterOrDigit(Current()) ||
                    Current() == '_'
                )
            )
            {
                Advance();
            }

            string text = _source[start.._position];


            TokenKind kind = text switch
            {
                "class" => TokenKind.Class,
                "static" => TokenKind.Static,
                "void" => TokenKind.Void,
                "byte" => TokenKind.Byte,

                _ => TokenKind.Identifier
            };


            return new Token(
                kind,
                text,
                line,
                column
            );
        }


        private Token ReadNumber()
        {
            int start = _position;

            int line = _line;
            int column = _column;


            // ---------------------------------------------
            // Hexadecimal number
            //
            // 0x21
            // ---------------------------------------------

            if (
                Current() == '0' &&
                (
                    Peek(1) == 'x' ||
                    Peek(1) == 'X'
                )
            )
            {
                Advance(); // 0
                Advance(); // x

                if (!IsHexDigit(Current()))
                {
                    throw new Exception(
                        $"Expected hexadecimal digits at " +
                        $"line {line}, column {column}."
                    );
                }

                while (
                    !IsAtEnd() &&
                    IsHexDigit(Current())
                )
                {
                    Advance();
                }
            }

            // ---------------------------------------------
            // Decimal number
            // ---------------------------------------------

            else
            {
                while (
                    !IsAtEnd() &&
                    char.IsDigit(Current())
                )
                {
                    Advance();
                }
            }


            string text = _source[start.._position];

            return new Token(
                TokenKind.Number,
                text,
                line,
                column
            );
        }


        private void SkipWhitespace()
        {
            while (
                !IsAtEnd() &&
                char.IsWhiteSpace(Current())
            )
            {
                Advance();
            }
        }


        private void SkipLineComment()
        {
            // Skip //
            Advance();
            Advance();

            while (
                !IsAtEnd() &&
                Current() != '\n'
            )
            {
                Advance();
            }
        }


        private void Advance()
        {
            if (IsAtEnd())
                return;


            char current = _source[_position];

            _position++;


            if (current == '\n')
            {
                _line++;
                _column = 1;
            }
            else
            {
                _column++;
            }
        }


        private char Current()
        {
            if (IsAtEnd())
                return '\0';

            return _source[_position];
        }


        private char Peek(int offset)
        {
            int position = _position + offset;

            if (position >= _source.Length)
                return '\0';

            return _source[position];
        }


        private bool IsAtEnd()
        {
            return _position >= _source.Length;
        }


        private static bool IsHexDigit(char character)
        {
            return
                char.IsDigit(character) ||
                character >= 'a' && character <= 'f' ||
                character >= 'A' && character <= 'F';
        }
    }
}
