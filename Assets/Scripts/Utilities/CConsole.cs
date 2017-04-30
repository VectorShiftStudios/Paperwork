using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class CConsole
{
	public delegate void VarDelegate(string[] Params);

	public abstract class CVarBase
	{
		public VarDelegate mExectue;
		public virtual void ChangeValue(string[] Params) { }
		public virtual string GetPrintableValue() { return "(exec)"; }
	}

	public class CVarInt : CVarBase
	{
		public int mValue;
		public int mMin;
		public int mMax;

		public CVarInt(int Value, int Min, int Max)
		{
			mValue = Value;
			mMin = Min;
			mMax = Max;
		}

		public override void ChangeValue(string[] Params)
		{
			if (Params.Length == 1)
			{
				int.TryParse(Params[0], out mValue);
				mValue = Mathf.Clamp(mValue, mMin, mMax);
			}
		}

		public override string GetPrintableValue()
		{
			return mValue.ToString();
		}
	}

	public class CVarBool : CVarBase
	{
		public bool mValue;

		public override void ChangeValue(string[] Params)
		{
			if (Params.Length == 1)
			{
				int temp = 0;
				int.TryParse(Params[0], out temp);
				mValue = (temp == 0) ? false : true;
			}
		}

		public override string GetPrintableValue()
		{
			return mValue.ToString();
		}
	}

	public class CVarString : CVarBase
	{
		public string mValue;

		public override void ChangeValue(string[] Params)
		{
			if (Params.Length == 1)
				mValue = Params[0];
		}

		public override string GetPrintableValue()
		{
			return mValue;
		}
	}

	public class CVarExecute : CVarBase
	{
	}

	public const int CONSOLE_LINE_MAX = 24;

	// Console Stuff	
	private List<string> _consolelines = new List<string>();
	private int _consoleScroll = 0;
	private bool _consoleShowing = true;
	private List<string> _cmdHistory = new List<string>();
	private int _historyIndex = -1;
	private string _lastConsoleText = "";
	private Dictionary<string, CVarBase> _vars;
	private List<string> _matches = new List<string>();
	private int _matchIndex = -1;
	private GameObject _autoCompleteBox;
	private GameObject _autoCompleteHighlighter;

	public CConsole()
	{
		Application.logMessageReceived += LogCallback;
		_vars = new Dictionary<string, CVarBase>();
		CreateCommand("help", _HelpCommand);
		CGame.UIResources.ConsoleInputText.GetComponent<InputField>().onEndEdit.AddListener(OnConsoleEdit);
	}

	public void CreateCommand(string Name, VarDelegate Delegate)
	{
		CVarExecute var = new CVarExecute();
		var.mExectue = Delegate;
		_vars.Add(Name, var);
	}

	public CVarInt CreateVar(string Name, int Value, int Min, int Max)
	{
		CVarInt var = new CVarInt(Value, Min, Max);
		_vars.Add(Name, var);
		return var;
	}

	public CVarBool CreateVar(string Name, bool Value)
	{
		CVarBool var = new CVarBool();
		var.mValue = Value;
		_vars.Add(Name, var);
		return var;
	}

	/// <summary>
	/// Print out all the currently registered commands and variables.
	/// </summary>
	private void _HelpCommand(string[] Params)
	{
		Debug.Log("Console Commands: ");

		foreach (KeyValuePair<string, CVarBase> varPair in _vars)
		{
			Debug.Log(varPair.Key + " : " + varPair.Value.GetPrintableValue());
		}
	}

	/// <summary>
	/// Intercepts Unity log calls.
	/// </summary>
	private void LogCallback(string Message, string StackTrace, LogType Type)
	{
		string prefix = "";
		string suffix = "";

		if (Type == LogType.Warning)
		{
			prefix = "<color=yellow>";
			suffix = "</color>";
		}
		else if (Type == LogType.Error || Type == LogType.Exception)
		{
			prefix = "<color=orange>";
			suffix = "</color>";
		}

		string[] lines = Message.Split('\n');

		for (int i = 0; i < lines.Length; ++i)
			_consolelines.Add(prefix + lines[i] + suffix);
		
		_UpdateConsoleText();
	}

	/// <summary>
	/// Display latest console text in the UI.
	/// </summary>
	private void _UpdateConsoleText()
	{
		string consoleText = "";

		int startLine = _consolelines.Count - CONSOLE_LINE_MAX - _consoleScroll;
		if (startLine < 0)
			startLine = 0;

		int endLine = startLine + CONSOLE_LINE_MAX;
		if (endLine > _consolelines.Count)
			endLine = _consolelines.Count;

		for (int i = startLine; i < endLine; ++i)
		{
			consoleText += "> " + _consolelines[i];

			if (i < endLine - 1)
				consoleText += "\n";
		}

		if (CGame.UIResources != null)
			CGame.UIResources.ConsoleText.text = consoleText;
	}

	/// <summary>
	/// Handle submitted console input.
	/// </summary>
	public void OnConsoleEdit(string InputText)
	{
		if (InputText == "")
			return;

		InputField field = CGame.UIResources.ConsoleInputText.GetComponent<InputField>();
		string cmdStr = field.text;

		for (int i = 0; i < _cmdHistory.Count; ++i)
		{
			if (_cmdHistory[i] == cmdStr)
			{
				_cmdHistory.RemoveAt(i);
				break;
			}
		}

		_cmdHistory.Add(cmdStr);
		_historyIndex = -1;
		
		field.text = "";
		//EventSystem.current.SetSelectedGameObject(UIResources.ConsoleInputText);
		field.Select();
		field.ActivateInputField();

		// Parse string into parms
		string[] fullCmd = cmdStr.Trim().Split(' ');

		if (fullCmd.Length == 0 || fullCmd[0] == "")
		{
			Debug.LogError("No Command Entered");
			return;
		}

		string cmd = fullCmd[0];
		string[] paramList = new string[fullCmd.Length - 1];

		if (fullCmd.Length > 1)
			Array.Copy(fullCmd, 1, paramList, 0, fullCmd.Length - 1);

		CVarBase var = null;
		if (_vars.TryGetValue(cmd, out var))
		{
			var.ChangeValue(paramList);
			if (var.mExectue != null)
				var.mExectue(paramList);

			Debug.Log("Command: [" + cmdStr + "] " + cmd + " is currently " + var.GetPrintableValue());
		}
		else
		{
			Debug.LogWarning("Command Unknown: [" + cmdStr + "] " + cmd);
		}
	}

	public void Show()
	{
		_consoleShowing = true;
		_historyIndex = -1;
		CGame.UIResources.Console.SetActive(true);
		EventSystem.current.SetSelectedGameObject(CGame.UIResources.ConsoleInputText.gameObject);

		CGame.UIResources.ConsoleInputText.GetComponent<InputField>().text = "";
		_lastConsoleText = "";
	}

	public void Hide()
	{
		_consoleShowing = false;
		_historyIndex = -1;
		CGame.UIResources.ConsoleInputText.GetComponent<InputField>().text = "";
		EventSystem.current.SetSelectedGameObject(null);
		CGame.UIResources.Console.SetActive(false);
		CGame.UIResources.ConsoleInputText.GetComponent<InputField>().DeactivateInputField();
		_ClearAutoComplete();
	}

	public List<string> FuzzyMatch(string Text)
	{
		List<string> results = new List<string>();

		string pattern = Text.ToLower();

		foreach (KeyValuePair<string, CVarBase> entry in _vars)
		{
			string str = entry.Key.ToLower();
			int pIdx = 0;
			int sIdx = 0;

			while (pIdx != pattern.Length && sIdx != str.Length)
			{
				if (pattern[pIdx] == str[sIdx])
					++pIdx;

				++sIdx;
			}

			if (pIdx == pattern.Length)
			{
				//Debug.Log("Match: " + str);
				results.Add(str);
			}
		}

		return results;
	}

	private void _ClearAutoComplete()
	{
		_matches.Clear();
		_matchIndex = -1;
		if (_autoCompleteBox != null)
		{
			GameObject.Destroy(_autoCompleteBox);
			_autoCompleteBox = null;
			_autoCompleteHighlighter = null;
		}
	}

	public void Update(CInputState InputState)
	{
		if (InputState.GetCommand("console").mReleased)
		{
			_consoleShowing = !_consoleShowing;

			if (_consoleShowing)
				Show();
			else
				Hide();
		}

		if (_consoleShowing)
		{
			InputField consoleInputField = CGame.UIResources.ConsoleInputText.GetComponent<InputField>();
			bool scrollConsole = false;

			if (consoleInputField.text != _lastConsoleText)
			{
				_lastConsoleText = consoleInputField.text;
				_ClearAutoComplete();

				if (_lastConsoleText != "")
				{
					_matches = FuzzyMatch(consoleInputField.text);
					
					if (_matches.Count != 0)
					{
						_matchIndex = 0;

						// Primary Parent
						_autoCompleteBox = new GameObject();
						RectTransform r = _autoCompleteBox.AddComponent<RectTransform>();
						r.SetParent(CGame.UIResources.CanvasTransform);
						r.pivot = new Vector2(0.0f, 1.0f);
						r.anchorMin = new Vector2(0.0f, 1.0f);
						r.anchorMax = new Vector2(1.0f, 1.0f);
						r.anchoredPosition = new Vector2(0, -CGame.UIResources.Console.GetComponent<RectTransform>().sizeDelta.y);
						r.sizeDelta = new Vector2(0, 20 * _matches.Count);

						Image img = _autoCompleteBox.AddComponent<Image>();
						img.color = CGame.GameUIStyle.TooltipBackground;

						// Highlighter
						_autoCompleteHighlighter = new GameObject();
						r = _autoCompleteHighlighter.AddComponent<RectTransform>();
						r.SetParent(_autoCompleteBox.transform);
						r.pivot = new Vector2(0.0f, 1.0f);
						r.anchorMin = new Vector2(0.0f, 1.0f);
						r.anchorMax = new Vector2(1.0f, 1.0f);
						r.anchoredPosition = new Vector2(0, 0);
						r.sizeDelta = new Vector2(0, 20);
						img = _autoCompleteHighlighter.AddComponent<Image>();
						img.color = CGame.GameUIStyle.ThemeColorA;

						for (int i = 0; i < _matches.Count; ++i)
						{
							GameObject text = new GameObject();
							r = text.AddComponent<RectTransform>();
							r.SetParent(_autoCompleteBox.transform);
							r.pivot = new Vector2(0.0f, 1.0f);
							r.anchorMin = new Vector2(0.0f, 1.0f);
							r.anchorMax = new Vector2(1.0f, 1.0f);
							r.anchoredPosition = new Vector2(5, -i * 20);
							r.sizeDelta = new Vector2(0, 20);

							Text t = text.AddComponent<Text>();
							t.font = CGame.GameUIStyle.FontA;
							t.fontSize = 12;
							t.text = _matches[i];
						}

						_autoCompleteBox.transform.GetChild(_matchIndex + 1).GetComponent<Text>().color = CGame.GameUIStyle.ThemeColorC;
					}
				}
			}

			if (Input.GetKeyDown(KeyCode.Tab))
			{
				if (_matchIndex != -1)
				{
					consoleInputField.text = _autoCompleteBox.transform.GetChild(_matchIndex + 1).GetComponent<Text>().text;
					consoleInputField.caretPosition = consoleInputField.text.Length;
				}
			}

			if (Input.GetKeyDown(KeyCode.UpArrow))
			{
				if (_matchIndex > 0)
				{
					_autoCompleteBox.transform.GetChild(_matchIndex + 1).GetComponent<Text>().color = CGame.GameUIStyle.ThemeColorE;
					_matchIndex = Mathf.Clamp(_matchIndex - 1, 0, _matches.Count - 1);
					_autoCompleteHighlighter.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -_matchIndex * 20);
					_autoCompleteBox.transform.GetChild(_matchIndex + 1).GetComponent<Text>().color = CGame.GameUIStyle.ThemeColorC;
				}
				else if (_cmdHistory.Count > 0)
				{
					if (_historyIndex == -1)
						_historyIndex = _cmdHistory.Count;

					_historyIndex = Mathf.Clamp(_historyIndex - 1, 0, _cmdHistory.Count - 1);
					consoleInputField.text = _cmdHistory[_historyIndex];
					_lastConsoleText = consoleInputField.text;
					_ClearAutoComplete();
				}

				consoleInputField.caretPosition = consoleInputField.text.Length;
			}

			if (Input.GetKeyDown(KeyCode.DownArrow))
			{
				if (_historyIndex == -1)
				{
					if (_matches.Count != 0)
					{
						_autoCompleteBox.transform.GetChild(_matchIndex + 1).GetComponent<Text>().color = CGame.GameUIStyle.ThemeColorE;
						_matchIndex = Mathf.Clamp(_matchIndex + 1, 0, _matches.Count - 1);
						_autoCompleteHighlighter.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -_matchIndex * 20);
						_autoCompleteBox.transform.GetChild(_matchIndex + 1).GetComponent<Text>().color = CGame.GameUIStyle.ThemeColorC;
					}
				}
				else
				{
					if (_cmdHistory.Count > 0)
					{
						if (_historyIndex == _cmdHistory.Count - 1)
						{
							_historyIndex = -1;
							consoleInputField.text = "";
						}
						else
						{
							_historyIndex = Mathf.Clamp(_historyIndex + 1, 0, _cmdHistory.Count - 1);
							consoleInputField.text = _cmdHistory[_historyIndex];
							consoleInputField.caretPosition = consoleInputField.text.Length;
							_lastConsoleText = consoleInputField.text;
							_ClearAutoComplete();
						}
					}
				}
			}

			if (Input.GetKey(KeyCode.PageDown))
			{
				--_consoleScroll;
				scrollConsole = true;
			}
			else if (Input.GetKey(KeyCode.PageUp))
			{
				++_consoleScroll;
				scrollConsole = true;
			}

			if (scrollConsole)
			{
				_consoleScroll = Mathf.Clamp(_consoleScroll, 0, _consolelines.Count - CONSOLE_LINE_MAX);
				_UpdateConsoleText();
			}
		}
	}
}