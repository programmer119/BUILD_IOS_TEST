using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Text;
using System.Text.RegularExpressions;

using UnityEditor;
using UnityEngine;
//using Scopely;
using UnityEditor.AddressableAssets;
using Settings = UnityEditor.AddressableAssets.Settings;

public static class BuildUtils
{
    /// <summary>
    /// 지정된 커맨드 라인에서 해당 파라미터의 값을 반환합니다.
    /// </summary>
    public static string GetParam(string commandLine, string paramName)
    {
        var regex = new Regex(string.Format("(^| )-{0} \"?([^\"]+)\"?( -)?", paramName));
        var matches = regex.Match(commandLine);
        if (matches.Success)
        {
            string[] splits = matches.Groups[2].Value.Split(' ');
            return splits[0];
        }
        
       return string.Empty;
    }
    
    public static bool TryParse(string s, out int result)
    {
        result = 0;
        if (string.IsNullOrEmpty(s))
            return false;
            
       return int.TryParse(s.Replace("@", string.Empty), out result);
    }
    
    public static bool TryGetValue(string commandLine, string paramName, out string value)
    {
        string param = GetParam(commandLine, paramName);
        if (!string.IsNullOrEmpty(param))
        {
            value = param;
            return true;
        }
        
       value = string.Empty;
        return false;
    }
    
    public static bool TryGetValue(string commandLine, string paramName, out bool value)
    {
        string param = GetParam(commandLine, paramName);
        if (bool.TryParse(param, out value))
            return true;
        return false;
    }
    
    public static bool TryGetValue(string commandLine, string paramName, out int value)
    {
        string param = GetParam(commandLine, paramName);
        if (TryParse(param, out value))
            return true;
        return false;
    }

    public static string CombineSymbols(string defineSymbols
        , string jobDefineSymbols
        , string version
        , string version2)
    {
        StringBuilder builder = new StringBuilder();
        if (!string.IsNullOrEmpty(defineSymbols))
        {
            builder.Append(defineSymbols);
            if (false == defineSymbols.EndsWith(";"))
                builder.Append(";");
        }

        if (!string.IsNullOrEmpty(jobDefineSymbols))
        {
            builder.Append(jobDefineSymbols);
            if (false == jobDefineSymbols.EndsWith(";"))
                builder.Append(";");
        }

        if (!string.IsNullOrEmpty(version))
        {
            builder.Append(version);
            builder.Append(";");
        }

        if (!string.IsNullOrEmpty(version2))
        {
            builder.Append(version2);
            builder.Append(";");
        }
        return builder.ToString();
    }

    public static string ToAppVersionDefine(string version)
    {
        return $"APP_VERSION_{version.Replace(".", "_")}";
    }

    public static string ToAppVersionNewerDefine(string versionText)
    {
        // 
        Version version = null;
        try
        {
            version = new Version(versionText);
        }
        catch (Exception e)
        {
            Debug.Log(e.ToString());
        }

        if (version != null)
            return $"APP_VERSION_{version.Major}_{version.Minor}_OR_NEWER";
        
        return string.Empty;
    }

    public static int GetIntValue(this IDictionary dictionary, string paramName, int defaultValue)
    {
        if (dictionary != null && dictionary.Contains(paramName))
        {
            string s = dictionary[paramName].ToString();
            if (int.TryParse(s, out int value))
                return value;
        }
        return defaultValue;
    }

    public static string GetStringValue(this IDictionary dictionary, string paramName, string defaultValue)
    {
        if (dictionary != null && dictionary.Contains(paramName))
        {
            return dictionary[paramName].ToString();
        }
        return defaultValue;
    }

    public static bool GetBooleanValue(this IDictionary dictionary, string paramName, bool defaultValue)
    {
        if (dictionary != null && dictionary.Contains(paramName))
        {
            string s = dictionary[paramName].ToString();
            if (bool.TryParse(s, out bool value))
                return value;
        }
        return defaultValue;
    }
}

public class BuildParams
{
    public const string kVersion = "APP_VERSION";
    public const string kGitBranch = "GIT_BRANCH";
    public const string kGitCommit = "GIT_COMMIT";
    public const string kBuildNumber = "BUILD_NUMBER";
    
