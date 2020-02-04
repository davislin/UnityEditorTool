using LitJson;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public class FNTFontCreate : ScriptableWizard
{

    public TextAsset FontFile;
    public Texture2D TextureFile;
    public Vector2 SpacingAdjustment = Vector2.zero;

    [MenuItem("Tools/BitmapFont/Exporter_FNT")]
    private static void CreateFont()
    {
        ScriptableWizard.DisplayWizard<FNTFontCreate>("Create Font");
    }


    private void OnWizardCreate()
    {
        if (FontFile == null || TextureFile == null)
        {
            Debug.LogError("找不到資源檔");
            return;
        }

        //var path = EditorUtility.SaveFolderPanel("Save Font", "", "");
        string path = EditorUtility.SaveFilePanelInProject("Save Font", FontFile.name, "", "");

        if (!string.IsNullOrEmpty(path))
        {
            ResolveFont(path);
        }
    }
    /// <summary>
    /// 文字轉換成編號
    /// </summary>
    public int ASC(string S)
    {
        int N = Convert.ToInt32(S[0]);
        return N;
    }

    private void ResolveFont(string exportPath)
    {
        string fontName = exportPath.Remove(0, exportPath.LastIndexOf("/") + 1);
        exportPath = exportPath.Remove(exportPath.LastIndexOf("/") + 1);
        if (!FontFile) throw new UnityException(FontFile.name + "is not a valid font-fnt file");


        Font font = new Font();

        JsonData jd = JsonMapper.ToObject(FontFile.text);


        //XmlNode info = xml.GetElementsByTagName("TextureFont")[0];
        //BaseWidth = ToInt(info, "width");
        //BaseHeight = ToInt(info, "height");
        JsonData jdchars = jd["frames"];

        List<string> chars = new List<string>();

        foreach (string key in jdchars.Keys)
        {
            string tmpkey = key;
            chars.Add(tmpkey);
        }

        CharacterInfo[] charInfos = new CharacterInfo[chars.Count];
        int MaxHight = 0;
        for (int cnt = 0; cnt < chars.Count; cnt++)
        {
            string value = chars[cnt];
            int charsX = (int)jdchars[value]["x"];
            int charsY = (int)jdchars[value]["y"];
            int charsW = (int)jdchars[value]["w"];
            int charsH = (int)jdchars[value]["h"];
            int offX = (int)jdchars[value]["offX"];
            int offY = (int)jdchars[value]["offY"];
            int sourceW = (int)jdchars[value]["sourceW"];
            int sourceH = (int)jdchars[value]["sourceH"];

            if(MaxHight<sourceH)
            {
                MaxHight = sourceH;
            }
            CharacterInfo charInfo = CreateCharInfo(ASC(value), charsX, charsY, charsW, charsH, offX, offY, sourceW, sourceH);

            charInfos[cnt] = charInfo;
        }


        Shader shader = Shader.Find("UI/Default");
        Material material = new Material(shader)
        {
            mainTexture = TextureFile
        };
        //資料夾不存在
        string matDic = $"{exportPath}Materials/" + fontName;
        if (!Directory.Exists(matDic))
        {
            Directory.CreateDirectory(matDic);
        }
        //創建材質
        AssetDatabase.CreateAsset(material, matDic + ".mat");


        font.material = material;
        font.name = fontName;
        font.characterInfo = charInfos;
        //設定字形高度
        SerializedObject so = new SerializedObject(font);
        so.Update();
        so.FindProperty("m_LineSpacing").floatValue = MaxHight + SpacingAdjustment.y;
        so.ApplyModifiedProperties();
        so.SetIsDifferentCacheDirty();
        AssetDatabase.CreateAsset(font, exportPath + fontName + ".fontsettings");
    }
    private CharacterInfo CreateCharInfo(int id, int x, int y, int w, int h, int xo, int yo, int xadvance, int lineBaseHeight)
    {
        Rect uv = new Rect
        {
            x = (float)x / TextureFile.width,
            y = (float)y / TextureFile.height,
            width = (float)w / TextureFile.width,
            height = (float)h / TextureFile.height
        };
        uv.y = 1f - uv.y - uv.height;

        Rect vert = new Rect
        {
            x = xo,
            y = -yo + lineBaseHeight / 2,

            width = w,
            height = -h
        };

        CharacterInfo charInfo = new CharacterInfo();
        charInfo.index = id;

#if UNITY_5_3_OR_NEWER || UNITY_5_3 || UNITY_5_2
        charInfo.uvBottomLeft = new Vector2(uv.xMin, uv.yMin);
        charInfo.uvBottomRight = new Vector2(uv.xMax, uv.yMin);
        charInfo.uvTopLeft = new Vector2(uv.xMin, uv.yMax);
        charInfo.uvTopRight = new Vector2(uv.xMax, uv.yMax);

        charInfo.minX = (int)vert.xMin;
        charInfo.maxX = (int)vert.xMax;
        charInfo.minY = (int)vert.yMax;
        charInfo.maxY = (int)vert.yMin;
        charInfo.advance = xadvance + (int)SpacingAdjustment.x;

        charInfo.bearing = (int)vert.x;
#else

#endif
        return charInfo;
    }

}
