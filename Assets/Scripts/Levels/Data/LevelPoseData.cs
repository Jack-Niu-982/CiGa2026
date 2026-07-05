using System;
using UnityEngine;

[Serializable]
public struct LevelPoseData
{
    public float x;
    public float y;
    public float angle;

    public LevelPoseData(float x, float y, float angle)
    {
        this.x = x;
        this.y = y;
        this.angle = angle;
    }

    public Vector2 Position => new Vector2(x, y);

    public Quaternion Rotation =>
        Quaternion.Euler(0f, 0f, angle);
}
