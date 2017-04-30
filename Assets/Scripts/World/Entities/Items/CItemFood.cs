using System;
using System.Collections.Generic;
using UnityEngine;

public class CItemFood : CItem
{
	public float mFoodDispensed;

	public override bool IsFood()
	{
		return true;
	}

	public override void Init(CWorld World)
	{
		base.Init(World);
		mType = EType.ITEM_FOOD;
	}
}
