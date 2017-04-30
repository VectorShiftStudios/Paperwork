using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;

public static class CUtility
{
	public static Color COLOR_PROGRESS_GOOD = new Color(130.0f / 255.0f, 212.0f / 255.0f, 0.0f, 1.0f);
	public static Color COLOR_PROGRESS_BAD = new Color(221.0f / 255.0f, 0.0f, 0.0f, 1.0f);

	/// <summary>
	/// Mesh direction helper 
	/// </summary>
	public static Quaternion[] FacingTable = 
	{
		Quaternion.LookRotation(new Vector3(0,0,-1)),		
		Quaternion.LookRotation(new Vector3(-1,0,0)),
		Quaternion.identity,		
		Quaternion.LookRotation(new Vector3(1,0,0)),
	};

	/// <summary>
	/// Get a formatted time string from number of ticks.
	/// </summary>
	public static string TimeStringFromTicks(int Ticks)
	{
		int seconds = Ticks / 20;
		int minutes = seconds / 60;

		return string.Format("{0}:{1:00}", minutes, seconds % 60);			
	}

	/// <summary>
	/// Turns all materials of an object to the ghost material.
	/// </summary>
	public static void GhostObjectMaterials(GameObject Gob)
	{
		MeshRenderer[] renderers = Gob.GetComponentsInChildren<MeshRenderer>();

		for (int i = 0; i < renderers.Length; ++i)
			renderers[i].material = CGame.WorldResources.ItemGhostMat;
	}

	/// <summary>
	/// Destroys all children of a game object.
	/// </summary>
	public static void DestroyChildren(GameObject Gob)
	{
		Transform transform = Gob.transform;

		for (int i = 0; i < transform.childCount; ++i)
		{
			GameObject.Destroy(transform.GetChild(i).gameObject);
		}
	}

	/// <summary>
	/// Sets the layer Id of an entire game object hierarchy.
	/// </summary>
	public static void SetLayerRecursively(Transform Gob, int Layer)
	{
		Gob.gameObject.layer = Layer;
		for (int i = 0; i < Gob.childCount; ++i)
		{
			SetLayerRecursively(Gob.GetChild(i), Layer);
		}
	}

	public static void WriteVec3(BinaryWriter W, Vector3 Vec3)
	{
		W.Write(Vec3.x);
		W.Write(Vec3.y);
		W.Write(Vec3.z);
	}

	public static Vector3 ReadVec3(BinaryReader R)
	{
		Vector3 v;

		v.x = R.ReadSingle();
		v.y = R.ReadSingle();
		v.z = R.ReadSingle();

		return v;
	}

	public static void WriteColor(BinaryWriter W, Color C)
	{
		W.Write(C.r);
		W.Write(C.g);
		W.Write(C.b);
		W.Write(C.a);
	}

	public static Color ReadColor(BinaryReader R)
	{
		Color c;

		c.r = R.ReadSingle();
		c.g = R.ReadSingle();
		c.b = R.ReadSingle();
		c.a = R.ReadSingle();

		return c;
	}

	public static string[] FIRST_NAMES = {
		"John",
		"Paul",
		"Rob",
		"Mike",
		"Tim",
		"Jeff"
	};

	public static string[] LAST_NAMES = {
		"Johnson",
		"Paulson",
		"Robson"
	};
	
	/// <summary>
	/// Generates a random name from the firstname/lastname lists.
	/// </summary>
	public static string GenerateRandomName(CRandomStream Rand)
	{
		string name = FIRST_NAMES[(int)(Rand.GetNextFloat() * FIRST_NAMES.Length)];
		name = name + " " + LAST_NAMES[(int)(Rand.GetNextFloat() * LAST_NAMES.Length)];

		return name;
	}

	/// <summary>
	/// Checks if a directory exists, and makes it if not.
	/// </summary>
	public static void MakeDirectory(string Path)
	{
		if (!Directory.Exists(Path))
			Directory.CreateDirectory(Path);
	}

	public static bool CheckBit(int Value, int Bit)
	{
		return ((Value & (1 << Bit)) != 0);
	}

	public static int SetBit(int Value, int Bit, bool Set)
	{
		//Debug.Log("Set Bit: " + Value +

		if (Set)
			return Value | (1 << Bit);
		else
			return Value & ~(1 << Bit);
	}

	public static int SetFlag(int Value, int Flag, bool Set)
	{
		if (Set)
			return Value | Flag;
		else
			return Value & ~Flag;
	}

	public static bool CheckFlag(int Value, int Flag)
	{
		return (Value & Flag) != 0;
	}
}

public static class CVector
{
	/// <summary>
	/// Convert a Vector3 to a Vector2(X, Z)
	/// </summary>
	public static Vector2 ToWorldVec2(this Vector3 V)
	{
		return new Vector2(V.x, V.z);
	}

	/// <summary>
	/// Convert a Vector2 to a Vector3(X, 0, Y)
	/// </summary>
	public static Vector3 ToWorldVec3(this Vector2 V)
	{
		return new Vector3(V.x, 0.0f, V.y);
	}

	/// <summary>
	/// Convert a Vector2 to an IntVec2(X, Y)
	/// </summary>
	public static IntVec2 ToIntVec2(this Vector2 V)
	{
		return new IntVec2((int)V.x, (int)V.y);
	}
}

public static class CRect
{
	public static Rect ExpandBy(this Rect R, int N)
	{
		R.xMin -= N;
		R.yMin -= N;
		R.xMax += N;
		R.yMax += N;

		return R;
	}

	public static Rect ClipToSize(this Rect R, int Width, int Height)
	{
		if (R.xMin < 0)
			R.xMin = 0;
		if (R.xMax > Width)
			R.xMax = Width;
		if (R.yMin < 0)
			R.yMin = 0;
		if (R.yMax > Height)
			R.yMax = Height;

		return R;
	}

	public static bool IsTileInside(this Rect R, IntVec2 P)
	{
		return P.X >= (int)R.xMin && P.X < (int)R.xMax &&
			P.Y >= (int)R.yMin && P.Y < (int)R.yMax;
	}

	/// <summary>
	/// Returns space intersected by both rects.
	/// </summary>
	public static Rect GetIntersection(Rect A, Rect B)
	{
		float xmin = Mathf.Max(A.xMin, B.xMin);
		float ymin = Mathf.Max(A.yMin, B.yMin);
		float xmax = Mathf.Min(A.xMax, B.xMax);
		float ymax = Mathf.Min(A.yMax, B.yMax);

		return Rect.MinMaxRect(xmin, ymin, xmax, ymax);
	}
}

public static class HashSet
{
	public static HashSet<T> MakeWithInitialCapacity<T>(int Capacity)
	{
		HashSet<T> set = new HashSet<T>(new T[Capacity]);
		set.Clear();

		return set;
	}
}