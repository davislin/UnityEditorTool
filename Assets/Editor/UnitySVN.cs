using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

public class UnitySVN
{
    private const string Add_CMD = "add";
    private const string COMMIT_CMD = "commit";
    private const string UPDATE_CMD = "update";
    private const string REVERT_CMD = "revert";
    private const string CLEARUP_CMD = "cleanup";
    private const string LOG_CMD = "log";
    private const string SVN_COMMIT = "Assets/SVN/Commit";
    private const string SVN_COMMIT_ALL = "Assets/SVN/CommitAll";
    private const string SVN_UPDATE = "Assets/SVN/Update";
    private const string SVN_UPDATE_ALL = "Assets/SVN/UpdateAll";

    /// <summary>  
    /// 創建一個SVN的cmd命令  
    /// </summary>  
    /// <param name="command">命令(可在help裡邊查看)</param>  
    /// <param name="path">命令激活路徑</param>  
    public static void SVNCommand(string command, string path)
    {
        //closeonend 2 表示假設提交沒錯，會自動關閉提交界面返回原工程，詳細描述可在  
        //TortoiseSVN/help/TortoiseSVN/Automating TortoiseSVN裡查看  
        string c = "/c tortoiseproc.exe /command:{0} /path:\"{1}\" /closeonend 2";
        c = string.Format(c, command, path);
        ProcessStartInfo info = new ProcessStartInfo("cmd.exe", c);
        info.WindowStyle = ProcessWindowStyle.Hidden;
        Process.Start(info);
    }
    /// <summary>  
    /// 提交選中內容  
    /// </summary>  
    [MenuItem(SVN_COMMIT)]
    public static void SVNCommit()
    {
        SVNCommand(COMMIT_CMD, GetSelectedObjectPath());
    }
    /// <summary>  
    /// 提交全部Assets文件夾內容  
    /// </summary>  
    [MenuItem(SVN_COMMIT_ALL)]
    public static void SVNCommitAll()
    {
        SVNCommand(COMMIT_CMD, Application.dataPath);
    }
    /// <summary>  
    /// 更新選中內容  
    /// </summary>  
    [MenuItem(SVN_UPDATE)]
    public static void SVNUpdate()
    {
        SVNCommand(UPDATE_CMD, GetSelectedObjectPath());
    }
    /// <summary>  
    /// 更新全部內容  
    /// </summary>  
    [MenuItem(SVN_UPDATE_ALL)]
    public static void SVNUpdateAll()
    {
        SVNCommand(UPDATE_CMD, Application.dataPath);
    }

    /// <summary>  
    /// 獲取全部選中物體的路徑  
    /// 包括meta文件  
    /// </summary>  
    /// <returns></returns>  
    private static string GetSelectedObjectPath()
    {
        string path = string.Empty;

        for (int i = 0; i < Selection.objects.Length; i++)
        {
            path += AssetsPathToFilePath(AssetDatabase.GetAssetPath(Selection.objects[i]));
            //路徑分隔符  
            path += "*";
            //meta文件  
            path += AssetsPathToFilePath(AssetDatabase.GetAssetPath(Selection.objects[i])) + ".meta";
            //路徑分隔符  
            path += "*";
        }

        return path;
    }
    /// <summary>  
    /// 將Assets路徑轉換為File路徑  
    /// </summary>  
    /// <param name="path">Assets/Editor/...</param>  
    /// <returns></returns>  
    public static string AssetsPathToFilePath(string path)
    {
        string m_path = Application.dataPath;
        m_path = m_path.Substring(0, m_path.Length - 6);
        m_path += path;

        return m_path;
    }