    // Job Params
    public const string kBuildScriptsOnly = "BUILD_SCRIPT_ONLY";
    public const string kDevelopment = "BUILD_DEVELOPMENT";
    public const string kBuildAssetBundle = "ASSET_BUNDLE_BUILD";
    public const string kCleanPlayerContent = "CLEAN_PLAYER_CONTENT";
    public const string kUseAPKExpansionFiles = "useAPKExpansionFiles";
    public const string kBuildAppBundle = "buildAppBundle";
    public const string kJobDefineSymbols = "JOB_DEFINE_SYMBOLS";

    // prodMode (Scopely SDK Production Mode)
    public const string kProdMode = "prodMode";
    
    // Global Properties - Environment Variables
    public const string kScriptingDefineSymbols = "DEFINE_SYMBOLS";
    public const string kAndroidBundleVersionCode = "ANDROID_BUNDLE_VERSION_CODE";
    public const string kIL2cpp = "IL2CPP";
    public const string kProvisioningProfileId = "PROVISION_UUID_DISTRIBUTION";

    public const string kGitCommitId = "GIT_COMMIT";

    public string Version { get; private set; }
    public int BundleVersionCode { get; private set; }
    public string DefineSymbols { get; private set; }
    public string JobDefineSymbols { get; private set; }
    public bool IsProductionMode { get; private set; }

    public bool IsBuildScriptOnly { get; private set; }
    public bool IsDevelopmentBuild { get; private set; }

    public bool IsBuildAssetBundle { get; private set; }

    public bool IsCleanPlayerContent { get; private set; }

    // Android Params
    public bool UseAPKExpansionFiles { get; private set; }
    public bool BuildAppBundle { get; private set; }

    // iOS Params
    public string ProvisionProfileId { get; private set; }

    public bool IL2cpp { get; private set; }
    public string GitCommit { get; private set; }
    public int BuildNumber { get; private set; }

    public BuildOptions BuildOptions { get; private set; }

    public BuildParams()
    {
        Version = string.Empty;
        BundleVersionCode = 0;
        DefineSymbols = string.Empty;
        JobDefineSymbols = string.Empty;

        IsProductionMode = false;

        IsBuildScriptOnly = false;
        IsDevelopmentBuild = false;
        IsBuildAssetBundle = false;
        IsCleanPlayerContent = false;

        UseAPKExpansionFiles = false;
        BuildAppBundle = false;

        ProvisionProfileId = string.Empty;

        IL2cpp = false;
        GitCommit = string.Empty;
        BuildNumber = 0;
    }

    public BuildParams(IDictionary variables)
        : this()
    {
        Version = variables.GetStringValue(kVersion, PlayerSettings.bundleVersion);        
        BundleVersionCode = variables.GetIntValue(kAndroidBundleVersionCode, 1);        
        DefineSymbols = variables.GetStringValue(kScriptingDefineSymbols, string.Empty).Trim();
        JobDefineSymbols = variables.GetStringValue(kJobDefineSymbols, string.Empty).Trim();

        IsProductionMode = variables.GetBooleanValue(kProdMode, false);

        IsBuildScriptOnly = variables.GetBooleanValue(kBuildScriptsOnly, false);
        IsDevelopmentBuild = variables.GetBooleanValue(kDevelopment, false);        
        IsBuildAssetBundle = variables.GetBooleanValue(kBuildAssetBundle, false);
        IsCleanPlayerContent = variables.GetBooleanValue(kCleanPlayerContent, false);
        UseAPKExpansionFiles = variables.GetBooleanValue(kUseAPKExpansionFiles, false);
        BuildAppBundle = variables.GetBooleanValue(kBuildAppBundle, false);

        ProvisionProfileId = variables.GetStringValue(kProvisioningProfileId, string.Empty);

        GitCommit = variables.GetStringValue(kGitCommitId, string.Empty);

        IL2cpp = variables.GetBooleanValue(kIL2cpp, true);
        BuildNumber = variables.GetIntValue(kBuildNumber, 1);
    }

