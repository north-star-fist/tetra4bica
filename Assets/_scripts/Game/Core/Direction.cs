using System;
using UnityEngine;

public static class Direction {

    public static readonly Vector2Int[] FOUR_DIRECTIONS = new Vector2Int[] {
        Vector2Int.left,
        Vector2Int.up,
        Vector2Int.right,
        Vector2Int.down
    };

    public static readonly Vector2Int LEFT = Vector2Int.left;
    public static readonly Vector2Int RIGHT = Vector2Int.right;
    public static readonly Vector2Int UP = Vector2Int.up;
    public static readonly Vector2Int DOWN = Vector2Int.down;

    public static Vector2Int RotateBy90(Vector2Int dir, bool clockWise) {
        Vector2Int rotated = (dir == RIGHT)
                    ? clockWise ? DOWN : UP
                    : (dir == DOWN)
                    ? clockWise ? LEFT : RIGHT
                    : (dir == LEFT)
                    ? clockWise ? UP : DOWN
                    : (dir == UP)
                    ? clockWise ? RIGHT : LEFT :
                    dir;
        if (rotated == dir) {
            throw new ArgumentException($"The direction {dir.ToString()} is not normalized or is not orthagonal!");
        }
        return rotated;
    }
}
