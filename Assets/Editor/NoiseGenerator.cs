using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Diagnostics;
using System;

public class NoiseGeneratorWindow : EditorWindow
{
    private enum NoiseType {
        White, Value, // Simple noises
        Perlin, Simplex, // Texture generation noises
        // Volumetric noises
        // Cellular noises
        FBM, // Composite noises
    }

    private int selectedTab = 0; // 0 = 2D Noise, 1 = 3D Noise
    float slice = 0.0f; // For 3D noise preview
    private NoiseType selectedNoiseType = NoiseType.Perlin;

    private float generationTime = 0.0f;

    private Noise noise;

    // Common settings
    private int resolution = 256;
    private int seed = 42;

    // Noise-specific settings
    //Value
    InterpolationType interpolationType = InterpolationType.Linear;
    private uint verticeCount = 16;

    // Perlin / Simplex
    private float scale = 0.025f;
    private int loop = 0;

    // FBM
    private int octaves = 8;
    private float lacunarity = 2.0f;
    private float persistence = 0.5f;
    private NoiseType fbmNoiseType = NoiseType.Perlin;
    private Noise fbmNoise = null;

    //Previews
    private Texture2D previewTexture2D;
    private Texture3D previewTexture3D;

    private Dictionary<NoiseType, System.Action> noiseSettingsDrawers;

    [MenuItem("Tools/Noise/Noise Generator")]
    public static void ShowWindow()
    {
        GetWindow<NoiseGeneratorWindow>("Noise Generator");
    }

    private void InstantiateSelectedNoise(NoiseType noiseType)
    {
        switch (noiseType)
        {
            case NoiseType.White:
                noise = new White();
                break;
            case NoiseType.Value:
                noise = new Value(verticeCount, interpolationType);
                break;
            case NoiseType.Perlin:
                noise = new Perlin(seed, loop);
                break;
            case NoiseType.Simplex:
                noise = new Simplex(scale);
                break;
            case NoiseType.FBM:
                if (fbmNoise == null)
                {
                    fbmNoise = new Perlin(seed, loop);
                }
                noise = new FBM(fbmNoise, octaves, lacunarity, persistence);
                break;
        }
    }

    private void UpdateNoiseParameters(NoiseType noiseType)
    {
        switch (noiseType)
        {
            case NoiseType.White:
                noise.SetParameters();
                break;
            case NoiseType.Value:
                noise.SetParameters(verticeCount, interpolationType, selectedTab);
                break;
            case NoiseType.Perlin:
                noise.SetParameters(scale, loop);
                break;
            case NoiseType.Simplex:
                noise.SetParameters(scale);
                break;
            case NoiseType.FBM:
                if (fbmNoise == null)
                    break;

                if(fbmNoise is Perlin)
                    noise.SetParameters(fbmNoise, octaves, lacunarity, persistence, scale, loop);
                else if (fbmNoise is Simplex)
                    noise.SetParameters(fbmNoise, octaves, lacunarity, persistence, scale);
                else if (fbmNoise is White)
                    noise.SetParameters(fbmNoise, octaves, lacunarity, persistence);
                else if (fbmNoise is Value)
                    noise.SetParameters(fbmNoise, octaves, lacunarity, persistence, verticeCount, interpolationType, selectedNoiseType);

                break;
        }
    }

    private void OnEnable()
    {
        noiseSettingsDrawers = new Dictionary<NoiseType, System.Action>
        {
            { NoiseType.White, DrawWhiteNoiseSettings },
            { NoiseType.Value, DrawValueSettings },
            { NoiseType.Perlin, DrawPerlinSettings },
            { NoiseType.Simplex, DrawSimplexSettings },
            { NoiseType.FBM, DrawFBMSettings }
        };

        InstantiateSelectedNoise(selectedNoiseType);
    }

    private void OnGUI()
    {
        GUILayout.Label("Noise Generator", EditorStyles.boldLabel);

        selectedTab = GUILayout.Toolbar(selectedTab, new string[] { "2D Noise", "3D Noise" });

        EditorGUILayout.Space();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.BeginVertical();

        // Common settings
        DrawCommonSettings();
        EditorGUILayout.Space();

        // Noise-specific settings

        noiseSettingsDrawers[selectedNoiseType]?.Invoke();

        EditorGUILayout.Space(20);

        if (GUILayout.Button("Generate Noise"))
        {
            GenerateNoise();
        }

        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(20);

        EditorGUILayout.BeginVertical(GUILayout.Width(256));
        RenderPreview();

        EditorGUILayout.Space();

        if (previewTexture2D != null && GUILayout.Button("Save as Asset"))
        {
            SaveNoiseAsAsset();
        }
        EditorGUILayout.EndVertical();
        EditorGUILayout.EndHorizontal();
    }

    private void DrawCommonSettings()
    {
        GUILayout.Label("Common Settings", EditorStyles.boldLabel);
        resolution = EditorGUILayout.IntField("Resolution", resolution);
        seed = EditorGUILayout.IntField("Seed", seed);

        NoiseType prevNoiseType = selectedNoiseType;
        selectedNoiseType = (NoiseType)EditorGUILayout.EnumPopup("Noise Type", selectedNoiseType);

        if (prevNoiseType != selectedNoiseType)
        {
            InstantiateSelectedNoise(selectedNoiseType);
        }
    }

    private void DrawWhiteNoiseSettings()
    {
        GUILayout.Label("White Noise Settings", EditorStyles.boldLabel);
        // No extra settings needed for white noise
    }

