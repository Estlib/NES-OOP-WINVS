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

            Token name =
                Expect(TokenKind.Identifier);

            Expect(TokenKind.LeftBrace);

            List<MethodDeclarationSyntax> methods =
                new();

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


            TypeSyntaxKind returnType =
                ParseType();


            Token name =
                Expect(TokenKind.Identifier);


            Expect(TokenKind.LeftParen);


            List<ParameterSyntax> parameters =
                new();


            if (Current.Kind != TokenKind.RightParen)
            {
                while (true)
                {
                    // Only byte parameters exist for now.
                    Expect(TokenKind.Byte);

                    Token parameterName =
                        Expect(TokenKind.Identifier);

                    parameters.Add(
                        new ParameterSyntax(
                            parameterName.Text,
                            parameterName
                        )
                    );


                    if (Current.Kind != TokenKind.Comma)
                        break;

                    Advance();
                }
            }


            Expect(TokenKind.RightParen);


            BlockSyntax body =
                ParseBlock();


            return new MethodDeclarationSyntax(
                isStatic,
                returnType,
                name.Text,
                parameters,
                body
            );
        }


        private TypeSyntaxKind ParseType()
        {
            if (Current.Kind == TokenKind.Void)
            {
                Advance();
                return TypeSyntaxKind.Void;
            }

            if (Current.Kind == TokenKind.Byte)
            {
                Advance();
                return TypeSyntaxKind.Byte;
            }


            throw new Exception(
                $"Expected type, but found " +
                $"'{Current.Text}' at line " +
                $"{Current.Line}, column {Current.Column}."
            );
        }


        private BlockSyntax ParseBlock()
        {
            Expect(TokenKind.LeftBrace);

            List<StatementSyntax> statements =
                new();

            while (Current.Kind != TokenKind.RightBrace)
            {
                statements.Add(
                    ParseStatement()
                );
            }

            Expect(TokenKind.RightBrace);

            return new BlockSyntax(
                statements
            );
        }


        private StatementSyntax ParseStatement()
        {
            if (Current.Kind == TokenKind.Byte)
            {
                return ParseVariableDeclaration();
            }


            if (Current.Kind == TokenKind.Return)
            {
                return ParseReturn();
            }


            return ParseIdentifierStatement();
        }


        private VariableDeclarationStatementSyntax
            ParseVariableDeclaration()
        {
            Expect(TokenKind.Byte);

            Token name =
                Expect(TokenKind.Identifier);

            Expect(TokenKind.Equals);

            ExpressionSyntax initializer =
                ParseExpression();

            Expect(TokenKind.Semicolon);


            return new VariableDeclarationStatementSyntax(
                name.Text,
                name,
                initializer
            );
        }


        private ReturnStatementSyntax ParseReturn()
        {
            Token returnToken =
                Expect(TokenKind.Return);


            // return;
            if (Current.Kind == TokenKind.Semicolon)
            {
                Advance();

                return new ReturnStatementSyntax(
                    null,
                    returnToken
                );
            }


            // return expression;
            ExpressionSyntax value =
                ParseExpression();

            Expect(TokenKind.Semicolon);


            return new ReturnStatementSyntax(
                value,
                returnToken
            );
        }


        private StatementSyntax ParseIdentifierStatement()
        {
            MemberAccessExpressionSyntax target =
                ParseMemberAccess();


            // ---------------------------------------------------------
            // Method call statement
            //
            // DoThing();
            // Palette.DoThing();
            // ---------------------------------------------------------

            if (Current.Kind == TokenKind.LeftParen)
            {
                IReadOnlyList<ExpressionSyntax> arguments =
                    ParseArguments();

                Expect(TokenKind.Semicolon);


                return new MethodCallStatementSyntax(
                    target,
                    arguments
                );
            }


            // ---------------------------------------------------------
            // Assignment
            // ---------------------------------------------------------

            Expect(TokenKind.Equals);

            ExpressionSyntax value =
                ParseExpression();

            Expect(TokenKind.Semicolon);


            return new AssignmentStatementSyntax(
                target,
                value
            );
        }


        // =============================================================
        // Expressions
        // =============================================================

        private ExpressionSyntax ParseExpression()
        {
            ExpressionSyntax left =
                ParsePrimaryExpression();


            while (Current.Kind == TokenKind.Plus)
            {
                Token operatorToken =
                    Current;

                Advance();


                ExpressionSyntax right =
                    ParsePrimaryExpression();


                left = new BinaryExpressionSyntax(
                    left,
                    operatorToken,
                    right
                );
            }


            return left;
        }


        private ExpressionSyntax ParsePrimaryExpression()
        {
            // ---------------------------------------------------------
            // Number
            // ---------------------------------------------------------

            if (Current.Kind == TokenKind.Number)
            {
                return ParseNumber();
            }


            // ---------------------------------------------------------
            // Identifier / method call
            // ---------------------------------------------------------

            if (Current.Kind == TokenKind.Identifier)
            {
                Token firstToken =
                    Current;


                MemberAccessExpressionSyntax target =
                    ParseMemberAccess();


                // Method call expression
                if (Current.Kind == TokenKind.LeftParen)
                {
                    IReadOnlyList<ExpressionSyntax> arguments =
                        ParseArguments();


                    return new MethodCallExpressionSyntax(
                        target,
                        arguments
                    );
                }


                // Bare variable identifier
                if (target.Parts.Count == 1)
                {
                    return new IdentifierExpressionSyntax(
                        target.Parts[0],
                        firstToken
                    );
                }


                throw new Exception(
                    $"Unexpected member access expression at " +
                    $"line {firstToken.Line}, " +
                    $"column {firstToken.Column}."
                );
            }


            throw new Exception(
                $"Expected expression, but found " +
                $"'{Current.Text}' at line " +
                $"{Current.Line}, column {Current.Column}."
            );
        }


        private IReadOnlyList<ExpressionSyntax> ParseArguments()
        {
            Expect(TokenKind.LeftParen);

            List<ExpressionSyntax> arguments =
                new();


            if (Current.Kind != TokenKind.RightParen)
            {
                while (true)
                {
                    arguments.Add(
                        ParseExpression()
                    );


                    if (Current.Kind != TokenKind.Comma)
                        break;


                    Advance();
                }
            }


            Expect(TokenKind.RightParen);

            return arguments;
        }


        private MemberAccessExpressionSyntax ParseMemberAccess()
        {
            List<string> parts =
                new();


            Token first =
                Expect(TokenKind.Identifier);

            parts.Add(
                first.Text
            );


            while (Current.Kind == TokenKind.Dot)
            {
                Advance();

                Token next =
                    Expect(TokenKind.Identifier);

                parts.Add(
                    next.Text
                );
            }


            return new MemberAccessExpressionSyntax(
                parts
            );
        }


        private NumberExpressionSyntax ParseNumber()
        {
            Token token =
                Expect(TokenKind.Number);


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
                value = int.Parse(
                    token.Text
                );
            }


            return new NumberExpressionSyntax(
                value,
                token
            );
        }


        // =============================================================
        // Helpers
        // =============================================================

        private Token Expect(
            TokenKind kind
        )
        {
            if (Current.Kind != kind)
            {
                throw new Exception(
                    $"Expected {kind}, but found " +
                    $"'{Current.Text}' at line " +
                    $"{Current.Line}, column {Current.Column}."
                );
            }


            Token token =
                Current;

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
