using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = System.Random;

public class ExtraUtils
{
    public static string RemoveSpace(string input)
    {
        string output = string.Copy(input);
        while (output.Length > 0)
        {
            if (output[0] == ' ')
            {
                output = output.Remove(0, 1);
            }
            else
            {
                break;
            }
        }

        while (output.Length > 0)
        {
            if (output[^1] == ' ')
            {
                output = output.Remove(output.Length - 1, 1);
            }
            else
            {
                break;
            }
        }

        return output;
    }

    public static float Cosine3Sides(float a, float b, float c)
    {
        return Mathf.Acos((a * a + b * b - c * c) / (2 * a * b)) * 180.0f / Mathf.PI;
    }

    public static float RoundToFloat(float num, float roundTo)
    {
        return Mathf.Round(num / roundTo) * roundTo;
    }

    public static int RoundFloatToInt(float num, int roundTo)
    {
        return Mathf.RoundToInt(num / roundTo) * roundTo;
    }

    public static Vector3 RoundToVec3(Vector3 pos, int roundTo)
    {
        return new(RoundToFloat(pos.x, roundTo), RoundToFloat(pos.y, roundTo), RoundToFloat(pos.z, roundTo));
    }

    public static Vector3Int RoundToVec3Int(Vector3 pos, int roundTo)
    {
        return new(RoundFloatToInt(pos.x, roundTo), RoundFloatToInt(pos.y, roundTo), RoundFloatToInt(pos.z, roundTo));
    }

    public static List<T> Shuffle<T>(List<T> toShuffle, int iterations = 10)
    {
        return Shuffle(toShuffle, new(), iterations);
    }

    public static List<T> Shuffle<T>(List<T> toShuffle, Random rnd, int iterations = 10)
    {
        toShuffle = new(toShuffle);

        for (int i = 0; i < iterations; i++)
        {
            for (int j = 0; j < toShuffle.Count; j++)
            {
                int rndSwitch = rnd.Next(toShuffle.Count);

                (toShuffle[rndSwitch], toShuffle[j]) = (toShuffle[j], toShuffle[rndSwitch]);
            }
        }

        return toShuffle;
    }

    public static Queue<T> Shuffle<T>(Queue<T> toShuffle, int iterations = 10) { return new(Shuffle(new List<T>(toShuffle), iterations)); }

    public static Vector2Int RoundVec2(Vector2 pos) { return new(Mathf.RoundToInt(pos.x), Mathf.RoundToInt(pos.y)); }

    public static Vector3 Abs(Vector3 pos) { return new(Mathf.Abs(pos.x), Mathf.Abs(pos.y), Mathf.Abs(pos.z)); }

    public static Vector2 Abs(Vector2 pos) { return new(Mathf.Abs(pos.x), Mathf.Abs(pos.y)); }
}

[Serializable]
public struct Bounds2D
{
    public Vector2 Center;
    public Vector2 Size;
    public float Rotation;
    public Vector2 HalfExtents { get { return Size / 2; } }

    public Bounds2D(Vector2 center, Vector2 size, float rotation)
    {
        Center = center;
        Size = size;
        Rotation = rotation;
    }

    public bool Contains(Vector2 point)
    {
        Vector2 localPoint = Quaternion.Euler(0, 0, -Rotation) * (point - Center);
        return Mathf.Abs(localPoint.x) < Size.x / 2 && Mathf.Abs(localPoint.y) < Size.y / 2;
    }

    public bool Intersects(Bounds2D bounds)
    {
        Vector2 direction = Center - bounds.Center;
        Vector2 halfSize = (Size + bounds.Size) / 2;

        direction = Quaternion.Euler(0, 0, -Rotation) * direction;
        halfSize = Quaternion.Euler(0, 0, -Rotation) * halfSize;

        return Mathf.Abs(direction.x) <= halfSize.x && Mathf.Abs(direction.y) <= halfSize.y;
    }

    public Vector2 Min
    {
        get
        {
            return Center - (Vector2)(Quaternion.Euler(0f, 0f, Rotation) * (Size / 2f));
        }
    }

    public Vector2 Max
    {
        get
        {
            return Center + (Vector2)(Quaternion.Euler(0f, 0f, Rotation) * (Size / 2f));
        }
    }
}

[Serializable]
public class CustomTuple<ItemOne, ItemTwo>
{
    public ItemOne Item1;
    public ItemTwo Item2;

    public static implicit operator CustomTuple<ItemOne, ItemTwo>(KeyValuePair<ItemOne, ItemTwo> d) => new(d);
    public static implicit operator KeyValuePair<ItemOne, ItemTwo>(CustomTuple<ItemOne, ItemTwo> d) => d;