    public BuildParams(string commandLine)
        : this()
    {
        Version = BuildUtils.GetParam(commandLine, kVersion);

        string bundlerVersionCode = BuildUtils.GetParam(commandLine, kAndroidBundleVersionCode);
        if (false == string.IsNullOrEmpty(bundlerVersionCode))
        {
            int bunldeVersionCode = -1;
            if (int.TryParse(bundlerVersionCode, out bunldeVersionCode))
                BundleVersionCode = bunldeVersionCode;
        }

        string defineSymbols = BuildUtils.GetParam(commandLine, kScriptingDefineSymbols);
        if (false == string.IsNullOrEmpty(defineSymbols))
            defineSymbols = defineSymbols.Trim();

        DefineSymbols = defineSymbols;

        bool isProductionMode = false;
        BuildUtils.TryGetValue(commandLine, kProdMode, out isProductionMode);
        IsProductionMode = isProductionMode;

        bool isBuildScriptOnly = false;
        BuildUtils.TryGetValue(commandLine, kBuildScriptsOnly, out isBuildScriptOnly);
        IsBuildScriptOnly = isBuildScriptOnly;

        bool isDevelopment = false;
        BuildUtils.TryGetValue(commandLine, kDevelopment, out isDevelopment);
        IsDevelopmentBuild = isDevelopment;

        bool useAPKExpansionFiles = false;
        BuildUtils.TryGetValue(commandLine, kUseAPKExpansionFiles, out useAPKExpansionFiles);
        UseAPKExpansionFiles = useAPKExpansionFiles;

        bool buildAppBundle = false;
        BuildUtils.TryGetValue(commandLine, kBuildAppBundle, out buildAppBundle);
        BuildAppBundle = buildAppBundle;

        bool isIL2cpp = true;
        BuildUtils.TryGetValue(commandLine, kIL2cpp, out isIL2cpp);
        IL2cpp = isIL2cpp;

        int buildNumber = -1;
        BuildUtils.TryGetValue(commandLine, kBuildNumber, out buildNumber);
        BuildNumber = buildNumber;
    }
}

/// <summary>
/// Project Builder (APK/IPA)
/// MenuItem과 Jenkins를 통해서 사용
/// </summary>
/// <seealso cref="BuildProcessor"/>
class ProjectBuilder
{
    public const string kScriptingDefineSymbols = "symbols";    
    public const string kProvisioning = "provisioning";
    
    public static string AndroidSdkRoot
    {
        get { return EditorPrefs.GetString("AndroidSdkRoot"); }
        set { EditorPrefs.SetString("AndroidSdkRoot", value); }
    }
    
    public static string JdkRoot
    {
        get { return EditorPrefs.GetString("JdkPath"); }
        set { EditorPrefs.SetString("JdkPath", value); }
    }
    
    private static string[] Scenes
    {
        get
        {
            return EditorBuildSettings.scenes.Select(x => x.path).ToArray();
        }
    }
    
    private static void EnsureSDKPath()
    {
        if (Application.platform == RuntimePlatform.WindowsEditor)
        {
            if (string.IsNullOrEmpty(AndroidSdkRoot))
                AndroidSdkRoot = "C:/Users/bmw122/AppData/Local/Android/sdk";
            if (string.IsNullOrEmpty(JdkRoot))
                JdkRoot = "C:/Program Files/Java/jdk1.8.0_121";
        }
        else if (Application.platform == RuntimePlatform.OSXEditor)
        {
            if (string.IsNullOrEmpty(AndroidSdkRoot))
                AndroidSdkRoot = "/Users/gamevil/Library/Android/sdk";
            if (string.IsNullOrEmpty(JdkRoot))
                JdkRoot = "/Library/Java/JavVirtualMachines/jdk1.8.0_131.jdk/Contents/Hone";
        }
    }

    public static string GetArchitectureName(int architecture)
    {
        // 0 - ARMv7
        // 1 - ARM64
        // 2 - Universal
        switch (architecture)
        {
            case 0: return "ARMv7";
            case 1: return "ARM64";
            case 2: return "Universal";
            default: return "Unknown Architecture";
        }
    }

    private static void SetScopelySdk(bool isVal)
    {
        // @TODO: 
        //if (isVal)
        //{
        //    // Turn Production Mode On            
        //    Scopely.Core.Editor.ScopelySdkConfigWindow.ProdModeOn();
        //}
        //else
        //{
        //    // Turn Production Mode Off
        //    Scopely.Core.Editor.ScopelySdkConfigWindow.ProdModeOff();
        //}
    }

