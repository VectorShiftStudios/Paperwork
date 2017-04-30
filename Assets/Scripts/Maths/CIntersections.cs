using UnityEngine;
using System;
using System.Collections.Generic;

public class CIntersections
{
	public static bool RayVsTrapezium(Ray R, Vector3 C1, Vector3 C2, Vector3 C3, Vector3 C4, out Vector3 HitPoint)
	{
		// Get plane of trap
		// Hit of Ray vs plane of trap
		// Check if hit is behind each edge

		HitPoint = Vector3.zero;
		return false;
	}

	public static float PointVsLine(Vector3 P, Vector3 LS, Vector3 LE, out Vector3 ProjPoint)
	{
		Vector3 axis = (LE - LS).normalized;
		float lineLen = (LE - LS).magnitude;
		float t = Vector3.Dot(axis, P - LS);
		Vector3 lineProj;

		if (t < 0.0f)
			lineProj = LS;
		else if (t > lineLen)
			lineProj = LE;
		else
			lineProj = LS + axis * t;

		ProjPoint = lineProj;
		return (P - lineProj).magnitude;
	}

	public static bool LineVsLine(Vector2 L1A, Vector2 L1B, Vector2 L2A, Vector2 L2B, out float T, out Vector2 Hit)
	{
		float p0_x = L1A.x;
		float p0_y = L1A.y;
		float p1_x = L1B.x;
		float p1_y = L1B.y;
		float p2_x = L2A.x;
		float p2_y = L2A.y;
		float p3_x = L2B.x;
		float p3_y = L2B.y;

		float s1_x, s1_y, s2_x, s2_y;
		s1_x = p1_x - p0_x; s1_y = p1_y - p0_y;
		s2_x = p3_x - p2_x; s2_y = p3_y - p2_y;

		// TODO: Potential divide by 0 here.
		float s, t;
		s = (-s1_y * (p0_x - p2_x) + s1_x * (p0_y - p2_y)) / (-s2_x * s1_y + s1_x * s2_y);
		t = (s2_x * (p0_y - p2_y) - s2_y * (p0_x - p2_x)) / (-s2_x * s1_y + s1_x * s2_y);

		if (s >= 0 && s <= 1 && t >= 0 && t <= 1)
		{
			T = t;
			Hit.x = p0_x + (t * s1_x);
			Hit.y = p0_y + (t * s1_y);
			return true;
		}

		T = 0.0f;
		Hit = Vector2.zero;
		return false;
	}

	/// <summary>
	/// Is a square touching a cell?
	/// </summary>
	public static bool IsSquareInCell(Vector2 Origin, int X, int Y, float Width)
	{
		if ((Origin.x + Width >= (X + 0)) &&
			(Origin.x - Width <= (X + 1)) &&
			(Origin.y + Width >= (Y + 0)) &&
			(Origin.y - Width <= (Y + 1)))
			return true;

		return false;
	}

	/// <summary>
	/// TODO: Used for LOS segment gathering. Probably needs to be a special case, and have a more generic version.
	/// </summary>
	public static bool LineVsCircle(Vector2 P1, Vector2 P2, Vector2 O, float Radius, out Vector2 HitA, out Vector2 HitB, out bool OpenA, out bool OpenB)
	{
		OpenA = true;
		OpenB = true;

		Vector2 localP1 = P1 - O;
		Vector2 localP2 = P2 - O;
		Vector2 p2mp1 = localP2 - localP1;

		float a = p2mp1.x * p2mp1.x + p2mp1.y * p2mp1.y;
		float b = 2 * (p2mp1.x * localP1.x + p2mp1.y * localP1.y);
		float c = localP1.x * localP1.x + localP1.y * localP1.y - Radius * Radius;
		float d = b * b - (4 * a * c);

		if (d < 0 || d == 0)
		{
			// Ignore tangential intersection for now (d == 0).
			HitA = Vector2.zero;
			HitB = Vector2.zero;
			return false;
		}

		float sqrD = Mathf.Sqrt(d);

		float u1 = (-b + sqrD) / (2 * a);
		float u2 = (-b - sqrD) / (2 * a);

		if ((u1 > 1.0f && u2 > 1.0f) || (u1 < 0.0f && u2 < 0.0f))
		{
			HitA = Vector2.zero;
			HitB = Vector2.zero;
			return false;
		}

		if (u1 < 0.0f)
		{
			u1 = 0.0f;
			OpenA = false;
		}
		else if (u1 > 1.0f)
		{
			u1 = 1.0f;
			OpenA = false;
		}

		if (u2 < 0.0f)
		{
			u2 = 0.0f;
			OpenB = false;
		}
		else if (u2 > 1.0f)
		{
			u2 = 1.0f;
			OpenB = false;
		}

		HitA = P1 + (u1 * p2mp1);
		HitB = P1 + (u2 * p2mp1);
		
		
		return true;
	}

	public static bool PointInTriangle(Vector2 P, Vector2 T1, Vector2 T2, Vector2 T3)
	{
		Vector2 A = T1;
		Vector2 B = T2;
		Vector2 C = T3;

		Vector2 v0 = C - A;
		Vector2 v1 = B - A;
		Vector2 v2 = P - A;

		float dot00 = Vector2.Dot(v0, v0);
		float dot01 = Vector2.Dot(v0, v1);
		float dot02 = Vector2.Dot(v0, v2);
		float dot11 = Vector2.Dot(v1, v1);
		float dot12 = Vector2.Dot(v1, v2);

		// TODO: Check for zero.
		float invDenom = 1 / (dot00 * dot11 - dot01 * dot01);
		float u = (dot11 * dot02 - dot01 * dot12) * invDenom;
		float v = (dot00 * dot12 - dot01 * dot02) * invDenom;

		if ((u >= 0) && (v >= 0) && (u + v < 1))
			return true;

		return false;
	}
}
