using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GPUSkinningMaterial
{
    public Material[] materials = null;

    public GPUSkinningExecuteOncePerFrame executeOncePerFrame = new GPUSkinningExecuteOncePerFrame();

    public void Destroy()
    {
        if (materials != null)
        {
            for (int i = 0; i < materials.Length; i++)
            {
                Object.Destroy(materials[i]);
                materials[i] = null;
            }
            materials = null;
        }
    }
}
