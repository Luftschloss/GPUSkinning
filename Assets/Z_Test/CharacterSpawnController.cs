using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CharacterSpawnController : MonoBehaviour
{
    public int rawSize = 100;

    public float space = 2;

    public GameObject defaultCharacter;
    public int defaultNum;

    public GameObject gpuSkinningCharacter;
    public int gpuSkinningNum;

    private List<GameObject> defaultArray = new List<GameObject>();

    private List<GameObject> gpuSkinningArray = new List<GameObject>();

    public Toggle toggle;


    private void Awake()
    {
        Application.targetFrameRate = 45;
    }

    private void OnEnable()
    {
        if (toggle)
            toggle.onValueChanged.AddListener(SetGPUInstancing);
    }

    private void OnDisable()
    {
        if (toggle)
            toggle.onValueChanged.RemoveListener(SetGPUInstancing);
    }

    public void GenerateDefault()
    {
        GenerateCharacter(true);
    }

    public void GenerateGPUSkinning()
    {
        GenerateCharacter(false);
    }

    private void GenerateCharacter(bool isDefault)
    {
        int start = isDefault ? defaultArray.Count : gpuSkinningArray.Count;
        GameObject prefab = isDefault ? defaultCharacter : gpuSkinningCharacter;
        if (prefab == null)
        {
            string s = isDefault ? "DefaulGO" : "GPUSkinningGO";
            Debug.Log(s + " is Null");
            return;
        }
        GameObject go;
        for (int i = 0; i < defaultNum; i++)
        {
            go = Instantiate(prefab, GetPosByIndex(start + i, isDefault), Quaternion.identity);
            if (isDefault)
                defaultArray.Add(go);
            else
                gpuSkinningArray.Add(go);
        }
    }

    public void ClearDefalut()
    {
        for (int i = 0; i < defaultArray.Count; i++)
        {
            Destroy(defaultArray[i]);
        }
        defaultArray.Clear();
    }

    public void ClearGPUSkinning()
    {
        for (int i = 0; i < gpuSkinningArray.Count; i++)
        {
            Destroy(gpuSkinningArray[i]);
        }
        gpuSkinningArray.Clear();
    }

    public void CLearAll()
    {
        ClearDefalut();
        ClearGPUSkinning();
    }

    public void Clear(OperationType type)
    {
        switch (type)
        {
            case OperationType.Default:
                ClearDefalut();
                break;
            case OperationType.Skinnning:
                ClearGPUSkinning();
                break;
            case OperationType.All:
                ClearDefalut();
                ClearGPUSkinning();
                break;
            default:
                break;
        }
    }

    private void ResetDefalutGrid()
    {
        for (int i = 0; i < defaultArray.Count; i++)
        {
            defaultArray[i].transform.position = GetPosByIndex(i, true);
        }
    }

    private void ResetSkinningGrid()
    {
        for (int i = 0; i < gpuSkinningArray.Count; i++)
        {
            gpuSkinningArray[i].transform.position = GetPosByIndex(i, false);
        }
    }

    public void ResetGrid(OperationType type)
    {
        switch (type)
        {
            case OperationType.Default:
                ResetDefalutGrid();
                break;
            case OperationType.Skinnning:
                ResetSkinningGrid();
                break;
            case OperationType.All:
                ResetDefalutGrid();
                ResetSkinningGrid();
                break;
            default:
                break;
        }
    }
    float midOffset = 2;
    private Vector3 GetPosByIndex(int index, bool isDefault)
    {
        int sign = isDefault ? 1 : -1;
        return new Vector3(sign * (space * (index / rawSize) + midOffset), 0, space * (index % rawSize));
    }

    public void SetGPUInstancing(bool open)
    {
        if (gpuSkinningArray.Count > 0)
            gpuSkinningArray[0].GetComponent<MeshRenderer>().sharedMaterial.enableInstancing = open;
        if (defaultArray.Count > 0)
            defaultArray[0].GetComponentInChildren<SkinnedMeshRenderer>().sharedMaterial.enableInstancing = open;
    }
}

public enum OperationType
{
    Default = 1,
    Skinnning = 2,
    All = 4,
}