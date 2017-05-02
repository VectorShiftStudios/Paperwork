using System;
using System.Diagnostics;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Analytics;

using Debug = UnityEngine.Debug;

// TODO: Check if timing is wrapping because of 32bit results??
// TODO: Add some way to specify default assets, probably in a game info asset.

/// <summary>
/// The primary game logic.
/// </summary>
public class CGame
{
	public const int VERSION_MAJOR = 0;
	public const int VERSION_MINOR = 30;
	public const string VERSION_NAME = "Alpha";
	public const string PRODUCT_NAME = "Paperwork";

	public const int LEVEL_FILE_VERSION = 1;
	public const int STATE_FILE_VERSION = 1;

	public const string REPLAYS_DIRECTORY = "Replays/";
	public const string SAVES_DIRECTORY = "Saves/";

	public const string REPLAY_FILE_EXTENSION = ".pwr";
	public const string SAVE_FILE_EXTENSION = ".pws";
	public const string FIBRE_FILE_EXTENSION = ".fibre";

	public static Color COLOR_BLUEPRRINT = new Color(0.2f, 0.5f, 1.0f, 1.0f);

	// Shared Persistent Systems
	public static CGame Game;
	public static CSteam Steam;
	public static CConfig Config;
	public static CConsole Console;
	public static CProfilerManager ProfilerManager;
	public static CProfiler PrimaryThreadProfiler;
	public static CProfiler SimThreadProfiler;
	public static CDebugLevels DebugLevels;
	public static CRandomStream UniversalRandom;

	public static CPrimaryResources PrimaryResources;
	public static CWorldResources WorldResources;
	public static CUIResources UIResources;
	public static CResources Resources;

	public static CAssetManager AssetManager;
	public static CUIManager UIManager;
	public static CIconBuilder IconBuilder;

	public static CToolkitUI ToolkitUI;
	public static CGameUIStyle GameUIStyle;

	public static CCameraManager CameraManager;
	public static CNet Net;
	// TODO: Debug prims should be a static system here maybe

	public static string PersistentDataDirectory;
	public static string DataDirectory;

	// Console Variables
	public static CConsole.CVarBool VarShowGrid;
	public static CConsole.CVarBool VarShowVisLines;
	public static CConsole.CVarBool VarShowDDATest;
	public static CConsole.CVarBool VarShowArcTest;
	public static CConsole.CVarBool VarFreePurchases;
	public static CConsole.CVarBool VarShowProfiler;
	public static CConsole.CVarBool VarShowBounds;
	public static CConsole.CVarBool VarShowDebugStats;
	public static CConsole.CVarInt VarShowMobility;
	public static CConsole.CVarInt VarShowSolidity;
	public static CConsole.CVarBool VarShowPathing;
	public static CConsole.CVarBool VarShowFlowField;
	public static CConsole.CVarBool VarShowNavMesh;
	public static CConsole.CVarInt VarShowNavRect;
	public static CConsole.CVarInt VarShowProxies;
	public static CConsole.CVarBool VarShowComfort;
	public static CConsole.CVarBool VarShowEfficiency;
	public static CConsole.CVarBool VarNoFow;
	public static CConsole.CVarBool VarPlaceItemDirect;

	private string[] _cmdArgs;
	private CInputState _inputState;
	private CGameSession _gameSession;
	private CAssetToolkit _assetToolkit;
	private CGameSession.CStartParams _gameStartParams;

	/// <summary>
	/// Launch another Paperwork instance.
	/// </summary>
	public void LaunchProcess(string Args)
	{
		try
		{
			Process.Start("Paperwork.exe", Args);
		}
		catch (Exception Ex)
		{
			Debug.LogError("External process failed to launch: " + Ex.Message);
		}
	}

