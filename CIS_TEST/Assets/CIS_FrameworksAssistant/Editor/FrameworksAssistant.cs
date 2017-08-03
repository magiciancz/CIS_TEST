using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using Process = System.Diagnostics.Process;
using ProcessInfo = System.Diagnostics.ProcessStartInfo;
using Debug = UnityEngine.Debug;
using System.IO.Compression;
using System.Text;

namespace CIS
{
	public class GitSubmoduleSync{
		
		public static bool IsSubmoduleInstalled(string submodulename, bool isInUnityProject)
		{
			string foldername = "";
			if(isInUnityProject == false)
				foldername = Path.Combine(Application.dataPath, "../"+submodulename);
			else
				foldername = Path.Combine(Application.dataPath, submodulename);

			foldername = foldername.Replace ("\\", "/");
			
			return File.Exists(Path.Combine(foldername, ".git"));
		}

		public static void BeginSyncSubmodule(string submodulename, bool isInUnityProject, string branch = "master")
		{
			if(string.IsNullOrEmpty(submodulename))
				return;
			

			{
				//string submodulename = "CommonSDKs";
				string remotename = "";
				string foldername = "";
				if(isInUnityProject == false)
					foldername = Path.Combine(Application.dataPath, "../"+submodulename);
				else
					foldername = Path.Combine(Application.dataPath, submodulename);
				
				foldername = foldername.Replace ("\\", "/");
				ProcessInfo processinfo = null;

				if(Directory.Exists(foldername))
				{
					//we have CommonSDKs folder, check git control file
					if(File.Exists(Path.Combine(foldername, ".git")))
					{
						//we already have submodule, try to get remote name
						processinfo = new ProcessInfo ()
						{
							WorkingDirectory = foldername,
							FileName = "git",
							Arguments = "remote -v",
							UseShellExecute = false,
							RedirectStandardError = true,
							RedirectStandardOutput = true,
							CreateNoWindow = true,
						};

						if(processinfo != null)
						{
							Process process = Process.Start(processinfo);
							process.WaitForExit();
							System.IO.StreamReader sr = process.StandardOutput;
							if(sr != null)
							{
								string output = "";
								while(!string.IsNullOrEmpty(output = sr.ReadLine()))
								{
									if(output.Contains(submodulename+".git") && output.Contains("developer.coconut.is:3737") && output.Contains("(fetch)"))
									{
										string[] outputparts = output.Split(new char[] {' ', '\t', '\n'});
										if(outputparts != null && outputparts.Length>0)
										{
											remotename = outputparts[0];
											break;
										}
									}
								}

								sr.Close();
							}
						}

						if(!string.IsNullOrEmpty(submodulename))
						{
							processinfo = new ProcessInfo ()
							{
								WorkingDirectory = foldername,
								FileName = "git",
								Arguments = "pull "+remotename + " "+branch,
								UseShellExecute = false,
								RedirectStandardError = true,
								RedirectStandardOutput = true,
							};

							if(processinfo != null )
							{
								Process process = Process.Start(processinfo);
								process.WaitForExit();
								string error = process.StandardError.ReadToEnd();
								string output = process.StandardOutput.ReadToEnd();
								string message = "";
								if(!string.IsNullOrEmpty(error))
									message += error +"\n";
								if(!string.IsNullOrEmpty(output))
									message += output;
								EditorUtility.DisplayDialog(submodulename, message, "OK");
							}

						}
					}
					else
					{
						//local CommonSDKs, do nothing
						EditorUtility.DisplayDialog(submodulename, "We got local SDK folder without git info, abort!", "OK");
					}
				}
				else
				{
					//we don't have CommonSDKs folder, using git submodule add to add one

					//get 
					processinfo = new ProcessInfo ()
					{
						
						WorkingDirectory = (isInUnityProject?Application.dataPath:Path.Combine(Application.dataPath , "../")),
						FileName = "git",
						Arguments = "submodule add -b " + branch + " https://robot:robotatcoconut@developer.coconut.is:3737/git/"+submodulename+".git",
						UseShellExecute = false,
						RedirectStandardError = true,
						RedirectStandardOutput = true,
					};

					if(processinfo != null)
					{
						Process process = Process.Start (processinfo);
						process.WaitForExit ();
						string error = process.StandardError.ReadToEnd();
						string output = process.StandardOutput.ReadToEnd();
						string message = "";
						if(!string.IsNullOrEmpty(error))
							message += error +"\n";
						if(!string.IsNullOrEmpty(output))
							message += output;
						EditorUtility.DisplayDialog(submodulename, message, "OK");
					}

				}

			}			
		}

