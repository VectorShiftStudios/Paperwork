using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// Bindable command.
/// </summary>
public class CInputCommand
{
	public enum EModifier
	{
		NONE,
		CTRL,
		SHIFT,
		ALT
	}

	public string mName;
	public bool mGlobal;

	public bool mDown;
	public bool mPressed;
	public bool mReleased;
	
	// TODO: Have a list of keys that can affect this command.
	public EModifier mModifier;
	public KeyCode mButton;
}

/// <summary>
/// Current input state.
/// This is all dumb.
/// </summary>
public class CInputState
{
	public bool mOverUI;

	public Vector3 mMousePosition;

	public bool mMouseLeft;
	public bool mMouseLeftDown;
	public bool mMouseLeftUp;

	public bool mMouseRight;
	public bool mMouseRightDown;
	public bool mMouseRightUp;

	private CInputCommand _dummyCommand = new CInputCommand();

	public Dictionary<string, CInputCommand> mCmds = new Dictionary<string, CInputCommand>();

	public void RegisterCommand(string Name, KeyCode Button, CInputCommand.EModifier Modifier, bool Global = false)
	{
		CInputCommand cmd = new CInputCommand();
		cmd.mName = Name;
		cmd.mModifier = Modifier;
		cmd.mButton = Button;
		cmd.mGlobal = Global;
		mCmds[Name] = cmd;
	}

	public void RegisterCommand(string Name, KeyCode Button, bool Global = false)
	{
		RegisterCommand(Name, Button, CInputCommand.EModifier.NONE, Global);
	}

	public void RegisterCommand(string Name, string ButtonString, bool Global = false)
	{
		KeyCode keyCode = KeyCode.None;
		CInputCommand.EModifier modifier = CInputCommand.EModifier.NONE;

		string[] args = ButtonString.Split('+');

		try
		{
			if (args.Length == 2)
			{
				modifier = (CInputCommand.EModifier)Enum.Parse(typeof(CInputCommand.EModifier), args[0], true);
				keyCode = (KeyCode)Enum.Parse(typeof(KeyCode), args[1], true);
			}
			else if (args.Length == 1)
			{
				keyCode = (KeyCode)Enum.Parse(typeof(KeyCode), args[0], true);
			}

			RegisterCommand(Name, keyCode, modifier, Global);
		}
		catch (Exception Ex)
		{
			Debug.LogError("Bad key binding for " + Name + ": " + Ex.Message);
		}
	}

	public string GetButtonString()
	{
		return "";
	}

	public CInputCommand GetCommand(string Name)
	{
		CInputCommand result;

		if (!mCmds.TryGetValue(Name, out result))
			return _dummyCommand;

		return result;
	}

	/// <summary>
	/// Checks if any registered command is down.
	/// </summary>
	public bool IsAnyKeyDown()
	{
		foreach (KeyValuePair<string, CInputCommand> entry in mCmds)
		{
			CInputCommand c = entry.Value;

			if (c.mDown)
				return true;
		}

		return false;
	}

	/// <summary>
	/// Called once per frame.
	/// Determines new input state.
	/// </summary>
	public void Update()
	{
		mMouseLeftDown = false;
		mMouseLeftUp = false;
		mMouseRightDown = false;
		mMouseRightUp = false;

		EventSystem ev = EventSystem.current;

		mMousePosition = Input.mousePosition;

		// NOTE: There seems to be a bug in Unity where an input field remains the current selected object even after
		// it defocuses itself on end of editing.
		if (ev.currentSelectedGameObject != null)
		{
			InputField input = ev.currentSelectedGameObject.GetComponent<InputField>();

			if (input != null && !input.isFocused)
				ev.SetSelectedGameObject(null);
		}

		foreach (KeyValuePair<string, CInputCommand> entry in mCmds)
		{
			CInputCommand c = entry.Value;

			if (c.mButton != KeyCode.None)
			{
				if (ev.currentSelectedGameObject == null || c.mGlobal)
				{
					if (c.mModifier != CInputCommand.EModifier.NONE)
					{
						bool modiferDown = false;

						if (c.mModifier == CInputCommand.EModifier.CTRL && (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)))
							modiferDown = true;

						if (modiferDown)
						{
							c.mDown = Input.GetKey(c.mButton);
							c.mPressed = Input.GetKeyDown(c.mButton);
							c.mReleased = Input.GetKeyUp(c.mButton);
						}
						else
						{
							c.mReleased = c.mDown;
							c.mDown = false;
							c.mPressed = false;
						}
					}
					else
					{
						if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
						{
							c.mReleased = c.mDown;
							c.mDown = false;
							c.mPressed = false;
						}
						else
						{
							c.mDown = Input.GetKey(c.mButton);
							c.mPressed = Input.GetKeyDown(c.mButton);
							c.mReleased = Input.GetKeyUp(c.mButton);
						}
					}
				}
				else
				{
					c.mDown = false;
					c.mPressed = false;
					// TODO: Only Released if Down in command was true
					c.mReleased = false;
				}
			}
		}

		mMouseLeftDown = false;
		mMouseLeftUp = false;

		// TODO: Events for enter/exit game/ui.
		bool newOver = ev.IsPointerOverGameObject();
		if (mOverUI != newOver)
			mOverUI = newOver;

		if (Input.GetMouseButtonDown(0))
		{
			if (!mOverUI)
			{
				ev.SetSelectedGameObject(null);

				mMouseLeft = true;
				mMouseLeftDown = true;
				mMouseLeftUp = false;
			}
		}

		if (Input.GetMouseButtonUp(0))
		{
			if (mMouseLeft)
			{
				mMouseLeft = false;
				mMouseLeftUp = true;
			}
		}

		if (Input.GetMouseButtonDown(1))
		{
			if (!mOverUI)
			{
				ev.SetSelectedGameObject(null);

				mMouseRight = true;
				mMouseRightDown = true;
				mMouseRightUp = false;
			}
		}

		if (Input.GetMouseButtonUp(1))
		{
			if (mMouseRight)
			{
				mMouseRight = false;
				mMouseRightUp = true;
			}
		}
	}
}
