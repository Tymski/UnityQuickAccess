using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

public class QuickAccessWindow : EditorWindow
{
    private string path = "";
    private string query = "";
    string queryOld = "";
    private Vector2 scrollPosition;
    string prevQuery = "";
    string prevPath = "";
    static bool focusQuery;

    Dictionary<Type, string> typeColors = new()
    {
        { typeof(MonoScript), "#AACDDC" },
        { typeof(Texture2D), "#F7C6C5" },
        { typeof(DefaultAsset), "#F3DA4A" }
    };

    [MenuItem("Tools/QuickAccess %q")]
    public static void ShowWindow()
    {
        QuickAccessWindow window = GetWindow<QuickAccessWindow>("QuickAccess");
        window.Focus();
        focusQuery = true;
    }

    private List<string> sortedAssets = new();

    private static GUIStyle cachedStyle;
    private static GUIStyle rowEven;
    private static GUIStyle rowOdd;

    private Texture2D MakeTex(int width, int height, Color col)
    {
        Color[] pix = new Color[width * height];

        for (int i = 0; i < pix.Length; i++)
            pix[i] = col;

        Texture2D result = new(width, height);
        result.SetPixels(pix);
        result.Apply();

        return result;
    }

    private void OnGUI()
    {
        if (cachedStyle == null)
        {
            cachedStyle = new GUIStyle(GUI.skin.label);
            //cachedStyle.normal.textColor = Color.black;
            cachedStyle.alignment = TextAnchor.MiddleLeft;
            cachedStyle.richText = true;
            rowOdd = new(cachedStyle);
            rowEven = new(cachedStyle);
            rowOdd.normal.background = MakeTex(1, 1, new Color(0.21f, 0.21f, 0.21f));
            rowEven.normal.background = MakeTex(1, 1, new Color(0.235f, 0.235f, 0.235f));
        }

        GUI.SetNextControlName("Path");
        path = EditorGUILayout.TextField("Path", path);
        GUI.SetNextControlName("Query");
        query = EditorGUILayout.TextField("Query", query);



        if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Return)
        {
            Close();
            UnityEngine.Object asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(sortedAssets[0]);
            if (Event.current.control && !Event.current.shift) AssetDatabase.OpenAsset(asset);
            else if (!Event.current.control && !Event.current.shift) Selection.activeObject = asset;
            else if (Event.current.control && Event.current.shift) EditorUtility.RevealInFinder(sortedAssets[0]);
        }

