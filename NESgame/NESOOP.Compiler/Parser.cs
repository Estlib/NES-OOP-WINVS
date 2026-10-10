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
                    TypeSyntaxKind parameterType =
                        ParseType();

                    if (parameterType == TypeSyntaxKind.Void)
                    {
                        throw new Exception(
                            "Parameters cannot have type void."
                        );
                    }

                    Token parameterName =
                        Expect(TokenKind.Identifier);

                    parameters.Add(
                        new ParameterSyntax(
                            parameterType,
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

            if (Current.Kind == TokenKind.Bool)
            {
                Advance();
                return TypeSyntaxKind.Bool;
            }

            throw new Exception(
                $"Expected type, but found '{Current.Text}' " +
                $"at line {Current.Line}, column {Current.Column}."
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
            if (
                Current.Kind == TokenKind.Byte ||
                Current.Kind == TokenKind.Bool
            )
            {
                return ParseVariableDeclaration();
            }


            if (Current.Kind == TokenKind.Return)
            {
                return ParseReturn();
            }


            if (Current.Kind == TokenKind.If)
            {
                return ParseIf();
            }


            return ParseIdentifierStatement();
        }


        private VariableDeclarationStatementSyntax
    ParseVariableDeclaration()
        {
            TypeSyntaxKind type =
                ParseType();


            if (type == TypeSyntaxKind.Void)
            {
                throw new Exception(
                    "Local variables cannot have type void."
                );
            }


            Token name =
                Expect(TokenKind.Identifier);


            Expect(TokenKind.Equals);


            ExpressionSyntax initializer =
                ParseExpression();


            Expect(TokenKind.Semicolon);


            return new VariableDeclarationStatementSyntax(
                type,
                name.Text,
                name,
                initializer
            );
        }

        private IfStatementSyntax ParseIf()
        {
            Expect(TokenKind.If);

            Expect(TokenKind.LeftParen);

            ExpressionSyntax condition =
                ParseExpression();

            Expect(TokenKind.RightParen);


            BlockSyntax thenBlock =
                ParseBlock();


            BlockSyntax? elseBlock =
                null;


            if (Current.Kind == TokenKind.Else)
            {
                Advance();

                elseBlock =
                    ParseBlock();
            }


            return new IfStatementSyntax(
                condition,
                thenBlock,
                elseBlock
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
            return ParseBitwiseOrExpression();
        }


        private ExpressionSyntax ParseBitwiseOrExpression()
        {
            ExpressionSyntax left =
                ParseBitwiseXorExpression();

            while (Current.Kind == TokenKind.Pipe)
            {
                Token operatorToken = Current;
                Advance();

                ExpressionSyntax right =
                    ParseBitwiseXorExpression();

                left = new BinaryExpressionSyntax(
                    left,
                    operatorToken,
                    right
                );
            }

            return left;
        }


        private ExpressionSyntax ParseBitwiseXorExpression()
        {
            ExpressionSyntax left =
                ParseBitwiseAndExpression();

            while (Current.Kind == TokenKind.Caret)
            {
                Token operatorToken = Current;
                Advance();

                ExpressionSyntax right =
                    ParseBitwiseAndExpression();

                left = new BinaryExpressionSyntax(
                    left,
                    operatorToken,
                    right
                );
            }

            return left;
        }


        private ExpressionSyntax ParseBitwiseAndExpression()
        {
            ExpressionSyntax left =
                ParseEqualityExpression();

            while (Current.Kind == TokenKind.Ampersand)
            {
                Token operatorToken = Current;
                Advance();

                ExpressionSyntax right =
                    ParseEqualityExpression();

                left = new BinaryExpressionSyntax(
                    left,
                    operatorToken,
                    right
                );
            }

            return left;
        }


        private ExpressionSyntax ParseEqualityExpression()
        {
            ExpressionSyntax left =
                ParseComparisonExpression();

            while (
                Current.Kind == TokenKind.EqualsEquals ||
                Current.Kind == TokenKind.BangEquals
            )
            {
                Token operatorToken =
                    Current;

                Advance();

                ExpressionSyntax right =
                    ParseComparisonExpression();

                left = new BinaryExpressionSyntax(
                    left,
                    operatorToken,
                    right
                );
            }

            return left;
        }


        private ExpressionSyntax ParseComparisonExpression()
        {
            ExpressionSyntax left =
                ParseShiftExpression();

            while (
                Current.Kind == TokenKind.Less ||
                Current.Kind == TokenKind.LessEquals ||
                Current.Kind == TokenKind.Greater ||
                Current.Kind == TokenKind.GreaterEquals
            )
            {
                Token operatorToken =
                    Current;

                Advance();

                ExpressionSyntax right =
                    ParseShiftExpression();

                left = new BinaryExpressionSyntax(
                    left,
                    operatorToken,
                    right
                );
            }

            return left;
        }


        private ExpressionSyntax ParseShiftExpression()
        {
            ExpressionSyntax left =
                ParseAdditiveExpression();

            while (
                Current.Kind == TokenKind.ShiftLeft ||
                Current.Kind == TokenKind.ShiftRight
            )
            {
                Token operatorToken =
                    Current;

                Advance();

                ExpressionSyntax right =
                    ParseAdditiveExpression();

                left = new BinaryExpressionSyntax(
                    left,
                    operatorToken,
                    right
                );
            }

            return left;
        }


        private ExpressionSyntax ParseAdditiveExpression()
        {
            ExpressionSyntax left =
                ParsePrimaryExpression();

            while (
                Current.Kind == TokenKind.Plus ||
                Current.Kind == TokenKind.Minus
            )
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
            // Parenthesized expression
            // ---------------------------------------------------------

            if (Current.Kind == TokenKind.LeftParen)
            {
                Advance();

                ExpressionSyntax expression =
                    ParseExpression();

                Expect(TokenKind.RightParen);

                return expression;
            }
            // ---------------------------------------------------------
            // Number
            // ---------------------------------------------------------

            if (Current.Kind == TokenKind.Number)
            {
                return ParseNumber();
            }
            // ---------------------------------------------------------
            // Boolean
            // ---------------------------------------------------------

            if (
                Current.Kind == TokenKind.True ||
                Current.Kind == TokenKind.False
            )
            {
                Token token =
                    Current;

                bool value =
                    Current.Kind == TokenKind.True;

                Advance();


                return new BooleanExpressionSyntax(
                    value,
                    token
                );
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
