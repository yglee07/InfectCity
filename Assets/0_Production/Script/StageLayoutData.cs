using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class StageLayoutData
{
    public int stageId;
    public float unitSize = 1f;
    public Vec2 origin;
    public List<GroupData> groups;
}

[Serializable]
public class GroupData
{
    public string type;       // "Citizen", "GunCitizen", "Purple"
    public string pattern;    // "Rect", "Line", "Single"

    public Vec2 start;
    public Vec2 pos;

    public int rows;
    public int cols;
    public int count;

    public string dir;        // "X" or "Z"
    public float spacing = 1f;
}

[Serializable]
public class Vec2
{
    public float x;
    public float z;

    public Vector3 ToVector3(float y = 0f)
    {
        return new Vector3(x, y, z);
    }
}
