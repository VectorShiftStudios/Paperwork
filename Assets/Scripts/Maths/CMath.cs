using UnityEngine;
using System;
using System.Collections.Generic;

public class CMath
{
	/// <summary>
	/// Calculate speed to acheive required distance on an arc.
	/// </summary>
	public static float GetSpeedForArc(float Distance, float Angle, float Gravity)
	{
		// Gravity is m/s^2
		// Equation of motion: x = i + vt - 0.5Gt^2
		// y = tan(angle) * distance - 0.5Gt^2

		// 0 = tan(45) * Distance - 0.5 * Gravity * t^2

		float sqrt2 = Mathf.Sqrt(2);

		float t = (sqrt2 * Mathf.Sqrt(Distance) * Mathf.Sqrt(Mathf.Tan(Mathf.Deg2Rad * Angle))) / Mathf.Sqrt(Mathf.Abs(Gravity));
		float speed = -((Gravity * t * t) / (2 * t)) * sqrt2;

		return speed;
	}

	/// <summary>
	/// Calculates required missle velocity to hit target.
	/// Must be on flat plane.
	/// Gravity in m/s^2.
	/// Angle in degrees.
	/// Speed in m/s.
	/// </summary>
	public static Vector3 GetMissileVelocity(Vector3 Start, Vector3 Target, float Angle, float Gravity)
	{
		Vector3 disp = Target - Start;
		float distance = disp.magnitude;
		Vector3 dir = disp.normalized;
		
		Vector3 aimDir = disp;
		aimDir.y = Mathf.Tan(Mathf.Deg2Rad * Angle) * distance;
		aimDir.Normalize();

		float speed = CMath.GetSpeedForArc(distance, Angle, Gravity);

		return aimDir * speed;
	}

	/// <summary>
	/// Map a value from one range to another.
	/// Does NOT clamp value to target range.
	/// </summary>
	public static float MapRange(float Value, float SrcStart, float SrcEnd, float DestStart, float DestEnd)
	{
		return (Value - SrcStart) / (SrcEnd - SrcStart) * (DestEnd - DestStart) + DestStart;
	}

	/// <summary>
	/// Map a value from one range to another.
	/// Clamps value to the target range.
	/// </summary>
	public static float MapRangeClamp(float Value, float SrcStart, float SrcEnd, float DestStart, float DestEnd)
	{
		return Mathf.Clamp((Value - SrcStart) / (SrcEnd - SrcStart) * (DestEnd - DestStart) + DestStart, DestStart, DestEnd);
	}
}
