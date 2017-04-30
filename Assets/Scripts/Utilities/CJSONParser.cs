using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class CJSONValue
{
	public enum EValueType
	{
		NULL,
		STRING,
		NUMBER,
		BOOL,
		OBJECT,
		ARRAY
	}

	public EValueType mType;
	public string mString;
	public double mNumber;
	public bool mBool;
	public Dictionary<string, CJSONValue> mValues = new Dictionary<string, CJSONValue>();

	public CJSONValue()
	{
		mType = EValueType.NULL;
	}

	public CJSONValue(string Value)
	{
		mType = EValueType.STRING;
		mString = Value;
	}

	public CJSONValue(double Value)
	{
		mType = EValueType.NUMBER;
		mNumber = Value;
	}

	public CJSONValue(bool Value)
	{
		mType = EValueType.BOOL;
		mBool = Value;
	}

	public int GetCount()
	{
		return mValues.Count;
	}

	public bool HasString(string Path)
	{
		return false;
	}

	public CJSONValue this[string Path]
	{
		get
		{
			return _ResolvePath(Path);
		}
	}

	public CJSONValue this[int Index]
	{
		get
		{
			CJSONValue result = null;
			mValues.TryGetValue(Index.ToString(), out result);
			return result;
		}
	}

	private CJSONValue _ResolvePath(string Path)
	{
		CJSONValue next = this;
		string path = Path;

		if (Path == "")
			return this;

		while (true)
		{
			int delim = path.IndexOf('.');
			if (delim != -1)
			{
				string name = path.Substring(0, delim);

				if (path.Length <= delim + 1)
				{
					next = null;
					break;
				}

				path = path.Substring(delim + 1, Path.Length - (delim + 1));

				if (!next.mValues.TryGetValue(name, out next))
				{
					next = null;
					break;
				}
			}
			else if (delim == 0)
			{
				next = null;
				break;
			}
			else
			{
				if (!next.mValues.TryGetValue(path, out next))
					next = null;

				break;
			}
		}

		return next;
	}

	public string GetString(string Path, string Default = "")
	{
		CJSONValue value = _ResolvePath(Path);

		if (value == null || value.mType != EValueType.STRING)
			return Default;

		return value.mString;
	}

	public bool GetBool(string Path, bool Default = false)
	{
		CJSONValue value = _ResolvePath(Path);

		if (value == null || value.mType != EValueType.BOOL)
			return Default;

		return value.mBool;
	}

	public int GetInt(string Path, int Default = 0)
	{
		CJSONValue value = _ResolvePath(Path);

		if (value == null || value.mType != EValueType.NUMBER)
			return Default;

		return (int)value.mNumber;
	}

	public float GetFloat(string Path, float Default = 0.0f)
	{
		CJSONValue value = _ResolvePath(Path);

		if (value == null || value.mType != EValueType.NUMBER)
			return Default;

		return (float)value.mNumber;
	}
}

public class CJSONParser
{
	public enum EToken
	{
		NONE,
		STRING,
		NUMBER,
		TRUE,
		FALSE,
		NULL,
		OPEN_OBJECT,
		CLOSE_OBJECT,
		LIST_SEPARATOR,
		ASSIGNMENT,
		OPEN_ARRAY,
		CLOSE_ARRAY,
		EOF,
	}

	public enum EParseResult
	{
		FAILED,
		NONE,
		SUCCESS
	}

	public struct SParseResult
	{
		public EParseResult mResult;
		public CJSONValue mValue;

		public SParseResult(EParseResult Result)
		{	
			mResult = Result;
			mValue = null;
		}

		public SParseResult(CJSONValue Value)
		{
			mResult = EParseResult.SUCCESS;
			mValue = Value;
		}
	}

	private string _stream;
	private int _streamIndex;
	private char _nextChar;
	private EToken _nextTokenType;
	private int _nextTokenStart;
	private string _nextTokenLexeme;
	private double _nextTokenNumber;

	public CJSONParser()
	{
	}

	public CJSONValue Parse(string FilePath)
	{
		_streamIndex = 0;
		_stream = File.ReadAllText(FilePath);

		SParseResult result = _Parse();

		if (result.mResult == EParseResult.FAILED)
		{
			int line, column;
			_GetFilePositionFromBufferPosition(_streamIndex, out line, out column);
			Debug.LogError("JSON Parse Error : " + FilePath + ":" + line + ":" + column + " (" + _nextTokenType + ":" + _nextTokenStart + ")");
			return null;
		}
		else
		{
			//Debug.Log("Parse Complete!");
			//_PrintValue(result.mValue, 0);
			return result.mValue;
		}
	}

	private string _GetIndents(int Count)
	{
		string result = "";

		while (Count-- > 0)
			result += "  ";

		return result;
	}

