using System;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

public enum EFibreOpcodeType
{
	NOP,

	MOV,

	NOT,
	NEG,

	MUL,
	DIV,
	INC,
	DEC,
	MOD,

	EQL,
	NQL,
	LES,
	GRT,
	LQL,
	GQL,

	AND,
	LOR,

	JMP,
	JIZ,
	JNZ,

	CAL,
	RET,
	FNC,

	COUNT,
}

public struct SFibreFilePos
{
	public short mFileIndex;
	public short mBufferIndex;

	public SFibreFilePos(int FileIndex, int BufferIndex)
	{
		mFileIndex = (short)FileIndex;
		mBufferIndex = (short)BufferIndex;
	}
}

public class CFibreOpcode
{
	public SFibreFilePos mFilePos;
	public EFibreOpcodeType mOpcode;
	public uint[] mOperand = new uint[4];
}

public enum EFibreType
{
	UNDEFINED,
	NUMBER,
	STRING,
	BOOL,
	FUNCTION,

	COUNT,
}

public class CFibreReg
{
	public EFibreType mType;
	public int mID;
	public bool mBool;
	public string mString;
	public double mNumber;

	public CFibreReg()
	{
		mType = EFibreType.UNDEFINED;
	}

	public CFibreReg(bool Value)
	{
		mType = EFibreType.BOOL;
		mBool = Value;
	}

	public CFibreReg(string Value)
	{
		mType = EFibreType.STRING;
		mString = Value;
	}

	public CFibreReg(double Value)
	{
		mType = EFibreType.NUMBER;
		mNumber = Value;
	}

	public string PrintVal()
	{
		if (mType == EFibreType.NUMBER) return mNumber.ToString();
		if (mType == EFibreType.STRING) return '"' + mString + '"';
		if (mType == EFibreType.BOOL) return mBool.ToString();
		if (mType == EFibreType.UNDEFINED) return "undefined";
		if (mType == EFibreType.FUNCTION)
		{
			if (mID < -1)
				return mString + ", Interop(" + -mID + ")";
			else
				return mString + ", P" + mID;
		}

		return "(_PrintVal Can't Print Type!)";
	}
}

public class CFibreCompileException : Exception
{
	public CFibreCompileException() { }
	public CFibreCompileException(string Message) : base(Message) { }
}

/// <summary>
/// FibreScript is a simple dynamic C like language that compiles to bytecode and runs on a VM.
/// </summary>
public class CFibreScript
{
	public enum ETokenType
	{
		NONE,
		EOF,

		STATEMENT_TERMINATOR,
		LIST_SEPARATOR,

		INCLUDE,

		RETURN,		

		IDENTIFIER,
		NUMBER_LITERAL,
		STRING_LITERAL,
		BOOL_LITERAL,
		UNDEFINED,

		OPEN_PAREN,
		CLOSE_PAREN,

		OPEN_SCOPE,
		CLOSE_SCOPE,

		ASSIGN,

		MEMBER,

		ADD,
		SUBTRACT,
		MULTIPLY,
		DIVIDE,
		MOD,

		NOT,
		
		TYPE_VAR,
		TYPE_FUNC,

		COND_IF,
		COND_ELSE,
		COND_WHILE,
		COND_FOR,

		EQUAL,
		NOT_EQUAL,
		GREATER,
		LESS,
		GREATER_EQUAL,
		LESS_EQUAL,

		LOGICAL_AND,
		LOGICAL_OR,
	}
	
	public struct SToken
	{
		public ETokenType mType;
		public string mLexeme;
		public double mNumber;
		public bool mBool;
		public SFibreFilePos mFilePos;
	}

	public enum EParseResult
	{
		NONE,
		STATEMENT,
		R_VALUE,
		L_VALUE,
	}

	public struct SParseResult
	{
		public EParseResult mResult;
		public uint mRegister;
		public bool mTempReg;

		public SParseResult(EParseResult Result)
		{
			mResult = Result;
			mRegister = 0;
			mTempReg = false;
		}

		public SParseResult(EParseResult Result, uint Register)
		{
			mResult = Result;
			mRegister = Register;
			mTempReg = false;
		}
	}

	public class CSymbolDecl
	{
		public string mIdentifier;
		public uint mRegister;
	}

	public class CSymbolStack
	{
		public int mIndex;
		public uint mRestoreReg;
	}

	public class CSourceFile
	{
		public string mFilePath;
		public string mStream;
	}

	public delegate SParseResult ParseDelegate();

	private static SFibreFilePos _nullFilePos = new SFibreFilePos(-1, -1);

	// Lexing & Parsing
	private int _sourceFileIndex;
	private string _stream;
	private int _streamIndex;
	private char _nextChar;
	private SToken _nextToken;

	// Code generation
	private uint _regCounter;	
	private List<CSymbolDecl> _symbolStack = new List<CSymbolDecl>();
	private List<CSymbolStack> _symbolStackScopes = new List<CSymbolStack>();
	private List<string> _errors = new List<string>();

	// Program Contents
	public List<CFibreReg> mData = new List<CFibreReg>();
	public List<CFibreOpcode> mProgram = new List<CFibreOpcode>();
	public Dictionary<string, CFibreReg> mFunctionDefs = new Dictionary<string, CFibreReg>();
	public List<CSourceFile> mSourceFiles = new List<CSourceFile>();

	public void Init()
	{
		_regCounter = _GetGlobalRegister(0);
		mData.Clear();
		mProgram.Clear();
		_symbolStack.Clear();
		_symbolStackScopes.Clear();
		_errors.Clear();

		mData.Add(new CFibreReg(false));
		mData.Add(new CFibreReg(true));
		mData.Add(new CFibreReg());
		
		_PushSymbolScope();
	}

	public void PushInteropFunction(string Name, int CallID)
	{
		CFibreReg val = new CFibreReg();
		val.mType = EFibreType.FUNCTION;
		val.mID = -(CallID + 10);
		val.mString = Name;
		mData.Add(val);

		CSymbolDecl symbol = new CSymbolDecl();
		symbol.mIdentifier = Name;
		symbol.mRegister = _GetDataRegister(mData.Count - 1);
		_symbolStack.Add(symbol);
	}

