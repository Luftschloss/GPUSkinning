using UnityEngine;

/// <summary>
/// Sample时的基本单位
/// </summary>
public class GPUSkinningAnimation : ScriptableObject
{
    #region properties assigned at "StartSample"
    public string guid = null;

    /// <summary>
    /// name (related to sampling result file, user-defined in Inspector)
    /// </summary>
    public string name = null;

    /// <summary>
    /// Sampling Bone Data Array
    /// </summary>
    public GPUSkinningBone[] bones = null;

    /// <summary>
    /// root bone index
    /// </summary>
    public int rootBoneIndex = 0;

    #endregion

    /// <summary>
    /// Sampling Clip Data Array
    /// </summary>
    public GPUSkinningClip[] clips = null;

    public Bounds bounds;

    /// <summary>
    /// Texture Width(pow(2,n))
    /// </summary>
    public int textureWidth = 0;

    /// <summary>
    /// Texture Height(pow(2,n))
    /// </summary>
    public int textureHeight = 0;

    public float[] lodDistances = null;

    public Mesh[] lodMeshes = null;

    public float sphereRadius = 1.0f;
}
