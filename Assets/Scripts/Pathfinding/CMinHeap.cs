using System;
using System.Collections.Generic;
using UnityEngine;

public class CNRSearchNode
{
	public Vector2 mPosition;
	public CNavRect mRect;
	public float mCost;
	public float mPathCost;
	public CNRSearchNode mNextMove;
	public CNRSearchNode mNextListElem;

	public CNRSearchNode()
	{

	}

	public CNRSearchNode(CNavRect Rect, Vector2 position, float cost, float pathCost, CNRSearchNode next)
	{
		mRect = Rect;
		mPosition = position;
		mCost = cost;
		mPathCost = pathCost;
		mNextMove = next;
	}
}

public class CNRMinHeap
{
	public CNRSearchNode listHead;

	public bool HasNext()
	{
		return listHead != null;
	}

	public void Add(CNRSearchNode item)
	{
		if (listHead == null)
		{
			listHead = item;
		}
		else if (listHead.mNextMove == null && item.mCost <= listHead.mCost)
		{
			item.mNextListElem = listHead;
			listHead = item;
		}
		else
		{
			CNRSearchNode ptr = listHead;

			while (ptr.mNextListElem != null && ptr.mNextListElem.mCost < item.mCost)
				ptr = ptr.mNextListElem;

			item.mNextListElem = ptr.mNextListElem;
			ptr.mNextListElem = item;
		}
	}

	public CNRSearchNode ExtractFirst()
	{
		CNRSearchNode result = listHead;
		listHead = listHead.mNextListElem;
		return result;
	}
}