using System;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;

public class CUnityInterface : MonoBehaviour
{
	public string CommandLineArguments = "";

	private CGame _game;

#if UNITY_EDITOR
	[DllImport("kernel32", SetLastError = true, CharSet = CharSet.Unicode)]
	internal static extern IntPtr LoadLibrary(string lpFileName);
#endif

	//-------------------------------------------------------------
	// Unity Events
	//-------------------------------------------------------------	
	void Awake()
	{
#if UNITY_EDITOR
		IntPtr lib = LoadLibrary("Assets\\Plugins\\x86_64\\PaperworkNative.dll");
#endif

		CSteam.SteamRestartIfNecessary();
	}

	void Start()
	{
#if !UNITY_EDITOR
		CommandLineArguments = System.Environment.CommandLine;
#endif
		_game = new CGame(this, CommandLineArguments);
	}

	void Update()
	{
		if (_game != null)
			_game.Update();
	}

	void OnGUI()
	{
		if (_game != null)
			_game.OnGUI();
	}

	void OnDestroy()
	{
		if (_game != null)
			_game.Destroy();
	}
}
