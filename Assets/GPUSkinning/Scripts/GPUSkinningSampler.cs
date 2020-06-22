using UnityEngine;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Diagnostics;
using Debug = UnityEngine.Debug;

#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteInEditMode]
public class GPUSkinningSampler : MonoBehaviour 
{
#if UNITY_EDITOR
    [HideInInspector]
    [SerializeField]
	public string animName = null;

    /// <summary>
    /// Temp Single Clip
    /// </summary>
    [HideInInspector]
    [System.NonSerialized]
	public AnimationClip animClip = null;

    /// <summary>
    /// AnimationClip Editor in Inspector
    /// </summary>
    [HideInInspector]
    [SerializeField]
    public AnimationClip[] animClips = null;

    [HideInInspector]
    [SerializeField]
    public GPUSkinningWrapMode[] wrapModes = null;

    [HideInInspector]
    [SerializeField]
    public int[] fpsList = null;

    [HideInInspector]
    [SerializeField]
    public bool[] rootMotionEnabled = null;

    [HideInInspector]
    [SerializeField]
    public bool[] individualDifferenceEnabled = null;

    [HideInInspector]
    [SerializeField]
    public Mesh[] lodMeshes = null;

    [HideInInspector]
    [SerializeField]
    public float[] lodDistances = null;

    [HideInInspector]
    [SerializeField]
    private float sphereRadius = 1.0f;

    [HideInInspector]
    [SerializeField]
    public bool createNewShader = false;

    /// <summary>
    /// Current Sampling Clip Index
    /// </summary>
    [HideInInspector]
    [System.NonSerialized]
    public int samplingClipIndex = -1;

    [HideInInspector]
    [SerializeField]
    public TextAsset texture = null;

    [HideInInspector]
    [SerializeField]
	public GPUSkinningQuality skinQuality = GPUSkinningQuality.Bone2;

    [HideInInspector]
    [SerializeField]
	public Transform rootBoneTransform = null;

    [HideInInspector]
    [SerializeField]
    public GPUSkinningAnimation anim = null;

    [HideInInspector]
    [SerializeField]
	public GPUSkinningShaderType shaderType = GPUSkinningShaderType.Unlit;

	[HideInInspector]
	[System.NonSerialized]
	public bool isSampling = false;

    [HideInInspector]
    [SerializeField]
    public Mesh savedMesh = null;

    [HideInInspector]
    [SerializeField]
    public Material[] savedMtrls = null;

    [HideInInspector]
    [SerializeField]
    public Shader savedShader = null;

    [HideInInspector]
    [SerializeField]
    public bool updateOrNew = true;

    private Animation animation = null;

	private Animator animator = null;
    private RuntimeAnimatorController runtimeAnimatorController = null;

	private SkinnedMeshRenderer smr = null;

    /// <summary>
    /// Total Sampling Info create at first "StartSample"
    /// </summary>
	private GPUSkinningAnimation gpuSkinningAnimation = null;

    /// <summary>
    /// Current Clip for Sampling
    /// </summary>
    private GPUSkinningClip gpuSkinningClip = null;

    private Vector3 rootMotionPosition;

    private Quaternion rootMotionRotation;

    /// <summary>
    /// Current Clip Sample Frames
    /// </summary>
	[HideInInspector]
	[System.NonSerialized]
	public int samplingTotalFrams = 0;

    /// <summary>
    /// Current Clip Sampling Frame Index
    /// </summary>
	[HideInInspector]
	[System.NonSerialized]
	public int samplingFrameIndex = 0;

	public const string TEMP_SAVED_ANIM_PATH = "GPUSkinning_Temp_Save_Anim_Path";
	public const string TEMP_SAVED_MTRL_PATH = "GPUSkinning_Temp_Save_Mtrl_Path";
	public const string TEMP_SAVED_MESH_PATH = "GPUSkinning_Temp_Save_Mesh_Path";
    public const string TEMP_SAVED_SHADER_PATH = "GPUSkinning_Temp_Save_Shader_Path";
    public const string TEMP_SAVED_TEXTURE_PATH = "GPUSkinning_Temp_Save_Texture_Path";

    public void BeginSample()
    {
        samplingClipIndex = 0;
    }

    public void EndSample()
    {
        samplingClipIndex = -1;
    }

    /// <summary>
    /// check have any clip unsampled
    /// </summary>
    /// <returns></returns>
    public bool IsSamplingProgress()
    {
        return samplingClipIndex != -1;
    }

    public bool IsAnimatorOrAnimation()
    {
        return animator != null; 
    }

