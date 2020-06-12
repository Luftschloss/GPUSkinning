using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GPUSkinningShadowManager : MonoBehaviour
{
    public MeshRenderer skin;

    public Vector3 lightDir;

    public float groundHeight;

    public Color shadowColor;

    [Range(0, 1)]
    public float shadowFalloff = 0;

    private void Update()
    {
        if (skin == null)
        {
            Debug.LogWarning("No Mesh");
            return;
        }

        if(skin.material == null)
        {
            Debug.LogWarning("No Material");
            return;
        }
        Debug.Log(11111);
        skin.sharedMaterial.SetColor("_Color", shadowColor);
        skin.sharedMaterial.SetVector("_LightDir", new Vector4(lightDir.x, lightDir.y, lightDir.z, groundHeight));
        skin.sharedMaterial.SetFloat("_ShadowFalloff", shadowFalloff);
    }
}