	/// <summary>
	/// Game startup.
	/// </summary>
	public CGame(CUnityInterface Interface, string CommandLineArgs)
	{	
		_cmdArgs = CommandLineArgs.Split(' ');
		string[] parms;

#if !UNITY_EDITOR
		DataDirectory = Application.dataPath + "/Data/";
		PersistentDataDirectory = Application.persistentDataPath + "/";
#else
		DataDirectory = "Data/";
		PersistentDataDirectory = "SaveData/";
#endif

		Config = new CConfig(PersistentDataDirectory + "config.txt");
		Config.Load();

#if !UNITY_EDITOR
		DataDirectory = Application.dataPath + "/Data/";
		PersistentDataDirectory = Application.persistentDataPath + "/";

		if (_CheckArg("dev", out parms))
		{
			Screen.SetResolution(1280, 720, false);
		}
		else
		{
			string resType = Config.GetString("ResolutionType");

			if (resType == "default")
			{
				Resolution r = Screen.resolutions[Screen.resolutions.Length - 1];
				Screen.SetResolution(r.width, r.height, true);
			}
			else if (resType == "fullscreen" || resType == "windowed")
			{
				Resolution r = Screen.resolutions[Screen.resolutions.Length - 1];
				int resX = (int)Config.GetFloat("ResolutionWidth");
				int resY = (int)Config.GetFloat("ResolutionHeight");

				Screen.SetResolution(resX, resY, (resType == "fullscreen"));
			}
		}
#endif

		CUtility.MakeDirectory(PersistentDataDirectory + SAVES_DIRECTORY);
		CUtility.MakeDirectory(PersistentDataDirectory + REPLAYS_DIRECTORY);

		PrimaryResources = Interface.GetComponent<CPrimaryResources>();
		WorldResources = Interface.GetComponent<CWorldResources>();
		UIResources = Interface.GetComponent<CUIResources>();
		ToolkitUI = Interface.GetComponent<CToolkitUI>();
		GameUIStyle = Interface.GetComponent<CGameUIStyle>();

		Console = new CConsole();
		
		Debug.Log("Save game directory: " + PersistentDataDirectory);
		Debug.Log("Data directory: " + DataDirectory);

		VarShowGrid = Console.CreateVar("show_grid", false);
		VarShowVisLines = Console.CreateVar("show_los", false);
		VarShowDDATest = Console.CreateVar("show_ddatest", false);
		VarShowArcTest = Console.CreateVar("show_arctest", false);
		VarShowBounds = Console.CreateVar("show_bounds", false);
		VarShowDebugStats = Console.CreateVar("show_debugstats", false);
		VarShowMobility = Console.CreateVar("show_mobility", 0, 0, CWorld.MAX_PLAYERS);
		VarShowSolidity = Console.CreateVar("show_solidity", 0, 0, CWorld.MAX_PLAYERS + 1);
		VarShowProfiler = Console.CreateVar("show_profiler", false);
		VarNoFow = Console.CreateVar("no_fow", false);
		VarPlaceItemDirect = Console.CreateVar("place_item_direct", false);
		VarShowComfort = Console.CreateVar("show_comfort", false);
		VarShowEfficiency = Console.CreateVar("show_efficiency", false);

		VarShowPathing = Console.CreateVar("pathing", false);
        VarShowFlowField = Console.CreateVar("show_flowfield", false);
		VarShowNavMesh = Console.CreateVar("show_navmesh", false);
		VarShowNavRect = Console.CreateVar("show_navrect", 0, 0, CWorld.MAX_PLAYERS);
		VarShowProxies = Console.CreateVar("show_proxies", 0, 0, CWorld.MAX_PLAYERS);

		VarFreePurchases = Console.CreateVar("freebuy", true);
        Console.CreateCommand("gameui", (Params) => { UIManager.ToggleUIActive(); });
		Console.CreateCommand("quit", (Params) => { ExitApplication(); });
		Console.CreateCommand("exit", (Params) => { ExitApplication(); });
		Console.CreateCommand("set_owed", (Params) => { if (_gameSession == null) return; _gameSession.SetOwed(1000); });
		Console.CreateCommand("set_stamina", (Params) => { if (_gameSession == null) return; _gameSession.SetStamina(10.0f); });
		Console.CreateCommand("set_hunger", (Params) => { if (_gameSession == null) return; _gameSession.SetHunger(80); });
		Console.CreateCommand("rebuild_icons", (Params) => { IconBuilder.RebuildItemIcons(true); });
		
		Game = this;
		Steam = new CSteam();
		PrimaryThreadProfiler = new CProfiler();
		SimThreadProfiler = new CProfiler();
		DebugLevels = new CDebugLevels();
		UniversalRandom = new CRandomStream();
		AssetManager = new CAssetManager();
		Net = new CNet();
		Resources = new CResources();
		CameraManager = new CCameraManager();
		UIManager = new CUIManager(ToolkitUI, GameUIStyle);
		CDebug.StaticInit();
		AssetManager.Init();
		ProfilerManager = new CProfilerManager();
		ProfilerManager.Init();
		IconBuilder = new CIconBuilder();
		IconBuilder.Init();
		_inputState = new CInputState();

		Console.Hide();
		Analytics.SetUserId(Steam.mSteamID.ToString());

		// TODO: Backquote is not ~, investigate.
		// TOOD: Allow the same command to have multiple keys associated with it.
		_inputState.RegisterCommand("console", Config.GetString("KeyConsole"), true);

		_inputState.RegisterCommand("escape", Config.GetString("KeyEscape"));

		_inputState.RegisterCommand("focusOnSpawn", Config.GetString("KeyFocusOnSpawn"));

		_inputState.RegisterCommand("camForward", Config.GetString("KeyCamForward"));
		_inputState.RegisterCommand("camLeft", Config.GetString("KeyCamLeft"));
		_inputState.RegisterCommand("camBackward", Config.GetString("KeyCamBackward"));
		_inputState.RegisterCommand("camRight", Config.GetString("KeyCamRight"));
		_inputState.RegisterCommand("camRotateLeft", KeyCode.Delete);
		_inputState.RegisterCommand("camRotateRight", KeyCode.PageDown);

		_inputState.RegisterCommand("itemPlaceRotate", Config.GetString("KeyPlaceRotate"));
		_inputState.RegisterCommand("itemPlaceRepeat", Config.GetString("KeyPlaceRepeat"));

		_inputState.RegisterCommand("action1", Config.GetString("KeyAction1"));
		_inputState.RegisterCommand("action2", Config.GetString("KeyAction2"));
		_inputState.RegisterCommand("action3", Config.GetString("KeyAction3"));
		_inputState.RegisterCommand("action4", Config.GetString("KeyAction4"));
		
		_inputState.RegisterCommand("openOptions", Config.GetString("KeyOptionsMenu"));

		_inputState.RegisterCommand("reload", KeyCode.F5);
		_inputState.RegisterCommand("space", KeyCode.Space);

		_inputState.RegisterCommand("editorDelete", Config.GetString("KeyEditorDelete"));
		_inputState.RegisterCommand("editorDuplicate", Config.GetString("KeyEditorDuplicate"));
		_inputState.RegisterCommand("editorUndo", Config.GetString("KeyEditorUndo"));
		_inputState.RegisterCommand("editorRedo", Config.GetString("KeyEditorRedo"));
		_inputState.RegisterCommand("editorSave", Config.GetString("KeyEditorSave"));

		// Apply default settings
		//Application.targetFrameRate = 60;
		//QualitySettings.antiAliasing

		// Volume range: 0.0 - -80.0
		// TODO: Volume in DB is exponential, making 0 to 1 range for config ineffective.
		UIResources.MasterMixer.SetFloat("MasterVolume", CMath.MapRangeClamp(Config.GetFloat("MasterVolume"), 0, 1, -80, -12));
		UIResources.MasterMixer.SetFloat("MusicVolume", CMath.MapRangeClamp(Config.GetFloat("MusicVolume"), 0, 1, -80, 0));
		UIResources.MasterMixer.SetFloat("SoundsVolume", CMath.MapRangeClamp(Config.GetFloat("SoundsVolume"), 0, 1, -80, 0));
		UIResources.MasterMixer.SetFloat("UISoundsVolume", CMath.MapRangeClamp(Config.GetFloat("UISoundsVolume"), 0, 1, -80, 0));

		// NOTE: BE SUPER CAREFUL WITH THIS
		// You can corrupt ALL the item assets if not careful.
		// Saves asset to disk, but asset currently in memory won't reflect new version.
		//_resaveAllItemAssetsToLastestVersion();

		// TODO: This bootstraps all model assets on startup. 
		// If the model asset is first loaded by the sim thread, then it will crash as it tries to generate the meshes.
		// Should probably only generate meshes when they are pulled in by the main thread.
		_testItemAssetVersion();

		if (_CheckArg("toolkit", out parms))
		{	
			StartAssetToolkit();
		}
		else if (_CheckArg("map", out parms))
		{
			if (parms != null && parms.Length > 0)
			{
				CGameSession.CStartParams startParams = new CGameSession.CStartParams();
				startParams.mPlayType = CGameSession.EPlayType.SINGLE;
				startParams.mUserPlayerIndex = 0; // Will be set by the level when loaded.				
				startParams.mLevelName = parms[0];
				StartGameSession(startParams);
			}
		}
		else
		{
			UIManager.AddInterface(new CMainMenuUI());
		}
	}