	public void CompileFile(string Filepath)
	{
		Debug.Log("Compiling fibre script: " + Filepath);

		System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
		sw.Start();

		_ParseFile(Filepath);
		_BuildFunctionTable();
		
		sw.Stop();
		Debug.Log("Compiled: " + sw.Elapsed.TotalMilliseconds + "ms ");
	}

	private bool _IsWhiteSpace(char Char)
	{
		return (Char == ' ' || Char == '\n' || Char == '\r' || Char == '\t');
	}

	private bool _IsAlpha(char Char)
	{
		return ((Char >= 'a' && Char <= 'z') || (Char >= 'A' && Char <= 'Z'));
	}

	private bool _IsDigit(char Char)
	{
		return (Char >= '0' && Char <= '9');
	}

	private bool _ParseToken(char Char, ETokenType Type)
	{
		if (_nextChar == Char)
		{
			_nextToken.mType = Type;
			_ReadNextChar();
			return true;
		}

		return false;
	}

	private void _UpgradeTokenIfKeyword()
	{
		if (_nextToken.mLexeme == "var") _nextToken.mType = ETokenType.TYPE_VAR;
		else if (_nextToken.mLexeme == "function") _nextToken.mType = ETokenType.TYPE_FUNC;
		else if (_nextToken.mLexeme == "if") _nextToken.mType = ETokenType.COND_IF;
		else if (_nextToken.mLexeme == "else") _nextToken.mType = ETokenType.COND_ELSE;
		else if (_nextToken.mLexeme == "while") _nextToken.mType = ETokenType.COND_WHILE;
		else if (_nextToken.mLexeme == "for") _nextToken.mType = ETokenType.COND_FOR;
		else if (_nextToken.mLexeme == "include") _nextToken.mType = ETokenType.INCLUDE;
		else if (_nextToken.mLexeme == "return") _nextToken.mType = ETokenType.RETURN;
		else if (_nextToken.mLexeme == "undefined") _nextToken.mType = ETokenType.UNDEFINED;
		else if (_nextToken.mLexeme == "true") { _nextToken.mType = ETokenType.BOOL_LITERAL; _nextToken.mBool = true; }
		else if (_nextToken.mLexeme == "false") { _nextToken.mType = ETokenType.BOOL_LITERAL; _nextToken.mBool = false; }
	}

	private void _ReadNextChar()
	{
		if (_streamIndex == _stream.Length)
			_nextChar = '\0';
		else
			_nextChar = _stream[_streamIndex++];
	}

	private void _ReadNextToken()
	{
		_nextToken.mType = ETokenType.NONE;
		_nextToken.mLexeme = "";
		_nextToken.mNumber = 0.0;
		_nextToken.mBool = false;
		_nextToken.mFilePos = new SFibreFilePos(_sourceFileIndex, _streamIndex);

		while (_IsWhiteSpace(_nextChar))
			_ReadNextChar();
		
		while (_nextChar == '/')
		{
			_ReadNextChar();
			if (_nextChar == '/')
			{
				_ReadNextChar();
				while (_nextChar != '\n' && _nextChar != '\r' && _nextChar != '\0')
					_ReadNextChar();

				while (_IsWhiteSpace(_nextChar))
					_ReadNextChar();
			}
			else if (_nextChar == '*')
			{
				_ReadNextChar();
				while (true)
				{
					if (_nextChar == '\0')
					{
						break;
					}
					else if (_nextChar == '*')
					{
						_ReadNextChar();
						if (_nextChar == '/')
						{
							_ReadNextChar();

							while (_IsWhiteSpace(_nextChar))
								_ReadNextChar();

							break;
						}
					}
					else
					{
						_ReadNextChar();
					}
				}
			}
			else
			{
				// Special case to handle divide
				_nextToken.mType = ETokenType.DIVIDE;
				_nextToken.mFilePos.mBufferIndex = (short)(_streamIndex - 1);
				return;
			}
		}
		
		if (_nextChar == '\0')
		{
			_nextToken.mType = ETokenType.EOF;
			return;
		}

		_nextToken.mFilePos.mBufferIndex = (short)(_streamIndex - 1);

		if (_IsDigit(_nextChar))
		{
			_nextToken.mType = ETokenType.NUMBER_LITERAL;
			_nextToken.mLexeme = _nextChar.ToString();
			_ReadNextChar();

			while (_IsDigit(_nextChar) || _nextChar == '.')
			{
				if (_nextChar == '.')
				{
					_nextToken.mLexeme += ".";
					_ReadNextChar();

					if (!_IsDigit(_nextChar))
						throw new CFibreCompileException("(" + GetFileLocation(_nextToken.mFilePos) + ") Number format incorrect.");

					while (_IsDigit(_nextChar))
					{
						_nextToken.mLexeme += _nextChar;
						_ReadNextChar();
					}

					break;
				}
				else
				{
					_nextToken.mLexeme += _nextChar;
					_ReadNextChar();
				}
			}

			if (!Double.TryParse(_nextToken.mLexeme, out _nextToken.mNumber))
				throw new CFibreCompileException("(" + GetFileLocation(_nextToken.mFilePos) + ") Couldn't parse number.");

			return;
		}
		
		if (_IsAlpha(_nextChar))
		{
			_nextToken.mType = ETokenType.IDENTIFIER;
			_nextToken.mLexeme = _nextChar.ToString();
			_ReadNextChar();

			while (_IsAlpha(_nextChar) || _IsDigit(_nextChar) || _nextChar == '_')
			{
				_nextToken.mLexeme += _nextChar;
				_ReadNextChar();
			}

			_UpgradeTokenIfKeyword();
			return;
		}

		if (_nextChar == '"')
		{
			_nextToken.mType = ETokenType.STRING_LITERAL;
			_nextToken.mLexeme = "";
			_ReadNextChar();

			while (_nextChar != '"' && _nextChar != '\0')
			{
				_nextToken.mLexeme += _nextChar;
				_ReadNextChar();
			}

			_ReadNextChar();
			return;
		}

		if (_ParseToken('.', ETokenType.MEMBER)) return;
		if (_ParseToken('+', ETokenType.ADD)) return;
		if (_ParseToken('-', ETokenType.SUBTRACT)) return;		
		if (_ParseToken('*', ETokenType.MULTIPLY)) return;
		if (_ParseToken('/', ETokenType.DIVIDE)) return; // TODO: Probably being handled above.
		if (_ParseToken('%', ETokenType.MOD)) return;
		if (_ParseToken('(', ETokenType.OPEN_PAREN)) return;
		if (_ParseToken(')', ETokenType.CLOSE_PAREN)) return;
		if (_ParseToken(';', ETokenType.STATEMENT_TERMINATOR)) return;
		if (_ParseToken(',', ETokenType.LIST_SEPARATOR)) return;
		if (_ParseToken('{', ETokenType.OPEN_SCOPE)) return;
		if (_ParseToken('}', ETokenType.CLOSE_SCOPE)) return;

		if (_nextChar == '!')
		{
			_ReadNextChar();

			if (_nextChar == '=')
			{
				_ReadNextChar();
				_nextToken.mType = ETokenType.NOT_EQUAL;
			}
			else
			{
				_nextToken.mType = ETokenType.NOT;
			}

			return;
		}

		if (_nextChar == '=')
		{
			_ReadNextChar();

			if (_nextChar == '=')
			{
				_ReadNextChar();
				_nextToken.mType = ETokenType.EQUAL;
			}
			else
			{
				_nextToken.mType = ETokenType.ASSIGN;
			}

			return;
		}

		if (_nextChar == '<')
		{
			_ReadNextChar();

			if (_nextChar == '=')
			{
				_ReadNextChar();
				_nextToken.mType = ETokenType.LESS_EQUAL;
			}
			else
			{
				_nextToken.mType = ETokenType.LESS;
			}

			return;
		}

		if (_nextChar == '>')
		{
			_ReadNextChar();

			if (_nextChar == '=')
			{
				_ReadNextChar();
				_nextToken.mType = ETokenType.GREATER_EQUAL;
			}
			else
			{
				_nextToken.mType = ETokenType.GREATER;
			}

			return;
		}

		if (_nextChar == '&')
		{
			_ReadNextChar();

			if (_nextChar == '&')
			{
				_ReadNextChar();
				_nextToken.mType = ETokenType.LOGICAL_AND;
				return;			
			}
			else
			{
				throw new CFibreCompileException("(" + GetFileLocation(_nextToken.mFilePos) + ") Unknown character.");
			}
		}

		if (_nextChar == '|')
		{
			_ReadNextChar();

			if (_nextChar == '|')
			{
				_ReadNextChar();
				_nextToken.mType = ETokenType.LOGICAL_OR;
				return;
			}
			else
			{
				throw new CFibreCompileException("(" + GetFileLocation(_nextToken.mFilePos) + ") Unknown character.");
			}
		}

		throw new CFibreCompileException("(" + GetFileLocation(_nextToken.mFilePos) + ") Could not lex string.");
	}

