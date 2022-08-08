using System;
using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using Debug = UnityEngine.Debug;
#if UNITY_IOS
using UnityEditor.iOS.Xcode;
#endif
using System.Text;

/// <summary>
/// 빌드 전 처리, 후 처리
/// </summary>
/// <seealso cref="ProjectBuilder"/>
public class BuildProcessor : IPreprocessBuildWithReport, IPostprocessBuildWithReport
#if UNITY_ANDROID
    , UnityEditor.Android.IPostGenerateGradleAndroidProject
#endif
{
    public int callbackOrder => 100000;

    public void OnPostGenerateGradleAndroidProject(string path)
    {
        Debug.Log($"BuildProcessor::OnPostGenerateGradleAndroidProject(string), {path}");

        var propertiesPath = Path.Combine(path, "gradle.properties");
        if (File.Exists(propertiesPath))
        {
            var template = File.ReadAllText(propertiesPath, Encoding.UTF8);            
            var builder = new StringBuilder(template);
            builder.AppendLine("artifactoryUser=villians-build-agent");
            builder.AppendLine("artifactoryPassword = AKCp8kqWvG9Sa1LZMtKF3hQB6raGgfwRunngnvdywvjjFTNw9JNGRqDkbynSX4D1PbbV9wdWv");
            File.WriteAllText(propertiesPath, builder.ToString(), Encoding.UTF8);
        }
        else
        {
            var builder = new StringBuilder();
            builder.AppendLine("artifactoryUser=villians-build-agent");
            builder.AppendLine("artifactoryPassword = AKCp8kqWvG9Sa1LZMtKF3hQB6raGgfwRunngnvdywvjjFTNw9JNGRqDkbynSX4D1PbbV9wdWv");
            File.WriteAllText(propertiesPath, builder.ToString(), Encoding.UTF8);
        }
    }

#if UNITY_EDITOR
    [MenuItem("Build/Test/iOS/Post Process")]
#endif
    public static void TestPostProcess()
    {
        PostProcess(Path.Combine(Environment.CurrentDirectory, "XcodeProject"));
    }

    public static void PostProcess(string outputPath)
    {
        Debug.Log($"PostProcess: {outputPath}");

#if UNITY_IOS && !DISABLE_POSTPROCESS_BUILD
        var projectPath = PBXProject.GetPBXProjectPath(outputPath);
        var project = new PBXProject();

        project.ReadFromFile(projectPath);

        Debug.Assert(project != null, "project");

        // Get main target guid
        var targetGuid = project.GetUnityMainTargetGuid();
        Debug.Assert(targetGuid != null, "targetGuid");
        Debug.Log($"targetGuid: {targetGuid}");

        var phases = project.GetAllBuildPhasesForTarget(targetGuid);
        foreach (var phase in phases)
        {
            Debug.Log($"Type: {project.GetBuildPhaseType(phase)}, Name: {project.GetBuildPhaseName(phase)} ({phase})");
        }

        // Add GoogleService-Info.plist to Resources build phase
        var resourcesBuildPhase = project.GetResourcesBuildPhaseByTarget(targetGuid);
        if (resourcesBuildPhase == null)
        {
            resourcesBuildPhase = project.AddResourcesBuildPhase(targetGuid);
            Debug.Log($"Try Add Resources Build Phase, Result: {resourcesBuildPhase != null}");
        }

        if (resourcesBuildPhase == null)
        {
            foreach (var phase in phases)
            {
                if (project.GetBuildPhaseType(phase) == "PBXResourcesBuildPhase")
                {
                    resourcesBuildPhase = phase;
                    break;
                }
            }

            if (resourcesBuildPhase == null)
            {
                Debug.Log("Not Found Resources Build Phase");
            }
        }

        Debug.Log($"resourcesBuildPhase: {resourcesBuildPhase}");

        var realPath = Path.Combine(Environment.CurrentDirectory, "Assets/Plugins/iOS/GoogleService-Info.plist");
        Debug.Log(realPath);

        // var resourcesFileGuid = project.FindFileGuidByRealPath(realPath, PBXSourceTree.Source);
        // project.FindFileGuidByRealPath()
        var resourcesFileGuid = project.FindFileGuidByRealPath(realPath, PBXSourceTree.Absolute);

        if (resourcesFileGuid != null)
        {
            Debug.Assert(resourcesBuildPhase != null, "resourcesBuildPhase");
            Debug.Assert(resourcesFileGuid != null, "resourcesFileGuid");

            project.AddFileToBuildSection(targetGuid, resourcesBuildPhase, resourcesFileGuid);
            project.AddFrameworkToProject(targetGuid, "StoreKit.framework", false);
            project.WriteToFile(projectPath);
        }

        // Load Plist
        var plistPath = Path.Combine(outputPath, "Info.plist");
        var plist = new PlistDocument();

        plist.ReadFromFile(plistPath);

        // URL Types
        var urlTypes = plist.root["CFBundleURLTypes"] as PlistElementArray;

        if (urlTypes == null)
            plist.root["CFBundleURLTypes"] = new PlistElementArray();

        // Google sign-in URL Type (FirebaseAuth가 없어서 직접 추가)
        var googleSignInUrl1 = urlTypes.AddDict();
        googleSignInUrl1["CFBundleURLName"] = new PlistElementString("com.birdletter.villains");

        var googleSignInUrl1Schemes = googleSignInUrl1.CreateArray("CFBundleURLSchemes");
        googleSignInUrl1Schemes.AddString("com.birdletter.villains");

        var googleSignInUrl2 = urlTypes.AddDict();
        googleSignInUrl2["CFBundleURLName"] = new PlistElementString("google");

        var googleSignInUrl2Schemes = googleSignInUrl2.CreateArray("CFBundleURLSchemes");
        googleSignInUrl2Schemes.AddString("com.googleusercontent.apps.1082723771061-79uf33pkhrf7onegafd97mp1r54jdt8a");

        // Save Plist
        plist.WriteToFile(plistPath);

        // Entitlements
        var entitlements = new ProjectCapabilityManager(projectPath
            , "Entitlements.entitlements"
            , null
            , project.GetUnityMainTargetGuid());

        entitlements.AddSignInWithApple();

        entitlements.WriteToFile();
#endif
    }

    public void OnPostprocessBuild(BuildReport report)
    {
        Debug.Log("BuildProcessor::OnPostprocessBuild(BuildReport)");
        Debug.Log($"report.summary.outputPath: {report.summary.outputPath}");
        PostProcess(report.summary.outputPath);
    }

    public void OnPreprocessBuild(BuildReport report)
    {
        Debug.Log("BuildProcessor::OnPreprocessBuild(BuildReport)");

#if UNITY_ANDROID
        PlayerSettings.Android.keystorePass = "qmfhtm01";
        PlayerSettings.Android.keyaliasName = "villains";
        PlayerSettings.Android.keyaliasPass = "qmfhtm01";
#endif

//#if !DISABLE_PREPROCESS_BUILD
//        if (report.summary.totalErrors == 0)
//            BuildQuantumCode();
//#endif
    }

    public static void BuildQuantumCode()
    {
        Debug.Log("BuildProcessor::BuildQuantumCode()");

        var process = new Process();

        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.WorkingDirectory = Path.Combine(Directory.GetCurrentDirectory(), "../ProjectMQuantum");

#if UNITY_EDITOR_WIN
        // vswhere.exe를 사용하여 MSBuild.exe의 경로를 찾는다
        var vswhereProcess = new Process();

        vswhereProcess.StartInfo.UseShellExecute = false;
        vswhereProcess.StartInfo.CreateNoWindow = true;
        vswhereProcess.StartInfo.RedirectStandardOutput = true;
        vswhereProcess.StartInfo.FileName = Path.Combine(Directory.GetCurrentDirectory(), "Tools\\vswhere.exe");
        vswhereProcess.StartInfo.Arguments = "-latest -requires Microsoft.Component.MSBuild -find MSBuild\\**\\Bin\\MSBuild.exe";

        vswhereProcess.Start();
        vswhereProcess.WaitForExit();

        var msbuildPath = vswhereProcess.StandardOutput.ReadLine();

        if (!File.Exists(msbuildPath))
        {
            Debug.LogWarningFormat("'msbuild.exe' not found! (exceptedPath={0})", msbuildPath);
            return;
        }

        process.StartInfo.UseShellExecute = true;
        process.StartInfo.RedirectStandardOutput = false;
        process.StartInfo.FileName = msbuildPath;
        process.StartInfo.Arguments = "quantum_code.sln /p:Configuration=Release";
#elif UNITY_EDITOR_OSX
        const string monoCommandsPath = "/Library/Frameworks/Mono.framework/Versions/Current/Commands";

        process.StartInfo.UseShellExecute = false;
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.FileName = monoCommandsPath + "/msbuild";
        process.StartInfo.Arguments = "./quantum_code.sln /p:Configuration=Release";
        process.StartInfo.EnvironmentVariables["PATH"] += ":" + monoCommandsPath;
#else
        return;
#endif

        process.Start();
        process.WaitForExit();

        if (process.StartInfo.RedirectStandardOutput)
        {
            if (process.ExitCode == 0)
                Debug.Log(process.StandardOutput.ReadToEnd());
            else
                Debug.LogError(process.StandardOutput.ReadToEnd());
        }
    }
}