using UnityEngine;
using System.Collections;
using UnityEditor;

[RequireComponent(typeof(Camera))]
[ExecuteInEditMode]
public class StyleBlitImageEffect : MonoBehaviour
{
    private Shader shader;
    //public Texture2D StyleTexture;
    public Texture2D[] StyleTextures;
    public Texture2D LUTTexture;
    public Texture2D SourceNormalTexture;
    public Texture2D NoiseTexture;

    [Range(0.01f, 0.2f)]
    public float Fragmentation = 0.07f;
    [Range(0.0f, 5.0f)]
    public float Smoothness = 2.0f;

    public Color BackgroundColor = Color.white;

    private Material _material;
    
    void Start()
    {

        Shader materialIdShader = Shader.Find("Unlit/MaterialID");
        foreach(Renderer r in FindObjectsOfType<Renderer>())
        {
            foreach(Material m in r.sharedMaterials)
            {
                if(m.shader == materialIdShader)
                {
                    //Debug.Log(m.name);
                    m.SetInt("_StyleCount", StyleTextures.Length);
                }
            }
        }

        int w = StyleTextures[0].width;
        int h = StyleTextures[0].height;

        Texture2DArray stylesArray = new Texture2DArray(w, h, StyleTextures.Length, StyleTextures[0].format, false, false);
        for (int i = 0; i < StyleTextures.Length; i++)
        {
            for (int j = 0; j < StyleTextures[i].mipmapCount; j++)
            {
                Graphics.CopyTexture(StyleTextures[i], 0, j, stylesArray, i, j);
            }
        }
        stylesArray.Apply(false, false);

        GetComponent<Camera>().depthTextureMode = DepthTextureMode.DepthNormals;
        if (_material == null)
        {
            shader = Shader.Find("StyleBlitImageEffect");
            _material = new Material(shader);
            _material.hideFlags = HideFlags.DontSave;
        }

        _material.SetTexture("sourceStyles", stylesArray);        

        SetParams();
    }

    void Update()
    {
    #if UNITY_EDITOR
        SetParams();
    #endif
    }

    // Called by the camera to apply the image effect
    void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        RenderTexture temp = RenderTexture.GetTemporary(
        source.width,
        source.height, 0,
        source.format);

        Graphics.Blit(source, temp, _material, 0);

        _material.SetTexture("styleMask", source);
        Graphics.Blit(temp, destination, _material, 1);
        //Graphics.Blit(source, destination, _material, 0);
        RenderTexture.ReleaseTemporary(temp);
    }

    private void SetParams()
    {
        _material.SetFloat("_threshold", Fragmentation);
        _material.SetFloat("_votespan", Smoothness);
        _material.SetColor("_bgColor", BackgroundColor);
        //_material.SetTexture("sourceStyle", StyleTexture);
        _material.SetTexture("normalToSourceLUT", LUTTexture);
        _material.SetTexture("sourceNormals", SourceNormalTexture);
        _material.SetTexture("noiseTexture", NoiseTexture);
        _material.SetInt("styleCount", StyleTextures.Length);
    }
}