using System;
using UnityEngine;

public struct IntVec2
{
    public static IntVec2 Up = new IntVec2(0, 1);
    public static IntVec2 Right = new IntVec2(1, 0);
    public static IntVec2 Down = new IntVec2(0, -1);
    public static IntVec2 Left = new IntVec2(-1, 0);

	public static IntVec2 UpRight = new IntVec2(1, 1);
	public static IntVec2 UpLeft = new IntVec2(-1, 1);
	public static IntVec2 DownRight = new IntVec2(1, -1);
	public static IntVec2 DownLeft = new IntVec2(-1, -1);

	public int X;
    public int Y;

    public IntVec2(int X, int Y)
    {
        this.X = X;
        this.Y = Y;
    }

    public IntVec2(Vector2 Vec2)
    {
        X = (int)Vec2.x;
        Y = (int)Vec2.y;
    }

    public int LengthSqr()
    {
        return X * X + Y * Y;
    }

    public IntVec2 Add(int X, int Y)
    {
        IntVec2 result = this;

        result.X += X;
        result.Y += Y;

        return result;
    }

	public Vector2 ToVector2()
	{
		return new Vector2(X, Y);
	}

	public Vector2 ToVector2(float ScaleFactor)
	{
		return new Vector2(X * ScaleFactor, Y * ScaleFactor);
	}

	public Vector3 ToVector3()
	{
		return new Vector3(X, 0, Y);
	}

	public Vector3 ToVector3(float ScaleFactor)
	{
		return new Vector3(X * ScaleFactor, 0, Y * ScaleFactor);
	}

	public static int ManhattanDistance(IntVec2 A, IntVec2 B)
    {
        int dX = Math.Abs(A.X - B.X);
        int dY = Math.Abs(A.Y - B.Y);

        return dX + dY;
    }

	public static float DiagonalDistance(IntVec2 A, IntVec2 B)
	{
		float dX = Math.Abs(A.X - B.X);
		float dY = Math.Abs(A.Y - B.Y);

		return (dX + dY) + (IntVec2AdjacencyTables.DIAGONAL_MOVE_COST - 2.0f) * (dX < dY ? dX : dY);
	}

    public static int EuclideanDistanceSqr(IntVec2 A, IntVec2 B)
    {
        IntVec2 d = A - B;
        return d.X * d.X + d.Y * d.Y;
    }

    public static float EuclideanDistance(IntVec2 A, IntVec2 B)
    {
        IntVec2 d = A - B;
        return (float)Math.Sqrt(d.X * d.X + d.Y * d.Y);
    }

    public static IntVec2 operator +(IntVec2 LHS, IntVec2 RHS)
    {
        return new IntVec2(LHS.X + RHS.X, LHS.Y + RHS.Y);
    }

    public static IntVec2 operator -(IntVec2 LHS, IntVec2 RHS)
    {
        return new IntVec2(LHS.X - RHS.X, LHS.Y - RHS.Y);
    }

	public static IntVec2 operator *(IntVec2 LHS, int RHS)
	{
		return new IntVec2(LHS.X * RHS, LHS.Y * RHS);
	}

    public static bool operator ==(IntVec2 LHS, IntVec2 RHS)
    {
        return LHS.X == RHS.X && LHS.Y == RHS.Y;
    }

    public static bool operator !=(IntVec2 LHS, IntVec2 RHS)
    {
        return !(LHS == RHS);
    }

    public override bool Equals(object obj)
    {
        if (obj == null)
            return false;

        IntVec2 iv2 = (IntVec2)obj;
        return this == iv2;
    }

    public override int GetHashCode()
    {
		/*
            Hash data into an unsigned 32-bit integer.

            #  [Bits]      |   Data
            ===============================
            16 [31 - 16]   |   X Coordinate
            16 [15 -  0]   |   Y Coordinate
        */

		return (X << 16) | Y;
    }

	public override string ToString()
	{
		return string.Format("({0}, {1})", X, Y);
	}
}