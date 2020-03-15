using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Camera))]
[ExecuteInEditMode]
public class SetReplacementShader : MonoBehaviour
{
    void Start()
    {
        GetComponent<Camera>().SetReplacementShader(Shader.Find("Hidden/StyleMask"), null);
    }

}
