using System;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;
using System.Text;

public class CSteam
{
	public const int APP_ID = 485910;

	[DllImport("PaperworkNative")]
	private static extern float GetSimpleValue(float Value);

	[DllImport("PaperworkNative")]
	private static extern bool pwSteamInit();

	[DllImport("PaperworkNative")]
	private static extern bool pwSteamRestartIfNecessary(int AppID);

	[DllImport("PaperworkNative")]
	private static extern void pwSteamUpdate();

	[DllImport("PaperworkNative")]
	private static extern void pwSteamShutdown();

	[DllImport("PaperworkNative")]
	private static extern IntPtr pwSteamGetName();

	[DllImport("PaperworkNative")]
	private static extern UInt64 pwSteamGetID();

	/// <summary>
	/// Copy native c-string into a managed buffer.
	/// TODO: Move to an interop utility class.
	/// </summary>
	public static string PtrToStringUTF8(IntPtr nativeUtf8)
	{
		if (nativeUtf8 == IntPtr.Zero)
		{
			return string.Empty;
		}

		int len = 0;

		while (Marshal.ReadByte(nativeUtf8, len) != 0)
		{
			++len;
		}

		if (len == 0)
		{
			return string.Empty;
		}

		byte[] buffer = new byte[len];
		Marshal.Copy(nativeUtf8, buffer, 0, buffer.Length);
		return Encoding.UTF8.GetString(buffer);
	}

	public static bool SteamRestartIfNecessary()
	{
		if (pwSteamRestartIfNecessary(APP_ID))
		{
			Debug.Log("STEAM RESTART");
			Application.Quit();
			return true;
		}

		return false;
	}

	public bool mInitialized;
	public string mUserName;
	public UInt64 mSteamID;

	/// <summary>
	/// Standard constructor.
	/// </summary>
	public CSteam()
	{
		Debug.Log("Initializing Steam");
		mInitialized = pwSteamInit();

		if (mInitialized)
		{
			Debug.Log("Steam Initialize Success");
			mUserName = PtrToStringUTF8(pwSteamGetName());
			mSteamID = pwSteamGetID();
			Debug.Log("Steam User Name: " + mUserName + "(" + mSteamID + ")");
		}
		else
		{
			Debug.Log("Steam Initialize Failed");
			Application.Quit();
		}
	}

	/// <summary>
	/// Called every frame.
	/// </summary>
	public void Update()
	{
		pwSteamUpdate();
	}

	/// <summary>
	/// Called when game is terminated.
	/// </summary>
	public void Destroy()
	{
		// Calling shutdown from editor seems to cause crash when game is relaunched in same editor instance? Maybe because the owning process isn't destroyed?
		#if !UNITY_EDITOR
			pwSteamShutdown();
		#endif
	}
}
