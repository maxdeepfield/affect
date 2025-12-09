using UnityEngine;

/// <summary>
/// Adds a comment field to a GameObject in the inspector.
/// </summary>
public class Comment : MonoBehaviour
{
    [TextArea(3, 10)]
    public string text;
}