		public static void RemoveSubmodule(string submodulename, bool isInUnityProject)
		{

			if(string.IsNullOrEmpty(submodulename))
				return;

			{
				string remotename = "";
				string foldername = "";
				string workpath = "";
				if(isInUnityProject == false)
				{
					foldername = Path.Combine(Application.dataPath, "../"+submodulename);
					workpath = Application.dataPath.Replace("Assets", "");
				}
				else
				{
					foldername = Path.Combine(Application.dataPath, submodulename);
					workpath = Application.dataPath;
				}


				foldername = foldername.Replace ("\\", "/");
				ProcessInfo processinfo = null;

				if(Directory.Exists(foldername))
				{
					//we have CommonSDKs folder, check git control file
					if(File.Exists(Path.Combine(foldername, ".git")))
					{
						//submodule exist, try to deinit it
						//step 1: 'git submodule deinit <submodulename>'
						processinfo = new ProcessInfo ()
						{
							WorkingDirectory = workpath,
							FileName = "git",
							Arguments = "submodule deinit -f "+submodulename,
							UseShellExecute = false,
							RedirectStandardError = true,
							RedirectStandardOutput = true,
							CreateNoWindow = true,
						};

						if(processinfo != null)
						{
							Process process = Process.Start(processinfo);
							process.WaitForExit();
							//Debug.Log("1:"+process.StandardOutput.ReadToEnd());
							//Debug.Log("1:"+process.StandardError.ReadToEnd());
						}

						//step 2: 'git rm --cached <submodulename>'
						processinfo = new ProcessInfo ()
						{
							WorkingDirectory = workpath,
							FileName = "git",
							Arguments = "rm --cached "+submodulename,
							UseShellExecute = false,
							RedirectStandardError = true,
							RedirectStandardOutput = true,
							CreateNoWindow = true,
						};

						if(processinfo != null)
						{
							Process process = Process.Start(processinfo);
							process.WaitForExit();
							//Debug.Log("2:"+process.StandardOutput.ReadToEnd());
							//Debug.Log("2:"+process.StandardError.ReadToEnd());
						}


						//setp 3: 'git rev-parse --show-toplevel' 
						string gitroot = "";
						processinfo = new ProcessInfo ()
						{
							WorkingDirectory = foldername,
							FileName = "git",
							Arguments = "rev-parse --show-toplevel",
							UseShellExecute = false,
							RedirectStandardError = true,
							RedirectStandardOutput = true,
							CreateNoWindow = true,
						};

						if(processinfo != null)
						{
							Process process = Process.Start(processinfo);
							process.WaitForExit();
							System.IO.StreamReader sr = process.StandardOutput;
							gitroot = sr.ReadToEnd().Replace("\n","");
							//Debug.Log("3:"+process.StandardOutput.ReadToEnd());
							//Debug.Log("3:"+process.StandardError.ReadToEnd());
						}


						Debug.Log("gitroot:"+gitroot);
						//setp 4: 'rm .git/modules/<submodulename>'
						processinfo = new ProcessInfo ()
						{
							WorkingDirectory = gitroot,
							FileName = "rm",
							Arguments = "-rf .git/modules/"+Application.dataPath.Replace("Assets","").Replace(gitroot,"")+(isInUnityProject?"Assets/":"")+submodulename,
							UseShellExecute = false,
							RedirectStandardError = true,
							RedirectStandardOutput = true,
							CreateNoWindow = true,
						};

						if(processinfo != null)
						{
							Process process = Process.Start(processinfo);
							process.WaitForExit();
							//Debug.Log("4:"+process.StandardOutput.ReadToEnd());
							//Debug.Log("4:"+process.StandardError.ReadToEnd());
						}

						//step 5: 'rm folder'
						processinfo = new ProcessInfo ()
						{
							WorkingDirectory = workpath,
							FileName = "rm",
							Arguments = "-rf "+submodulename,
							UseShellExecute = false,
							RedirectStandardError = true,
							RedirectStandardOutput = true,
							CreateNoWindow = true,
						};

						if(processinfo != null)
						{
							Process process = Process.Start(processinfo);
							process.WaitForExit();
							//Debug.Log("5:"+process.StandardOutput.ReadToEnd());
							//Debug.Log("5:"+process.StandardError.ReadToEnd());
						}


					}
					else
					{
						//local CommonSDKs, do nothing
						EditorUtility.DisplayDialog(submodulename, "We got local SDK folder without git info, abort!", "OK");
					}
				}

			}			

		}

	}

