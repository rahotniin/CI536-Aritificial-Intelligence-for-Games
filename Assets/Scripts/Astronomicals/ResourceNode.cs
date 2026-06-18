using System;
using TMPro;
using UnityEngine;

public partial class ResourceNode : MonoBehaviour
{
    public Kind kind;
}

[Serializable]
public struct Resources
{
    public uint green;
    public uint red;

    public static bool operator> (Resources lhs, Resources rhs)
    {
        return lhs.green > rhs.green || lhs.red > rhs.red;
    }

    public static bool operator< (Resources lhs, Resources rhs)
    {
        return lhs.green < rhs.green || lhs.red < rhs.red;
    }

    public static Resources operator- (Resources lhs, Resources rhs)
    {
        lhs.green -= rhs.green;
        lhs.red   -= rhs.red;
        return lhs;
    }
}

[Serializable]
public struct ResourcesText
{
    public TMP_Text green;
    public TMP_Text red;
}

partial class ResourceNode
{
    public enum Kind
    {
        Green,
        Red,
    }
}