    public static implicit operator CustomTuple<ItemOne, ItemTwo>(Tuple<ItemOne, ItemTwo> t) => new(t.Item1, t.Item2);
    public static implicit operator Tuple<ItemOne, ItemTwo>(CustomTuple<ItemOne, ItemTwo> t) => new(t.Item1, t.Item2);

    public CustomTuple(ItemOne key, ItemTwo value)
    {
        Item1 = key;
        Item2 = value;
    }

    private CustomTuple(KeyValuePair<ItemOne, ItemTwo> pair)
    {
        Item1 = pair.Key;
        Item2 = pair.Value;
    }
}

[Serializable]
public class CustomTuple<ItemOne, ItemTwo, ItemThree>
{
    public ItemOne Item1;
    public ItemTwo Item2;
    public ItemThree Item3;

    public static implicit operator CustomTuple<ItemOne, ItemTwo, ItemThree>(Tuple<ItemOne, ItemTwo, ItemThree> t) => new(t.Item1, t.Item2, t.Item3);
    public static implicit operator Tuple<ItemOne, ItemTwo, ItemThree>(CustomTuple<ItemOne, ItemTwo, ItemThree> t) => new(t.Item1, t.Item2, t.Item3);

    public CustomTuple(ItemOne one, ItemTwo two, ItemThree three)
    {
        Item1 = one;
        Item2 = two;
        Item3 = three;
    }
}

[Serializable]
public struct Vector2IntSerializable
{
    public int x;
    public int y;

    public static implicit operator Vector2Int(Vector2IntSerializable v) => new(v.x, v.y);
    public static implicit operator Vector2(Vector2IntSerializable v) => new(v.x, v.y);
    public static implicit operator Vector2IntSerializable(Vector2Int v) => new(v.x, v.y);

    public Vector2IntSerializable(int x, int y)
    {
        this.x = x;
        this.y = y;
    }
}

[Serializable]
public struct Vector2Serializable
{
    public float x;
    public float y;

    public static implicit operator Vector2(Vector2Serializable v) => new(v.x, v.y);
    public static implicit operator Vector2Serializable(Vector2Int v) => new(v.x, v.y);
    public static implicit operator Vector2Serializable(Vector2 v) => new(v.x, v.y);

    public static Vector2Serializable operator +(Vector2Serializable a, Vector2Serializable b)
    {
        return new(a.x + b.x, a.y + b.y);
    }

    public static Vector2Serializable operator +(Vector2Serializable a, float b)
    {
        return new(a.x + b, a.y + b);
    }

    public Vector2Serializable(float x, float y)
    {
        this.x = x;
        this.y = y;
    }

    public float Random(System.Random rnd)
    {
        float lower = x < y ? x : y;

        return ((float)rnd.NextDouble() * Range()) + lower;
    }

    public float Range()
    {
        return MathF.Abs(x - y);
    }
}

[Serializable]
public struct Vector3Serializable
{
    public float x;
    public float y;
    public float z;

    public static implicit operator Vector3(Vector3Serializable v) => new(v.x, v.y, v.z);
    public static implicit operator Vector3Serializable(Vector3Int v) => new(v.x, v.y, v.z);
    public static implicit operator Vector3Serializable(Vector3 v) => new(v.x, v.y, v.z);

    public static Vector3Serializable operator +(Vector3Serializable a, Vector3Serializable b)
    {
        return new Vector3Serializable(a.x + b.x, a.y + b.y, a.z + b.z);
    }

    public static Vector3Serializable operator *(Vector3Serializable a, Vector3Serializable b)
    {
        return new Vector3Serializable(a.x * b.x, a.y * b.y, a.z * b.z);
    }

    public static Vector3Serializable operator -(Vector3Serializable a, Vector3Serializable b)
    {
        return new Vector3Serializable(a.x - b.x, a.y - b.y, a.z - b.z);
    }

    public static Vector3Serializable operator +(Vector3Serializable a, Vector3 b)
    {
        return new Vector3Serializable(a.x + b.x, a.y + b.y, a.z + b.z);
    }

    public Vector3Serializable(float x, float y, float z)
    {
        this.x = x;
        this.y = y;
        this.z = z;
    }
}

[Serializable]
public struct ColorSerializable
{
    public float r;
    public float g;
    public float b;
    public float a;

    public static implicit operator Color(ColorSerializable c) => new(c.r, c.g, c.b, c.a);
    public static implicit operator ColorSerializable(Color c) => new(c.r, c.g, c.b, c.a);

    public static implicit operator ColorSerializable(Color32 c) => new(c.r, c.g, c.b, c.a);

    public ColorSerializable(float r, float g, float b, float a)
    {
        this.r = r;
        this.g = g;
        this.b = b;
        this.a = a;
    }
}