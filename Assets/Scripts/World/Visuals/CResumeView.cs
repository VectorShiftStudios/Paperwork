using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CResumeView : CStateView
{
	private GameObject _Gob;

	public int mID;
	public int mOwner;

	public SUnitBasicStats mStats;

	// UI Stuff
	public CEmployeeEntry mUIEmployeeEntry;

	public void CopyInitialState(CResume Resume)
	{
		mID = Resume.mID;
		mOwner = Resume.mOwner;
		mStats = Resume.mStats;
	}

	public void CopyState(CResume Resume)
	{

	}

	protected override void _New(CUserSession UserSession)
	{
		UserSession.OnResumeAdded(this);
	}

	protected override void _Destroy(CUserSession UserSession)
	{
		UserSession.OnResumeRemoved(this);
	}
}