    public void StartSample()
    {
        if (isSampling)
        {
            return;
        }

        if (string.IsNullOrEmpty(animName.Trim()))
        {
            ShowDialog("Animation name is empty.");
            return;
        }

        if (rootBoneTransform == null)
        {
            ShowDialog("Please set Root Bone.");
            return;
        }

        if (animClips == null || animClips.Length == 0)
        {
            ShowDialog("Please set Anim Clips.");
            return;
        }

        animClip = animClips[samplingClipIndex];
        if (animClip == null)
        {
            isSampling = false;
            return;
        }

        int numFrames = (int)(GetClipFPS(animClip, samplingClipIndex) * animClip.length);
        if (numFrames == 0)
        {
            isSampling = false;
            return;
        }

        smr = GetComponentInChildren<SkinnedMeshRenderer>();
        if (smr == null)
        {
            ShowDialog("Cannot find SkinnedMeshRenderer.");
            return;
        }
        if (smr.sharedMesh == null)
        {
            ShowDialog("Cannot find SkinnedMeshRenderer.mesh.");
            return;
        }

        Mesh mesh = smr.sharedMesh;
        if (mesh == null)
        {
            ShowDialog("Missing Mesh");
            return;
        }

        samplingFrameIndex = 0;

        gpuSkinningAnimation = anim == null ? ScriptableObject.CreateInstance<GPUSkinningAnimation>() : anim;
        gpuSkinningAnimation.name = animName;

        if (anim == null)
        {
            gpuSkinningAnimation.guid = System.Guid.NewGuid().ToString();
        }

        List<GPUSkinningBone> bones_result = new List<GPUSkinningBone>();
        CollectBones(bones_result, smr.bones, mesh.bindposes, null, rootBoneTransform, 0);
        GPUSkinningBone[] newBones = bones_result.ToArray();
        GenerateBonesGUID(newBones);

        //set bones isExposed info
        if (anim != null)
            RestoreCustomBoneData(anim.bones, newBones);

        gpuSkinningAnimation.bones = newBones;
        gpuSkinningAnimation.rootBoneIndex = 0;

        //get override clipindex
        int numClips = gpuSkinningAnimation.clips == null ? 0 : gpuSkinningAnimation.clips.Length;
        int overrideClipIndex = -1;
        for (int i = 0; i < numClips; ++i)
        {
            if (gpuSkinningAnimation.clips[i].name == animClip.name)
            {
                overrideClipIndex = i;
                break;
            }
        }

        Debug.Log(string.Format("StartSample:{0} numFrames:{1} OverrideClipIdx:{2} ClipLen{3} \n {4}", animClip.name, numFrames, overrideClipIndex, animClip.length, new StackTrace().ToString()));

        //new SampleClip
        gpuSkinningClip = new GPUSkinningClip();
        //SetSampleCilpInfo from SampleClips in Inspector
        gpuSkinningClip.name = animClip.name;
        gpuSkinningClip.fps = GetClipFPS(animClip, samplingClipIndex);
        gpuSkinningClip.length = animClip.length;
        gpuSkinningClip.wrapMode = wrapModes[samplingClipIndex];
        gpuSkinningClip.frames = new GPUSkinningFrame[numFrames];
        gpuSkinningClip.rootMotionEnabled = rootMotionEnabled[samplingClipIndex];
        gpuSkinningClip.individualDifferenceEnabled = individualDifferenceEnabled[samplingClipIndex];

        if (gpuSkinningAnimation.clips == null)
        {
            gpuSkinningAnimation.clips = new GPUSkinningClip[] { gpuSkinningClip };
        }
        else
        {
            //new record
            if (overrideClipIndex == -1)
            {
                List<GPUSkinningClip> clips = new List<GPUSkinningClip>(gpuSkinningAnimation.clips);
                clips.Add(gpuSkinningClip);
                gpuSkinningAnimation.clips = clips.ToArray();
            }
            else
            {
                GPUSkinningClip overridedClip = gpuSkinningAnimation.clips[overrideClipIndex];
                RestoreCustomClipData(overridedClip, gpuSkinningClip);
                gpuSkinningAnimation.clips[overrideClipIndex] = gpuSkinningClip;
            }
        }

        SetCurrentAnimationClip();
        PrepareRecordAnimator();

        isSampling = true;
    }

