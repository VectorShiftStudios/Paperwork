using System;
using System.Collections.Generic;
using UnityEngine;

public class CNavRectPather
{
	private static int[] _brRects = new int[5000];
	private static int _brTurn = 0;
	
	public static CNRSearchNode FindPath(CNavRectMesh NavMesh, Vector2 Start, Vector2 End, int OccupiedID)
	{
		// TODO: Make a fast path for walking to some really close position.
		// TODO: Check region ID of start and end points.
		// TODO: Clamp/Check positions.

		CNRSearchNode node = _FindReversedPath(NavMesh, End, Start, OccupiedID);

		// Node expansion places a node on each side of a portal. Since portals are convex this guarantees 
		// we can reach all nodes by walking in a straight line.
		if (node != null)
		{
			CNRSearchNode expandNode = node.mNextMove;

			// TODO: Bias nodes on portal closer to next node?

			// Expand each node that is not the start and end
			while (expandNode != null && expandNode.mNextMove != null)
			{
				// Clip to rect
				Vector2 p = expandNode.mPosition;
				Rect r = expandNode.mRect.mRect;
				p.x = Mathf.Clamp(p.x, r.xMin * 0.5f + 0.25f, r.xMax * 0.5f - 0.25f);
				p.y = Mathf.Clamp(p.y, r.yMin * 0.5f + 0.25f, r.yMax * 0.5f - 0.25f);
				expandNode.mPosition = p;

				p = expandNode.mPosition;
				r = expandNode.mNextMove.mRect.mRect;
				p.x = Mathf.Clamp(p.x, r.xMin * 0.5f + 0.25f, r.xMax * 0.5f - 0.25f);
				p.y = Mathf.Clamp(p.y, r.yMin * 0.5f + 0.25f, r.yMax * 0.5f - 0.25f);
				expandNode.mNextMove = new CNRSearchNode(expandNode.mNextMove.mRect, p, 0, 0, expandNode.mNextMove);

				expandNode = expandNode.mNextMove.mNextMove;
			}
		}

		return node;
	}

	public static CNRSearchNode FindDistance(CNavRectMesh NavMesh, Vector2 Start, Vector2 End, int OccupiedID)
	{
		CNRSearchNode node = _FindReversedPath(NavMesh, End, Start, OccupiedID);

		return node;
	}

