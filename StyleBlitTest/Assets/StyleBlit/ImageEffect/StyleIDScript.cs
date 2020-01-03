using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Renderer))]
[DefaultExecutionOrder(1)]
public class StyleIDScript : MonoBehaviour
{    
    public int StyleId;
    // Start is called before the first frame update
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



    // Update is called once per frame
    void Update()
    {
        
    }
}