    private static void BuildPlayer(string[] scenes
        , string targetFileName
        , BuildTargetGroup buildTargetGroup
        , BuildTarget buildTarget
        , BuildOptions buildOptions)
    {
        StringBuilder builder = new StringBuilder();
        builder.AppendLine($"Target File Name:\t\t {targetFileName}");
        builder.AppendLine($"BuildTargetGroup:\t\t {buildTargetGroup}");
        builder.AppendLine($"BuildTarget:\t\t {buildTarget}");
        builder.AppendLine($"BuildOptions:\t\t {buildOptions}");
 
        var architecture = PlayerSettings.GetArchitecture(buildTargetGroup);
        var architectureName = GetArchitectureName(architecture);

        builder.AppendLine($"Architecture:\t\t {architectureName}");
        builder.AppendLine($"App Identifier:\t\t {PlayerSettings.GetApplicationIdentifier(buildTargetGroup)}");
        builder.AppendLine($"AdditionalIl2CppArgs:\t\t {PlayerSettings.GetAdditionalIl2CppArgs()}");
        builder.AppendLine($"ApiCompatibilityLevel:\t\t {PlayerSettings.GetApiCompatibilityLevel(buildTargetGroup)}");
        builder.AppendLine($"Default Scripting Backend:\t\t {PlayerSettings.GetDefaultScriptingBackend(buildTargetGroup)}");
        builder.AppendLine($"Il2Cpp Compiler Config:\t\t {PlayerSettings.GetIl2CppCompilerConfiguration(buildTargetGroup)}");
        builder.AppendLine($"Incremental Il2 Cpp Build:\t\t {PlayerSettings.GetIncrementalIl2CppBuild(buildTargetGroup)}");
        builder.AppendLine($"Managed Stripping Level:\t\t {PlayerSettings.GetManagedStrippingLevel(buildTargetGroup)}");

        var apis = PlayerSettings.GetGraphicsAPIs(buildTarget);
        builder.AppendLine();
        builder.AppendLine("#Graphics APIs");

        if (!(apis == null || apis.Length == 0))
        {
            for (int i = 0; i < apis.Length; i++)
            {
                builder.AppendLine($"{apis[i]}");
            }
        }

        if (buildTargetGroup == BuildTargetGroup.Android)
        {
            builder.AppendLine();
            builder.AppendLine("#Android Settings");
            builder.AppendLine($"Target Architectures:\t\t {PlayerSettings.Android.targetArchitectures}");            
        }
        else if (buildTargetGroup == BuildTargetGroup.iOS)
        {
            builder.AppendLine();
            builder.AppendLine("#iOS Settings");
            builder.AppendLine($"script Call Optimization:\t\t {PlayerSettings.iOS.scriptCallOptimization}");
            builder.AppendLine($"sdk Version:\t\t {PlayerSettings.iOS.sdkVersion}");            
        }

        Debug.Log(builder.ToString());

        EditorUserBuildSettings.SwitchActiveBuildTarget(buildTargetGroup, buildTarget);
        
        UnityEditor.Build.Reporting.BuildReport res = BuildPipeline.BuildPlayer(scenes
            , targetFileName
            , buildTarget
            , buildOptions);

        builder.Length = 0;
        builder.AppendFormat("{0} time taken by the build\r\n", res.summary.totalTime);
        builder.AppendFormat("Number of errors: {0}\r\n", res.summary.totalErrors);
        builder.AppendFormat("Build Result: {0}\r\n", res.summary.result);
        Debug.Log(builder.ToString());
        
       	if (res.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
        {
            builder.Length = 0;
            builder.AppendLine("[LOG] Build Step List : ");
            for (int i = 0; i < res.steps.Length; ++i)
            {
                builder.AppendLine(res.steps[i].ToString());
            }
            Debug.Log(builder.ToString());
            
            throw new Exception("BuildPlayer failure: " + res.summary.result.ToString());            
        }
    }

#if UNITY_EDITOR
    [MenuItem("Build/Clean Player Content")]
    public static void CleanPlayerContent()
    {
        Debug.Log("Clean Player Content");
        Settings.AddressableAssetSettings.CleanPlayerContent(AddressableAssetSettingsDefaultObject.Settings.ActivePlayerDataBuilder);
    }

    [MenuItem("Build/Player Content")]
    public static void BuildPlayerContent()
    {
#if UNITY_EDITOR
        Debug.Log("BuildPlayerContent() in Editor");
        var labels = AddressableAssetSettingsDefaultObject.Settings.GetLabels();
        labels.Sort((x, y) => x.CompareTo(y));

        var builder = new StringBuilder();
        foreach (var label in labels)
        {
            builder.AppendLine(label);
        }

        File.WriteAllText("Assets/Resources/AddressableLabels.txt", builder.ToString(), Encoding.UTF8);
#else
        Debug.Log("BuildPlayerContent()");
#endif
        Settings.AddressableAssetSettings.BuildPlayerContent();
    }

    [MenuItem("Build/Clean And Build Player Content")]
    public static void CleanAndBuildPlayerContent()
    {
        CleanPlayerContent();
        BuildPlayerContent();
    }

    [MenuItem("Build/PC Standalone")]
    public static void BuildPCStandalone()
    {
        Debug.Log("Build PC Standalone Start");

        BuildTargetGroup buildTargetGroup = BuildTargetGroup.Standalone;
        BuildTarget buildTarget = BuildTarget.StandaloneWindows;

#if UNITY_EDITOR
        // if (UnityEditorInternal.InternalEditorUtility.HasPro())
        PlayerSettings.SplashScreen.showUnityLogo = false;
#endif

        // Command Line
        IDictionary variables = Environment.GetEnvironmentVariables();
        LogVariables(variables);

        // string commandLine = Environment.CommandLine;
        BuildParams buildParams = new BuildParams(variables);
        BuildOptions buildOptions = BuildOptions.None;

        if (buildParams.IsBuildScriptOnly)
        {
            buildOptions |= BuildOptions.BuildScriptsOnly;
        }

        if (buildParams.IsDevelopmentBuild)
        {
            buildOptions |= BuildOptions.Development | BuildOptions.ConnectWithProfiler;
        }

        bool isIL2cpp = buildParams.IL2cpp;

        string defineSymbols = BuildUtils.CombineSymbols(buildParams.DefineSymbols
            , buildParams.JobDefineSymbols
            , BuildUtils.ToAppVersionDefine(buildParams.Version)
            , BuildUtils.ToAppVersionNewerDefine(buildParams.Version));

        if (!string.IsNullOrEmpty(defineSymbols))
            PlayerSettings.SetScriptingDefineSymbolsForGroup(buildTargetGroup, defineSymbols);

        PlayerSettings.bundleVersion = buildParams.Version;

        PlayerSettings.SetScriptingBackend(buildTargetGroup, isIL2cpp ? ScriptingImplementation.IL2CPP : ScriptingImplementation.Mono2x);
        // PlayerSettings.Android.targetArchitectures = (isIL2cpp) ? (AndroidArchitecture.ARMv7 | AndroidArchitecture.ARM64) : AndroidArchitecture.ARMv7;
        // PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARMv7;
        // PlayerSettings.Android.targetArchitectures = AndroidArchitecture.All ^ AndroidArchitecture.X86;

        SaveBuildInfo(buildParams.GitCommit, buildParams.BuildNumber);

        string targetFileName = buildTargetGroup.ToString();

        // @TODO: 
        SetScopelySdk(buildParams.IsProductionMode);        

        // Build
        BuildPlayer(Scenes, targetFileName, buildTargetGroup, buildTarget, buildOptions);
    }
#endif

#if UNITY_EDITOR
    [MenuItem("Build/Android")]
#endif
    public static void BuildAndroid()
    {
        Debug.Log("Build Android Start");

        BuildTargetGroup buildTargetGroup = BuildTargetGroup.Android;
        BuildTarget buildTarget = BuildTarget.Android;

#if UNITY_EDITOR
        // if (UnityEditorInternal.InternalEditorUtility.HasPro())
        PlayerSettings.SplashScreen.showUnityLogo = false;
#endif

        // Command Line
        IDictionary variables = Environment.GetEnvironmentVariables();
        LogVariables(variables);

        // string commandLine = Environment.CommandLine;
        BuildParams buildParams = new BuildParams(variables);
        BuildOptions buildOptions = BuildOptions.None;

        if (buildParams.IsBuildScriptOnly)
        {
            buildOptions |= BuildOptions.BuildScriptsOnly;
        }
        
        if (buildParams.IsDevelopmentBuild)
        {
            buildOptions |= BuildOptions.Development | BuildOptions.ConnectWithProfiler;
        }

        bool isIL2cpp = buildParams.IL2cpp;

        string defineSymbols = BuildUtils.CombineSymbols(buildParams.DefineSymbols
            , buildParams.JobDefineSymbols
            , BuildUtils.ToAppVersionDefine(buildParams.Version)
            , BuildUtils.ToAppVersionNewerDefine(buildParams.Version));

        if (!string.IsNullOrEmpty(defineSymbols))
            PlayerSettings.SetScriptingDefineSymbolsForGroup(buildTargetGroup, defineSymbols);

        EditorUserBuildSettings.exportAsGoogleAndroidProject = true;
        EditorUserBuildSettings.androidBuildSystem = AndroidBuildSystem.Gradle;
        EditorUserBuildSettings.androidETC2Fallback = AndroidETC2Fallback.Quality32BitDownscaled;
        PlayerSettings.bundleVersion = buildParams.Version;
        string bundleVersion = buildParams.Version.Replace(".", "0");
        if (int.TryParse(bundleVersion, out var code))
        {
            PlayerSettings.Android.bundleVersionCode = code;
        }
        else
        {
            Debug.LogWarningFormat("Couldn't Parse {0}, BundleVersion has been set to {1}"
                , bundleVersion
                , buildParams.BundleVersionCode);
            PlayerSettings.Android.bundleVersionCode = buildParams.BundleVersionCode;
        }        
        PlayerSettings.Android.useAPKExpansionFiles = buildParams.UseAPKExpansionFiles;        
        EditorUserBuildSettings.buildAppBundle = buildParams.BuildAppBundle;
        
        PlayerSettings.SetScriptingBackend(buildTargetGroup, isIL2cpp ? ScriptingImplementation.IL2CPP : ScriptingImplementation.Mono2x);
        PlayerSettings.Android.targetArchitectures = (isIL2cpp) ? (AndroidArchitecture.ARMv7 | AndroidArchitecture.ARM64) : AndroidArchitecture.ARMv7;
        // PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARMv7;
        // PlayerSettings.Android.targetArchitectures = AndroidArchitecture.All ^ AndroidArchitecture.X86;

        SaveBuildInfo(buildParams.GitCommit, buildParams.BuildNumber);

        if (buildParams.IsBuildAssetBundle)
        {
            if (buildParams.IsCleanPlayerContent)
            {
                CleanPlayerContent();
            }
            BuildPlayerContent();
        }

        string targetFileName = "GradleProject";

        SetScopelySdk(buildParams.IsProductionMode);
        
        // Build
        BuildPlayer(Scenes, targetFileName, buildTargetGroup, buildTarget, buildOptions);
   	}

    private static void LogVariables(IDictionary dictionary)
    {
        StringBuilder builder = new StringBuilder();
        foreach (DictionaryEntry x in dictionary)
        {
            builder.AppendLine($"{x.Key} = {x.Value}");
        }
        File.WriteAllText("EnvironmentVariables.tmp", builder.ToString(), Encoding.UTF8);
    }

#if UNITY_EDITOR
    [MenuItem("Build/iOS")]
#endif
    public static void BuildiOS()
    {
        Debug.Log("Build iOS Start");

        BuildTargetGroup buildTargetGroup = BuildTargetGroup.iOS;
        BuildTarget buildTarget = BuildTarget.iOS;
        
#if UNITY_EDITOR
        // if (UnityEditorInternal.InternalEditorUtility.HasPro())
        PlayerSettings.SplashScreen.showUnityLogo = false;
#endif

        // Command Line
        IDictionary variables = Environment.GetEnvironmentVariables();
        LogVariables(variables);

        // string commandLine = Environment.CommandLine;
        BuildParams buildParams = new BuildParams(variables);
        BuildOptions buildOptions = BuildOptions.None;
        
        if (buildParams.IsBuildScriptOnly)
        {
            buildOptions |= BuildOptions.BuildScriptsOnly;
        }
        
        if (buildParams.IsDevelopmentBuild)
        {
            buildOptions |= BuildOptions.Development | BuildOptions.ConnectWithProfiler;
        }

        string defineSymbols = BuildUtils.CombineSymbols(buildParams.DefineSymbols
            , buildParams.JobDefineSymbols
            , BuildUtils.ToAppVersionDefine(buildParams.Version)
            , BuildUtils.ToAppVersionNewerDefine(buildParams.Version));

        if (!string.IsNullOrEmpty(defineSymbols))
            PlayerSettings.SetScriptingDefineSymbolsForGroup(buildTargetGroup, defineSymbols);

        //PlayerSettings.bundleVersion = buildParams.Version;
        //string bundleVersion = buildParams.Version.Replace(".", "0");
        // if (int.TryParse(bundleVersion, out var code))
        // PlayerSettings.iOS.buildNumber = 

        if (PlayerSettings.GetUseDefaultGraphicsAPIs(BuildTarget.iOS) == false)
        {
            Debug.Log("[LOG] iOS DefaultGraphicsAPIs is false : do svn revert ProjectSettings");

            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.iOS, true);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.iOS, new UnityEngine.Rendering.GraphicsDeviceType[] {
                UnityEngine.Rendering.GraphicsDeviceType.Metal,
                UnityEngine.Rendering.GraphicsDeviceType.OpenGLES2
            });
        }

