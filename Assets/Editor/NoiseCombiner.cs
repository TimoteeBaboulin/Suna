using System;
using UnityEditor;
using UnityEngine;

public class NoiseCombiner : EditorWindow
{
    Texture[] textures = new Texture[4];

    [MenuItem("Tools/Noise/Noise Combiner")]
    public static void ShowWindow()
    {
        GetWindow<NoiseCombiner>("Noise Combiner");
    }

    private void OnGUI()
    {
        for(int i = 0; i < textures.Length; i++)
        {
            textures[i] = EditorGUILayout.ObjectField(textures[i], typeof(Texture), false) as Texture;
        }

        // Verify that all textures match in size and format

        for(int i = 1; i < textures.Length; i++)
        {
            if (textures[i] == null)
            {
                continue;
            }

            if (textures[i].width != textures[0].width || textures[i].height != textures[0].height)
            {
                EditorGUILayout.HelpBox("All textures must have the same size", MessageType.Error);
                return;
            }

            if (((textures[i] is Texture2D) && (textures[0] is Texture3D)) || ((textures[i] is Texture3D) && (textures[0] is Texture2D)))
            {
                EditorGUILayout.HelpBox("All textures must be the same type (Texture2D or Texture3D)", MessageType.Error);
                return;
            }
        }

        if (GUILayout.Button("Combine"))
        {
            Combine();
        }
    }

    private void Combine()
    {
        string path = EditorUtility.SaveFilePanelInProject("Save Combined Noise Texture", "NoiseTexture", "asset", "Save your combines noise texture");

        if (!string.IsNullOrEmpty(path))
        {
            if(textures[0] is Texture2D)
            {
                Combine2D(path);
            }
            else
            {
                Combine3D(path);
            }
        }
    }

    private void Combine3D(string path)
    {
        Texture3D combinedNoise = new Texture3D(textures[0].width, textures[0].height, (textures[0] as Texture3D).depth, TextureFormat.RGBA32, false);

        for (int z = 0; z < combinedNoise.depth; z++)
        {
            for (int y = 0; y < combinedNoise.height; y++)
            {
                for (int x = 0; x < combinedNoise.width; x++)
                {
                    Color color = Color.black;
                    color.r = textures[0] != null ? (textures[0] as Texture3D).GetPixel(x, y, z).r : 0;
                    color.g = textures[1] != null ? (textures[1] as Texture3D).GetPixel(x, y, z).r : 0;
                    color.b = textures[2] != null ? (textures[2] as Texture3D).GetPixel(x, y, z).r : 0;
                    color.a = textures[3] != null ? (textures[3] as Texture3D).GetPixel(x, y, z).r : 0;
                    combinedNoise.SetPixel(x, y, z, color);
                }
            }
        }

        combinedNoise.Apply();

        AssetDatabase.CreateAsset(combinedNoise, path);
    }

    private void Combine2D(string path)
    {
        Texture2D combinedNoise = new Texture2D(textures[0].width, textures[0].height, TextureFormat.RGBA32, false);

        for(int y = 0; y < combinedNoise.height; y++)
        {
            for (int x = 0; x < combinedNoise.width; x++)
            {
                Color color = Color.black;
                color.r = textures[0] != null ? (textures[0] as Texture2D).GetPixel(x, y).r : 0;
                color.g = textures[1] != null ? (textures[1] as Texture2D).GetPixel(x, y).r : 0;
                color.b = textures[2] != null ? (textures[2] as Texture2D).GetPixel(x, y).r : 0;
                color.a = textures[3] != null ? (textures[3] as Texture2D).GetPixel(x, y).r : 0;

                combinedNoise.SetPixel(x, y, color);
            }
        }

        combinedNoise.Apply();

        AssetDatabase.CreateAsset(combinedNoise, path);
    }
}