	private void _PrintValue(CJSONValue Value, int Indent)
	{
		if (Value.mType ==  CJSONValue.EValueType.NULL)
		{
			Debug.Log(_GetIndents(Indent) + "Null");
		}
		else if (Value.mType == CJSONValue.EValueType.STRING)
		{
			Debug.Log(_GetIndents(Indent) + "String: " + Value.mString);
		}
		else if (Value.mType == CJSONValue.EValueType.NUMBER)
		{
			Debug.Log(_GetIndents(Indent) + "Number: " + Value.mNumber);
		}
		else if (Value.mType == CJSONValue.EValueType.BOOL)
		{
			Debug.Log(_GetIndents(Indent) + "Bool: " + Value.mBool);
		}
		else if (Value.mType == CJSONValue.EValueType.OBJECT)
		{
			Debug.Log(_GetIndents(Indent) + "Object:");

			foreach (KeyValuePair<string, CJSONValue> pair in Value.mValues)
			{
				Debug.Log(_GetIndents(Indent + 1) + "Key: " + pair.Key);
				_PrintValue(pair.Value, Indent + 1);
			}
		}
		else if (Value.mType == CJSONValue.EValueType.ARRAY)
		{
			Debug.Log(_GetIndents(Indent) + "Array:");

			foreach (KeyValuePair<string, CJSONValue> pair in Value.mValues)
			{
				Debug.Log(_GetIndents(Indent + 1) + "Key: " + pair.Key);
				_PrintValue(pair.Value, Indent + 1);
			}
		}
	}

	private void _ReadNextChar()
	{
		if (_streamIndex < _stream.Length)
			_nextChar = _stream[_streamIndex++];
		else
			_nextChar = '\0';
	}

	private bool _IsAlpha(char Char)
	{
		return ((Char >= 'a' && Char <= 'z') || (Char >= 'A' && Char <= 'Z') || (Char == '_'));
	}

	private bool _IsDigit(char Char)
	{
		return (Char >= '0' && Char <= '9');
	}

	private bool _ReadNextToken()
	{
		while (_nextChar == ' ' || _nextChar == '\t' || _nextChar == '\r' || _nextChar == '\n')
		{
			_ReadNextChar();
		}

		_nextTokenType = EToken.NONE;
		_nextTokenLexeme = "";
		_nextTokenNumber = 0.0;
		_nextTokenStart = _streamIndex;

		if (_nextChar == '\0')
		{
			_nextTokenType = EToken.EOF;
			return true;
		}

		if (_nextChar == '{') { _nextTokenType = EToken.OPEN_OBJECT; _ReadNextChar(); return true; }
		if (_nextChar == '}') { _nextTokenType = EToken.CLOSE_OBJECT; _ReadNextChar(); return true; }
		if (_nextChar == '[') { _nextTokenType = EToken.OPEN_ARRAY; _ReadNextChar(); return true; }
		if (_nextChar == ']') { _nextTokenType = EToken.CLOSE_ARRAY; _ReadNextChar(); return true; }
		if (_nextChar == ',') { _nextTokenType = EToken.LIST_SEPARATOR; _ReadNextChar(); return true; }
		if (_nextChar == ':') { _nextTokenType = EToken.ASSIGNMENT; _ReadNextChar(); return true; }

		if (_nextChar == '"')
		{
			_nextTokenType = EToken.STRING;
			_ReadNextChar();

			while (_nextChar != '\0')
			{
				if (_nextChar == '"')
				{
					_ReadNextChar();
					return true;
				}

				_nextTokenLexeme += _nextChar;
				_ReadNextChar();
			}

			return false;
		}

		if (_IsAlpha(_nextChar))
		{
			_nextTokenType = EToken.STRING;
			_nextTokenLexeme += _nextChar;
			_ReadNextChar();

			while (_IsAlpha(_nextChar) || _IsDigit(_nextChar))
			{
				_nextTokenLexeme += _nextChar;
				_ReadNextChar();
			}

			if (_nextTokenLexeme == "true") { _nextTokenType = EToken.TRUE; }
			else if (_nextTokenLexeme == "false") { _nextTokenType = EToken.FALSE; }
			else if (_nextTokenLexeme == "null") { _nextTokenType = EToken.NULL; }

			return true;
		}

		if (_IsDigit(_nextChar))
		{
			_nextTokenType = EToken.NUMBER;
			_nextTokenLexeme += _nextChar;
			_ReadNextChar();

			while (_IsDigit(_nextChar))
			{
				_nextTokenLexeme += _nextChar;
				_ReadNextChar();
			}

			if (_nextChar == '.')
			{
				_nextTokenLexeme += _nextChar;
				_ReadNextChar();

				if (!_IsDigit(_nextChar))
					return false;

				_nextTokenLexeme += _nextChar;
				_ReadNextChar();

				while (_IsDigit(_nextChar))
				{
					_nextTokenLexeme += _nextChar;
					_ReadNextChar();
				}
			}

			double.TryParse(_nextTokenLexeme, out _nextTokenNumber);
			return true;
		}

		return false;
	}

