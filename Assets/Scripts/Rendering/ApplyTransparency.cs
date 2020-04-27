using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ApplyTransparency : MonoBehaviour
{
    public float Opacity;
    public Material Mat;

    void OnRenderImage(RenderTexture src, RenderTexture dst) {
        Mat.SetFloat("_Opacity", Opacity);
        Graphics.Blit(src, dst, Mat);
    }
}