	public class DownloadTempPath{
		static public string TempPath {
			get { return Path.Combine(Application.dataPath.Replace("Assets", ""), "cistempdownload/");}
		}
	}

	public class UnZipTool{
		
		public static void UnZipFile(string zipfile)
		{
#if UNITY_EDITOR_OSX
			ProcessInfo processinfo = null;
			Debug.Log(Path.GetDirectoryName(zipfile));

			processinfo = new ProcessInfo ()
			{
				WorkingDirectory = Path.GetDirectoryName(zipfile),
				FileName = "unzip",
				Arguments = "-u "+Path.GetFileName(zipfile),
				UseShellExecute = false,
				RedirectStandardError = false,
				RedirectStandardOutput = false,
			};

			if(processinfo != null )
			{
				Process process = Process.Start(processinfo);
				process.WaitForExit();
				//string error = process.StandardError.ReadToEnd();
				//string output = process.StandardOutput.ReadToEnd();

				//Debug.Log("error:"+error);
				//Debug.Log("output:"+output);

				Debug.Log(string.Format("Unzip {0} complete.", zipfile));

			}

#else
            ProcessInfo processinfo = null;
            Debug.Log(Path.GetDirectoryName(zipfile));

            processinfo = new ProcessInfo()
            {
                WorkingDirectory = Path.GetDirectoryName(zipfile),
                FileName = "cmd.exe",
                Arguments = "/c  " + Path.Combine(Application.dataPath, "CIS_FrameworksAssistant//Editor//7Zip//7z.exe") + " x -y " + Path.GetFileName(zipfile),
                UseShellExecute = false,
                RedirectStandardError = false,
                RedirectStandardOutput = false,
            };

            if (processinfo != null)
            {
                Process process = Process.Start(processinfo);
                process.WaitForExit();
                //string error = process.StandardError.ReadToEnd();
                //string output = process.StandardOutput.ReadToEnd();

                //Debug.Log("error:"+error);
                //Debug.Log("output:"+output);

                Debug.Log(string.Format("Unzip {0} complete.", zipfile));

            }
#endif
        }
	}

	[InitializeOnLoad]
	public class FrameworksAssistantWindow : EditorWindow {

		[Serializable]
		class FrameworkInfo
		{
			public string name;
			public string gitURL;
			public string sdkname;
			public string sdkgitURL;
			public string description;
		}

		[Serializable]
		class DownloadInfo
		{
			public WWW www;
			public string name;
			public string destfolder;

			public DownloadInfo(WWW _www, string _name, string _dest = "")
			{
				www = _www;
				name = _name;
				destfolder = _dest;
			}
		}

		[Serializable]
		class FrameworkConfig
		{
			public List<FrameworkInfo> list = new List<FrameworkInfo>();
		}