	private void _ExpectToken(ETokenType Type)
	{
		if (_nextToken.mType != Type)
			throw new CFibreCompileException("(" + GetFileLocation(_nextToken.mFilePos) + ") Expected token " + Type + ", got " + _nextToken.mType);

		_ReadNextToken();
	}

	private string _PrintReg(uint Value)
	{
		int type = (int)(Value >> 30);
		int index = (int)(Value & 0x3FFFFFFF);

		if (type == 0) return "D" + index + "(" + mData[index].PrintVal() +  ")";
		if (type == 1) return "G" + index;
		if (type == 2) return "L" + index;

		return Value.ToString() + " " + type + " " + index;
	}

	private uint _GetDataLocation(double Value)
	{
		int index = -1;

		for (int i = 0; i < mData.Count; ++i)
		{
			if (mData[i].mType == EFibreType.NUMBER && mData[i].mNumber == Value)
			{
				index = i;
				break;
			}
		}

		if (index == -1)
		{
			CFibreReg val = new CFibreReg();
			val.mType = EFibreType.NUMBER;
			val.mNumber = Value;
			index = mData.Count;
			mData.Add(val);
		}

		return _GetDataRegister(index);
	}

	private uint _GetDataLocation(string Value)
	{
		int index = -1;

		for (int i = 0; i < mData.Count; ++i)
		{
			if (mData[i].mType == EFibreType.STRING && mData[i].mString == Value)
			{
				index = i;
				break;
			}
		}

		if (index == -1)
		{
			CFibreReg val = new CFibreReg();
			val.mType = EFibreType.STRING;
			val.mString = Value;
			index = mData.Count;
			mData.Add(val);
		}

		return _GetDataRegister(index);
	}

	private int _GetDataFunction(string Identifier)
	{
		int index = -1;

		for (int i = 0; i < mData.Count; ++i)
		{
			if (mData[i].mType == EFibreType.FUNCTION && mData[i].mString == Identifier)
			{
				index = i;
				break;
			}
		}

		if (index == -1)
		{
			CFibreReg val = new CFibreReg();
			val.mType = EFibreType.FUNCTION;
			val.mString = Identifier;
			val.mID = -1;
			index = mData.Count;
			mData.Add(val);
		}

		return index;
	}

	private CSymbolDecl _GetSymbol(string Identifier, bool LimitScope = false)
	{
		int target = 0;

		if (LimitScope)
		{
			target = _symbolStackScopes[_symbolStackScopes.Count - 1].mIndex;
		}

		for (int i = _symbolStack.Count - 1; i >= target; --i)
		{
			if (_symbolStack[i].mIdentifier == Identifier)
			{
				return _symbolStack[i];
			}
		}

		return null;
	}

	private int _GetSymbolLocation(string Identifier)
	{
		int index = -1;

		for (int i = 0; i < _symbolStack.Count; ++i)
		{
			if (_symbolStack[i].mIdentifier == Identifier)
			{
				return i;
			}
		}

		return index;
	}

	private void _PushSymbolScope(bool RebaseLocalRegisters = false)
	{
		CSymbolStack stack = new CSymbolStack();
		stack.mIndex = _symbolStack.Count;
		stack.mRestoreReg = _regCounter;

		if (RebaseLocalRegisters)
			_regCounter = _GetLocalRegister(0);

		_symbolStackScopes.Add(stack);
	}