	/// <summary>
	/// Method that switfly finds the best path from start to end. Doesn't reverse outcome.
	/// </summary>
	private static CNRSearchNode _FindReversedPath(CNavRectMesh NavMesh, Vector2 Start, Vector2 End, int OccupiedID)
	{	
		System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
		sw.Start();

		Start.x = Mathf.Clamp(Start.x, 0, 99);
		Start.y = Mathf.Clamp(Start.y, 0, 99);
		End.x = Mathf.Clamp(End.x, 0, 99);
		End.y = Mathf.Clamp(End.y, 0, 99);

		CNavRect startRect = null;
		CNavRect endRect = null;

		startRect = NavMesh.mRectLookup[(int)(Start.x * 2.0f), (int)(Start.y * 2.0f)];
		endRect = NavMesh.mRectLookup[(int)(End.x * 2.0f), (int)(End.y * 2.0f)];

		if (startRect == null || endRect == null)
			return null;

		int occupiedID = 10000 + OccupiedID;

		if ((startRect.mFlags > 10000 && startRect.mFlags != occupiedID) ||
			(endRect.mFlags > 10000 && endRect.mFlags != occupiedID))
			return null;

		// TODO: Ideally we only clamp if we must.
		Start.x = Mathf.Clamp(Start.x, startRect.mRect.xMin * 0.5f + 0.25f, startRect.mRect.xMax * 0.5f - 0.25f);
		Start.y = Mathf.Clamp(Start.y, startRect.mRect.yMin * 0.5f + 0.25f, startRect.mRect.yMax * 0.5f - 0.25f);
		End.x = Mathf.Clamp(End.x, endRect.mRect.xMin * 0.5f + 0.25f, endRect.mRect.xMax * 0.5f - 0.25f);
		End.y = Mathf.Clamp(End.y, endRect.mRect.yMin * 0.5f + 0.25f, endRect.mRect.yMax * 0.5f - 0.25f);
		
		double rectFindTime = sw.Elapsed.TotalMilliseconds;

		CNRSearchNode startNode = new CNRSearchNode(startRect, Start, 0, 0, null);
		CNRMinHeap openList = new CNRMinHeap();
		openList.Add(startNode);
		_brRects[startRect.mIndex] = ++_brTurn;

		double allocateTime = sw.Elapsed.TotalMilliseconds - rectFindTime;

		CNRSearchNode current = null;
		int steps = 0;
		while (openList.HasNext())
		{
			++steps;
			current = openList.ExtractFirst();
			
			if (current.mRect == endRect)
			{
				sw.Stop();
				//Debug.Log("Steps: " + steps + " Allocs: " + searchNodeCount + " T: " + sw.Elapsed.TotalMilliseconds + "ms F: " + rectFindTime + "ms A: " + allocateTime + "ms");
				float moveCost = (End - current.mPosition).SqrMagnitude();
				return new CNRSearchNode(endRect, End, 0, moveCost + current.mPathCost, current);
			}

			for (int i = 0; i < current.mRect.mPortals.Count; ++i)
			{
				CNavRectPortal p = current.mRect.mPortals[i];
				// TODO: Some way to ditch this check?
				CNavRect r = p.mRectA != current.mRect ? p.mRectA : p.mRectB;

				if (_brRects[r.mIndex] == _brTurn)
					continue;

				if (r.mFlags > 10000 && r.mFlags != occupiedID)
					continue;

				_brRects[r.mIndex] = _brTurn;

				float moveCost = (p.mCentre - current.mPosition).SqrMagnitude();
				float costToEnd = (End - p.mCentre).SqrMagnitude();

				float currentPathCost = current.mPathCost + moveCost;
				float entirePathCost = currentPathCost + costToEnd;

				CNRSearchNode node = new CNRSearchNode(r, p.mCentre, entirePathCost, currentPathCost, current);

				openList.Add(node);
			}
		}

		sw.Stop();
		//Debug.Log("(FAILED) Steps: " + steps + " Time: " + sw.Elapsed.TotalMilliseconds + "ms");

		return null;
	}

	/// <summary>
	/// Can you reach a node by walking 
	/// </summary>
	public static bool IsNodeReachable(CMap Map, int PlayerID, Vector2 Position, CNRSearchNode Node)
	{
		// Get two rays that are the width of travel.
		// Create a path 0.4 units wide.
		Vector2 A = Position;
		Vector2 B = Node.mPosition;
		Vector2 dir = (B - A).normalized;
		float halfWidth = 0.24f;
		Vector2 l1A = new Vector2(-dir.y * halfWidth + A.x, dir.x * halfWidth + A.y);
		Vector2 l1B = new Vector2(-dir.y * halfWidth + B.x, dir.x * halfWidth + B.y);
		Vector2 l2A = new Vector2(dir.y * halfWidth + A.x, -dir.x * halfWidth + A.y);
		Vector2 l2B = new Vector2(dir.y * halfWidth + B.x, -dir.x * halfWidth + B.y);

		// TODO: Trace nodes uses the mobility set which is not needed for anything else.
		return (Map.TraceNodes(PlayerID, l1A, l1B) && Map.TraceNodes(PlayerID, l2A, l2B));
	}
	
	public static void DebugDraw(CNRSearchNode Node)
	{
		CNRSearchNode startNode = Node;

		if (Node == null)
			return;

		while (Node != null)
		{
			Rect r = Node.mRect.mRect;
			Rect rect = new Rect(r.x * 0.5f, r.y * 0.5f, r.width * 0.5f, r.height * 0.5f);

			CDebug.DrawYRectQuad(Node.mPosition.ToWorldVec3(), 0.5f, 0.5f, Color.green, false);

			if (Node != startNode)
			{
				Vector3 rc = rect.center.ToWorldVec3();// + new Vector3(0, 0.01f, 0);
				Color c = Color.magenta;
				CDebug.DrawYRectQuad(rc, rect.width, rect.height, new Color(c.r, c.g, c.b, 0.3f), false);
			}

			if (Node.mNextMove != null)
			{
				CDebug.DrawLine(Node.mPosition.ToWorldVec3(), Node.mNextMove.mPosition.ToWorldVec3(), Color.red, false);
			}

			Node = Node.mNextMove;
		}
	}
}