        if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Q && Event.current.control)
        {
            EditorGUI.FocusTextInControl("Query");
        }
        if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.E && Event.current.control)
        {
            EditorGUI.FocusTextInControl("Path");
        }

        if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.W && Event.current.control)
        {
            Close();
        }

        float searchResultHeight = 18;
        float totalHeight = searchResultHeight * sortedAssets.Count;
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.ExpandHeight(true));
        row = 0;
        for (int i = 0; i < sortedAssets.Count; i++)
        {
            string asset = sortedAssets[i];
            DisplaySearchResultRow(asset, i, searchResultHeight);
        }
        EditorGUILayout.LabelField($"And {count - 100} more...");

        EditorGUILayout.EndScrollView();

        if (focusQuery)
        {
            EditorGUI.FocusTextInControl("Query");
            focusQuery = false;
        }
    }
    int row = 0;

    //private void DisplaySearchResultRow(string asset, int rowIndex, float rowHeight)
    //{
    //    Texture2D icon = AssetDatabase.GetCachedIcon(asset) as Texture2D;

    //    GUILayout.BeginHorizontal();
    //    if (GUILayout.Button(icon, GUIStyle.none, GUILayout.Width(rowHeight), GUILayout.Height(rowHeight)))
    //    {
    //        Selection.activeObject = AssetDatabase.LoadAssetAtPath(asset, typeof(UnityEngine.Object));
    //    }
    //    GUILayout.Space(-5);
    //    if (GUILayout.Button(ColorFileName(asset), cachedStyle, GUILayout.Height(rowHeight - 2)))
    //    {
    //        Selection.activeObject = AssetDatabase.LoadAssetAtPath(asset, typeof(UnityEngine.Object));
    //    }
    //    GUILayout.EndHorizontal();
    //}

    private void DisplaySearchResultRow(string asset, int rowIndex, float rowHeight)
    {
        Texture2D icon = AssetDatabase.GetCachedIcon(asset) as Texture2D;

        UnityEngine.Object assetObject = AssetDatabase.LoadAssetAtPath(asset, typeof(UnityEngine.Object));
        if (assetObject == null) return;
        GUIStyle style = row++ % 2 == 0 ? rowEven : rowOdd;

        GUILayout.BeginHorizontal();
        if (GUILayout.Button(icon, GUIStyle.none, GUILayout.Width(rowHeight), GUILayout.Height(rowHeight)))
        {
            Selection.activeObject = assetObject;
        }
        GUILayout.Space(-5);
        if (GUILayout.Button(ColorNameToType(asset, assetObject), style, GUILayout.Height(rowHeight), GUILayout.Width(280)))
        {
            Debug.Log(assetObject.GetType());
            Selection.activeObject = assetObject;
        }
        GUILayout.Space(-5);
        if (GUILayout.Button(ColorFileNameWithPath(asset), style, GUILayout.Height(rowHeight)))
        {
            Selection.activeObject = assetObject;
        }
        GUILayout.EndHorizontal();
        //GUI.color = Color.black;
        //GUILayout.Box("", GUILayout.ExpandWidth(true), GUILayout.Height(1));
        //GUI.color = Color.white;
    }


    private void Update()
    {
        if (query != prevQuery)
        {
            prevQuery = query;
            Search();
        }
        if (path != prevPath)
        {
            prevPath = path;
            Search();
        }
    }

    private string ColorFileNameWithPath(string filePath)
    {
        string fileName = Path.GetFileNameWithoutExtension(filePath);
        string directoryName = Path.GetDirectoryName(filePath);
        string fileExtension = Path.GetExtension(filePath);

        return "<color=grey>" + directoryName + "\\</color><color=white>" + BoldMatchingLetters(fileName, queryOld) + "</color>" + fileExtension;
    }

    private string ColorNameToType(string filePath, UnityEngine.Object asset)
    {
        string result = "";
        if (typeColors.ContainsKey(asset.GetType())) result = "<color=" + typeColors[asset.GetType()] + ">";
        result += Path.GetFileName(filePath);
        if (typeColors.ContainsKey(asset.GetType())) result += "</color>";
        return result;
    }

    private string BoldMatchingLetters(string filePath, string search)
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
                sb.Append("<b>" + fileName[i] + "</b>");
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
    private void Search()
    {
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

    public static int LevenshteinDistance(string s, string t)
    {
        if (s == t) return 0;
        if (s.Length == 0) return t.Length;
        if (t.Length == 0) return s.Length;
        int[,] d = new int[s.Length + 1, t.Length + 1];
        for (int i = 0; i <= s.Length; i++) d[i, 0] = i;
        for (int j = 0; j <= t.Length; j++) d[0, j] = j;
        for (int i = 1; i <= s.Length; i++)
        {
            for (int j = 1; j <= t.Length; j++)
            {
                int cost = (t[j - 1] == s[i - 1]) ? 0 : 1;
                d[i, j] = Math.Min(
                    Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                    d[i - 1, j - 1] + cost);
            }
        }
        return d[s.Length, t.Length];
    }

    public static int DamerauLevenshteinDistance(string s, string t)
    {
        int n = s.Length;
        int m = t.Length;
        int[,] d = new int[n + 1, m + 1];

        for (int i = 0; i <= n; i++) d[i, 0] = i;
        for (int j = 0; j <= m; j++) d[0, j] = j;

        for (int i = 1; i <= n; i++)
        {
            for (int j = 1; j <= m; j++)
            {
                int cost = (t[j - 1] == s[i - 1]) ? 0 : 1;

                d[i, j] = Math.Min(
                    Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                    d[i - 1, j - 1] + cost);

                if (i > 1 && j > 1 && s[i - 1] == t[j - 2] && s[i - 2] == t[j - 1])
                {
                    d[i, j] = Math.Min(d[i, j], d[i - 2, j - 2] + cost);
                }
            }
        }
        return d[n, m];
    }

    public static double JaroDistance(string s1, string s2)
    {
        int m = 0;
        int t = 0;

        int s1Length = s1.Length;
        int s2Length = s2.Length;

        if (s1Length == 0 && s2Length == 0) return 1.0;

        int range = Math.Max(0, Math.Max(s1Length, s2Length) / 2 - 1);

        bool[] s1Matches = new bool[s1Length];
        bool[] s2Matches = new bool[s2Length];

        for (int i = 0; i < s1Length; i++)
        {
            int start = Math.Max(0, i - range);
            int end = Math.Min(i + range + 1, s2Length);

            for (int j = start; j < end; j++)
            {
                if (s1Matches[i] || s2Matches[j]) continue;
                if (s1[i] != s2[j]) continue;

                s1Matches[i] = true;
                s2Matches[j] = true;
                m++;
                break;
            }
        }

        if (m == 0) return 0.0;

        int k = 0;
        for (int i = 0; i < s1Length; i++)
        {
            if (!s1Matches[i]) continue;
            while (!s2Matches[k]) k++;
            if (s1[i] != s2[k]) t++;
            k++;
        }

        return (m / (double)s1Length + m / (double)s2Length + (m - t / 2.0) / m) / 3.0;
    }

    //public static float MyDistance(string s1, string s2)
    //{
    //    float distance = 0;

    //    for (int i = 0; i < s1.Length; i++)
    //    {
    //        char c = s1[i];
    //        int index = s2.IndexOf(c);
    //        if (index < 0)
    //        {
    //            distance += 100;
    //        }
    //        else
    //        {
    //            distance += index;
    //            if (char.IsUpper(s2[index]))
    //            {
    //                distance -= 10;
    //            }
    //        }
    //    }
    //    Debug.Log("Distance from " + s1 + " to " + s2 + " is " + distance);

    //    return distance;
    //}

    //public static float MyDistance(string s1, string s2)
    //{
    //    float distance = 0;
    //    int lastIndex = -1;

    //    for (int i = 0; i < s1.Length; i++)
    //    {
    //        char c = s1[i];
    //        int index = s2.IndexOf(c, lastIndex + 1);
    //        if (index < 0)
    //        {
    //            distance += 100;
    //        }
    //        else
    //        {
    //            distance += index;
    //            if (char.IsUpper(s2[index]))
    //            {
    //                distance -= 10;
    //            }
    //            lastIndex = index;
    //        }
    //    }

    //    Debug.Log("Distance from " + s1 + " to " + s2 + " is " + distance);
    //    return distance;
    //}

    public static float MyDistance(string s1, string s2)
    {
        s1 = s1.ToLowerInvariant();
        float distance = 0;
        int lastIndex = -1;

        for (int i = 0; i < s1.Length; i++)
        {
            char c = s1[i];
            int index = s2.IndexOf(c.ToString(), lastIndex + 1, comparisonType: StringComparison.OrdinalIgnoreCase);
            if (index < 0)
            {
                distance += 100;
                int index2 = s2.IndexOf(c.ToString(), comparisonType: StringComparison.OrdinalIgnoreCase);
                if (index2 > 0) distance -= 30;
            }
            else
            {
                distance += index - lastIndex;
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

        //Debug.Log("Distance from " + s1 + " to " + s2 + " is " + distance);
        return distance;
    }

    void OnDisable()
    {
        EditorPrefs.SetString(pathKey, path);
        EditorPrefs.SetString(queryKey, query);
    }

    void OnEnable()
    {
        path = EditorPrefs.GetString(pathKey, "");
        query = EditorPrefs.GetString(queryKey, "");
    }

    string queryKey = "com.tymski.quickaccess.path";
    string pathKey = "com.tymski.quickaccess.query";
}

