using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEditor;
using UnityEngine;

// script for continuous jitter rendering nad fps capping
[DisallowMultipleComponent]
public class Jitters : MonoBehaviour
{
    [Range(1, 120)]
    public int fps = 24;

    // skipping frame in StyleBlit refresh
    [Range(0, 10)]
    public int skipFrame = 1;

    // write-enabled input noise texture
    public Texture m_NoiseInput;

    private static int nFramesSkipped = 0;

    // generates the jitter texture
    // jitter suppresses temporal artifacts - randomizer subject to change
    private void RenderJitterTexture(Texture2D nt)
    {
        //Random.InitState(Time.frameCount);
        for (int y = 0; y < m_NoiseInput.height; y++)
        {
            for (int x = 0; x < m_NoiseInput.width; x++)
            {
                Color c = new Color(Random.value, Random.value, Random.value, Random.value);
                nt.SetPixel(x, y, c);
            }
        }
        nt.Apply();
    }

    // tries to find jitter texture in case none is provided
    private bool ObtainJitterTexture()
    {
        Texture[] texas = (Texture[])Resources.FindObjectsOfTypeAll(typeof(Texture));
        foreach (Texture texture in texas)
        {
            if (texture.name == "jitter")
            {
                m_NoiseInput = texture;
                return (m_NoiseInput);
            }
        }

        return false;
    }

    void Update()
    {
        if (!m_NoiseInput && ObtainJitterTexture())
            return;

        if (nFramesSkipped == skipFrame)
        {
            RenderJitterTexture((Texture2D)m_NoiseInput);
            nFramesSkipped = 0;
        }
        else
        {
            nFramesSkipped++;
        }
    }

    void Start()
    {
        Application.targetFrameRate = fps;
        QualitySettings.vSyncCount = 0;
    }
}