	/// <summary>
	/// Load all item assets and check their version.
	/// </summary>
	private void _testItemAssetVersion()
	{
		List<CItemAsset> assets = AssetManager.GetAllItemAssets();

		Debug.LogWarning("Checking Item Asset Version (" + CItemAsset.VERSION + ")");

		for (int i = 0; i < assets.Count; ++i)
		{
			Debug.LogWarning(assets[i].mName + " " + assets[i].mVersion);
		}
	}

	/// <summary>
	/// Save all existing item assets in the latest version.
	/// </summary>
	private void _resaveAllItemAssetsToLastestVersion()
	{
		List<CItemAsset> assets = AssetManager.GetAllItemAssets();

		for (int i = 0; i < assets.Count; ++i)
		{
			assets[i].Save();
		}
	}

	private bool _CheckArg(string Arg, out string[] Params)
	{
		Params = new string[0];
		string[] args = _cmdArgs;

		for (int i = 0; i < args.Length; ++i)
		{
			if (args[i] == "-" + Arg)
			{
				int paramCount = args.Length - i - 1;

				if (paramCount > 0)
				{
					Params = new string[paramCount];
					Array.Copy(args, i + 1, Params, 0, paramCount);
				}

				return true;
			}
		}

		return false;
	}

	public void StartAssetToolkit()
	{
		if (_assetToolkit != null)
			_assetToolkit.Destroy();

		_assetToolkit = new CAssetToolkit();
		_assetToolkit.Init(ToolkitUI);
	}

