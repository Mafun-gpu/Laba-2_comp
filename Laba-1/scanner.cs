using System;
using System.Collections.Generic;
using System.Text;

namespace Laba_1
{
    public enum TokenCode
    {
        Keyword = 1,           // ключевое слово (val)
        Identifier = 2,        // идентификатор
        Space = 3,             // пробел
        AssignOp = 4,          // оператор присваивания (=)
        ListOf = 12,           // ключевое слово listOf
        LBracket = 5,          // открывающая скобка (
        StringLiteral = 6,     // строковый литерал
        Comma = 7,             // запятая
        RBracket = 8,          // закрывающая скобка )
        Semicolon = 9,         // конец оператора ;
        Integer = 10,          // целое число
        Float = 11,            // вещественное число
        Error = 99             // недопустимый символ
    }

    public class Token
    {
        public TokenCode Code { get; set; }
        public string Type { get; set; }
        public string Lexeme { get; set; }
        public int StartPos { get; set; }
        public int EndPos { get; set; }
        public int Line { get; set; }

        public override string ToString()
        {
            return $"[{Line}:{StartPos}-{EndPos}] ({(int)Code}) {Type} : '{Lexeme}'";
        }
    }

    public class Scanner
    {
        private string _text;
        private int _pos;
        private int _line;
        private int _linePos;
        private List<Token> _tokens;

        public Scanner()
        {
            _tokens = new List<Token>();
        }

        public List<Token> Scan(string text)
        {
            _text = text;
            _pos = 0;
            _line = 1;
            _linePos = 1;
            _tokens.Clear();

            while (!IsEnd())
            {
                // пропускаем все пробельные символы без создания токенов
                if (char.IsWhiteSpace(CurrentChar()))
                {
                    Advance();
                    continue;
                }
                char ch = CurrentChar();

                if (char.IsDigit(ch) || (ch == '-' && char.IsDigit(PeekNext())))
                    ReadNumber();
                else
                {
                    switch (ch)
                    {
                        case var c when char.IsWhiteSpace(c):
                            Advance();
                            break;

                        case var c when char.IsLetter(c) && c >= 65 && c <= 122:
                            ReadIdentifierOrKeyword();
                            break;
                        case '=':
                            AddToken(TokenCode.AssignOp, "оператор присваивания", ch.ToString());
                            Advance();
                            break;
                        case '(':
                            AddToken(TokenCode.LBracket, "открывающая скобка", ch.ToString());
                            Advance();
                            break;
                        case ')':
                            AddToken(TokenCode.RBracket, "закрывающая скобка", ch.ToString());
                            Advance();
                            break;
                        case ',':
                            AddToken(TokenCode.Comma, "запятая", ch.ToString());
                            Advance();
                            break;
                        case ';':
                            AddToken(TokenCode.Semicolon, "конец оператора", ch.ToString());
                            Advance();
                            break;
                        case '"':
                            ReadStringLiteral();
                            break;
                        default:
                            AddToken(TokenCode.Error, "недопустимый символ", ch.ToString());
                            Advance();
                            break;
                    }
                }

                /*
                // Если была зафиксирована ошибка, завершаем сканирование
                if (_tokens.Count > 0 && _tokens[_tokens.Count - 1].Code == TokenCode.Error)
                {
                    break;
                }
                */
            }

            return _tokens;
        }

        // Читаем последовательность LETTER → ключевые слова: val, listOf, или Identifier 
        private void ReadIdentifierOrKeyword()
        {
            int startPos = _linePos;
            var sb = new StringBuilder();

            // ⟵ граф: только LETTER (A–Z, a–z), без цифр и _
            while (!IsEnd() && IsLatinLetter(CurrentChar()))
            {
                sb.Append(CurrentChar());
                Advance();
            }

            string lexeme = sb.ToString();
            if (lexeme == "val")
            {
                AddToken(TokenCode.Keyword, "ключевое слово", lexeme, startPos, _linePos - 1, _line);
            }
            else if (lexeme == "listOf")
            {
                AddToken(TokenCode.ListOf, "ключевое слово", lexeme, startPos, _linePos - 1, _line);
            }
            else
            {
                AddToken(TokenCode.Identifier, "идентификатор", lexeme, startPos, _linePos - 1, _line);
            }
        }

        // Метод для проверки, является ли символ латинской буквой
        private bool IsLatinLetter(char ch)
        {
            return (ch >= 'a' && ch <= 'z') || (ch >= 'A' && ch <= 'Z');
        }

        private void ReadNumber()
        {
            int startPos = _linePos;
            var sb = new StringBuilder();
            bool isFloat = false;

            if (CurrentChar() == '-')
            {
                sb.Append(CurrentChar());
                Advance();
            }

            while (!IsEnd())
            {
                if (char.IsDigit(CurrentChar()))
                {
                    sb.Append(CurrentChar());
                    Advance();
                }
                else if (CurrentChar() == '.' && char.IsDigit(PeekNext()) && !isFloat)
                {
                    isFloat = true;
                    sb.Append(CurrentChar());
                    Advance();
                }
                else break;
            }

            var lexeme = sb.ToString();
            if (isFloat)
                AddToken(TokenCode.Float, "вещественное число", lexeme, startPos, _linePos - 1, _line);
            else
                AddToken(TokenCode.Integer, "целое число", lexeme, startPos, _linePos - 1, _line);
        }

        private void ReadStringLiteral()
        {
            int startPos = _linePos;
            Advance(); // Пропускаем первую кавычку
            var sb = new StringBuilder();
            bool closed = false;

            while (!IsEnd())
            {
                char ch = CurrentChar();
                if (ch == '"')  // Найдена закрывающая кавычка
                {
                    closed = true;
                    Advance(); // Пропускаем закрывающую кавычку
                    break;
                }
                else
                {
                    sb.Append(ch);  // Добавляем символ в строку, включая запятые и пробелы
                    Advance();
                }
            }

            if (closed)
            {
                AddToken(TokenCode.StringLiteral, "строковый литерал", sb.ToString(), startPos, _linePos - 1, _line);
            }
            else
            {
                // Если строка не закрылась, помечаем как ошибку
                AddToken(TokenCode.Error, "незакрытая строка", sb.ToString(), startPos, _linePos - 1, _line);
            }
        }

        private bool IsEnd() => _pos >= _text.Length;
        private char CurrentChar() => IsEnd() ? '\0' : _text[_pos];
        private char PeekNext() => (_pos + 1) >= _text.Length ? '\0' : _text[_pos + 1];

        private void Advance()
        {
            if (CurrentChar() == '\n')
            {
                _line++;
                _linePos = 0;
            }
            _pos++;
            _linePos++;
        }

        private void AddToken(TokenCode code, string type, string lexeme, int startPos, int endPos, int line)
        {
            _tokens.Add(new Token
            {
                Code = code,
                Type = type,
                Lexeme = lexeme,
                StartPos = startPos,
                EndPos = endPos,
                Line = line
            });
        }

        private void AddToken(TokenCode code, string type, string lexeme)
        {
            AddToken(code, type, lexeme, _linePos, _linePos, _line);
        }
    }
}