    private void DrawValueSettings()
    {
        GUILayout.Label("Value Noise Settings", EditorStyles.boldLabel);
        verticeCount = (uint)EditorGUILayout.IntField("Vertice Count", (int)verticeCount);
        interpolationType = (InterpolationType)EditorGUILayout.EnumPopup("Interpolation Type", interpolationType);

        if(verticeCount < 1 || verticeCount > resolution || resolution % verticeCount != 0)
        {
            EditorGUILayout.HelpBox("Vertice count must be a factor of the resolution", MessageType.Error);
        }
    }

    private void DrawPerlinSettings()
    {
        GUILayout.Label("Perlin Noise Settings", EditorStyles.boldLabel);
        scale = EditorGUILayout.FloatField("Scale", scale);
        loop = EditorGUILayout.IntField("Loop", loop);

        EditorGUILayout.HelpBox("Keep the loop value to 0 if you don't want the noise to loop", MessageType.Info);
    }


    private void DrawSimplexSettings()
    {
        GUILayout.Label("Simplex Noise Settings", EditorStyles.boldLabel);
        scale = EditorGUILayout.FloatField("Scale", scale);
    }

    private void DrawFBMSettings()
    {
        GUILayout.Label("Fractal Brownian Motion Settings", EditorStyles.boldLabel);
        octaves = EditorGUILayout.IntField("Octaves", octaves);
        lacunarity = EditorGUILayout.FloatField("Lacunarity", lacunarity);
        persistence = EditorGUILayout.FloatField("Persistence", persistence);

        NoiseType prevNoiseType = fbmNoiseType;
        fbmNoiseType = (NoiseType)EditorGUILayout.EnumPopup("Noise Type to use", fbmNoiseType);

        if (fbmNoiseType == NoiseType.FBM)
        {
            EditorGUILayout.HelpBox("Can't nest FBM noise", MessageType.Error);
            return;
        }

        if (prevNoiseType != fbmNoiseType)
        {
            if (fbmNoiseType == NoiseType.Perlin)
            {
                fbmNoise = new Perlin(seed, loop);
            }
            else if (fbmNoiseType == NoiseType.Simplex)
            {
                fbmNoise = new Simplex(scale);
            }
            else if(fbmNoiseType == NoiseType.White)
            {
                fbmNoise = new White();
            }
            else if(fbmNoiseType == NoiseType.Value)
            {
                fbmNoise = new Value(verticeCount, interpolationType);
            }
        }

        noiseSettingsDrawers[fbmNoiseType]?.Invoke();
    }

    private void RenderPreview()
    {
        GUILayout.Label("Preview", EditorStyles.boldLabel);
        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.alignment = TextAnchor.MiddleCenter;
        if (selectedTab == 0 && previewTexture2D != null)
        {
            GUILayout.Label(previewTexture2D, style, GUILayout.Width(256), GUILayout.Height(256));
            if(generationTime > 0) GUILayout.Label("Generation Time: " + generationTime.ToString("F3") + "s");
        }
        else if (selectedTab == 1 && previewTexture3D != null)
        {
            float prevSlice = slice;
            slice = GUILayout.HorizontalSlider(slice, 0f, 1f);
            EditorGUILayout.Space(10);
            int sliceIndex = (int)(slice * resolution);
            
            if (previewTexture2D == null) previewTexture2D = new Texture2D(resolution, resolution);

            if (prevSlice != slice)
            {
                for (int y = 0; y < resolution; y++)
                {
                    for (int x = 0; x < resolution; x++)
                    {
                        previewTexture2D.SetPixel(x, y, previewTexture3D.GetPixel(x, y, sliceIndex));
                    }
                }
                previewTexture2D.Apply();
            }

            GUILayout.Label(previewTexture2D, style, GUILayout.Width(256), GUILayout.Height(256));
            if(generationTime > 0) GUILayout.Label("Generation Time: " + generationTime.ToString("F3") + "s");
        }
        else
        {
            GUILayout.Box("No preview available", style, GUILayout.Width(256), GUILayout.Height(256));
        }
    }

    private void GenerateNoise()
    {
        UnityEngine.Debug.Log("Generating noise...");

        if(selectedTab == 0)
        {
            previewTexture2D = new Texture2D(resolution, resolution);
            previewTexture3D = null;
        }
        else
        {
            previewTexture2D = null;
            previewTexture3D = new Texture3D(resolution, resolution, resolution, TextureFormat.RGBA32, mipChain: false);
        }

        UpdateNoiseParameters(selectedNoiseType);

        Stopwatch sw = Stopwatch.StartNew();

        for (int y = 0; y < resolution; y++)
        {
            for (int x = 0; x < resolution; x++)
            {
                if(selectedTab == 0)
                {
                    float value = noise.Noise2D(x, y);
                    previewTexture2D.SetPixel(x, y, color: new Color(value, value, value));
                }
                else
                {
                    for (int z = 0; z < resolution; z++)
                    {
                        float value3D = noise.Noise3D(x, y, z);
                        previewTexture3D.SetPixel(x, y, z, color: new Color(value3D, value3D, value3D));
                    }
                }
            }
        }

        sw.Stop();
        generationTime = (float)sw.Elapsed.TotalSeconds;

        if (selectedTab == 0)
            previewTexture2D.Apply();
        else
            previewTexture3D.Apply();
    }

    private void SaveNoiseAsAsset()
    {
        string path = EditorUtility.SaveFilePanelInProject("Save Noise Texture", "NoiseTexture", "asset", "Save your generated noise texture");
        if (!string.IsNullOrEmpty(path))
        {
            AssetDatabase.CreateAsset(selectedTab == 0 ? previewTexture2D : previewTexture3D, path);
            UnityEngine.Debug.Log("Saved noise texture at: " + path);
        }
    }
}
