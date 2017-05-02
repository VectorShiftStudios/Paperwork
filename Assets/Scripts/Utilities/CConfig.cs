using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manage parameters that can be saved/loaded.
/// </summary>
public class CConfig
{
	public Dictionary<string, string> _entries;
	public string mFilePath;

	public CConfig(string FilePath)
	{
		mFilePath = FilePath;

		_entries = new Dictionary<string, string>();

		_entries.Add("MasterVolume", 0.98f.ToString());
		_entries.Add("MusicVolume", 1.ToString());
		_entries.Add("SoundsVolume", 1.ToString());
		_entries.Add("UISoundsVolume", 1.ToString());

		_entries.Add("ResolutionType", "default");
		_entries.Add("ResolutionWidth", "1280");
		_entries.Add("ResolutionHeight", "720");

		_entries.Add("KeyConsole", KeyCode.BackQuote.ToString());
		_entries.Add("KeyEscape", KeyCode.Escape.ToString());
		_entries.Add("KeyOptionsMenu", KeyCode.F10.ToString());

		_entries.Add("KeyFocusOnSpawn", KeyCode.Home.ToString());

		_entries.Add("KeyCamForward", KeyCode.W.ToString());
		_entries.Add("KeyCamLeft", KeyCode.A.ToString());
		_entries.Add("KeyCamBackward", KeyCode.S.ToString());
		_entries.Add("KeyCamRight", KeyCode.D.ToString());

		_entries.Add("KeyPlaceRotate", KeyCode.Space.ToString());
		_entries.Add("KeyPlaceRepeat", KeyCode.LeftShift.ToString());

		_entries.Add("KeyAction1", KeyCode.Alpha1.ToString());
		_entries.Add("KeyAction2", KeyCode.Alpha2.ToString());
		_entries.Add("KeyAction3", KeyCode.Alpha3.ToString());
		_entries.Add("KeyAction4", KeyCode.Alpha4.ToString());

		_entries.Add("KeyEditorDelete", CInputCommand.EModifier.CTRL + "+" + KeyCode.R.ToString());
		_entries.Add("KeyEditorDuplicate", CInputCommand.EModifier.CTRL + "+" + KeyCode.D.ToString());
		_entries.Add("KeyEditorUndo", CInputCommand.EModifier.CTRL + "+" + KeyCode.Z.ToString());
		_entries.Add("KeyEditorRedo", CInputCommand.EModifier.CTRL + "+" + KeyCode.Y.ToString());
		_entries.Add("KeyEditorSave", CInputCommand.EModifier.CTRL + "+" + KeyCode.S.ToString());
	}

	public void Save()
	{
		string configText = "";

		foreach (KeyValuePair<string, string> entry in _entries)
		{
			configText += entry.Key + " = " + entry.Value + "\n";
		}

		File.WriteAllText(mFilePath, configText);
	}

	public void Load()
	{
		string configText = "";

		try
		{
			configText = File.ReadAllText(mFilePath);
		}
		catch (FileNotFoundException Ex)
		{
		}
		catch (Exception Ex)
		{
			Debug.LogError("Loading config: " + Ex.Message);
		}

		string[] lines = configText.Split('\n');

		for (int i = 0; i < lines.Length; ++i)
		{
			string[] data = lines[i].Split('=');

			if (data.Length == 2)
			{
				string key = data[0].Trim();
				string value = data[1].Trim();

				if (key != "" && value != "")
					_entries[key] = value;
			}
		}

		Save();
	}

	public float GetFloat(string Name)
	{
		float result = 0.0f;

		try
		{
			string value = _entries[Name];
			result = float.Parse(value);
		}
		catch (Exception Ex)
		{
			Debug.LogError("Bad float for " + Name + ": " + Ex.Message);
		}

		return result;
	}

	public string GetString(string Name)
	{
		string result = "";

		try
		{
			result = _entries[Name];
		}
		catch (Exception Ex)
		{
			Debug.LogError("Bad string for " + Name + ": " + Ex.Message);
		}

		return result;
	}
}