    #region MenuItem Funcs
    [MenuItem("SVN/Update(更新)", false, 1)]
    public static void SVN_Update()
    {
        List<string> pathList = new List<string>();
        pathList.Add(SVNProjectPath + "/Assets");
        pathList.Add(SVNProjectPath + "/Packages");
        pathList.Add(SVNProjectPath + "/ProjectSettings");
        Update(paths: pathList.ToArray());
    }
    [MenuItem("SVN/Revert(還原)", false, 3)]
    public static void SVN_Revert()
    {
        List<string> pathList = new List<string>();
        pathList.Add(SVNProjectPath + "/Assets");
        pathList.Add(SVNProjectPath + "/Packages");
        pathList.Add(SVNProjectPath + "/ProjectSettings");
        Revert(pathList.ToArray());
    }
    static string SVNProjectPath
    {
        get
        {
            System.IO.DirectoryInfo parent = System.IO.Directory.GetParent(Application.dataPath);
            return parent.ToString();
        }
    }
    [MenuItem("SVN/Commit(送交)", false, 2)]
    public static void SVN_Commit()
    {
        List<string> pathList = new List<string>();
        pathList.Add(SVNProjectPath + "/Assets");
        pathList.Add(SVNProjectPath + "/Packages");
        pathList.Add(SVNProjectPath + "/ProjectSettings");

        WrappedCommadn(command: COMMIT_CMD, paths: pathList.ToArray(), newThread: true);
    }
    [MenuItem("SVN/Commit(更新並送交)", false, 2)]
    public static void SVN_UpdateAndCommit()
    {
        List<string> pathList = new List<string>();
        pathList.Add(SVNProjectPath + "/Assets");
        pathList.Add(SVNProjectPath + "/Packages");
        pathList.Add(SVNProjectPath + "/ProjectSettings");

        Commit("UnitySVN Upload", true, pathList.ToArray());
    }
    [MenuItem("SVN/Add(新增)", false, 6)]
    public static void SVN_Add()
    {
        List<string> pathList = new List<string>();
        pathList.Add(SVNProjectPath + "/Assets");
        pathList.Add(SVNProjectPath + "/Packages");
        pathList.Add(SVNProjectPath + "/ProjectSettings");

        Add(pathList.ToArray());
    }
    #endregion
    [MenuItem("SVN/CleanUp(清理)", false, 4)]
    static void SVNCleanUp()
    {
        List<string> pathList = new List<string>();
        pathList.Add(SVNProjectPath + "/Assets");
        pathList.Add(SVNProjectPath + "/Packages");
        pathList.Add(SVNProjectPath + "/ProjectSettings");
        WrappedCommadn(command: CLEARUP_CMD, paths: pathList.ToArray(), newThread: true, extCommand: "/logmsg:\"CleanUp\"");
    }

    [MenuItem("SVN/Log(紀錄)", false, 5)]
    static void SVNLog()
    {
        List<string> pathList = new List<string>();
        pathList.Add(SVNProjectPath + "/Assets");
        pathList.Add(SVNProjectPath + "/Packages");
        pathList.Add(SVNProjectPath + "/ProjectSettings");
        WrappedCommadn(command: LOG_CMD, paths: pathList.ToArray(), newThread: true, extCommand: "/logmsg:\"Open Log\"");
    }
    #region Wrapped Funcs
    // add
    public static void Add(params string[] paths)
    {
        WrappedCommadn(Add_CMD, paths, false);
    }
    // update
    public static void Update(params string[] paths)
    {
        WrappedCommadn(UPDATE_CMD, paths, false);
        //SaveAndRefresh();
    }
    // revert
    public static void Revert(params string[] paths)
    {
        WrappedCommadn(REVERT_CMD, paths, false);
        //SaveAndRefresh();
    }
    // add->update->commit
    public static void Commit(string log, bool add = true, params string[] paths)
    {
        Update(paths);
        string extMsg = log ?? string.Empty;
        WrappedCommadn(command: COMMIT_CMD, paths: paths, newThread: true, extCommand: $"/logmsg:\"{extMsg}\"");
    }

