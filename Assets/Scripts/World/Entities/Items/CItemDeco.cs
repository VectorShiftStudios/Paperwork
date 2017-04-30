using System;
using System.Collections.Generic;
using UnityEngine;

public class CItemDeco : CItem
{
	public override void Init(CWorld World)
	{
		base.Init(World);
		mType = EType.ITEM_DECO;
	}
}