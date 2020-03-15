using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[RequireComponent(typeof(Renderer))]
[DefaultExecutionOrder(1)]
public class StyleIDScript : MonoBehaviour
{    
    public int StyleId;
   
    void Start()
    {
        foreach (Material m in GetComponent<Renderer>().materials)
        {
            m.SetInt("_StyleId", StyleId);
        }

    }

    private void OnDisable()
    {
        foreach (Material m in GetComponent<Renderer>().materials)
        {
            m.SetInt("_StyleCount", 0);
        }
    }

    private void OnEnable()
    {
        foreach (Material m in GetComponent<Renderer>().materials)
        {
            m.SetInt("_StyleId", StyleId);
            m.SetInt("_StyleCount", FindObjectOfType<StyleBlitMultiImageEffect>().StyleTextures.Length);
        }
    }

}
