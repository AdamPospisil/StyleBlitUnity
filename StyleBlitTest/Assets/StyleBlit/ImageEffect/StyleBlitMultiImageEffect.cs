using UnityEngine;
using System.Collections;
#if UNITY_EDITOR
using UnityEditor;
#endif

[RequireComponent(typeof(Camera))]
[ExecuteInEditMode]
[DefaultExecutionOrder(2)]
public class StyleBlitMultiImageEffect : MonoBehaviour
{
    private Shader shader;
    //public Texture2D StyleTexture;
    public Texture2D[] StyleTextures;
    public Texture2D LUTTexture;
    public Texture2D SourceNormalTexture;
    public Texture2D NoiseTexture;
    public GameObject maskingCamera;
    public RenderTexture StyleMask;

    [Range(0.01f, 0.2f)]
    public float Fragmentation = 0.07f;
    [Range(0.0f, 5.0f)]
    public float Smoothness = 2.0f;

    //public Color BackgroundColor = Color.white;

    private Material _material;

    private void Reset()
    {
#if UNITY_EDITOR
        LUTTexture = (Texture2D)AssetDatabase.LoadAssetAtPath("Assets/StyleBlit/Material/Shader/Textures/lut.png", typeof(Texture2D));
        SourceNormalTexture = (Texture2D)AssetDatabase.LoadAssetAtPath("Assets/StyleBlit/Material/Shader/Textures/s_normals.png", typeof(Texture2D));
        NoiseTexture = (Texture2D)AssetDatabase.LoadAssetAtPath("Assets/StyleBlit/Material/Shader/Textures/jitter.png", typeof(Texture2D));
        int styleCnt = 7;
        StyleTextures = new Texture2D[styleCnt];
        for(int i = 0; i < styleCnt; i++)
        {
            string stylePath = string.Format("Assets/StyleBlit/Material/StyleTextures/source_{0}.png", i + 1);
            StyleTextures[i] = (Texture2D)AssetDatabase.LoadAssetAtPath(stylePath, typeof(Texture2D));
        }
        if (maskingCamera)
        {
            DestroyImmediate(maskingCamera);
        }
        CreateMaskingCamera();
#endif
    }

    void OnApplicationQuit()
    {
        if (maskingCamera)
        {
            DestroyImmediate(maskingCamera);
        }
        CreateMaskingCamera();
    }

    void Start()
    {
        //Shader materialIdShader = Shader.Find("Unlit/MaterialID");
        foreach (Renderer r in FindObjectsOfType<Renderer>())
        {
            foreach (Material m in r.sharedMaterials)
            {
                if (m.HasProperty("_StyleId"))
                {
                    //Debug.Log(m.name);
                    m.SetInt("_StyleCount", StyleTextures.Length);
                }
            }
        }

        int w = StyleTextures[0].width;
        int h = StyleTextures[0].height;

        Texture2DArray stylesArray = new Texture2DArray(w, h, StyleTextures.Length, StyleTextures[0].format, false, false);
        stylesArray.filterMode = FilterMode.Point;
        stylesArray.wrapMode = TextureWrapMode.Repeat;
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
            shader = Shader.Find("StyleBlitMultiImageEffect");
            _material = new Material(shader);
            _material.hideFlags = HideFlags.DontSave;
        }

        _material.SetTexture("sourceStyles", stylesArray);

        SetParams();

        // masking camera is not set and it either has no parent or if there is a parent it does not have StyleBlitMultiImageEffect component
        if (!maskingCamera && (!transform.parent || !transform.parent.GetComponent<StyleBlitMultiImageEffect>()))
        {
            CreateMaskingCamera();
        }
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

        _material.SetTexture("styleMask", StyleMask);
        Graphics.Blit(source, temp, _material, 0);        
        Graphics.Blit(temp, destination, _material, 1);
        //Graphics.Blit(source, destination, _material, 0);
        RenderTexture.ReleaseTemporary(temp);
    }

    private void CreateMaskingCamera()
    {
        // clone camera
        maskingCamera = Instantiate(gameObject, transform);
        // delete all components except Camera and Transform
        foreach (Component com in maskingCamera.GetComponents<Component>())
        {
            if (com.GetType() != typeof(Camera) && com.GetType() != typeof(Transform))
            {
                if (Application.isPlaying)
                {
                    Destroy(com);
                }
                else
                {
                    DestroyImmediate(com);
                }
            }
        }
        // reset local position/rotation
        maskingCamera.transform.localPosition = Vector3.zero;
        maskingCamera.transform.localRotation = Quaternion.identity;
        maskingCamera.name = "Masking Camera";
        // set script for rendering with replaced shaders
        maskingCamera.AddComponent<SetStyleMask>();

        Camera mc = maskingCamera.GetComponent<Camera>();
        // set clear color to black
        mc.clearFlags = CameraClearFlags.SolidColor;
        mc.backgroundColor = Color.black;
        // create new mask render texture and set camera to render into it
        StyleMask = new RenderTexture(mc.pixelWidth, mc.pixelHeight, 0, RenderTextureFormat.RG16, 0);
        mc.targetTexture = StyleMask;
    }

    private void SetParams()
    {
        _material.SetFloat("_threshold", Fragmentation);
        _material.SetFloat("_votespan", Smoothness);
        //_material.SetColor("_bgColor", BackgroundColor);
        //_material.SetTexture("sourceStyle", StyleTexture);
        _material.SetTexture("normalToSourceLUT", LUTTexture);
        _material.SetTexture("sourceNormals", SourceNormalTexture);
        _material.SetTexture("noiseTexture", NoiseTexture);
        _material.SetInt("styleCount", StyleTextures.Length);
    }
}