		[Serializable]
		enum InstalledType
		{
			NotInstalled,
			GitInstalled,
			DownloadInstalled,
		}

		[Serializable]
		enum InstallStep{
			InstallEmpty,
			InstallStart,
			InstallEnd,
		}

		static string configpath = "";

		static Dictionary<string, string> branch = null;

		static Dictionary<string, string> Branch{
			get 
			{
				if(branch == null)
				{
					branch = new Dictionary<string, string>();
				}

				branch.Clear();
					
				string filepath = Application.dataPath+"/CIS_ProjectRelated/Editor/SubmoduleBranch.txt";
				if(File.Exists(filepath))
				{
					using(StreamReader sr = File.OpenText(filepath))
					{
						if(sr != null)
						{
							string linecontent = sr.ReadLine();
							while(!string.IsNullOrEmpty(linecontent))
							{
								string[] branchcontent = linecontent.Split(':');
								branch[branchcontent[0]] = branchcontent[1];
								linecontent = sr.ReadLine();
							}

							sr.Close();
						}

					}
				}

				if(!branch.ContainsKey("default"))
					branch["default"] = "master";


				return branch;
			}
			set 
			{
				if(true)
				{
					branch = value;

					string path = System.IO.Path.Combine(Application.dataPath, "CIS_ProjectRelated");
					if(!System.IO.Directory.Exists(path))
						System.IO.Directory.CreateDirectory(path);

					string rpath = System.IO.Path.Combine(path, "Resources");
					if(!System.IO.Directory.Exists(rpath))
						System.IO.Directory.CreateDirectory(rpath);

					string epath = System.IO.Path.Combine(path, "Editor");
					if(!System.IO.Directory.Exists(epath))
						System.IO.Directory.CreateDirectory(epath);
					
					
					using(StreamWriter sw = File.CreateText(Application.dataPath+"/CIS_ProjectRelated/Editor/SubmoduleBranch.txt"))
					{
						if(sw != null)
						{
							foreach(string key in branch.Keys)
							{
								sw.WriteLine(key+":"+branch[key]);	
							}

							sw.Flush();
							sw.Close();
						}
					}
				}
			}
		}

		static FrameworkConfig _frameworkConfig;

		static Dictionary<string, InstalledType> _frameIsInstalled = new Dictionary<string, InstalledType>();

		static FrameworksAssistantWindow windows = null;

		[UnityEditor.Callbacks.DidReloadScripts]
		private static void OnScriptsReloaded() {
			// check Editor Prefeb
			string key = Application.dataPath+Application.productName+"CIS_Assistant_FirstInstall";

			if(!EditorPrefs.HasKey(key))
			{
				//1.open Assistant Window
				ShowAssistantWindow();

				//2.open online Doc
				OpenDocs();

				//3.write key to EditorPrefs
				EditorPrefs.SetBool(key, true);
			}


		}


		static bool IsDownload(string modulename, bool isinproject)
		{
			string folder = "";
			if(isinproject)
				folder = Application.dataPath;
			else
				folder = Application.dataPath.Replace("Assets", "");

			folder = Path.Combine(folder, modulename);
			if(Directory.Exists(folder))
				return true;

			return false;
		}

		static void UpdateIsInstalled(FrameworkConfig config)
		{
			if(config == null)
				return;

			foreach(FrameworkInfo info in config.list)
			{
				if(!_frameIsInstalled.ContainsKey(info.name))
					_frameIsInstalled.Add(info.name, InstalledType.NotInstalled);

				if(GitSubmoduleSync.IsSubmoduleInstalled(info.name, true) && (string.IsNullOrEmpty(info.sdkname) || GitSubmoduleSync.IsSubmoduleInstalled(info.sdkname, false)))
					_frameIsInstalled[info.name] = InstalledType.GitInstalled;
				else if(IsDownload(info.name, true) && (string.IsNullOrEmpty(info.sdkname) || IsDownload(info.sdkname, false)))
					_frameIsInstalled[info.name] = InstalledType.DownloadInstalled;
				else
					_frameIsInstalled[info.name] = InstalledType.NotInstalled;
			}
		}
			
