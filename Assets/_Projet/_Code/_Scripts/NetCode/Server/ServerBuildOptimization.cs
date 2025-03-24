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
            LightmapSettings.lightmaps = null;
            LightmapSettings.lightProbes = null;
            var lights = GameObject.Find("Lighting");
            if (lights != null)
            {
                Object.DestroyImmediate(lights);
            }
            Debug.Log("in light");
        }
    }

    public void OnProcessShader(Shader shader, ShaderSnippetData snippet, IList<ShaderCompilerData> data)
    {
        if (BuildPipeline.isBuildingPlayer &&
            EditorUserBuildSettings.activeBuildTarget == BuildTarget.StandaloneWindows64 &&
            EditorUserBuildSettings.standaloneBuildSubtarget == StandaloneBuildSubtarget.Server)
        {

            if (shader == null)
            {
                Debug.Log("in process shader");
                return;
            }
            data.Clear();
        }
    }
    public void OnProcessComputeShader(ComputeShader shader, string kernelName, IList<ShaderCompilerData> data)
    {
        if (BuildPipeline.isBuildingPlayer &&
            EditorUserBuildSettings.activeBuildTarget == BuildTarget.StandaloneWindows64 &&
            EditorUserBuildSettings.standaloneBuildSubtarget == StandaloneBuildSubtarget.Server)
        {
            if (shader == null)
            {
                Debug.Log("in process compute shader");
                return;
            }
            data.Clear();
        }
    }
}
#endif



