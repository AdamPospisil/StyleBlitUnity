using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Camera))]
[ExecuteInEditMode]
public class SetStyleMask : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        GetComponent<Camera>().SetReplacementShader(Shader.Find("Hidden/StyleMask"), null);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