		static void UpdateFrameworksConfig(FrameworkConfig config)
		{
			string content = EditorJsonUtility.ToJson(config, true);
			StreamWriter sw = File.CreateText(configpath);
			if(sw != null)
			{
				sw.Write(content);
				sw.Flush();
				sw.Close();
			}

		}

		static FrameworkConfig GetFrameworksConfig()
		{
			if(!File.Exists(configpath))
				return null;

			StreamReader sr = File.OpenText(configpath);
			if(sr != null)
			{
				FrameworkConfig config = JsonUtility.FromJson<FrameworkConfig>(sr.ReadToEnd());
				sr.Close();
				return config;
			}

			return null;


		}

		static float escapetime = 0.0f;

		static string _showMessage = "";

		static InstallStep installstep = InstallStep.InstallEmpty;

		static private List<DownloadInfo> downloadQueue = new List<DownloadInfo>();
		static private Dictionary<string, string> moveDic = new Dictionary<string, string>();

		[MenuItem("CIS_Frameworks/Assistant")]
		static void ShowAssistantWindow()
		{
			windows = EditorWindow.GetWindow<FrameworksAssistantWindow>();
			windows.Show();
		}

		[MenuItem("CIS_Frameworks/Docs")]
		static void OpenDocs()
		{
			Application.OpenURL("https://git.coconut.is:3737/CIS_FrameworksManual");
		}

		//[MenuItem("CIS_Frameworks/GZipTest")]
		static void GZipTest()
		{
			string tempdownpath = DownloadTempPath.TempPath;
			string tempdownzip = Path.Combine(tempdownpath, "CIS_Support.zip");

			UnZipTool.UnZipFile(tempdownzip);
		}

		public static Texture2D LoadPNG(string filePath) {

			Texture2D tex = null;
			byte[] fileData;

			if (File.Exists(filePath))     {
				fileData = File.ReadAllBytes(filePath);
				tex = new Texture2D(2, 2);
				tex.LoadImage(fileData); //..this will auto-resize the texture dimensions.
			}
			return tex;
		}

		void StartHttpDownload(string submodulename, bool isInUnityProject, string branch)
		{
			if(installstep == InstallStep.InstallEmpty)
				installstep = InstallStep.InstallStart;

			//1.create download temp folder
			string tempdownpath = DownloadTempPath.TempPath;
			if(!Directory.Exists(tempdownpath))
				Directory.CreateDirectory(tempdownpath);

			//2. downland relative SDKs and unzip it
			string url = "";
			if (!string.IsNullOrEmpty(submodulename))
			{
				url = string.Format("https://developer.coconut.is:3737/git/{0}/repository/archive.zip?ref={1}&private_token=68gGHUWujqC5Z6TE9axE", submodulename,branch);
				downloadQueue.Add(new DownloadInfo(new WWW(url), submodulename, isInUnityProject?"Assets":""));

			}
		}

		void DeleteHttpDownload(string submodulename, bool isInUnityProject)
		{
			if(!string.IsNullOrEmpty(submodulename))
			{
				string finalpath = isInUnityProject?Application.dataPath:Application.dataPath.Replace("Assets","");
				string namepath = Path.Combine(finalpath, submodulename);
				if(Directory.Exists(namepath))
					Directory.Delete(namepath, true);									
			}
				
		}

