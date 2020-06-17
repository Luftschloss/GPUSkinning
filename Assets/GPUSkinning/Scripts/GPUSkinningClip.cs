using UnityEngine;
using System.Collections;

/// <summary>
/// Sample Clip Unit
/// </summary>
[System.Serializable]
public class GPUSkinningClip
{
    public string name = null;

    /// <summary>
    /// AnimationClip length
    /// </summary>
    public float length = 0.0f;

    public int fps = 0;

    public GPUSkinningWrapMode wrapMode = GPUSkinningWrapMode.Once;

    /// <summary>
    /// Sample Frame Info
    /// </summary>
    public GPUSkinningFrame[] frames = null;

    /// <summary>
    /// Data StartIndex(Offset) in AnimDataFile
    /// </summary>
    public int pixelSegmentation = 0;

    public bool rootMotionEnabled = false;

    public bool individualDifferenceEnabled = false;

    public GPUSkinningAnimEvent[] events = null;
}