    /// <summary>
    /// Sample Frame（default 0 is AnimatinClip.frameRate，or from Inspector）
    /// </summary>
    /// <param name="clip"></param>
    /// <param name="clipIndex"></param>
    /// <returns></returns>
    private int GetClipFPS(AnimationClip clip, int clipIndex)
    {
        return fpsList[clipIndex] == 0 ? (int)clip.frameRate : fpsList[clipIndex];
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="src"></param>
    /// <param name="dest"></param>
    private void RestoreCustomClipData(GPUSkinningClip src, GPUSkinningClip dest)
    {
        if(src.events != null)
        {
            int totalFrames = (int)(dest.length * dest.fps);
            dest.events = new GPUSkinningAnimEvent[src.events.Length];
            for(int i = 0; i < dest.events.Length; ++i)
            {
                GPUSkinningAnimEvent evt = new GPUSkinningAnimEvent();
                evt.eventId = src.events[i].eventId;
                evt.frameIndex = Mathf.Clamp(src.events[i].frameIndex, 0, totalFrames - 1);
                dest.events[i] = evt;
            }
        }
    }

    /// <summary>
    /// set bone isExposed info(origData is set by Editor, new Data from origData)
    /// </summary>
    /// <param name="bonesOrig"></param>
    /// <param name="bonesNew"></param>
    private void RestoreCustomBoneData(GPUSkinningBone[] bonesOrig, GPUSkinningBone[] bonesNew)
    {
        for(int i = 0; i < bonesNew.Length; ++i)
        {
            for(int j = 0; j < bonesOrig.Length; ++j)
            {
                if(bonesNew[i].guid == bonesOrig[j].guid)
                {
                    bonesNew[i].isExposed = bonesOrig[j].isExposed;
                    break;
                }
            }
        }
    }

    /// <summary>
    /// 通过HierarchyPath生成MD5作为Bone的GUID，所以要求同层骨骼命名有意义，避免重复
    /// </summary>
    /// <param name="bones"></param>
    private void GenerateBonesGUID(GPUSkinningBone[] bones)
    {
        int numBones = bones == null ? 0 : bones.Length;
        for(int i = 0; i < numBones; ++i)
        {
            string boneHierarchyPath = GPUSkinningUtil.BoneHierarchyPath(bones, i);
            string guid = GPUSkinningUtil.MD5(boneHierarchyPath);
            bones[i].guid = guid;
        }
    }

    /// <summary>
    /// Core Function: Prepared Step
    /// Animator Play PreWork
    /// Why Prepare????
    /// </summary>
    private void PrepareRecordAnimator()
    {
        if (animator != null)
        {
            //record total frame
            int numFrames = (int)(gpuSkinningClip.fps * gpuSkinningClip.length);

            animator.applyRootMotion = gpuSkinningClip.rootMotionEnabled;
            //rebind all the animated properties and mesh data with the Animator
            animator.Rebind();
            animator.recorderStartTime = 0;
            //numFrames=0 recording continue until the user calls StopRecording
            //numFrames maximum value is 10000
            //start record
            animator.StartRecording(numFrames);
            for (int i = 0; i < numFrames; ++i)
            {
                animator.Update(1.0f / gpuSkinningClip.fps);
            }
            //stop record
            animator.StopRecording();
            animator.StartPlayback();
        }
    }

    private void SetCurrentAnimationClip()
    {
        if (animation == null)
        {
            AnimatorOverrideController animatorOverrideController = new AnimatorOverrideController();
            AnimationClip[] clips = runtimeAnimatorController.animationClips;
            AnimationClipPair[] pairs = new AnimationClipPair[clips.Length];
            for (int i = 0; i < clips.Length; ++i)
            {
                AnimationClipPair pair = new AnimationClipPair();
                pairs[i] = pair;
                pair.originalClip = clips[i];
                pair.overrideClip = animClip;
            }
            animatorOverrideController.runtimeAnimatorController = runtimeAnimatorController;
            animatorOverrideController.clips = pairs;
            animator.runtimeAnimatorController = animatorOverrideController;
        }
    }

    private void CreateLODMeshes(Bounds bounds, string dir)
    {
        gpuSkinningAnimation.lodMeshes = null;
        gpuSkinningAnimation.lodDistances = null;
        gpuSkinningAnimation.sphereRadius = sphereRadius;

        if(lodMeshes != null)
        {
            List<Mesh> newMeshes = new List<Mesh>();
            List<float> newLodDistances = new List<float>();
            for (int i = 0; i < lodMeshes.Length; ++i)
            {
                Mesh lodMesh = lodMeshes[i];
                if(lodMesh != null)
                {
                    Mesh newMesh = CreateNewMesh(lodMesh, "GPUSkinning_Mesh_LOD" + (i + 1));
                    newMesh.bounds = bounds;
                    string savedMeshPath = dir + "/GPUSKinning_Mesh_" + animName + "_LOD" + (i + 1) + ".asset";
                    AssetDatabase.CreateAsset(newMesh, savedMeshPath);
                    newMeshes.Add(newMesh);
                    newLodDistances.Add(lodDistances[i]);
                }
            }
            gpuSkinningAnimation.lodMeshes = newMeshes.ToArray();

            newLodDistances.Add(9999);
            gpuSkinningAnimation.lodDistances = newLodDistances.ToArray();
        }

        EditorUtility.SetDirty(gpuSkinningAnimation);
    }

    private Mesh CreateNewMesh(Mesh mesh, string meshName)
    {
        Vector3[] normals = mesh.normals;
        Vector4[] tangents = mesh.tangents;
        Color[] colors = mesh.colors;
        Vector2[] uv = mesh.uv;

        Mesh newMesh = new Mesh();
        newMesh.name = meshName;
        newMesh.vertices = mesh.vertices;
        if (normals != null && normals.Length > 0) { newMesh.normals = normals; }
        if (tangents != null && tangents.Length > 0) { newMesh.tangents = tangents; }
        if (colors != null && colors.Length > 0) { newMesh.colors = colors; }
        if (uv != null && uv.Length > 0) { newMesh.uv = uv; }

        int numVertices = mesh.vertexCount;
        BoneWeight[] boneWeights = mesh.boneWeights;
        Vector4[] uv2 = new Vector4[numVertices];
		Vector4[] uv3 = new Vector4[numVertices];
        Transform[] smrBones = smr.bones;
        for(int i = 0; i < numVertices; ++i)
        {
            BoneWeight boneWeight = boneWeights[i];

			BoneWeightSortData[] weights = new BoneWeightSortData[4];
			weights[0] = new BoneWeightSortData(){ index=boneWeight.boneIndex0, weight=boneWeight.weight0 };
			weights[1] = new BoneWeightSortData(){ index=boneWeight.boneIndex1, weight=boneWeight.weight1 };
			weights[2] = new BoneWeightSortData(){ index=boneWeight.boneIndex2, weight=boneWeight.weight2 };
			weights[3] = new BoneWeightSortData(){ index=boneWeight.boneIndex3, weight=boneWeight.weight3 };
			System.Array.Sort(weights);

			GPUSkinningBone bone0 = GetBoneByTransform(smrBones[weights[0].index]);
			GPUSkinningBone bone1 = GetBoneByTransform(smrBones[weights[1].index]);
			GPUSkinningBone bone2 = GetBoneByTransform(smrBones[weights[2].index]);
			GPUSkinningBone bone3 = GetBoneByTransform(smrBones[weights[3].index]);

            Vector4 skinData_01 = new Vector4();
			skinData_01.x = GetBoneIndex(bone0);
			skinData_01.y = weights[0].weight;
			skinData_01.z = GetBoneIndex(bone1);
			skinData_01.w = weights[1].weight;
			uv2[i] = skinData_01;

			Vector4 skinData_23 = new Vector4();
			skinData_23.x = GetBoneIndex(bone2);
			skinData_23.y = weights[2].weight;
			skinData_23.z = GetBoneIndex(bone3);
			skinData_23.w = weights[3].weight;
			uv3[i] = skinData_23;
        }
        newMesh.SetUVs(1, new List<Vector4>(uv2));
		newMesh.SetUVs(2, new List<Vector4>(uv3));

        newMesh.triangles = mesh.triangles;

        //copy submesh info
        newMesh.subMeshCount = mesh.subMeshCount;
        for (int i = 0; i < mesh.subMeshCount; i++)
        {
            int[] indies = mesh.GetIndices(i);
            MeshTopology meshTopology = mesh.GetTopology(i);
            newMesh.SetIndices(indies, meshTopology, i);
            int[] triangles = mesh.GetTriangles(i);
            newMesh.SetTriangles(triangles, i);
        }

        return newMesh;
    }

	private class BoneWeightSortData : System.IComparable<BoneWeightSortData>
	{
		public int index = 0;

		public float weight = 0;

		public int CompareTo(BoneWeightSortData b)
		{
			return weight > b.weight ? -1 : 1;
		}
	}

    /// <summary>
    /// iterative collect all bones from root
    /// </summary>
    /// <param name="bones_result"></param>
    /// <param name="bones_smr"></param>
    /// <param name="bindposes"></param>
    /// <param name="parentBone"></param>
    /// <param name="currentBoneTransform"></param>
    /// <param name="currentBoneIndex"></param>
	private void CollectBones(List<GPUSkinningBone> bones_result, Transform[] bones_smr, Matrix4x4[] bindposes, GPUSkinningBone parentBone, Transform currentBoneTransform, int currentBoneIndex)
	{
		GPUSkinningBone currentBone = new GPUSkinningBone();
		bones_result.Add(currentBone);

		int indexOfSmrBones = System.Array.IndexOf(bones_smr, currentBoneTransform);
		currentBone.transform = currentBoneTransform;
		currentBone.name = currentBone.transform.gameObject.name;
		currentBone.bindpose = indexOfSmrBones == -1 ? Matrix4x4.identity : bindposes[indexOfSmrBones];
		currentBone.parentBoneIndex = parentBone == null ? -1 : bones_result.IndexOf(parentBone);

		if(parentBone != null)
		{
			parentBone.childrenBonesIndices[currentBoneIndex] = bones_result.IndexOf(currentBone);
		}

		int numChildren = currentBone.transform.childCount;
		if(numChildren > 0)
		{
			currentBone.childrenBonesIndices = new int[numChildren];
			for(int i = 0; i < numChildren; ++i)
			{
				CollectBones(bones_result, bones_smr, bindposes, currentBone, currentBone.transform.GetChild(i) , i);
			}
		}
	}

    /// <summary>
    /// calculate animData (offset)startIndex for each clip
    /// calculate matries texture size
    /// </summary>
    /// <param name="gpuSkinningAnim"></param>
    private void SetSthAboutTexture(GPUSkinningAnimation gpuSkinningAnim)
    {
        int numPixels = 0;

        GPUSkinningClip[] clips = gpuSkinningAnim.clips;
        int numClips = clips.Length;
        for (int clipIndex = 0; clipIndex < numClips; ++clipIndex)
        {
            GPUSkinningClip clip = clips[clipIndex];
            clip.pixelSegmentation = numPixels;

            GPUSkinningFrame[] frames = clip.frames;
            int numFrames = frames.Length;
            //a bone matrix(3x4),need 3 pixels(float 3*4)
            numPixels += gpuSkinningAnim.bones.Length * 3 * numFrames;
        }
        
        CalculateTextureSize(numPixels, out gpuSkinningAnim.textureWidth, out gpuSkinningAnim.textureHeight);
    }

    /// <summary>
    /// Core Function:Store Bone Animation Info
    /// Write Matries Texture(*3x4:ignore last raw(0,0,0,1))
    /// </summary>
    /// <param name="dir"></param>
    /// <param name="gpuSkinningAnim"></param>
    private void CreateTextureMatrix(string dir, GPUSkinningAnimation gpuSkinningAnim)
    {
        Texture2D texture = new Texture2D(gpuSkinningAnim.textureWidth, gpuSkinningAnim.textureHeight, TextureFormat.RGBAHalf, false, true);
        Color[] pixels = texture.GetPixels();
        int pixelIndex = 0;
        for (int clipIndex = 0; clipIndex < gpuSkinningAnim.clips.Length; ++clipIndex)
        {
            GPUSkinningClip clip = gpuSkinningAnim.clips[clipIndex];
            GPUSkinningFrame[] frames = clip.frames;
            int numFrames = frames.Length;
            for (int frameIndex = 0; frameIndex < numFrames; ++frameIndex)
            {
                GPUSkinningFrame frame = frames[frameIndex];
                Matrix4x4[] matrices = frame.matrices;
                int numMatrices = matrices.Length;
                for (int matrixIndex = 0; matrixIndex < numMatrices; ++matrixIndex)
                {
                    Matrix4x4 matrix = matrices[matrixIndex];
                    pixels[pixelIndex++] = new Color(matrix.m00, matrix.m01, matrix.m02, matrix.m03);
                    pixels[pixelIndex++] = new Color(matrix.m10, matrix.m11, matrix.m12, matrix.m13);
                    pixels[pixelIndex++] = new Color(matrix.m20, matrix.m21, matrix.m22, matrix.m23);
                }
            }
        }
        texture.SetPixels(pixels);
        texture.Apply();

        string savedPath = dir + "/GPUSKinning_Texture_" + animName + ".bytes";
        using (FileStream fileStream = new FileStream(savedPath, FileMode.Create))
        {
            byte[] bytes = texture.GetRawTextureData();
            fileStream.Write(bytes, 0, bytes.Length);
            fileStream.Flush();
            fileStream.Close();
            fileStream.Dispose();
        }
        WriteTempData(TEMP_SAVED_TEXTURE_PATH, savedPath);
    }

    /// <summary>
    /// CalculateTextureSize by pixels
    /// width first then height
    /// </summary>
    /// <param name="numPixels"></param>
    /// <param name="texWidth"></param>
    /// <param name="texHeight"></param>
    private void CalculateTextureSize(int numPixels, out int texWidth, out int texHeight)
    {
        texWidth = 1;
        texHeight = 1;
        while (true)
        {
            if (texWidth * texHeight >= numPixels) break;
            texWidth *= 2;
            if (texWidth * texHeight >= numPixels) break;
            texHeight *= 2;
        }
    }

    public void MappingAnimationClips()
    {
        if(animation == null)
        {
            return;
        }

        List<AnimationClip> newClips = null;
        AnimationClip[] clips = AnimationUtility.GetAnimationClips(gameObject);
        if (clips != null)
        {
            for (int i = 0; i < clips.Length; ++i)
            {
                AnimationClip clip = clips[i];
                if (clip != null)
                {
                    if (animClips == null || System.Array.IndexOf(animClips, clip) == -1)
                    {
                        if (newClips == null)
                        {
                            newClips = new List<AnimationClip>();
                        }
                        newClips.Clear();
                        if (animClips != null) newClips.AddRange(animClips);
                        newClips.Add(clip);
                        animClips = newClips.ToArray();
                    }
                }
            }
        }

        if(animClips != null && clips != null)
        {
            for(int i = 0; i < animClips.Length; ++i)
            {
                AnimationClip clip = animClips[i];
                if (clip != null)
                {
                    if(System.Array.IndexOf(clips, clip) == -1)
                    {
                        if(newClips == null)
                        {
                            newClips = new List<AnimationClip>();
                        }
                        newClips.Clear();
                        newClips.AddRange(animClips);
                        newClips.RemoveAt(i);
                        animClips = newClips.ToArray();
                        --i;
                    }
                }
            }
        }
    }

    private void InitTransform()
    {
        transform.parent = null;
        transform.position = Vector3.zero;
        transform.eulerAngles = Vector3.zero;
    }

    private void Awake()
	{
        animation = GetComponent<Animation>();
		animator = GetComponent<Animator>();
        if (animator == null && animation == null)
        {
            DestroyImmediate(this);
            ShowDialog("Cannot find Animator Or Animation Component");
            return;
        }
        if(animator != null && animation != null)
        {
            DestroyImmediate(this);
            ShowDialog("Animation is not coexisting with Animator");
            return;
        }
        if (animator != null)
        {
            if (animator.runtimeAnimatorController == null)
            {
                DestroyImmediate(this);
                ShowDialog("Missing RuntimeAnimatorController");
                return;
            }
            if (animator.runtimeAnimatorController is AnimatorOverrideController)
            {
                DestroyImmediate(this);
                ShowDialog("RuntimeAnimatorController could not be a AnimatorOverrideController");
                return;
            }
            runtimeAnimatorController = animator.runtimeAnimatorController;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            InitTransform();
            return;
        }
        if(animation != null)
        {
            MappingAnimationClips();
            animation.Stop();
            animation.cullingType = AnimationCullingType.AlwaysAnimate;
            InitTransform();
            return;
        }
	}

    /// <summary>
    /// Sampling
    /// </summary>
	private void Update()
	{
		if(!isSampling)
		{
			return;
		}

        int totalFrams = (int)(gpuSkinningClip.length * gpuSkinningClip.fps);
		samplingTotalFrams = totalFrams;

        //Finish Current Clip Sampling, and save samplingData
        if (samplingFrameIndex >= totalFrams)
        {
            Debug.Log("Update=>" + gpuSkinningClip.name + "==>" + totalFrams);
            if (animator != null)
            {
                animator.StopPlayback();
            }

            string savePath = null;
            //first time set a save path
            if (anim == null)
            {
                savePath = EditorUtility.SaveFolderPanel("GPUSkinning Sampler Save", GetUserPreferDir(), animName);
            }
            else
            {
                string animPath = AssetDatabase.GetAssetPath(anim);
                savePath = new FileInfo(animPath).Directory.FullName.Replace('\\', '/');
            }

			if(!string.IsNullOrEmpty(savePath))
			{
				if(!savePath.Contains(Application.dataPath.Replace('\\', '/')))
				{
					ShowDialog("Must select a directory in the project's Asset folder.");
				}
				else
				{
					SaveUserPreferDir(savePath);

					string dir = "Assets" + savePath.Substring(Application.dataPath.Length);

                    //File1:Refresh/Create "GPUSking_Anim"
                    string savedAnimPath = dir + "/GPUSKinning_Anim_" + animName + ".asset";
                    SetSthAboutTexture(gpuSkinningAnimation);
                    
                    EditorUtility.SetDirty(gpuSkinningAnimation);           //refresh editor window

                    if (anim != gpuSkinningAnimation)
                    {
                        AssetDatabase.CreateAsset(gpuSkinningAnimation, savedAnimPath);
                    }
                    WriteTempData(TEMP_SAVED_ANIM_PATH, savedAnimPath);
                    anim = gpuSkinningAnimation;
                    //File2:Record "GPUSkinning_Texture"
                    CreateTextureMatrix(dir, anim);

                    if (samplingClipIndex == 0)
                    {
                        //File3:Mesh
                        Mesh newMesh = CreateNewMesh(smr.sharedMesh, "GPUSkinning_Mesh");
                        if (savedMesh != null)
                        {
                            newMesh.bounds = savedMesh.bounds;
                        }
                        string savedMeshPath = dir + "/GPUSKinning_Mesh_" + animName + ".asset";
                        AssetDatabase.CreateAsset(newMesh, savedMeshPath);
                        WriteTempData(TEMP_SAVED_MESH_PATH, savedMeshPath);
                        savedMesh = newMesh;

                        //File4:Material (New Shader)
                        CreateShaderAndMaterial(dir);

                        //File5:Mesh LOD
                        CreateLODMeshes(newMesh.bounds, dir);
                    }

                    //Refresh Data
					AssetDatabase.Refresh();
					AssetDatabase.SaveAssets();
				}
			}
            //mark finish current clip sampling
            isSampling = false;
            return;
        }

        //sample frame time in clip
        float time = gpuSkinningClip.length * ((float)samplingFrameIndex / totalFrams);

        //new sample frame
        GPUSkinningFrame frame = new GPUSkinningFrame();
        gpuSkinningClip.frames[samplingFrameIndex] = frame;
        frame.matrices = new Matrix4x4[gpuSkinningAnimation.bones.Length];

        //animation play
        //Component1:Animator
        if (animation == null)
        {
            animator.playbackTime = time;
            animator.Update(0);
        }
        //Component2:Animation
        else
        {
            animation.Stop();
            AnimationState animState = animation[animClip.name];
            if(animState != null)
            {
                animState.time = time;
                animation.Sample();
                animation.Play();
            }
        }
        StartCoroutine(SamplingCoroutine(frame, totalFrams));
    }

    /// <summary>
    /// SapmleCore[Start more than one SamplingCoroutine, a SamplingCoroutine for a frame of clip]
    /// </summary>
    /// <param name="frame"></param>
    /// <param name="totalFrames"></param>
    /// <returns></returns>
    private IEnumerator SamplingCoroutine(GPUSkinningFrame frame, int totalFrames)
    {
        //*after animation update then record matricesInfo
		yield return new WaitForEndOfFrame();

        GPUSkinningBone[] bones = gpuSkinningAnimation.bones;
        int numBones = bones.Length;
        for(int i = 0; i < numBones; ++i)
        {
            Transform boneTransform = bones[i].transform;
            GPUSkinningBone currentBone = GetBoneByTransform(boneTransform);
			//GPUSkinningBone currentBone = bones[i];
            frame.matrices[i] = currentBone.bindpose;
            do
            {
                Matrix4x4 mat = Matrix4x4.TRS(currentBone.transform.localPosition, currentBone.transform.localRotation, currentBone.transform.localScale);
                frame.matrices[i] = mat * frame.matrices[i];            //对每一根骨骼计算当前帧的变换矩阵累积（M(trs)xM(bindPose)）
                if (currentBone.parentBoneIndex == -1)                  //break at rootBone
                    break;
                else
                    currentBone = bones[currentBone.parentBoneIndex];
            }
            while (true);
        }

        //first frame
        if(samplingFrameIndex == 0)
        {
            rootMotionPosition = bones[gpuSkinningAnimation.rootBoneIndex].transform.localPosition;
            rootMotionRotation = bones[gpuSkinningAnimation.rootBoneIndex].transform.localRotation;
        }
        else
        {
            Vector3 newPosition = bones[gpuSkinningAnimation.rootBoneIndex].transform.localPosition;
            Quaternion newRotation = bones[gpuSkinningAnimation.rootBoneIndex].transform.localRotation;
            Vector3 deltaPosition = newPosition - rootMotionPosition;

            //目的是什么（*在Awake的时候会根据选定的根骨骼节点InitTransform）
            frame.rootMotionDeltaPositionQ = Quaternion.Inverse(Quaternion.Euler(transform.forward)) * Quaternion.Euler(deltaPosition.normalized);
            frame.rootMotionDeltaPositionL = deltaPosition.magnitude;
            frame.rootMotionDeltaRotation = Quaternion.Inverse(rootMotionRotation) * newRotation;
            rootMotionPosition = newPosition;
            rootMotionRotation = newRotation;

            //second frame
            if(samplingFrameIndex == 1)
            {
                gpuSkinningClip.frames[0].rootMotionDeltaPositionQ = gpuSkinningClip.frames[1].rootMotionDeltaPositionQ;
                gpuSkinningClip.frames[0].rootMotionDeltaPositionL = gpuSkinningClip.frames[1].rootMotionDeltaPositionL;
                gpuSkinningClip.frames[0].rootMotionDeltaRotation = gpuSkinningClip.frames[1].rootMotionDeltaRotation;
            }
        }

        ++samplingFrameIndex;
    }

    /// <summary>
    /// if need new shader create(clone by bonetype)
    /// </summary>
    /// <param name="dir"></param>
	private void CreateShaderAndMaterial(string dir)
	{
        Shader shader = null;
        if (createNewShader)
        {
            string shaderTemplate =
                shaderType == GPUSkinningShaderType.Unlit ? "GPUSkinningUnlit_Template" :
                shaderType == GPUSkinningShaderType.StandardSpecular ? "GPUSkinningSpecular_Template" :
                shaderType == GPUSkinningShaderType.StandardMetallic ? "GPUSkinningMetallic_Template" : string.Empty;

            string shaderStr = ((TextAsset)Resources.Load(shaderTemplate)).text;
            shaderStr = shaderStr.Replace("_$AnimName$_", animName);
            shaderStr = SkinQualityShaderStr(shaderStr);
            string shaderPath = dir + "/GPUSKinning_Shader_" + animName + ".shader";
            File.WriteAllText(shaderPath, shaderStr);
            WriteTempData(TEMP_SAVED_SHADER_PATH, shaderPath);
            AssetDatabase.ImportAsset(shaderPath);
            shader = AssetDatabase.LoadMainAssetAtPath(shaderPath) as Shader;
        }
        else
        {
            string shaderName =
                shaderType == GPUSkinningShaderType.Unlit ? "GPUSkinning/GPUSkinning_Unlit_Skin" :
                shaderType == GPUSkinningShaderType.StandardSpecular ? "GPUSkinning/GPUSkinning_Specular_Skin" :
                shaderType == GPUSkinningShaderType.StandardMetallic ? "GPUSkinning/GPUSkinning_Metallic_Skin" : string.Empty;
            shaderName +=
                skinQuality == GPUSkinningQuality.Bone1 ? 1 :
                skinQuality == GPUSkinningQuality.Bone2 ? 2 :
                skinQuality == GPUSkinningQuality.Bone4 ? 4 : 1;
            shader = Shader.Find(shaderName);
            WriteTempData(TEMP_SAVED_SHADER_PATH, AssetDatabase.GetAssetPath(shader));
        }
		Material mtrl = new Material(shader);
		if(smr.sharedMaterial != null)
		{
			mtrl.CopyPropertiesFromMaterial(smr.sharedMaterial);
		}
		string savedMtrlPath = dir + "/GPUSKinning_Material_" + animName + ".mat";
		AssetDatabase.CreateAsset(mtrl, savedMtrlPath);
        WriteTempData(TEMP_SAVED_MTRL_PATH, savedMtrlPath);
	}

	private string SkinQualityShaderStr(string shaderStr)
	{
		GPUSkinningQuality removalQuality1 = 
			skinQuality == GPUSkinningQuality.Bone1 ? GPUSkinningQuality.Bone2 : 
			skinQuality == GPUSkinningQuality.Bone2 ? GPUSkinningQuality.Bone1 : 
			skinQuality == GPUSkinningQuality.Bone4 ? GPUSkinningQuality.Bone1 : GPUSkinningQuality.Bone1;

		GPUSkinningQuality removalQuality2 = 
			skinQuality == GPUSkinningQuality.Bone1 ? GPUSkinningQuality.Bone4 : 
			skinQuality == GPUSkinningQuality.Bone2 ? GPUSkinningQuality.Bone4 : 
			skinQuality == GPUSkinningQuality.Bone4 ? GPUSkinningQuality.Bone2 : GPUSkinningQuality.Bone1;

		shaderStr = Regex.Replace(shaderStr, @"_\$" + removalQuality1 + @"[\s\S]*?" + removalQuality1 + @"\$_", string.Empty);
		shaderStr = Regex.Replace(shaderStr, @"_\$" + removalQuality2 + @"[\s\S]*?" + removalQuality2 + @"\$_", string.Empty);
		shaderStr = shaderStr.Replace("_$" + skinQuality, string.Empty);
		shaderStr = shaderStr.Replace(skinQuality + "$_", string.Empty);

		return shaderStr;
	}

    private GPUSkinningBone GetBoneByTransform(Transform transform)
	{
		GPUSkinningBone[] bones = gpuSkinningAnimation.bones;
		int numBones = bones.Length;
        for(int i = 0; i < numBones; ++i)
        {
            if(bones[i].transform == transform)
            {
                return bones[i];
            }
        }
        return null;
	}

    private int GetBoneIndex(GPUSkinningBone bone)
    {
        return System.Array.IndexOf(gpuSkinningAnimation.bones, bone);
    }

	public static void ShowDialog(string msg)
	{
		EditorUtility.DisplayDialog("GPUSkinning", msg, "OK");
	}

	private void SaveUserPreferDir(string dirPath)
	{
		PlayerPrefs.SetString("GPUSkinning_UserPreferDir", dirPath);
	}

	private string GetUserPreferDir()
	{
		return PlayerPrefs.GetString("GPUSkinning_UserPreferDir", Application.dataPath);
	}

    public static void WriteTempData(string key, string value)
    {
        PlayerPrefs.SetString(key, value);
    }

    public static string ReadTempData(string key)
    {
        return PlayerPrefs.GetString(key, string.Empty);
    }

    public static void DeleteTempData(string key)
    {
        PlayerPrefs.DeleteKey(key);
    }
#endif
}
