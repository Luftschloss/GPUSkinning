using UnityEngine;
using System.Collections;

/// <summary>
/// 用来记录骨骼的bindpose及父子绑定关系（childrenBonesIndices）
/// </summary>
[System.Serializable]
public class GPUSkinningBone
{
	[System.NonSerialized]
	public Transform transform = null;

	public Matrix4x4 bindpose;

    //*easy to search root
	public int parentBoneIndex = -1;

    //*child bone index array
	public int[] childrenBonesIndices = null;

	[System.NonSerialized]
	public Matrix4x4 animationMatrix;

	public string name = null;

    public string guid = null; 

    /// <summary>
    /// isExposed in hierarchy
    /// </summary>
    public bool isExposed = false;

    [System.NonSerialized]
    private bool bindposeInvInit = false;
    [System.NonSerialized]
    private Matrix4x4 bindposeInv;
    public Matrix4x4 BindposeInv
    {
        get
        {
            if(!bindposeInvInit)
            {
                bindposeInv = bindpose.inverse;
                bindposeInvInit = true;
            }
            return bindposeInv;
        }
    }
}
