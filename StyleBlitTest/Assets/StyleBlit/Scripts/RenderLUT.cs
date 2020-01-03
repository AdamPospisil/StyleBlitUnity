using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;

public class RenderLUT : MonoBehaviour
{
    [DllImport("/Assets/alglibnet2.dll")]
    private static extern void kdtreebuild(double[,] xy, int nx, int ny, int normtypeout, out alglib.kdtree kdt, alglib.xparams _params);
    [DllImport("/Assets/alglibnet2.dll")]
    private static extern int kdtreequeryknn(alglib.kdtree kdt, double[] x, int k, alglib.xparams _params);

    // saves texture as .png image
    private static void saveTexture(string path, Texture2D tex)
    {
        byte[] bytes = tex.EncodeToPNG();
        Debug.Log("Texture saved in " + path);
        File.WriteAllBytes(path, bytes);
    }

    // generates look-up table texture
    // mapping translation from normal value to style texture coordinate using ann alg
    private static void render(Texture2D source, ref Texture2D lut)
    {
        Texture2D translator = new Texture2D(source.width, source.height, TextureFormat.ARGB32, false);
        double[,] points = new double[source.width * source.height, 2];
        int i = 0;
        for (int y = 0; y < source.height; y++)
        {
            for (int x = 0; x < source.width; x++)
            {
                Color c = source.GetPixel(x, y);
                points[i, 0] = c.r;
                points[i, 1] = c.g;
                translator.SetPixel((int)(points[i, 0] * source.width), (int)(points[i, 1] * source.height), new Color(x / (float)source.width, y / (float)source.height, 0, 1));
                i++;
            }
        }

        translator.Apply();

        alglib.kdtree kdt;
        alglib.kdtreebuild(points, 2, 0, 2, out kdt);
        double[,] result = new double[0, 0];

        for (int y = 0; y < source.height; y++)
        {
            for (int x = 0; x < source.width; x++)
            {
                double[] q = new double[] { x / (double)source.width, y / (double)source.height };
                alglib.kdtreequeryknn(kdt, q, 1);
                alglib.kdtreequeryresultsx(kdt, ref result);

                double[] nn = { result[0, 0], result[0, 1] };
                Color tint = translator.GetPixel((int)(nn[0] * source.width), (int)(nn[1] * source.height));

                lut.SetPixel(x, y, tint);
            }
        }
        lut.Apply();
    }

    public static void createTexture(string path, Texture2D sourceNormal)
    {
        Texture2D lut = new Texture2D(sourceNormal.width, sourceNormal.height, TextureFormat.RGBAFloat, false);

        render((Texture2D)sourceNormal, ref lut);

        saveTexture(path, lut);
    }
}