		void UpdateAll()
		{
			//1.Update CIS_FrameworksAssistant first

			//2.Update CIS_Support 

			//3.Update other managers

			for( int i=0; i<_frameworkConfig.list.Count; i++)
			{

				string managerbranch = "";
				if(branch.ContainsKey(_frameworkConfig.list[i].name))
					managerbranch = branch[_frameworkConfig.list[i].name];
				
				switch(_frameIsInstalled[_frameworkConfig.list[i].name])
				{
				case InstalledType.DownloadInstalled:
					StartHttpDownload(_frameworkConfig.list[i].sdkname, false, string.IsNullOrEmpty(managerbranch)?branch["default"]:managerbranch);
					StartHttpDownload(_frameworkConfig.list[i].name, true, string.IsNullOrEmpty(managerbranch)?branch["default"]:managerbranch);
					break;
				case InstalledType.GitInstalled:
					GitSubmoduleSync.BeginSyncSubmodule(_frameworkConfig.list[i].name, true, string.IsNullOrEmpty(managerbranch)?branch["default"]:managerbranch);
					GitSubmoduleSync.BeginSyncSubmodule(_frameworkConfig.list[i].sdkname, false, string.IsNullOrEmpty(managerbranch)?branch["default"]:managerbranch);
					AssetDatabase.Refresh();
					break;
				}
					
			}
		}

		void DeleteAll()
		{
			//1.Delete all managers

			//2.Delete CIS_Support

			//3.Delete CIS_FrameworksAssistant last
			EditorApplication.LockReloadAssemblies();

			if(windows != null)
				windows.Close();

			for( int i=_frameworkConfig.list.Count-1; i>=0; i--)
			{
				switch(_frameIsInstalled[_frameworkConfig.list[i].name])
				{
				case InstalledType.DownloadInstalled:
					DeleteHttpDownload(_frameworkConfig.list[i].sdkname, false);
					DeleteHttpDownload(_frameworkConfig.list[i].name, true);
					break;
				case InstalledType.GitInstalled:
					GitSubmoduleSync.RemoveSubmodule(_frameworkConfig.list[i].name, true);
					GitSubmoduleSync.RemoveSubmodule(_frameworkConfig.list[i].sdkname, false);
					break;
				}

			}

			EditorApplication.UnlockReloadAssemblies();

			AssetDatabase.Refresh();

		}


		void OnEnable()
		{
			configpath = Path.Combine(Application.dataPath, "CIS_FrameworksAssistant/Editor/frameworkconfig.txt");

			_frameworkConfig = GetFrameworksConfig();

			UpdateIsInstalled(_frameworkConfig);

			branch = Branch;

		}

		static FrameworksAssistantWindow()
		{
			EditorApplication.update += Updates;
		}

		void OnInspectorUpdate()
		{
			Repaint();

			if(installstep == InstallStep.InstallEnd)
			{
				AssetDatabase.Refresh();
				installstep = InstallStep.InstallEmpty;
				EditorUtility.ClearProgressBar();
			}
		}

		static void Updates()
        { 
			escapetime += Time.deltaTime;
			if(escapetime > 2.0f)
			{
				if(_frameworkConfig == null)
				{
					configpath = Path.Combine(Application.dataPath, "CIS_FrameworksAssistant/Editor/frameworkconfig.txt");
					_frameworkConfig = GetFrameworksConfig();
				}

				UpdateIsInstalled(_frameworkConfig);
				escapetime = 0.0f;

			}

			if (downloadQueue != null && downloadQueue.Count > 0 && downloadQueue [0].www != null) {
				if (downloadQueue [0].www.isDone) {
					WWW www = downloadQueue[0].www;
					string downloadframeworkname = downloadQueue[0].name;
					string downloaddest = downloadQueue[0].destfolder;
					byte[] data = www.bytes;
					string tempdownpath = DownloadTempPath.TempPath;
					if(!Directory.Exists(tempdownpath))
						Directory.CreateDirectory(tempdownpath);
					
					string tempdownzip = Path.Combine(tempdownpath, downloadframeworkname+".zip");

					System.IO.File.WriteAllBytes(tempdownzip, data);

					UnZipTool.UnZipFile(tempdownzip);

					//4. set move folder to hashset
					moveDic.Add(downloadframeworkname, downloaddest);

					/*
					string[] dirs = Directory.GetDirectories(tempdownpath);

					foreach(string dir in dirs)
					{
						if(dir.Contains(downloadframeworkname))
						{
							string destpath = Path.Combine(Path.Combine(Application.dataPath.Replace("Assets", ""),downloaddest), downloadframeworkname);
							if(Directory.Exists(destpath))
								Directory.Delete(destpath,true);

							Directory.Move(dir, destpath);
							break;
						}
					}
					*/

					downloadQueue.RemoveAt(0);
					_showMessage = "";
				
				} else {
					_showMessage = string.Format("Downloading {0}: {1}%",downloadQueue[0].name, downloadQueue[0].www.progress*100);
				}
			
			}
				

			if(downloadQueue != null && downloadQueue.Count == 0 && installstep == InstallStep.InstallStart)
			{
				string[] dirs = Directory.GetDirectories(DownloadTempPath.TempPath);

				EditorApplication.LockReloadAssemblies();
				foreach(string dir in dirs)
				{
					foreach(string key in moveDic.Keys)
					{
						if(dir.Contains(key))
						{
							string destpath = Path.Combine(Path.Combine(Application.dataPath.Replace("Assets", ""),moveDic[key]), key);
							if(Directory.Exists(destpath))
								Directory.Delete(destpath,true);

							Directory.Move(dir, destpath);
							break;

						}
					}
				}
				EditorApplication.UnlockReloadAssemblies();

				moveDic.Clear();

				installstep = InstallStep.InstallEnd;
			}
		}

