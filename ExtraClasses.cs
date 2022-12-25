using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class FloatRange
{
    public float Max;
    public float Min;

    public float Range
    {
        get
        {
            return Mathf.Abs(Max - Min);
        }
    }

    public float Random(System.Random rnd)
    {
        return Min + ((float)rnd.NextDouble() * Range);
    }    
}

[Serializable]
public class CustomDictionary<TKey, TValue>
{
    public List<CustomPair<TKey, TValue>> Pairs;

    public static implicit operator CustomDictionary<TKey, TValue>(Dictionary<TKey, TValue> d) => new(d);
    public static implicit operator Dictionary<TKey, TValue>(CustomDictionary<TKey, TValue> d) => d.GetDictionary();

    public int Count
    {
        get
        {
            return Pairs.Count;
        }
    }

    public CustomPair<TKey, TValue> this[int i]
    {
        get
        {
            return Pairs[i];
        }
    }

    public CustomDictionary(Dictionary<TKey, TValue> dictionary)
    {
        foreach (TKey key in dictionary.Keys)
        {
            Pairs.Add(new(key, dictionary[key]));
        }
    }

    public CustomDictionary()
    {
        Pairs = new();
    }

    public void Add(TKey key, TValue value)
    {
        Pairs.Add(new(key, value));
    }

    public void Add(CustomPair<TKey, TValue> pair)
    {
        Pairs.Add(pair);
    }

    public Dictionary<TKey, TValue> GetDictionary()
    {
        Dictionary<TKey, TValue> dict = new();

        for (int i = 0; i < Pairs.Count; i++)
        {
            dict.Add(Pairs[i].Key, Pairs[i].Value);
        }

        return dict;
    }

    public CustomPair<TKey, TValue> TakePairAt(int i)
    {
        if (i < Pairs.Count && i > -1)
        {
            CustomPair<TKey, TValue> pair = Pairs[i];
            Pairs.RemoveAt(i);
            return pair;
        }

        return default;
    }

    public CustomPair<TKey, TValue> GetPair(TKey key)
    {
        List<CustomPair<TKey, TValue>> pairs = new(Pairs);

        foreach (CustomPair<TKey, TValue> pair in pairs)
        {
            if (pair.Key.Equals(key))
            {
                return pair;
            }
        }

        return default;
    }

    public void Add(KeyValuePair<TKey, TValue> pairing)
    {
        Pairs.Add(pairing);
    }

    public void Shuffle(int iterations = 10)
    {
        System.Random rnd = new();
        for (int i = 0; i < iterations; i++)
        {
            for (int j = 0; j < Pairs.Count; j++)
            {
                int rndSwitch = rnd.Next(Pairs.Count);

                CustomPair<TKey, TValue> tempKey = Pairs[j];

                Pairs[j] = Pairs[rndSwitch];
                Pairs[rndSwitch] = tempKey;
            }
        }
    }
}

[Serializable]
public class CustomPair<TKey, TValue>
{
    public TKey Key;
    public TValue Value;

    public static implicit operator CustomPair<TKey, TValue>(KeyValuePair<TKey, TValue> d) => new(d);
    public static implicit operator KeyValuePair<TKey, TValue>(CustomPair<TKey, TValue> d) => d;

    public CustomPair(TKey key, TValue value)
    {
        Key = key;
        Value = value;
    }

    private CustomPair(KeyValuePair<TKey, TValue> pair)
    {
        Key = pair.Key;
        Value = pair.Value;
    }
}

[Serializable]
public class Vector2IntSerializable
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

    public Vector2IntSerializable()
    {
        x = 0;
        y = 0;
    }
}

[Serializable]
public class Vector2Serializable
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

    public Vector2Serializable(float x, float y)
    {
        this.x = x;
        this.y = y;
    }

    public Vector2Serializable()
    {
        x = 0;
        y = 0;
    }
}

[Serializable]
public class Vector3Serializable
{
    public float x;
    public float y;
    public float z;

    public static implicit operator Vector3(Vector3Serializable v) => new(v.x, v.y, v.z);
    public static implicit operator Vector3Serializable(Vector3Int v) => new(v.x, v.y, v.z);
    public static implicit operator Vector3Serializable(Vector3 v) => new(v.x, v.y, v.z);

    public Vector3Serializable(float x, float y, float z)
    {
        this.x = x;
        this.y = y;
        this.z = z;
    }

    public Vector3Serializable()
    {
        x = 0;
        y = 0;
        z = 0;
    }
}

[Serializable]
public class ColorSerializable
{
    public float r;
    public float g;
    public float b;
    public float a;

    public static implicit operator Color(ColorSerializable c) => new(c.r, c.g, c.b, c.a);
    public static implicit operator ColorSerializable(Color c) => new(c.r, c.g, c.b, c.a);

    public ColorSerializable(float r, float g, float b, float a)
    {
        this.r = r;
        this.g = g;
        this.b = b;
        this.a = a;
    }

    public ColorSerializable()
    {
        r = 1;
        g = 1;
        b = 1;
        a = 1;
    }
}