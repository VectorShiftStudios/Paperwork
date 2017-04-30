using System;

public static class IntVec2AdjacencyTables
{
    public const float CARDINAL_MOVE_COST = 1.0f;
    public const float DIAGONAL_MOVE_COST = 1.41f;
    public const float DIAGONAL_MOVE_COST_SQR = 1.41f * 1.41f;

    public static IntVec2[] AdjacentTiles;
	public static float[] AdjacentTileMoveCosts;

    static IntVec2AdjacencyTables()
    {
        AdjacentTiles = new IntVec2[8];
		AdjacentTiles[0] = new IntVec2(0, -1);
		AdjacentTiles[1] = new IntVec2(1, -1);
		AdjacentTiles[2] = new IntVec2(1, 0);
		AdjacentTiles[3] = new IntVec2(1, 1);
		AdjacentTiles[4] = new IntVec2(0, 1);
		AdjacentTiles[5] = new IntVec2(-1, 1);
		AdjacentTiles[6] = new IntVec2(-1, 0);
		AdjacentTiles[7] = new IntVec2(-1, -1);

		AdjacentTileMoveCosts = new float[8];
		AdjacentTileMoveCosts[0] = CARDINAL_MOVE_COST;
		AdjacentTileMoveCosts[1] = DIAGONAL_MOVE_COST;
		AdjacentTileMoveCosts[2] = CARDINAL_MOVE_COST;
		AdjacentTileMoveCosts[3] = DIAGONAL_MOVE_COST;
		AdjacentTileMoveCosts[4] = CARDINAL_MOVE_COST;
		AdjacentTileMoveCosts[5] = DIAGONAL_MOVE_COST;
		AdjacentTileMoveCosts[6] = CARDINAL_MOVE_COST;
		AdjacentTileMoveCosts[7] = DIAGONAL_MOVE_COST;
	}
}