using System;
using System.Collections.Generic;
using UnityEngine;

public class CItemRest : CItem
{
	public override bool IsCouch()
	{
		return true;
	}

	public override void Init(CWorld World)
	{
		base.Init(World);
		mType = EType.ITEM_REST;
	}

	public bool IsFree()
	{
		return true;
	}
}