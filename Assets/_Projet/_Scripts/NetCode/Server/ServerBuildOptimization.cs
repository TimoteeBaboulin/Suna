using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.Rendering;
#endif
using UnityEngine;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
public class ServerBuildOptimization : IPreprocessComputeShaders, IPreprocessShaders, IProcessSceneWithReport
{
    public int callbackOrder => 0;

    //public void OnPreprocessBuild(BuildReport report)
    //{
    //    if (BuildPipeline.isBuildingPlayer &&
    //        EditorUserBuildSettings.activeBuildTarget == BuildTarget.StandaloneLinux64 &&
    //        EditorUserBuildSettings.standaloneBuildSubtarget == StandaloneBuildSubtarget.Server)
    //    {
    //        Debug.Log("MyCustomBuildProcessor.OnPreprocessBuild for target " + report.summary.platform + " at path " + report.summary.outputPath);
    //    }
    //}


    public void OnProcessScene(Scene scene, BuildReport report)
    {
        if (BuildPipeline.isBuildingPlayer &&
            EditorUserBuildSettings.activeBuildTarget == BuildTarget.StandaloneWindows64 &&
            EditorUserBuildSettings.standaloneBuildSubtarget == StandaloneBuildSubtarget.Server)
        {
            // Disable all Camera components
            foreach (var cam in Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))
            {
                Object.DestroyImmediate(cam.gameObject);
            }

            // Disable all light sources
            foreach (var light in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
            {
                Object.DestroyImmediate(light.gameObject);
            }

            // Disable all SkinnedMeshRenderers and MeshRenderers
            foreach (var renderer in Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))
            {
                Object.DestroyImmediate(renderer.gameObject);
            }
        }
    }

    public void OnProcessShader(Shader shader, ShaderSnippetData snippet, IList<ShaderCompilerData> data)
    {
        if (BuildPipeline.isBuildingPlayer &&
            EditorUserBuildSettings.activeBuildTarget == BuildTarget.StandaloneWindows64 &&
            EditorUserBuildSettings.standaloneBuildSubtarget == StandaloneBuildSubtarget.Server)
        {

            // Allow HDRP and required shaders for UI elements or text rendering
            if (shader.name.Contains("HDRP") || shader.name.Contains("ProBuilder") || shader.name.Contains("Shader Graphs"))
            {
                Debug.Log($"Keeping shader: {shader.name}");
                return;
            }

            // Strip unnecessary graphical shaders
            Debug.Log($"Stripping shader: {shader.name}");
            data.Clear();
        }
    }
    public void OnProcessComputeShader(ComputeShader shader, string kernelName, IList<ShaderCompilerData> data)
    {
        if (BuildPipeline.isBuildingPlayer &&
            EditorUserBuildSettings.activeBuildTarget == BuildTarget.StandaloneWindows64 &&
            EditorUserBuildSettings.standaloneBuildSubtarget == StandaloneBuildSubtarget.Server)
        {
            // Allow HDRP and required shaders for UI elements or text rendering
            if (shader.name.Contains("HDRP") || shader.name.Contains("ProBuilder") || shader.name.Contains("Shader Graphs"))
            {
                Debug.Log($"Keeping shader: {shader.name}");
                return;
            }

            // Strip unnecessary graphical shaders
            Debug.Log($"Stripping shader: {shader.name}");
            data.Clear();
        }
    }
}
#endif