	private void _PopSymbolScope()
	{
		CSymbolStack stack = _symbolStackScopes[_symbolStackScopes.Count - 1];
		_symbolStack.RemoveRange(stack.mIndex, _symbolStack.Count - stack.mIndex);
		_symbolStackScopes.RemoveAt(_symbolStackScopes.Count - 1);
		_regCounter = stack.mRestoreReg;
	}

	private uint _GetDataRegister(int Index)
	{
		return (uint)Index;
	}

	private uint _GetGlobalRegister(int Index)
	{
		return (uint)Index | 0x40000000;
	}

	private uint _GetLocalRegister(int Index)
	{
		return (uint)Index | 0x80000000;
	}

	private void _Emit(SFibreFilePos FilePos, EFibreOpcodeType Opcode, uint[] Operands)
	{
		CFibreOpcode opcode = new CFibreOpcode();
		opcode.mFilePos = FilePos;
		opcode.mOpcode = Opcode;
		opcode.mOperand = Operands;
		mProgram.Add(opcode);
	}

	private void _Emit(SFibreFilePos FilePos, int ProgramPos, EFibreOpcodeType Opcode, uint[] Operands)
	{
		CFibreOpcode opcode = new CFibreOpcode();
		opcode.mFilePos = FilePos;
		opcode.mOpcode = Opcode;
		opcode.mOperand = Operands;
		mProgram[ProgramPos] = opcode;
	}

	private void _EmitMov(uint Dest, uint Source)
	{
		if (Dest != Source)
			_Emit(_nullFilePos, EFibreOpcodeType.MOV, new uint[] { Dest, Source });
	}

	private SParseResult _ParseAndEmitNullaryOp(ParseDelegate OpParse)
	{
		SToken token = _nextToken;
		_ReadNextToken();
		SParseResult parseFactor = OpParse();
		if (parseFactor.mResult == EParseResult.NONE)
			throw new CFibreCompileException("(" + GetFileLocation(token.mFilePos) + ") There must be valid operand for the operator " + token.mType);

		return parseFactor;
	}

	private SParseResult _ParseAndEmitUnaryOp(EFibreOpcodeType OpcodeType, ParseDelegate OpParse)
	{
		SToken token = _nextToken;
		_ReadNextToken();

		SParseResult parseFactor = OpParse();
		if (parseFactor.mResult == EParseResult.NONE)
			throw new CFibreCompileException("(" + GetFileLocation(token.mFilePos) + ") There must be a valid operand for the operator " + token.mType);

		uint tempReg = _regCounter;
		_Emit(token.mFilePos, OpcodeType, new uint[] { tempReg, parseFactor.mRegister });
		parseFactor.mResult = EParseResult.R_VALUE;
		parseFactor.mRegister = tempReg;
		parseFactor.mTempReg = true;
		return parseFactor;
	}

	private bool _ParseAndEmitBinaryOp(ETokenType OpToken, EFibreOpcodeType OpcodeType, ref SParseResult FirstTerm, ParseDelegate OpParse)
	{
		if (_nextToken.mType != OpToken)
			return false;

		SToken token = _nextToken;
		_ReadNextToken();

		if (FirstTerm.mTempReg) ++_regCounter;

		SParseResult nextTerm = OpParse();
		if (nextTerm.mResult == EParseResult.NONE)
			throw new CFibreCompileException("(" + GetFileLocation(token.mFilePos) + ") There must be a valid operand for the operator " + token.mType);

		if (FirstTerm.mTempReg) --_regCounter;

		uint tempReg = _regCounter;
		_Emit(token.mFilePos, OpcodeType, new uint[] { tempReg, FirstTerm.mRegister, nextTerm.mRegister });
		FirstTerm.mResult = EParseResult.R_VALUE;
		FirstTerm.mRegister = tempReg;
		FirstTerm.mTempReg = true;
		return true;
	}

	private SParseResult _ParseFactor()
	{
		if (_nextToken.mType == ETokenType.NUMBER_LITERAL)
		{
			SParseResult result = new SParseResult(EParseResult.R_VALUE, _GetDataLocation(_nextToken.mNumber));
			_ReadNextToken();
			return result;
		}
		else if (_nextToken.mType == ETokenType.BOOL_LITERAL)
		{
			SParseResult result = new SParseResult(EParseResult.R_VALUE);
			if (_nextToken.mBool) result.mRegister = _GetDataRegister(1);
			else result.mRegister = _GetDataRegister(0);
			_ReadNextToken();
			return result;
		}
		else if (_nextToken.mType == ETokenType.STRING_LITERAL)
		{
			SParseResult result = new SParseResult(EParseResult.R_VALUE, _GetDataLocation(_nextToken.mLexeme));
			_ReadNextToken();
			return result;
		}
		else if (_nextToken.mType == ETokenType.IDENTIFIER)
		{
			string identifier = _nextToken.mLexeme;
			SToken token = _nextToken;
			_ReadNextToken();

			if (_nextToken.mType == ETokenType.OPEN_PAREN)
			{
				_ReadNextToken();				

				SParseResult argExpr = _ParseAssignment();
				
				int argCount = 0;
				uint reg = _regCounter;

				if (argExpr.mResult != EParseResult.NONE)
				{
					++argCount;
					//_Emit(0, EFibreOpcodeType.MOV, new uint[] { _regCounter++, argExpr.mRegister });
					_EmitMov(_regCounter++, argExpr.mRegister);

					while (_nextToken.mType == ETokenType.LIST_SEPARATOR)
					{
						_ReadNextToken();
						argExpr = _ParseAssignment();
						if (argExpr.mResult == EParseResult.NONE)
							throw new CFibreCompileException("(" + GetFileLocation(_nextToken.mFilePos) + ") Expected argument.");

						++argCount;
						//_Emit(0, EFibreOpcodeType.MOV, new uint[] { _regCounter++, argExpr.mRegister });
						_EmitMov(_regCounter++, argExpr.mRegister);
					}
				}

				_ExpectToken(ETokenType.CLOSE_PAREN);

				CSymbolDecl symbol = _GetSymbol(identifier);
				if (symbol == null)
				{
					uint dataLoc = _GetDataRegister(_GetDataFunction(identifier));
					_Emit(token.mFilePos, EFibreOpcodeType.CAL, new uint[] { dataLoc, reg, (uint)argCount });
				}
				else
				{
					_Emit(token.mFilePos, EFibreOpcodeType.CAL, new uint[] { symbol.mRegister, reg, (uint)argCount });
				}

				_regCounter = reg;

				SParseResult result = new SParseResult(EParseResult.R_VALUE, _regCounter);
				return result;
			}
			else
			{
				CSymbolDecl symbol = _GetSymbol(identifier);
				if (symbol == null)
					throw new CFibreCompileException("(" + GetFileLocation(token.mFilePos) + ") Identifier not declared.");

				if ((symbol.mRegister >> 30) == 0)
					return new SParseResult(EParseResult.R_VALUE, symbol.mRegister);
				else
					return new SParseResult(EParseResult.L_VALUE, symbol.mRegister);
			}
		}
		else if (_nextToken.mType == ETokenType.OPEN_PAREN)
		{
			_ReadNextToken();

			SParseResult parseExpr = _ParseAssignment();
			if (parseExpr.mResult == EParseResult.NONE)
				throw new CFibreCompileException("(" + GetFileLocation(_nextToken.mFilePos) + ") Expected expression.");

			_ExpectToken(ETokenType.CLOSE_PAREN);

			return parseExpr;
		}

		return new SParseResult(EParseResult.NONE);
	}