    /// <summary>
    /// Wrap SVN Command
    /// </summary>
    /// <param name="command"></param>
    /// <param name="path"></param>
    /// <param name="extCommand"></param>
    public static void WrappedCommadn(string command, string[] paths, bool newThread = false, string extCommand = null)
    {
        if (paths == null || paths.Length == 0)
        {
            return;
        }
        string pathString = string.Join("*", paths); ;
        //Debug.Log($"WrappedCommadn=>\ncommand[{command}]\npath[{pathString}]\nnewThread[{newThread}]\nextCommand[{extCommand}]");
        var commandString = $"/c tortoiseproc.exe /command:{command} /path:\"{pathString}\" {extCommand} /closeonend 2";

        CreateProcess(newThread, commandString);
    }

    private static void CreateProcess(bool newThread, string commandString)
    {
        ProcessStartInfo info = new ProcessStartInfo("cmd.exe", commandString);
        info.WindowStyle = ProcessWindowStyle.Hidden;
        if (newThread)
        {
            System.Threading.ThreadPool.QueueUserWorkItem((_obj) =>
            {
                RunProcess(info);
            });
        }
        else
        {
            RunProcess(info);
        }
    }
    #endregion

    #region Help Funcs
    public static HashSet<string> GetAssets()
    {
        HashSet<string> allAssets = new HashSet<string>();
        const string BaseFolder = "Assets";
        foreach (var obj in Selection.objects)
        {
            var assetPath = AssetDatabase.GetAssetPath(obj);

            List<string> fullDirs = FullDirectories(assetPath, BaseFolder);
            allAssets.UnionWith(fullDirs);

            var dps = AssetDatabase.GetDependencies(assetPath, true);
            foreach (var dp in dps)
            {
                if (dp != assetPath)
                {
                    List<string> dpsDirs = FullDirectories(dp, BaseFolder);
                    allAssets.UnionWith(dpsDirs);
                }
            }
        }
        return allAssets;
    }
    public static List<string> GetAssetPathList()
    {
        var path = new List<string>(GetAssets());
        path.Sort((_l, _r) =>
        {
            if (_l.Length > _r.Length)
            {
                return 1;
            }
            if (_l.Length < _r.Length)
            {
                return -1;
            }
            return 0;
        });
        return path;
    }
    public static void SaveAndRefresh()
    {
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }
    public static List<string> FullDirectories(string path, string baseFolder)
    {
        List<string> retVal = new List<string>();
        retVal.Add(path);
        retVal.Add(path + ".meta");
        baseFolder = baseFolder.Replace("\\", "/");
        var dir = System.IO.Path.GetDirectoryName(path).Replace("\\", "/");
        while (string.IsNullOrEmpty(dir) == false && dir != baseFolder)
        {
            retVal.Add(dir);
            retVal.Add(dir + ".meta");
            dir = System.IO.Path.GetDirectoryName(dir).Replace("\\", "/");
        }
        return retVal;
    }
    private static void RunProcess(ProcessStartInfo info)
    {
        Process p = null;
        try
        {
            using (p = Process.Start(info))
            {
                p.WaitForExit();
            }
        }
        catch (System.Exception ex)
        {
            UnityEngine.Debug.LogError(@ex.ToString());
            if (p != null)
            {
                p.Kill();
            }
        }
    }
    #endregion
    [MenuItem("Tools/更新Proto", false, 6)]
    static void UpdateProto()
    {
        string workPath = @"..\Tools\protobuf-3.6.1\";
        workPath = Path.GetFullPath(workPath);
        ProcessStartInfo info = new ProcessStartInfo($"{workPath}UpdateProto.bat");
        info.WindowStyle = ProcessWindowStyle.Hidden;
        info.WorkingDirectory = workPath;
        RunProcess(info);
    }
}

/*
/ closeonend：0不自動關閉對話框

/ closeonend：1會自動關閉，如果沒有錯誤

/ closeonend：2會自動關閉，如果沒有發生錯誤和衝突

/ closeonend：3會自動關閉，如果沒有錯誤，衝突和合併

/ closeonend：4會自動關閉，如果沒有錯誤，衝突和合併
 */