	public bool StartGameSession(CGameSession.CStartParams StartParams)
	{
		_gameStartParams = StartParams;

		if (_gameSession != null)
			_gameSession.Destroy();

		_gameSession = new CGameSession();
		if (!_gameSession.Init(StartParams))
		{
			Debug.LogError("Game session failed to launch");
			TerminateGameSession();
			return false;
		}

		_gameSession.Start();

		Analytics.CustomEvent("levelStart", new Dictionary<string, object>
		{
			{ "name", StartParams.mLevelName },
			{ "type", (int)StartParams.mPlayType }
		});

		return true;
	}

	public void TerminateGameSession(bool TransitionToMainMenu = false)
	{
		if (_gameSession != null)
		{
			_gameSession.Destroy();
			_gameSession = null;
		}

		if (TransitionToMainMenu)
		{
			UIManager.AddInterface(new CMainMenuUI());
		}

		// TODO: Game UI needs to fuck off.
	}

	public void ExitApplication()
	{
		Application.Quit();
	}
	
	/// <summary>
	/// Every render frame.
	/// </summary>
	public void Update()
	{
		IconBuilder.Update();

		long simStartTime = System.Diagnostics.Stopwatch.GetTimestamp();
		PrimaryThreadProfiler.Push(CProfiler.EID.I_BEGIN_PRIMARY);
		
		PrimaryThreadProfiler.Push(CProfiler.EID.I_INPUT);
		_inputState.Update();
		PrimaryThreadProfiler.Pop();

		if (_gameStartParams != null && _inputState.GetCommand("reload").mPressed)
		{
			TerminateGameSession();
			UIManager.HideErrorMesssage();
			StartGameSession(_gameStartParams);
		}

		CameraManager.Update(_inputState);

		// TODO: Ideally network will have its own thread, so it won't have an update pump here.
		PrimaryThreadProfiler.Push(CProfiler.EID.I_NET);
		Net.Update();
		PrimaryThreadProfiler.Pop();

		Console.Update(_inputState);

		if (_assetToolkit != null)
			_assetToolkit.Update(_inputState);

		if (_gameSession != null)
		{
			PrimaryThreadProfiler.Push(CProfiler.EID.I_GAME_SESSION);
			_gameSession.Update(_inputState);
			PrimaryThreadProfiler.Pop();
		}

		UIManager.Update();

		Steam.Update();

		PrimaryThreadProfiler.Pop();

		ProfilerManager.AddProfiledData(PrimaryThreadProfiler, -1);
		PrimaryThreadProfiler.Clear();
		
		//long time = System.Diagnostics.Stopwatch.GetTimestamp();
		//Debug.Log(time + " " + System.Diagnostics.Stopwatch.Frequency);
	}

	/// <summary>
	/// Render debug GUI.
	/// </summary>
	public void OnGUI()
	{
		GUI.skin.label.normal.textColor = Color.white;
		GUI.skin.font = UIResources.DebugFont;

		if (_gameSession != null)
			_gameSession.OnGUI();

		if (VarShowProfiler.mValue)
			ProfilerManager.OnGUI();
	}

	/// <summary>
	/// Application Exit.
	/// </summary>
	public void Destroy()
	{
		// TODO: Need an instant destroy that only needs to cleanup sim thread. No point in waiting on typical game session destruction?
		if (_gameSession != null)
			_gameSession.Destroy();

		Steam.Destroy();
	}
}