        PlayerSettings.bundleVersion = buildParams.Version;
        PlayerSettings.iOS.sdkVersion = iOSSdkVersion.DeviceSDK;
        PlayerSettings.iOS.targetOSVersionString = "10.0";        
        PlayerSettings.statusBarHidden = true;

        // PlayerSettings.SetScriptingBackend(buildTargetGroup, ScriptingImplementation.IL2CPP);

        // 0 - ARMv7
        // 1 - ARM64
        // 2 - Universal
        // PlayerSettings.SetArchitecture(BuildTargetGroup.iOS, 2);

        //PlayerSettings.iOS.iOSManualProvisioningProfileID = buildParams.ProvisionProfileId;
        //PlayerSettings.iOS.iOSManualProvisioningProfileType = ProvisioningProfileType.Distribution;

        SaveBuildInfo(buildParams.GitCommit, buildParams.BuildNumber);

        //string targetFileName = "IOS_BUILD";
        //string provisioning = BuildUtils.GetParam(commandLine, kProvisioning);
        //if (!string.IsNullOrEmpty(provisioning))
        //    targetFileName = string.Format("IOS_BUILD_{0}", provisioning);

        string targetFileName = "XcodeProject";
        if (Directory.Exists(targetFileName))
        {
            buildOptions |= BuildOptions.AcceptExternalModificationsToPlayer;
        }