	private SParseResult _ParseValue()
	{
		if (_nextTokenType == EToken.STRING)
		{
			CJSONValue value = new CJSONValue(_nextTokenLexeme);
			_ReadNextToken();
			return new SParseResult(value);
		}
		else if (_nextTokenType == EToken.NUMBER)
		{
			CJSONValue value = new CJSONValue(_nextTokenNumber);
			_ReadNextToken();
			return new SParseResult(value);
		}
		else if (_nextTokenType == EToken.TRUE)
		{
			CJSONValue value = new CJSONValue(true);
			_ReadNextToken();
			return new SParseResult(value);
		}
		else if (_nextTokenType == EToken.FALSE)
		{
			CJSONValue value = new CJSONValue(false);
			_ReadNextToken();
			return new SParseResult(value);
		}
		else if (_nextTokenType == EToken.NULL)
		{
			CJSONValue value = new CJSONValue();
			_ReadNextToken();
			return new SParseResult(value);
		}
		else
		{
			SParseResult result = _ParseObject();
			if (result.mResult != EParseResult.NONE)
				return result;

			result = _ParseArray();
			if (result.mResult != EParseResult.NONE)
				return result;
		}

		return new SParseResult(EParseResult.NONE);
	}

	private SParseResult _ParseArray()
	{
		if (_nextTokenType == EToken.OPEN_ARRAY)
		{
			_ReadNextToken();
			CJSONValue value = new CJSONValue();
			value.mType = CJSONValue.EValueType.ARRAY;

			bool requiredValue = false;
			while (true)
			{
				SParseResult result = _ParseValue();
				if (result.mResult == EParseResult.FAILED)
					return result;

				if (requiredValue && result.mResult != EParseResult.SUCCESS)
					return new SParseResult(EParseResult.FAILED);

				if (result.mResult == EParseResult.SUCCESS)
				{
					value.mValues[value.mValues.Count.ToString()] = result.mValue;

					if (_nextTokenType == EToken.LIST_SEPARATOR)
					{
						_ReadNextToken();
						requiredValue = true;
					}
					else
					{
						break;
					}
				}
				else
				{
					break;
				}
			}

			if (_nextTokenType != EToken.CLOSE_ARRAY)
				return new SParseResult(EParseResult.FAILED);

			_ReadNextToken();

			return new SParseResult(value);
		}

		return new SParseResult(EParseResult.NONE);
	}

	private SParseResult _ParseObject()
	{
		if (_nextTokenType == EToken.OPEN_OBJECT)
		{
			_ReadNextToken();
			CJSONValue value = new CJSONValue();
			value.mType = CJSONValue.EValueType.OBJECT;

			while (_nextTokenType == EToken.STRING)
			{
				string key = _nextTokenLexeme;
				_ReadNextToken();

				if (_nextTokenType != EToken.ASSIGNMENT)
					return new SParseResult(EParseResult.FAILED);

				_ReadNextToken();

				SParseResult result = _ParseValue();
				if (result.mResult != EParseResult.SUCCESS)
					return new SParseResult(EParseResult.FAILED);

				value.mValues[key] = result.mValue;

				if (_nextTokenType == EToken.LIST_SEPARATOR)
				{
					_ReadNextToken();

					if (_nextTokenType != EToken.STRING)
						return new SParseResult(EParseResult.FAILED);
				}
				else
				{
					break;
				}
			}
		
			if (_nextTokenType != EToken.CLOSE_OBJECT)
				return new SParseResult(EParseResult.FAILED);

			_ReadNextToken();

			return new SParseResult(value);
		}

		return new SParseResult(EParseResult.NONE);
	}

	private SParseResult _Parse()
	{
		_ReadNextChar();
		_ReadNextToken();

		SParseResult result = _ParseObject();
		if (result.mResult != EParseResult.NONE)
			return result;

		result = _ParseArray();
		if (result.mResult != EParseResult.NONE)
			return result;

		return new SParseResult(EParseResult.NONE);
	}

	private void _GetFilePositionFromBufferPosition(int Position, out int Line, out int Column)
	{
		Line = 1;
		Column = 0;

		if (_stream == null)
			return;

		for (int i = 0; i < _stream.Length; ++i)
		{
			if (_stream[i] == '\t')
				Column += 4;
			else
				++Column;

			if (i == Position)
				return;

			if (_stream[i] == '\n')
			{
				++Line;
				Column = 0;
			}
		}
	}
}
