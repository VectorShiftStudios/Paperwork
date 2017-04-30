using System;
using System.Collections.Generic;
using UnityEngine;

public class CItemSafe : CItem
{
	public int mValue;

	public override bool IsSafe()
	{
		return true;
	}

	public override void Init(CWorld World)
	{
		base.Init(World);
		mType = EType.ITEM_SAFE;

		mValue = 10000;
	}
	
	public int GivePapers(int Papers)
	{
		mValue += Papers;

		return Papers;
	}
}