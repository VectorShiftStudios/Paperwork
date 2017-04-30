using UnityEngine;
using UnityEditor;
using UnityEditor.Callbacks;
using System;
using System.IO;

public class CPostBuild
{
	private static string _sourcePath;
	private static string _destPath;
	
	[PostProcessBuild]
	public static void OnPostprocessBuild(BuildTarget target, string pathToBuiltProject)
	{
		_sourcePath = Application.dataPath + "/../";
		_destPath = pathToBuiltProject.Substring(0, pathToBuiltProject.LastIndexOf('/')) + "/";

		try
		{
			//Directory.Delete(_destPath + "Data", true);
		}
		catch (Exception Ex)
		{
			Debug.Log("Post Process Build: " + Ex.Message);
		}

		CopyDirectory("Data", _destPath + "Paperwork_Data/");
		CopyFile("steam_api.dll");
		//CopyFile("config.txt");
		//CopyDirectory("Cursors");
	}

    public static void CopyDirectory(string Name, string Dest)
    {        
        Directory.CreateDirectory(Dest + Name);

        string[] dirs = Directory.GetDirectories(_sourcePath + Name);
        string[] files = Directory.GetFiles(_sourcePath + Name);        
             
        foreach (string s in files)
        {
            string name = s.Substring(_sourcePath.Length);
            string dest = Dest + name;

            if (Path.GetExtension(name) != ".meta")
                File.Copy(s, dest, true);
        }
        
        foreach (string s in dirs)
        {
            string name = s.Substring(_sourcePath.Length);
            CopyDirectory(name, Dest);
        }
    }

	public static void CopyFile(string Name)
	{
		File.Copy(_sourcePath + Name, _destPath + Name, true);
	}	
}