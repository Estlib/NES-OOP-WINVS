using System;
using System.Collections.Generic;
using System.Text;

namespace NESOOP.Compiler
{
    public sealed class Parser
    {
        private readonly List<Token> _tokens;
        private int _position;


        public Parser(List<Token> tokens)
        {
            _tokens = tokens;
        }


        public CompilationUnitSyntax Parse()
        {
            List<ClassDeclarationSyntax> classes = new();

            while (Current.Kind != TokenKind.EndOfFile)
            {
                classes.Add(ParseClass());
            }

            return new CompilationUnitSyntax(classes);
        }


        private ClassDeclarationSyntax ParseClass()
        {
            Expect(TokenKind.Class);

            Token name = Expect(TokenKind.Identifier);

            Expect(TokenKind.LeftBrace);

            List<MethodDeclarationSyntax> methods = new();

            while (Current.Kind != TokenKind.RightBrace)
            {
                methods.Add(ParseMethod());
            }

            Expect(TokenKind.RightBrace);

            return new ClassDeclarationSyntax(
                name.Text,
                methods
            );
        }


        private MethodDeclarationSyntax ParseMethod()
        {
            bool isStatic = false;

            if (Current.Kind == TokenKind.Static)
            {
                Advance();
                isStatic = true;
            }

            Expect(TokenKind.Void);

            Token name = Expect(TokenKind.Identifier);

            Expect(TokenKind.LeftParen);
            Expect(TokenKind.RightParen);

            BlockSyntax body = ParseBlock();

            return new MethodDeclarationSyntax(
                isStatic,
                name.Text,
                body
            );
        }


        private BlockSyntax ParseBlock()
        {
            Expect(TokenKind.LeftBrace);

            List<StatementSyntax> statements = new();

            while (Current.Kind != TokenKind.RightBrace)
            {
                statements.Add(ParseStatement());
            }

            Expect(TokenKind.RightBrace);

            return new BlockSyntax(statements);
        }


        private StatementSyntax ParseStatement()
        {
            return ParseAssignment();
        }


        private AssignmentStatementSyntax ParseAssignment()
        {
            MemberAccessExpressionSyntax target =
                ParseMemberAccess();

            Expect(TokenKind.Equals);

            NumberExpressionSyntax value =
                ParseNumber();

            Expect(TokenKind.Semicolon);

            return new AssignmentStatementSyntax(
                target,
                value
            );
        }


        private MemberAccessExpressionSyntax ParseMemberAccess()
        {
            List<string> parts = new();

            Token first = Expect(TokenKind.Identifier);

            parts.Add(first.Text);

            while (Current.Kind == TokenKind.Dot)
            {
                Advance();

                Token next =
                    Expect(TokenKind.Identifier);

                parts.Add(next.Text);
            }

            return new MemberAccessExpressionSyntax(parts);
        }


        private NumberExpressionSyntax ParseNumber()
        {
            Token token = Expect(TokenKind.Number);

            int value;

            if (
                token.Text.StartsWith("0x") ||
                token.Text.StartsWith("0X")
            )
            {
                value = Convert.ToInt32(
                    token.Text[2..],
                    16
                );
            }
            else
            {
                value = int.Parse(token.Text);
            }

            return new NumberExpressionSyntax(
                value,
                token
            );
        }


        private Token Expect(TokenKind kind)
        {
            if (Current.Kind != kind)
            {
                throw new Exception(
                    $"Expected {kind}, but found " +
                    $"'{Current.Text}' at " +
                    $"line {Current.Line}, " +
                    $"column {Current.Column}."
                );
            }

            Token token = Current;

            Advance();

            return token;
        }


        private void Advance()
        {
            if (_position < _tokens.Count - 1)
            {
                _position++;
            }
        }


        private Token Current =>
            _tokens[_position];
    }
}
