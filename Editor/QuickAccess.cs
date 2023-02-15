using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

using Object = UnityEngine.Object;

public class QuickAccessWindow : EditorWindow
{
    static string queryKey = "";
    static string pathKey = "";
    static string selectedKey = "";

    static GUIStyle cachedStyle;
    static GUIStyle rowEven;
    static GUIStyle rowOdd;
    static GUIStyle selectedStyle;

    static bool focusQuery;
    string path = "";
    string query = "";
    string queryOld = "";
    string previousFrameQuery = "";
    string previousFramePath = "";
    int row = 0;
    int selected = 0;
    Vector2 scrollPosition;
    List<string> sortedAssets = new();

    Dictionary<Type, string> typeColors = new()
    {
        { typeof(MonoScript), "#AACDDC" },
        { typeof(Texture2D), "#F7C6C5" },
        { typeof(DefaultAsset), "#F3DA4A" }
    };

    [MenuItem("Tools/Tymski/QuickAccess %q")]
    public static void ShowWindow()
    {
        QuickAccessWindow window = GetWindow<QuickAccessWindow>("QuickAccess");
        window.Focus();
        focusQuery = true;
    }

    Texture2D MakeTex(int width, int height, Color col)
    {
        Color[] pix = new Color[width * height];
        for (int i = 0; i < pix.Length; i++) pix[i] = col;
        Texture2D result = new(width, height);
        result.SetPixels(pix);
        result.Apply();
        return result;
    }

    void OnGUI()
    {
        Initialize();
        if (rowOdd.normal.background == null) CreateBackgroundTextures();

        HandleInput1();

        GUI.SetNextControlName("Path");
        EditorGUIUtility.labelWidth = 35;
        path = EditorGUILayout.TextField("Path", path);
        GUI.SetNextControlName("Query");
        query = EditorGUILayout.TextField("Query", query);
        EditorGUIUtility.labelWidth = 0;

        float searchResultHeight = 18;
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.ExpandHeight(true));
        row = 0;
        for (int i = 0; i < sortedAssets.Count; i++)
        {
            string asset = sortedAssets[i];
            DisplaySearchResultRow(asset, i, searchResultHeight);
        }
        EditorGUILayout.LabelField($"And {count - 100} more...");

        EditorGUILayout.EndScrollView();

        HandleInput2();