	private SParseResult _ParseUnary()
	{
		if (_nextToken.mType == ETokenType.NOT)
			return _ParseAndEmitUnaryOp(EFibreOpcodeType.NOT, _ParseFactor);
		else if (_nextToken.mType == ETokenType.ADD)
			return _ParseAndEmitNullaryOp(_ParseFactor);
		else if (_nextToken.mType == ETokenType.SUBTRACT)
			return _ParseAndEmitUnaryOp(EFibreOpcodeType.NEG, _ParseFactor);

		return _ParseFactor();
	}

	private SParseResult _ParseDivMod()
	{
		SParseResult firstTerm = _ParseUnary();

		while (firstTerm.mResult != EParseResult.NONE)
		{
			if (_ParseAndEmitBinaryOp(ETokenType.DIVIDE, EFibreOpcodeType.DIV, ref firstTerm, _ParseUnary)) continue;
			else if (_ParseAndEmitBinaryOp(ETokenType.MOD, EFibreOpcodeType.MOD, ref firstTerm, _ParseUnary)) continue;
			else break;
		}
		
		return firstTerm;
	}

	private SParseResult _ParseMultiply()
	{
		SParseResult firstTerm = _ParseDivMod();

		while (firstTerm.mResult != EParseResult.NONE)
		{
			if (_ParseAndEmitBinaryOp(ETokenType.MULTIPLY, EFibreOpcodeType.MUL, ref firstTerm, _ParseDivMod)) continue;
			else break;
		}

		return firstTerm;
	}

	private SParseResult _ParseIncDec()
	{
		SParseResult firstTerm = _ParseMultiply();
		
		while (firstTerm.mResult != EParseResult.NONE)
		{
			if (_ParseAndEmitBinaryOp(ETokenType.ADD, EFibreOpcodeType.INC, ref firstTerm, _ParseMultiply)) continue;
			else if (_ParseAndEmitBinaryOp(ETokenType.SUBTRACT, EFibreOpcodeType.DEC, ref firstTerm, _ParseMultiply)) continue;
			else break;
		}

		return firstTerm;
	}

	private SParseResult _ParseRelational()
	{
		SParseResult firstTerm = _ParseIncDec();

		while (firstTerm.mResult != EParseResult.NONE)
		{
			if (_ParseAndEmitBinaryOp(ETokenType.EQUAL, EFibreOpcodeType.EQL, ref firstTerm, _ParseIncDec)) continue;
			else if (_ParseAndEmitBinaryOp(ETokenType.NOT_EQUAL, EFibreOpcodeType.NQL, ref firstTerm, _ParseIncDec)) continue;
			else if (_ParseAndEmitBinaryOp(ETokenType.LESS, EFibreOpcodeType.LES, ref firstTerm, _ParseIncDec)) continue;
			else if (_ParseAndEmitBinaryOp(ETokenType.GREATER, EFibreOpcodeType.GRT, ref firstTerm, _ParseIncDec)) continue;
			else if (_ParseAndEmitBinaryOp(ETokenType.LESS_EQUAL, EFibreOpcodeType.LQL, ref firstTerm, _ParseIncDec)) continue;
			else if (_ParseAndEmitBinaryOp(ETokenType.GREATER_EQUAL, EFibreOpcodeType.GQL, ref firstTerm, _ParseIncDec)) continue;
			else break;
		}

		return firstTerm;
	}

	private SParseResult _ParseLogicalAnd()
	{
		SParseResult firstTerm = _ParseRelational();

		while (firstTerm.mResult != EParseResult.NONE)
		{
			if (_nextToken.mType == ETokenType.LOGICAL_AND)
			{
				SToken token = _nextToken;
				_ReadNextToken();

				int jmpCond = mProgram.Count;
				_Emit(token.mFilePos, EFibreOpcodeType.NOP, null);

				SParseResult nextTerm = _ParseRelational();
				if (nextTerm.mResult == EParseResult.NONE)
					throw new CFibreCompileException("(" + GetFileLocation(token.mFilePos) + ") Expected operand for && operator.");

				_Emit(token.mFilePos, jmpCond, EFibreOpcodeType.JIZ, new uint[] { firstTerm.mRegister, (uint)mProgram.Count + 3});
				_Emit(token.mFilePos, EFibreOpcodeType.JIZ, new uint[] { nextTerm.mRegister, (uint)mProgram.Count + 3 });
				_EmitMov(_regCounter, _GetDataRegister(1));
				_Emit(token.mFilePos, EFibreOpcodeType.JMP, new uint[] { (uint)mProgram.Count + 2 });
				_EmitMov(_regCounter, _GetDataRegister(0));

				firstTerm.mResult = EParseResult.R_VALUE;
				firstTerm.mRegister = _regCounter;
				firstTerm.mTempReg = true;
			}
			else
			{
				break;
			}
		}

		return firstTerm;
	}

