using System;
using UnityEngine;

public class SelectionArea : MonoBehaviour
{
    GameObject scale;

    public Rect Bounds()
    {
        Rect bounds = new(
            transform.position.x,
            transform.position.z,
            transform.localScale.x,
            transform.localScale.z
        );

        return bounds;
    }

    public RectInt IntBounds()
    {
        float width = transform.localScale.x;
        float height = transform.localScale.z;
        int widthSign = Math.Sign(transform.localScale.x);
        int heightSign = Math.Sign(transform.localScale.z);
        RectInt bounds = new(
            Mathf.RoundToInt(transform.position.x) - widthSign,
            Mathf.RoundToInt(transform.position.z) - heightSign,
            Mathf.RoundToInt(width) + 2 * widthSign,
            Mathf.RoundToInt(height) + 2 * heightSign
        );

        return bounds;
    }
}