		void OnGUI()
		{
			if(_frameworkConfig == null)
				return;

			EditorGUILayout.BeginVertical();

			EditorGUILayout.Space ();
			EditorGUILayout.HelpBox("Make sure install git on your OS and run it in terminal without problem.", MessageType.Info, true);
			EditorGUILayout.Space();

			EditorGUILayout.Space ();
			EditorGUILayout.BeginHorizontal(GUI.skin.box);
			if(branch["default"] != (branch["default"] = EditorGUILayout.TextField("Default Branch:", branch["default"], GUILayout.Width(600))))
			{
				Branch = branch;
			}

			EditorGUILayout.Space ();

			Color oldcolor = GUI.color;
			GUI.color = Color.green;

			if(GUILayout.Button("Update All", GUILayout.Width(200)) && EditorUtility.DisplayDialog("Warning!","This will update all installed modules!", "Ok!", "Cancel"))
			{
				UpdateAll();
			}

			GUI.color = Color.red;
			if(GUILayout.Button("Delete All", GUILayout.Width(200)) && EditorUtility.DisplayDialog("Warning!","This will delete all installed modules!", "Ok!", "Cancel"))
			{
				DeleteAll();
			}

			GUI.color = oldcolor;


			EditorGUILayout.EndHorizontal();
			EditorGUILayout.Space ();

			if (!string.IsNullOrEmpty(_showMessage) && downloadQueue != null && downloadQueue.Count>0)
            {
				EditorUtility.DisplayProgressBar("The download percentage may not be available, please be patient.", _showMessage, 1.0f / downloadQueue.Count);
            }


           
			foreach(FrameworkInfo info in _frameworkConfig.list)
			{
				InstalledType installedtype = InstalledType.NotInstalled;
				if(_frameIsInstalled.ContainsKey(info.name))
					installedtype = _frameIsInstalled[info.name];

				GUI.color = oldcolor;//isinstalled?Color.green:Color.yellow;
				EditorGUILayout.BeginHorizontal(GUI.skin.box);

				GUILayout.Label(info.name + " : " + (installedtype != InstalledType.NotInstalled?"Installed":"Not installed"), GUILayout.Width(400));

				string managerbranch = "";
				if(branch.ContainsKey(info.name))
					managerbranch = branch[info.name];

				if(managerbranch != (managerbranch = EditorGUILayout.TextField(managerbranch, GUILayout.Width(300))))
				{
					branch[info.name] = managerbranch;
					Branch = branch;
				}

				EditorGUILayout.Space ();


				if(installedtype == InstalledType.NotInstalled || installedtype == InstalledType.DownloadInstalled)
				{
					GUI.color = installedtype == InstalledType.DownloadInstalled?Color.green:Color.yellow;
					EditorGUILayout.BeginHorizontal(GUI.skin.box);
					if(GUILayout.Button("Http download " + info.name, GUILayout.Width(400)))
					{

						StartHttpDownload(info.sdkname, false, string.IsNullOrEmpty(managerbranch)?branch["default"]:managerbranch);
						StartHttpDownload(info.name, true, string.IsNullOrEmpty(managerbranch)?branch["default"]:managerbranch);

					}
					EditorGUILayout.EndHorizontal();

				}

				if(installedtype == InstalledType.NotInstalled || installedtype == InstalledType.GitInstalled)
				{
					GUI.color = installedtype == InstalledType.GitInstalled?Color.green:Color.yellow;
					EditorGUILayout.BeginHorizontal(GUI.skin.box);
					if(GUILayout.Button("Git pull " + info.name, GUILayout.Width(400)))
					{
						GitSubmoduleSync.BeginSyncSubmodule(info.name, true, string.IsNullOrEmpty(managerbranch)?branch["default"]:managerbranch);
						GitSubmoduleSync.BeginSyncSubmodule(info.sdkname, false, string.IsNullOrEmpty(managerbranch)?branch["default"]:managerbranch);
						//UpdateFrameworksConfig(_frameworkConfig);
						AssetDatabase.Refresh();
					}
					EditorGUILayout.EndHorizontal();

				}


				if(installedtype != InstalledType.NotInstalled)
				{
					GUI.color = Color.red;
					EditorGUILayout.BeginHorizontal(GUI.skin.box);
					if(GUILayout.Button("Delete " + info.name, GUILayout.Width(400)))
					{
						if(EditorUtility.DisplayDialog("Warning!","This will delete "+ info.name + "!", "Ok!", "Cancel"))
						{
							if(installedtype == InstalledType.GitInstalled)
							{
								GitSubmoduleSync.RemoveSubmodule(info.name, true);
								GitSubmoduleSync.RemoveSubmodule(info.sdkname, false);
								AssetDatabase.Refresh();
							}
							else if(installedtype == InstalledType.DownloadInstalled)
							{

								DeleteHttpDownload(info.name, true);
								DeleteHttpDownload(info.sdkname, false);

							}

							AssetDatabase.Refresh();
						}

						if(info.name == "CIS_FrameworksAssistant" && windows != null)
							windows.Close();
							
					}
					EditorGUILayout.EndHorizontal();

				}




				EditorGUILayout.EndHorizontal();
								
			}

			GUI.color = oldcolor;

			EditorGUILayout.Space ();
			EditorGUILayout.Space ();
			EditorGUILayout.Space ();
			EditorGUILayout.Space ();

			Texture2D tex = LoadPNG(Application.dataPath+"/CIS_FrameworksAssistant/Editor/gitbook_icon.png");
			GUIContent docbuttoncontent = new GUIContent("Open CIS Frameworks Doc...", tex, "Open Coconut Island Studio frameworks document in your browser");

			int fontsize = GUI.skin.button.fontSize;
			FontStyle fontstyle = GUI.skin.button.fontStyle;
			Color fontcolor = GUI.skin.button.normal.textColor;

			GUI.skin.button.fontSize = 50;
			GUI.skin.button.fontStyle = FontStyle.Bold;
			GUI.skin.button.normal.textColor = new Color(54.0f/255.0f, 129.0f/255.0f, 252.0f/255.0f);

			if(GUILayout.Button(docbuttoncontent))
			{
				Application.OpenURL("https://git.coconut.is:3737/CIS_FrameworksManual");
			}
				
			GUI.skin.button.fontSize = fontsize;
			GUI.skin.button.fontStyle = fontstyle;
			GUI.skin.button.normal.textColor = fontcolor;
			EditorGUILayout.EndVertical();
		}


	}




}