	private SParseResult _ParseExpression()
	{
		SParseResult firstTerm = _ParseLogicalAnd();

		while (firstTerm.mResult != EParseResult.NONE)
		{
			if (_nextToken.mType == ETokenType.LOGICAL_OR)
			{
				SToken token = _nextToken;
				_ReadNextToken();

				int jmpCond = mProgram.Count;
				_Emit(token.mFilePos, EFibreOpcodeType.NOP, null);

				SParseResult nextTerm = _ParseLogicalAnd();
				if (nextTerm.mResult == EParseResult.NONE)
					throw new CFibreCompileException("(" + GetFileLocation(token.mFilePos) + ") Expected operand for || operator.");

				_Emit(token.mFilePos, jmpCond, EFibreOpcodeType.JNZ, new uint[] { firstTerm.mRegister, (uint)mProgram.Count + 1 });
				_Emit(token.mFilePos, EFibreOpcodeType.JIZ, new uint[] { nextTerm.mRegister, (uint)mProgram.Count + 3 });
				_EmitMov(_regCounter, _GetDataRegister(1));
				_Emit(token.mFilePos, EFibreOpcodeType.JMP, new uint[] { (uint)mProgram.Count + 2 });
				_EmitMov(_regCounter, _GetDataRegister(0));

				firstTerm.mResult = EParseResult.R_VALUE;
				firstTerm.mRegister = _regCounter;
				firstTerm.mTempReg = true;
			}
			else
			{
				break;
			}
		}
	
		return firstTerm;
	}

	private SParseResult _ParseVarDecl()
	{
		if (_nextToken.mType != ETokenType.TYPE_VAR)
			return new SParseResult(EParseResult.NONE);

		_ReadNextToken();

		if (_nextToken.mType != ETokenType.IDENTIFIER)
			throw new CFibreCompileException("(" + GetFileLocation(_nextToken.mFilePos) + ") Expected identifier for variable name.");

		CSymbolDecl symbol = _GetSymbol(_nextToken.mLexeme, true);
		if (symbol != null)
			throw new CFibreCompileException("(" + GetFileLocation(_nextToken.mFilePos) + ") Identifier already declared.");

		symbol = new CSymbolDecl();
		symbol.mIdentifier = _nextToken.mLexeme;
		symbol.mRegister = _regCounter;
		_symbolStack.Add(symbol);

		_ReadNextToken();

		SParseResult parseVar = new SParseResult(EParseResult.STATEMENT);

		if (_nextToken.mType == ETokenType.ASSIGN)
		{
			SToken token = _nextToken;
			_ReadNextToken();
			SParseResult nextParseExpr = _ParseAssignment();
			if (nextParseExpr.mResult == EParseResult.NONE)
				throw new CFibreCompileException("(" + GetFileLocation(token.mFilePos) + ") Must have an expression for assignment.");

			_EmitMov(symbol.mRegister, nextParseExpr.mRegister);
		}
		else
		{	
			_EmitMov(symbol.mRegister, _GetDataRegister(2));
		}

		++_regCounter;

		return parseVar;
	}

	private SParseResult _ParseAssignment()
	{
		SParseResult firstTerm = _ParseExpression();
		if (firstTerm.mResult == EParseResult.NONE) return firstTerm;

		if (_nextToken.mType == ETokenType.ASSIGN)
		{
			SToken token = _nextToken;

			if (firstTerm.mResult != EParseResult.L_VALUE)
				throw new CFibreCompileException("(" + GetFileLocation(token.mFilePos) + ") Left hand side of assignment is not assignable.");

			_ReadNextToken();

			if (firstTerm.mTempReg) ++_regCounter;
			SParseResult nextParseExpr = _ParseAssignment();
			if (firstTerm.mTempReg) --_regCounter;
			if (nextParseExpr.mResult == EParseResult.NONE)
				throw new CFibreCompileException("(" + GetFileLocation(token.mFilePos) + ") Must have an expression for assignment.");

			_EmitMov(firstTerm.mRegister, nextParseExpr.mRegister);

			firstTerm.mResult = EParseResult.L_VALUE;
		}

		return firstTerm;
	}

	private SParseResult _ParseStatement(bool EmbeddedStatement = false)
	{
		SParseResult parseTerm = new SParseResult(EParseResult.NONE);
		
		// TODO: Make sure we can't return unless within function def.
		if (_nextToken.mType == ETokenType.RETURN)
		{
			SFibreFilePos filePos = _nextToken.mFilePos;
			_ReadNextToken();

			parseTerm = _ParseAssignment();

			if (parseTerm.mResult != EParseResult.NONE)
			{
				_EmitMov(_GetLocalRegister(0), parseTerm.mRegister);
				_Emit(filePos, EFibreOpcodeType.RET, new uint[] { 1 });
			}
			else
			{
				_Emit(filePos, EFibreOpcodeType.RET, new uint[] { 0 });
			}

			parseTerm.mResult = EParseResult.STATEMENT;
		}

		if (parseTerm.mResult == EParseResult.NONE && !EmbeddedStatement)
			parseTerm = _ParseVarDecl();

		if (parseTerm.mResult == EParseResult.NONE)
			parseTerm = _ParseAssignment();

		if (parseTerm.mResult != EParseResult.NONE)
			_ExpectToken(ETokenType.STATEMENT_TERMINATOR);

		if (parseTerm.mResult == EParseResult.NONE)
		{
			if (_nextToken.mType == ETokenType.STATEMENT_TERMINATOR)
			{
				_ReadNextToken();
				return new SParseResult(EParseResult.STATEMENT);
			}
		}

		return parseTerm;
	}

