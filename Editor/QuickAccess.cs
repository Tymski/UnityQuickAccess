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

    [MenuItem("Tools/QuickAccess %q")]
    public static void ShowWindow()
    {
        QuickAccessWindow window = GetWindow<QuickAccessWindow>("QuickAccess");
        window.Focus();
    }

    private List<string> sortedAssets = new();

    private static GUIStyle cachedStyle;

    private void OnGUI()
    {
        if (cachedStyle == null)
        {
            cachedStyle = new GUIStyle(GUI.skin.label);
            //cachedStyle.normal.textColor = Color.black;
            cachedStyle.alignment = TextAnchor.MiddleLeft;
            cachedStyle.richText = true;
        }

        path = EditorGUILayout.TextField("Path", path);
        query = EditorGUILayout.TextField("Query", query);

        if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Return)
        {
            Search();
        }

        if (GUILayout.Button("Search"))
        {
            Search();
        }

        float searchResultHeight = 18;
        float totalHeight = searchResultHeight * sortedAssets.Count;
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.ExpandHeight(true));

        for (int i = 0; i < sortedAssets.Count; i++)
        {
            string asset = sortedAssets[i];
            DisplaySearchResultRow(asset, i, searchResultHeight);
        }
        EditorGUILayout.LabelField($"And {count - 100} more...");

        EditorGUILayout.EndScrollView();
    }

    //private void DisplaySearchResultRow(string asset, int rowIndex, float rowHeight)
    //{
    //    Rect labelRect = new(20, rowIndex * rowHeight, position.width - 20, rowHeight);
    //    Texture2D icon = AssetDatabase.GetCachedIcon(asset) as Texture2D;
    //    GUI.DrawTexture(new Rect(0, rowIndex * rowHeight, 20, rowHeight), icon, ScaleMode.ScaleToFit);
    //    //EditorGUI.SelectableLabel(labelRect, asset);

    //    if (GUILayout.Button(asset, cachedStyle))
    //    {
    //        Selection.activeObject = AssetDatabase.LoadAssetAtPath(asset, typeof(UnityEngine.Object));
    //    }
    //    //if (Event.current.type == EventType.MouseDown && Event.current.button == 0 && labelRect.Contains(Event.current.mousePosition))
    //    //{
    //    //    Selection.activeObject = AssetDatabase.LoadAssetAtPath(asset, typeof(UnityEngine.Object));
    //    //    Event.current.Use();
    //    //}
    //}

    private void DisplaySearchResultRow(string asset, int rowIndex, float rowHeight)
    {
        Texture2D icon = AssetDatabase.GetCachedIcon(asset) as Texture2D;

        GUILayout.BeginHorizontal();
        if (GUILayout.Button(icon, GUIStyle.none, GUILayout.Width(rowHeight), GUILayout.Height(rowHeight)))
        {
            Selection.activeObject = AssetDatabase.LoadAssetAtPath(asset, typeof(UnityEngine.Object));
        }
        GUILayout.Space(-5);
        if (GUILayout.Button(ColorFileName(asset), cachedStyle, GUILayout.Height(rowHeight - 2)))
        {
            Selection.activeObject = AssetDatabase.LoadAssetAtPath(asset, typeof(UnityEngine.Object));
        }
        GUILayout.EndHorizontal();
    }

    private void Update()
    {
        if (query != prevQuery)
        {
            prevQuery = query;
            Search();
        }
    }

    private string ColorFileName(string filePath)
    {
        string fileName = Path.GetFileNameWithoutExtension(filePath);
        string directoryName = Path.GetDirectoryName(filePath);
        string fileExtension = Path.GetExtension(filePath);

        return "<color=grey>" + directoryName + "\\</color><color=white>" + BoldMatchingLetters(fileName, queryOld) + "</color>" + fileExtension;
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
            if (assetPath.Contains(path))
            {
                assets[i] = assetPath;
            }
        }
        sortedAssets = assets.Where(s => s != null).OrderBy(s => MyDistance(query, Path.GetFileNameWithoutExtension(s))).ToList();
        sortedAssets = sortedAssets.Take(100).ToList();
        scrollPosition = Vector2.zero;
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
    //        }
    //    }
    //    return distance;
    //}

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
            }
            else
            {
                distance += index;
                if (char.IsUpper(s2[index]))
                {
                    distance -= 10;
                }
                lastIndex = index;
            }
        }

        //Debug.Log("Distance from " + s1 + " to " + s2 + " is " + distance);
        return distance;
    }
}