        if (focusQuery)
        {
            EditorGUI.FocusTextInControl("Query");
            focusQuery = false;
        }
    }

    void DisplaySearchResultRow(string asset, int rowIndex, float rowHeight)
    {
        Texture2D icon = AssetDatabase.GetCachedIcon(asset) as Texture2D;

        Object assetObject = AssetDatabase.LoadAssetAtPath(asset, typeof(Object));
        if (assetObject == null) return;
        GUIStyle style = row++ % 2 == 0 ? rowEven : rowOdd;
        if (rowIndex == selected) style = selectedStyle;

        GUILayout.BeginHorizontal();
        if (GUILayout.Button(icon, GUIStyle.none, GUILayout.Width(rowHeight), GUILayout.Height(rowHeight)))
        {
            PerformAction(assetObject, row);
        }
        GUILayout.Space(-5);
        if (GUILayout.Button(ColorNameToType(asset, assetObject), style, GUILayout.Height(rowHeight), GUILayout.Width(280)))
        {
            PerformAction(assetObject, row);
        }
        GUILayout.Space(-5);
        if (GUILayout.Button(ColorFileNameWithPath(asset), style, GUILayout.Height(rowHeight)))
        {
            PerformAction(assetObject, row);
        }
        GUILayout.EndHorizontal();
    }

    public void PerformAction(Object asset, int rowIndex)
    {
        if (Event.current.control || Event.current.shift) Close();
        EditorUtility.FocusProjectWindow();
        Selection.SetActiveObjectWithContext(asset, asset);
        if (Event.current.control && !Event.current.shift) AssetDatabase.OpenAsset(asset);
        else if (!Event.current.control && !Event.current.shift) Selection.activeObject = asset;
        else if (Event.current.control && Event.current.shift) EditorUtility.RevealInFinder(sortedAssets[selected]);
        selected = rowIndex - 1;
        Event.current.Use();
    }

    void HandleInput1()
    {
        if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.DownArrow)
        {
            selected++;
            Event.current.Use();
            Repaint();
        }
        if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.UpArrow)
        {
            selected--;
            selected = Mathf.Clamp(selected, 0, sortedAssets.Count);
            Event.current.Use();
            Repaint();
        }
        if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Return)
        {
            Close();
            Object asset = AssetDatabase.LoadAssetAtPath<Object>(sortedAssets[selected]);
            if (Event.current.control && !Event.current.shift)
            {
                AssetDatabase.OpenAsset(asset);
            }
            else if (!Event.current.control && !Event.current.shift)
            {
                EditorUtility.FocusProjectWindow();
                Selection.SetActiveObjectWithContext(asset, asset);
            }
            else if (Event.current.control && Event.current.shift)
            {
                EditorUtility.RevealInFinder(sortedAssets[selected]);
            }
        }
        if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.W && Event.current.control)
        {
            Close();
            Event.current.Use();
        }
        if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape)
        {
            Close();
            Event.current.Use();
        }
    }

    void HandleInput2()
    {
        if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Q && Event.current.control)
        {
            EditorGUI.FocusTextInControl("Query");
        }

        if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.E && Event.current.control)
        {
            EditorGUI.FocusTextInControl("Path");
        }
    }

    static bool initialized;
    void Initialize()
    {
        if (initialized) return;
        initialized = true;

        cachedStyle = new(GUI.skin.label);
        cachedStyle.alignment = TextAnchor.MiddleLeft;
        cachedStyle.richText = true;
        rowOdd = new(cachedStyle);
        rowEven = new(cachedStyle);
        selectedStyle = new(cachedStyle);
        CreateBackgroundTextures();
        Load();
    }

    void CreateBackgroundTextures()
    {
        rowOdd.normal.background = MakeTex(1, 1, new Color(0.21f, 0.21f, 0.21f));
        rowEven.normal.background = MakeTex(1, 1, new Color(0.235f, 0.235f, 0.235f));
        selectedStyle.normal.background = MakeTex(1, 1, new Color(0.25f, 0.25f, 0.535f));
    }

    void Update()
    {
        if (query != previousFrameQuery)
        {
            Search();
            previousFrameQuery = query;
        }
        if (path != previousFramePath)
        {
            Search();
            previousFramePath = path;
        }
    }

    string ColorFileNameWithPath(string filePath)
    {
        string fileName = Path.GetFileNameWithoutExtension(filePath);
        string directoryName = Path.GetDirectoryName(filePath);
        string fileExtension = Path.GetExtension(filePath);

        return "<color=grey>" + directoryName + "\\</color><color=white>" + BoldMatchingLetters(fileName, queryOld) + "</color>" + fileExtension;
    }

    string ColorNameToType(string filePath, UnityEngine.Object asset)
    {
        string result = "";
        if (typeColors.ContainsKey(asset.GetType())) result = "<color=" + typeColors[asset.GetType()] + ">";
        result += Path.GetFileName(filePath);
        if (typeColors.ContainsKey(asset.GetType())) result += "</color>";
        return result;
    }

    string BoldMatchingLetters(string filePath, string search)
    {
        string fileName = Path.GetFileNameWithoutExtension(filePath);
        string directoryName = Path.GetDirectoryName(filePath);
        string fileExtension = Path.GetExtension(filePath);

        StringBuilder sb = new(directoryName);

        int searchIndex = 0;
        for (int i = 0; i < fileName.Length; i++)
        {
            if (searchIndex >= search.Length)
            {
                sb.Append(fileName[i]);
                continue;
            }

            if (char.ToLower(fileName[i]) == char.ToLower(search[searchIndex]))
            {
                sb.Append("<b>").Append(fileName[i]).Append("</b>");
                searchIndex++;
            }
            else
            {
                sb.Append(fileName[i]);
            }
        }

        sb.Append(fileExtension);
        return sb.ToString();
    }

    int count = 0;
    void Search()
    {
        if (queryOld.CompareTo(query) != 0 || previousFramePath.CompareTo(path) != 0) selected = 0;
        queryOld = query;
        string[] guids = AssetDatabase.FindAssets("");
        string[] assets = new string[guids.Length];
        count = assets.Length;
        for (int i = 0; i < guids.Length; i++)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guids[i]);
            if (!Contains(assetPath, path)) continue;
            assets[i] = assetPath;
        }

        sortedAssets = assets.Where(s => s != null).OrderBy(s => MyDistance(query, What(s))).ToList();
        sortedAssets = sortedAssets.Take(100).ToList();
        scrollPosition = Vector2.zero;

        string What(string path)
        {
            if (query.Contains("/")) return path;
            if (query.Contains("\\")) return path;
            if (query.Contains(".")) return Path.GetFileName(path);
            return Path.GetFileNameWithoutExtension(path);
        }

        bool Contains(string assetPath, string s)
        {
            string[] split = s.Split(',');
            for (int j = 0; j < split.Length; j++)
            {
                if (!assetPath.Contains(split[j].Trim())) return false;
            }
            return true;
        }
    }

    public static float MyDistance(string s1, string s2)
    {
        float distance = 0;
        int lastIndex = -1;

        for (int i = 0; i < s1.Length; i++)
        {
            char c = s1[i];
            int index = 0;
            if (char.IsLower(c)) index = s2.IndexOf(c.ToString(), lastIndex + 1, comparisonType: StringComparison.OrdinalIgnoreCase);
            else index = s2.IndexOf(c.ToString(), lastIndex + 1, comparisonType: StringComparison.Ordinal);

            if (index < 0)
            {
                distance += 100;
                int index2 = s2.IndexOf(c.ToString(), comparisonType: StringComparison.OrdinalIgnoreCase);
                if (index2 > 0) distance -= 30;
            }
            else
            {
                distance += index - lastIndex;
                if (char.IsUpper(c) && char.IsUpper(s2[index])) distance -= 1;
                if (char.IsUpper(s2[index]))
                {
                    distance -= 15;
                }
                lastIndex = index;
            }
        }

        distance += (s2.Length - s1.Length) / 2f;

        if (s1.CompareTo(s2) == 0) distance -= 50;
        if (s1.Length > 0 && s2.Length > 0 && s1[0].CompareTo(s2[0]) == 0) distance -= 16;

        return distance;
    }

    void OnEnable()
    {
        Load();
        Search();
    }

    void OnDisable()
    {
        Save();
    }

    void Save()
    {
        SetKeys();
        EditorPrefs.SetString(pathKey, path);
        EditorPrefs.SetString(queryKey, query);
        EditorPrefs.SetInt(selectedKey, selected);
    }

    void Load()
    {
        SetKeys();
        path = previousFramePath = EditorPrefs.GetString(pathKey, "");
        query = queryOld = EditorPrefs.GetString(queryKey, "");
        selected = EditorPrefs.GetInt(selectedKey, selected);
    }

    void SetKeys()
    {
        queryKey = "unity_" + Application.productName + "_com.tymski.quickaccess.query";
        pathKey = "unity_" + Application.productName + "_com.tymski.quickaccess.path";
        selectedKey = "unity_" + Application.productName + "_com.tymski.quickaccess.selected";
    }
}