	private SParseResult _ParseLocalScopeStatements(bool EmbeddedStatement = false)
	{
		SParseResult parseStatement = _ParseStatement(EmbeddedStatement);

		if (parseStatement.mResult == EParseResult.NONE)
			parseStatement = _ParseScopeBlock();

		if (parseStatement.mResult == EParseResult.NONE)
		{
			if (_nextToken.mType == ETokenType.COND_IF)
			{
				SToken token = _nextToken;
				_ReadNextToken();

				_ExpectToken(ETokenType.OPEN_PAREN);

				SParseResult condExpr = _ParseExpression();
				if (condExpr.mResult == EParseResult.NONE)
					throw new CFibreCompileException("(" + GetFileLocation(token.mFilePos) + ") Conditional must evaluate an expression.");

				_ExpectToken(ETokenType.CLOSE_PAREN);

				// Emit Condition
				int jmpCond = mProgram.Count;
				_Emit(token.mFilePos, EFibreOpcodeType.NOP, null);

				SParseResult thenPath = _ParseLocalScopeStatements(true);
				if (thenPath.mResult == EParseResult.NONE)
					throw new CFibreCompileException("(" + GetFileLocation(token.mFilePos) + ") Conditional must have a 'then' body.");

				if (_nextToken.mType == ETokenType.COND_ELSE)
				{
					int ifPassJmp = mProgram.Count;
					_Emit(token.mFilePos, EFibreOpcodeType.NOP, null);
					_Emit(token.mFilePos, jmpCond, EFibreOpcodeType.JIZ, new uint[] { condExpr.mRegister, (uint)mProgram.Count });

					_ReadNextToken();
					SParseResult elsePath = _ParseLocalScopeStatements(true);
					if (elsePath.mResult == EParseResult.NONE)
						throw new CFibreCompileException("(" + GetFileLocation(token.mFilePos) + ") Conditional must have an 'else' body.");

					_Emit(token.mFilePos, ifPassJmp, EFibreOpcodeType.JMP, new uint[] { (uint)mProgram.Count });
				}
				else
				{
					_Emit(token.mFilePos, jmpCond, EFibreOpcodeType.JIZ, new uint[] { condExpr.mRegister, (uint)mProgram.Count });
				}

				return new SParseResult(EParseResult.STATEMENT);
			}
		}

		return parseStatement;
	}

	private SParseResult _ParseScopeBlock()
	{
		if (_nextToken.mType == ETokenType.OPEN_SCOPE)
		{
			_ReadNextToken();
			_PushSymbolScope();

			while (true)
			{
				SParseResult parseTerm = _ParseLocalScopeStatements();
				
				if (parseTerm.mResult == EParseResult.NONE)
					break;
			}

			_ExpectToken(ETokenType.CLOSE_SCOPE);

			_PopSymbolScope();
			
			SParseResult scopeResult = new SParseResult();
			scopeResult.mResult = EParseResult.STATEMENT;
			return scopeResult;
		}

		return new SParseResult(EParseResult.NONE);
	}

	private SParseResult _ParseFunctionDef()
	{
		if (_nextToken.mType == ETokenType.TYPE_FUNC)
		{
			int jmpFunc = mProgram.Count;
			_Emit(_nullFilePos, EFibreOpcodeType.NOP, null);

			_ReadNextToken();

			if (_nextToken.mType != ETokenType.IDENTIFIER)
				throw new CFibreCompileException("(" + GetFileLocation(_nextToken.mFilePos) + ") Expected identifier for function name.");

			string identifier = _nextToken.mLexeme;

			int dataLoc = _GetDataFunction(identifier);
			mData[dataLoc].mID = mProgram.Count;
			
			CSymbolDecl symbol = _GetSymbol(identifier);
			if (symbol != null)
			{
				throw new CFibreCompileException("(" + GetFileLocation(_nextToken.mFilePos) + ") Function uses an identifier that is already declared.");
			}
			else
			{
				symbol = new CSymbolDecl();
				symbol.mIdentifier = identifier;
				symbol.mRegister = _GetDataRegister(dataLoc);
				_symbolStack.Add(symbol);
			}

			_ReadNextToken();

			_ExpectToken(ETokenType.OPEN_PAREN);

			_PushSymbolScope(true);

			int funcHeaderLoc = mProgram.Count;
			_Emit(_nullFilePos, EFibreOpcodeType.NOP, null);
			int argCount = 0;

			while (_nextToken.mType == ETokenType.IDENTIFIER)
			{
				++argCount;
				CSymbolDecl argSymbol = new CSymbolDecl();
				argSymbol.mIdentifier = _nextToken.mLexeme;
				argSymbol.mRegister = _regCounter++;
				_symbolStack.Add(argSymbol);
				
				_ReadNextToken();

				if (_nextToken.mType == ETokenType.LIST_SEPARATOR)
				{
					_ReadNextToken();
				}
				else
				{
					break;
				}
			}

			_ExpectToken(ETokenType.CLOSE_PAREN);

			// TODO: Better to have arg count in function data definition?
			_Emit(_nullFilePos, funcHeaderLoc, EFibreOpcodeType.FNC, new uint[] { (uint)argCount });

			// TODO: Pass allow returns?
			SParseResult parseBody = _ParseScopeBlock();
			if (parseBody.mResult == EParseResult.NONE)
				throw new CFibreCompileException("(" + GetFileLocation(_nextToken.mFilePos) + ") Expected function body.");

			if (mProgram[mProgram.Count - 1].mOpcode != EFibreOpcodeType.RET)
				_Emit(_nextToken.mFilePos, EFibreOpcodeType.RET, new uint[] { 0 });

			_Emit(_nullFilePos, jmpFunc, EFibreOpcodeType.JMP, new uint[] { (uint)mProgram.Count });

			_PopSymbolScope();

			return new SParseResult(EParseResult.STATEMENT);
		}

		return new SParseResult(EParseResult.NONE);
	}

	private SParseResult _ParseInclude()
	{
		if (_nextToken.mType == ETokenType.INCLUDE)
		{
			_ReadNextToken();

			if (_nextToken.mType != ETokenType.STRING_LITERAL)
				throw new CFibreCompileException("(" + GetFileLocation(_nextToken.mFilePos) + ") Expected include file path string.");

			string includeFilePath = _nextToken.mLexeme;			

			SParseResult result = _ParseFile(includeFilePath);

			_ReadNextToken();

			_ExpectToken(ETokenType.STATEMENT_TERMINATOR);

			return result;
		}

		return new SParseResult(EParseResult.NONE);
	}

	private SParseResult _ParseGlobalScope()
	{
		while (true)
		{
			SParseResult parseLocalScope = _ParseLocalScopeStatements();

			if (parseLocalScope.mResult == EParseResult.NONE)
				parseLocalScope = _ParseFunctionDef();

			if (parseLocalScope.mResult == EParseResult.NONE)
				parseLocalScope = _ParseInclude();

			if (parseLocalScope.mResult == EParseResult.NONE)
				return new SParseResult(EParseResult.STATEMENT);
		}
	}