        if (buildParams.IsBuildAssetBundle)
        {
            if (buildParams.IsCleanPlayerContent)
            {
                CleanPlayerContent();
            }
            BuildPlayerContent();
        }

        SetScopelySdk(buildParams.IsProductionMode);
        
        // Build
        BuildPlayer(Scenes, targetFileName, buildTargetGroup, buildTarget, buildOptions);
    }

   	private static void SaveBuildInfo(string commitId, int buildNumber)
    {
        Debug.Log($"Save Commit Id: {commitId}, Build Number: {buildNumber}");

        string filePath = "Assets/Resources/BuildInfo.txt";
        if (false == string.IsNullOrEmpty(commitId))
            File.WriteAllText(filePath, $"{commitId.Substring(0, Math.Min(commitId.Length, 8))} {buildNumber}");
        else
            File.WriteAllText(filePath, $"{buildNumber}");

        AssetDatabase.Refresh();
    }

    public static void ReimportAll()
    {
        AssetDatabase.ImportAsset("Assets", ImportAssetOptions.ImportRecursive);
        AssetDatabase.Refresh();
    }

#if UNITY_EDITOR
    [MenuItem("Build/Test/Print Env")]
#endif
    public static void Test()
    {
        string[] args = Environment.GetCommandLineArgs();
        StringBuilder builder = new StringBuilder();

        builder.AppendLine("#Command Line Args");
        if (!(args == null || args.Length == 0))
        {
            for (int i = 0; i < args.Length; i++)
            {
                builder.AppendLine(args[i]);
            }
            builder.AppendLine();            
        }
        builder.AppendLine();


        builder.AppendLine("#Environment Variables");
        var variables = Environment.GetEnvironmentVariables();
        if (variables != null)
        {
            foreach (DictionaryEntry entry in variables)
            {
                builder.AppendLine($"{entry.Key}:{entry.Value}");
            }
            builder.AppendLine();
        }
        builder.AppendLine();

        builder.AppendLine("#CommandLine");
        builder.AppendLine(Environment.CommandLine);

        string filePath = "BuildTest.txt";
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
            Debug.Log($"Delete Previous File At: {filePath}");
        }
        File.WriteAllText(filePath, builder.ToString());
    }
}