using UnityEngine;

/// <summary>
/// Record时 当前帧的信息类
/// </summary>
[System.Serializable]
public class GPUSkinningFrame
{
    /// <summary>
    /// 所有骨骼的变换矩阵累积（M(trs)xM(bindPose)）
    /// </summary>
    public Matrix4x4[] matrices = null;


    #region 未理解根节点的变化记录是为了做什么
    /// <summary>
    /// 当前帧根位移逆变换
    /// </summary>
    public Quaternion rootMotionDeltaPositionQ;

    /// <summary>
    /// 当前帧根位移长度
    /// </summary>
    public float rootMotionDeltaPositionL;

    public Quaternion rootMotionDeltaRotation;

    [System.NonSerialized]
    private bool rootMotionInvInit = false;
    [System.NonSerialized]
    private Matrix4x4 rootMotionInv;
    public Matrix4x4 RootMotionInv(int rootBoneIndex)
    {
        if (!rootMotionInvInit)
        {
            rootMotionInv = matrices[rootBoneIndex].inverse;
            rootMotionInvInit = true;
        }
        return rootMotionInv;
    }

    #endregion
}