	private SParseResult _ParseFile(string FilePath)
	{
		for (int i = 0; i < mSourceFiles.Count; ++i)
		{
			if (mSourceFiles[i].mFilePath == FilePath)
				return new SParseResult(EParseResult.STATEMENT);
		}
		
		int currentSourceFileIndex = _sourceFileIndex;
		string currentStream = _stream;
		int currentStreamIndex = _streamIndex;
		char currentNextChar = _nextChar;
		SToken currentNextToken = _nextToken;

		try
		{
			_stream = File.ReadAllText(CGame.DataDirectory + FilePath);
		}
		catch (Exception e)
		{
			// If we don't have any source files, then this is not an include
			if (mSourceFiles.Count == 0)
				throw new CFibreCompileException("Could not find fibre file '" + CGame.DataDirectory + FilePath + "'");
			else
				throw new CFibreCompileException("(" + GetFileLocation(_nextToken.mFilePos) + ") Could not include fibre file '" + CGame.DataDirectory + FilePath + "'");
		}

		CSourceFile file = new CSourceFile();
		file.mFilePath = FilePath;
		file.mStream = _stream;
		mSourceFiles.Add(file);

		_sourceFileIndex = mSourceFiles.Count - 1;
		_streamIndex = 0;
		_ReadNextChar();
		_ReadNextToken();
		SParseResult result = _ParseGlobalScope();

		_ExpectToken(ETokenType.EOF);

		_sourceFileIndex = currentSourceFileIndex;
		_stream = currentStream;
		_streamIndex = currentStreamIndex;
		_nextChar = currentNextChar;
		_nextToken = currentNextToken;

		return result;
	}

	private void _BuildFunctionTable()
	{	
		for (int i = 0; i < mData.Count; ++i)
		{
			if (mData[i].mType == EFibreType.FUNCTION)
			{
				if (mData[i].mID == -1)
				{	
					// Find first time we called this function
					for (int j = 0; j < mProgram.Count; ++j)
					{
						if (mProgram[j].mOpcode == EFibreOpcodeType.CAL && (int)mProgram[j].mOperand[0] == i)
							throw new CFibreCompileException("(" + GetFileLocation(mProgram[j].mFilePos) + ") Function " + mData[i].mString + " was called but not defined!");
					}
				}

				mFunctionDefs[mData[i].mString] = mData[i];
			}
		}
	}

	private void _GetFilePositionFromBufferPosition(SFibreFilePos FilePos, out string FilePath, out int Line, out int Column)
	{
		string stream = mSourceFiles[FilePos.mFileIndex].mStream;

		FilePath = mSourceFiles[FilePos.mFileIndex].mFilePath;
		Line = 1;
		Column = 0;

		for (int i = 0; i < stream.Length; ++i)
		{
			if (stream[i] == '\t')
				Column += 4;
			else
				++Column;

			if (i == FilePos.mBufferIndex)
				return;

			if (stream[i] == '\n')
			{
				++Line;
				Column = 0;
			}
		}
	}

	public string GetFileLocation(SFibreFilePos FilePos)
	{
		string filePath;
		int line, column;
		_GetFilePositionFromBufferPosition(FilePos, out filePath, out line, out column);

		return filePath + ":" + line + ":" + column;
	}

	public string GetFileLocationFromOpcode(int OpcodeIndex)
	{
		if (OpcodeIndex >= 0 && OpcodeIndex < mProgram.Count)
		{
			SFibreFilePos filePos = mProgram[OpcodeIndex].mFilePos;

			if (filePos.mFileIndex != -1)
				return GetFileLocation(filePos);
		}

		return "no file location";
	}

	public void PrintProgramListing()
	{
		string output = "Data:\n\r";

		for (int i = 0; i < mData.Count; ++i)
		{
			output += "D" + i + "\t";
			if (i < 100) output += "\t";
			output += mData[i].mType + ", " + mData[i].PrintVal() + "\n\r";
		}

		output += "\n\rProgram:\n\r";

		for (int i = 0; i < mProgram.Count; ++i)
		{
			output += "P" + i + "\t";
			if (i < 100) output += "\t";
			output += mProgram[i].mOpcode + " ";

			switch (mProgram[i].mOpcode)
			{
				case EFibreOpcodeType.NOP:
					break;

				case EFibreOpcodeType.MOV:
				case EFibreOpcodeType.NOT:
				case EFibreOpcodeType.NEG:
					output += _PrintReg(mProgram[i].mOperand[0]) + ", " +_PrintReg(mProgram[i].mOperand[1]);
					break;
				
				case EFibreOpcodeType.INC:
				case EFibreOpcodeType.DEC:
				case EFibreOpcodeType.MUL:
				case EFibreOpcodeType.DIV:				
				case EFibreOpcodeType.MOD:
				case EFibreOpcodeType.EQL:
				case EFibreOpcodeType.NQL:
				case EFibreOpcodeType.LES:
				case EFibreOpcodeType.GRT:
				case EFibreOpcodeType.LQL:
				case EFibreOpcodeType.GQL:
				case EFibreOpcodeType.AND:
				case EFibreOpcodeType.LOR:
				output += _PrintReg(mProgram[i].mOperand[0]) + ", " + _PrintReg(mProgram[i].mOperand[1]) + ", " + _PrintReg(mProgram[i].mOperand[2]);
					break;

				case EFibreOpcodeType.JMP:
					output += "P" + mProgram[i].mOperand[0];
					break;

				case EFibreOpcodeType.JIZ:
				case EFibreOpcodeType.JNZ:
					output += _PrintReg(mProgram[i].mOperand[0]) + ", P" + mProgram[i].mOperand[1];
					break;

				case EFibreOpcodeType.CAL:
					output += _PrintReg(mProgram[i].mOperand[0]) + ", " + _PrintReg(mProgram[i].mOperand[1]) + ", " + mProgram[i].mOperand[2];
					break;
				
				case EFibreOpcodeType.FNC:
					output += mProgram[i].mOperand[0];
					break;

				case EFibreOpcodeType.RET:
					output += mProgram[i].mOperand[0];
					break;
			};

			output += "\n\r";
		}

		File.WriteAllText(CGame.DataDirectory + "listing.fsb", output);
	}
}