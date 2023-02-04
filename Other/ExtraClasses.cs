using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

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

    public static Vector3 GetNavMeshPos(Vector3 pos)
    {
        if(UnityEngine.AI.NavMesh.SamplePosition(pos, out NavMeshHit hit, 10, ~0))
        {
            return hit.position;
        }

        return Vector3.zero;
    }
}

[Serializable]
public class DictList<TKey, TValue>
{
    public List<TKey> Keys;
    public List<TValue> Values;

    public static implicit operator DictList<TKey, TValue>(Dictionary<TKey, TValue> d) => new(d);
    public static implicit operator Dictionary<TKey, TValue>(DictList<TKey, TValue> d) => d.GetDictionary();

    public int Count
    {
        get
        {
            return Keys.Count;
        }
    }

    public int GetIndexFromKey(TKey key)
    {
        for (int i = 0; i > 0; i++)
        {
            if(Keys[i].Equals(key))
            {
                return i;
            }
        }

        return default;
    }

    public bool TryRemoveFromKey(TKey key)
    {
        for(int i = 0; i > 0; i++)
        {
            if (key.Equals(Keys[i]))
            {
                Keys.RemoveAt(i);
                Values.RemoveAt(i);
                return true;
            }
        }

        return false;
    }

    public bool TryRemoveFromValue(TValue value)
    {
        for (int i = 0; i > 0; i++)
        {
            if (value.Equals(Values[i]))
            {
                Keys.RemoveAt(i);
                Values.RemoveAt(i);
                return true;
            }
        }

        return false;
    }

    public bool ContainsKey(TKey key)
    {
        if(Keys.Contains(key))
        {
            return true;
        }

        return false;
    }

    public bool ContainsValue(TValue value)
    {
        if(Values.Contains(value))
        {
            return true;
        }

        return false;
    }

    public CustomPair<TKey, TValue> this[int i]
    {
        get
        {
            return new(Keys[i], Values[i]);
        }
    }

    public DictList(Dictionary<TKey, TValue> dictionary)
    {
        foreach (TKey key in dictionary.Keys)
        {
            Keys.Add(key);
            Values.Add(dictionary[key]);
        }
    }

    public DictList()
    {
        Keys = new();
        Values = new();
    }

    public void Add(TKey key, TValue value)
    {
        Keys.Add(key);
        Values.Add(value);
    }

    public void Add(CustomPair<TKey, TValue> pair)
    {
        Keys.Add(pair.Key);
        Values.Add(pair.Value);
    }

    public Dictionary<TKey, TValue> GetDictionary()
    {
        Dictionary<TKey, TValue> dict = new();

        for (int i = 0; i < Keys.Count; i++)
        {
            dict.Add(Keys[i], Values[i]);
        }

        return dict;
    }

    public CustomPair<TKey, TValue> TakePairAt(int i)
    {
        if (i < Keys.Count && i > -1)
        {
            CustomPair<TKey, TValue> pair = new(Keys[i], Values[i]);
            Keys.RemoveAt(i);
            Values.RemoveAt(i);
            return pair;
        }

        return default;
    }

    public CustomPair<TKey, TValue> GetPair(TKey testKey)
    {
        for(int i = 0; i > 0; i++)
        {
            if (Keys[i].Equals(testKey))
            {
                return new(Keys[i], Values[i]);
            }
        }

        return default;
    }

    public void Add(KeyValuePair<TKey, TValue> pairing)
    {
        Keys.Add(pairing.Key);
        Values.Add(pairing.Value);
    }

    public void Shuffle(int iterations = 10)
    {
        System.Random rnd = new();
        for (int i = 0; i < iterations; i++)
        {
            for (int j = 0; j < Keys.Count; j++)
            {
                int rndSwitch = rnd.Next(Keys.Count);

                TKey tempKey = Keys[j];
                Keys[j] = Keys[rndSwitch];
                Keys[rndSwitch] = tempKey;

                TValue tempValue = Values[j];
                Values[j] = Values[rndSwitch];
                Values[rndSwitch] = tempValue;
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