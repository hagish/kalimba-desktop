using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System.IO;

public class Build : MonoBehaviour {
	private static string GetId() {
		return PlayerSettings.companyName + "_" + PlayerSettings.productName;
	}

	private static string GetAndStoreLastBuildDirectory() {
		string s = EditorPrefs.GetString(GetId() + "_last_build_directory");
		if (!Directory.Exists(s)) s = null;
		s = (!string.IsNullOrEmpty(s)) ? s : EditorUtility.SaveFolderPanel("build folder", "builds1", "builds2");
		EditorPrefs.SetString(GetId() + "_last_build_directory", s);
		return s;
	}

	[MenuItem("Build/Active And Run")]
	static void BuildActiveAndRun () {
		string locationPathNameBase = GetAndStoreLastBuildDirectory();
		
		if (!string.IsNullOrEmpty(locationPathNameBase)) {
			Debug.Log(string.Format("building to {0}", locationPathNameBase));
			
			BuildPlatform(EditorUserBuildSettings.activeBuildTarget, Path.Combine(locationPathNameBase, "active"), BuildOptions.AutoRunPlayer);
		}
	}

	[MenuItem("Build/Active")]
	static void BuildActive () {
		string locationPathNameBase = GetAndStoreLastBuildDirectory();
		
		if (!string.IsNullOrEmpty(locationPathNameBase)) {
			Debug.Log(string.Format("building to {0}", locationPathNameBase));
			
			BuildPlatform(EditorUserBuildSettings.activeBuildTarget, Path.Combine(locationPathNameBase, "active"));
		}
	}
	
	[MenuItem("Build/Active Debug")]
	static void BuildActiveDebug () {
		string locationPathNameBase = GetAndStoreLastBuildDirectory();
		
		if (!string.IsNullOrEmpty(locationPathNameBase)) {
			Debug.Log(string.Format("building to {0}", locationPathNameBase));

			BuildOptions bo = BuildOptions.AllowDebugging | BuildOptions.ConnectWithProfiler | BuildOptions.Development;
			BuildPlatform(EditorUserBuildSettings.activeBuildTarget, Path.Combine(locationPathNameBase, "active"), bo, "-debug");
		}
	}

	[MenuItem("Build/Active Debug And Run")]
	static void BuildActiveDebugAndRun () {
		string locationPathNameBase = GetAndStoreLastBuildDirectory();
		
		if (!string.IsNullOrEmpty(locationPathNameBase)) {
			Debug.Log(string.Format("building to {0}", locationPathNameBase));
			
			BuildOptions bo = BuildOptions.AllowDebugging | BuildOptions.ConnectWithProfiler | BuildOptions.Development | BuildOptions.AutoRunPlayer;
			BuildPlatform(EditorUserBuildSettings.activeBuildTarget, Path.Combine(locationPathNameBase, "active"), bo, "-debug");
		}
	}
	
	[MenuItem("Build/All")]
	static void BuildAll () {
		string locationPathNameBase = GetAndStoreLastBuildDirectory();

		if (!string.IsNullOrEmpty(locationPathNameBase)) {
			Debug.Log(string.Format("building to {0}", locationPathNameBase));

			var active = EditorUserBuildSettings.activeBuildTarget;

			BuildPlatform(BuildTarget.StandaloneOSXIntel, Path.Combine(locationPathNameBase, "osx"));
			BuildPlatform(BuildTarget.StandaloneWindows, Path.Combine(locationPathNameBase, "win"));
			BuildPlatform(BuildTarget.StandaloneLinuxUniversal, Path.Combine(locationPathNameBase, "linux"));

			EditorUserBuildSettings.SwitchActiveBuildTarget(active);
		}
	}

	[MenuItem("Build/Set directory")]
	static void SetDirectory () {
		EditorPrefs.DeleteKey(GetId() + "_last_build_directory");
		GetAndStoreLastBuildDirectory();
	}

	private static string GetName(BuildTarget target, string gameNamePostfix = "") {
		string name = PlayerSettings.productName + gameNamePostfix;

		switch(target) {
		case BuildTarget.StandaloneOSXIntel:
		case BuildTarget.StandaloneOSXIntel64:
		case BuildTarget.StandaloneOSXUniversal:
			return name + ".app";
		case BuildTarget.StandaloneWindows:
		case BuildTarget.StandaloneWindows64:
			return name + ".exe";
		default:
			return name;
		}
	}

	private static string[] GetLevels(BuildTarget target) {
		List<string> levels = new List<string>();

		foreach(var s in EditorBuildSettings.scenes) {
			if (s.enabled) levels.Add(s.path);
		}

		return levels.ToArray();
	}

	static void OnPreBuild(BuildTarget target, string locationPathNameBase, string completePath) {
		// osx
		if (target == BuildTarget.StandaloneOSXIntel || target == BuildTarget.StandaloneOSXIntel64 || target == BuildTarget.StandaloneOSXUniversal) {
			File.Move(Application.dataPath + "/Plugins/libpdcsharp.bundle", 
			          Application.dataPath + "/Plugins/libpdcsharp.bundle.skip");	
		}
	}

	static void OnPostBuild(BuildTarget target, string locationPathNameBase, string completePath) {
 		// osx
		if (target == BuildTarget.StandaloneOSXIntel || target == BuildTarget.StandaloneOSXIntel64 || target == BuildTarget.StandaloneOSXUniversal) {
			File.Move(Application.dataPath + "/Plugins/libpdcsharp.bundle.skip", 
			          Application.dataPath + "/Plugins/libpdcsharp.bundle");	

			File.Copy(Application.dataPath + "/Plugins/libpdcsharp.bundle", completePath 
			          + "/Contents/Frameworks/MonoEmbedRuntime/osx/libpdcsharp.bundle");
		}

		// win
		if (target == BuildTarget.StandaloneWindows || target == BuildTarget.StandaloneWindows64) {
			var dlls = new string[]{"libpthread-2.dll", "pthreadGC2.dll", "libpdcsharp.dll"};

			foreach(var dll in dlls) {
				if (!File.Exists(locationPathNameBase + "/" + dll)) {
					File.Copy(Application.dataPath + "/Plugins/" + dll, 
					          locationPathNameBase + "/" + dll);	
				}
			}
		}

		// win
		if (target == BuildTarget.StandaloneWindows || target == BuildTarget.StandaloneWindows64) {
			if (!File.Exists(locationPathNameBase + "/pthreadGC2.dll")) {
				File.Copy(Application.dataPath + "/Plugins/pthreadGC2.dll", 
				          locationPathNameBase + "/pthreadGC2.dll");	
			}
		}
	}

	static void BuildPlatform(BuildTarget target, string locationPathNameBase, BuildOptions options = BuildOptions.None, string gameNamePostfix = "") {
		Directory.CreateDirectory(locationPathNameBase);

		var s = Path.Combine(locationPathNameBase, GetName(target, gameNamePostfix));

		Debug.Log(string.Format("building {0} to {1}", target, s));

		OnPreBuild(target, locationPathNameBase, s);
		BuildPipeline.BuildPlayer(GetLevels(target), s, target, options);
		OnPostBuild(target, locationPathNameBase, s);
	}
}
