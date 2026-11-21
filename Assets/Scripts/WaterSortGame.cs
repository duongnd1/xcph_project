using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class WaterSortGame : MonoBehaviour
{
    private const string SavePrefix = "ChromaticPour.v1.";
    private const string Symbols = "0123456789ABCDEF";
    private static readonly string[] Groups = { "Main", "Daily", "Advanced", "Bonus" };
    private static readonly Color32[] Colors =
    {
        new Color32(255, 117, 126, 255),
        new Color32(93, 205, 190, 255),
        new Color32(255, 205, 103, 255),
        new Color32(151, 158, 255, 255),
        new Color32(249, 153, 233, 255),
        new Color32(248, 159, 94, 255),
        new Color32(110, 190, 245, 255),
        new Color32(192, 230, 117, 255),
        new Color32(203, 166, 136, 255),
        new Color32(227, 232, 241, 255),
        new Color32(111, 218, 148, 255),
        new Color32(192, 149, 237, 255),
        new Color32(249, 225, 161, 255)
    };
    private readonly List<List<int>> tubes = new List<List<int>>();
    private readonly Stack<int[][]> history = new Stack<int[][]>();
    private readonly Dictionary<int, string[]> catalog = new Dictionary<int, string[]>();
    private int group;
    private int capacity;
    private int level;
    private string levelInput;
    private bool cleared;
    private int selected = -1;
    private bool solved;
    private string feedback = "";
    private Texture2D backgroundArt;
    private Texture2D topPanelArt;
    private Texture2D levelFrameArt;
    private Texture2D tubeArt;
    private Texture2D restartButtonArt;
    private Texture2D nextButtonArt;
    private Texture2D winArt;
    private Texture2D starFilledArt;
    private readonly Texture2D[] fruitArt = new Texture2D[13];
    private static readonly string[] FruitSources =
    {
        "strawberry", "apple", "blueberry", "banana", "grape", "orange", "acorn", "mushroom",
        "strawberry", "apple", "blueberry", "banana", "grape"
    };

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Launch()
    {
        var root = new GameObject("Water Sort Game");
        Camera camera = root.AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color32(12, 18, 28, 255);
        camera.cullingMask = 0;
        root.AddComponent<WaterSortGame>();
    }

    private void Awake()
    {
        LoadArt();
        OpenGroup(Mathf.Clamp(PlayerPrefs.GetInt(SavePrefix + "group"), 0, Groups.Length - 1));
    }

    private void LoadArt()
    {
        backgroundArt = LoadArtTexture("Background/game_background");
        topPanelArt = LoadArtTexture("UI/panel_top");
        levelFrameArt = LoadArtTexture("UI/level_frame");
        tubeArt = LoadArtTexture("Tubes/tube_glass");
        restartButtonArt = LoadArtTexture("UI/button_restart");
        nextButtonArt = LoadArtTexture("UI/button_next");
        winArt = LoadArtTexture("Finish/you_win");
        starFilledArt = LoadArtTexture("Finish/star_filled");
        for (int i = 0; i < fruitArt.Length; i++)
            fruitArt[i] = LoadArtTexture("Fruits/fruit_" + FruitSources[i]);
    }

    private static Texture2D LoadArtTexture(string path)
    {
        return Resources.Load<Texture2D>("Art/FruitsBomb/" + path);
    }

    private static void DrawArt(Texture2D texture, Rect rect, ScaleMode scaleMode = ScaleMode.StretchToFill)
    {
        if (texture != null)
            GUI.DrawTexture(rect, texture, scaleMode, true);
    }

    private string GroupKey => SavePrefix + Groups[group];
    private string ClearedKey => GroupKey + ".cleared." + level;
    private string[] Levels => catalog[group];

    private void OpenGroup(int index)
    {
        group = index;
        if (!catalog.ContainsKey(group))
        {
            TextAsset asset = Resources.Load<TextAsset>("Levels" + Groups[group]);
            catalog.Add(group, asset.text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries));
        }

        PlayerPrefs.SetInt(SavePrefix + "group", group);
        LoadLevel(Mathf.Clamp(PlayerPrefs.GetInt(GroupKey + ".level"), 0, Levels.Length - 1));
    }

    private void LoadLevel(int index)
    {
        level = index;
        string[] fields = Levels[index].Split('|');
        capacity = int.Parse(fields[0]);
        tubes.Clear();
        foreach (string layout in fields[1].Split(','))
        {
            var tube = new List<int>();
            foreach (char color in layout)
                tube.Add(Symbols.IndexOf(color));
            tubes.Add(tube);
        }

        selected = -1;
        solved = false;
        cleared = PlayerPrefs.GetInt(ClearedKey) == 1;
        levelInput = (level + 1).ToString();
        feedback = "";
        history.Clear();
        PlayerPrefs.SetInt(GroupKey + ".level", level);
        PlayerPrefs.Save();
    }

    private void SelectTube(int index)
    {
        if (solved)
            return;

        if (selected == index)
        {
            selected = -1;
            feedback = "";
            return;
        }

        if (selected < 0)
        {
            if (tubes[index].Count > 0)
                selected = index;
            return;
        }

        List<int> source = tubes[selected];
        List<int> target = tubes[index];
        int color = source[source.Count - 1];
        if (target.Count == capacity || (target.Count > 0 && target[target.Count - 1] != color))
        {
            feedback = "Choose an empty tube or one with the same top color.";
            return;
        }

        var snapshot = new int[tubes.Count][];
        for (int i = 0; i < tubes.Count; i++)
            snapshot[i] = tubes[i].ToArray();
        history.Push(snapshot);

        while (target.Count < capacity && source.Count > 0 && source[source.Count - 1] == color)
        {
            source.RemoveAt(source.Count - 1);
            target.Add(color);
        }

        selected = -1;
        feedback = "";
        solved = tubes.TrueForAll(IsComplete);
        if (solved && !cleared)
        {
            cleared = true;
            PlayerPrefs.SetInt(ClearedKey, 1);
            PlayerPrefs.Save();
        }
    }

    private bool IsComplete(List<int> tube)
    {
        return tube.Count == 0 ||
            (tube.Count == capacity && tube.TrueForAll(color => color == tube[0]));
    }

    private void Undo()
    {
        int[][] snapshot = history.Pop();
        for (int i = 0; i < tubes.Count; i++)
        {
            tubes[i].Clear();
            tubes[i].AddRange(snapshot[i]);
        }

        selected = -1;
        solved = false;
        feedback = "";
    }

    private void OnGUI()
    {
        const float width = 480;
        const float height = 630;
        Matrix4x4 previousMatrix = GUI.matrix;
        float scale = Mathf.Min(Screen.width / width, Screen.height / height);
        GUI.matrix = Matrix4x4.TRS(
            new Vector3((Screen.width - width * scale) / 2, (Screen.height - height * scale) / 2, 0),
            Quaternion.identity, new Vector3(scale, scale, 1));
        DrawArt(backgroundArt, new Rect(0, 0, width, height), ScaleMode.ScaleAndCrop);
        Fill(new Rect(0, 0, width, height), new Color32(10, 40, 70, 38));
        DrawArt(topPanelArt, new Rect(0, 0, width, 118));

        var heading = new GUIStyle(GUI.skin.label) { fontSize = 29, fontStyle = FontStyle.Bold };
        heading.normal.textColor = Color.white;
        GUI.Label(new Rect(30, 30, 420, 45), "Chromatic Pour", heading);
        for (int i = 0; i < Groups.Length; i++)
        {
            GUI.enabled = i != group;
            if (GUI.Button(new Rect(32 + i * 105, 83, 100, 30), Groups[i]))
                OpenGroup(i);
        }
        GUI.enabled = true;
        DrawArt(levelFrameArt, new Rect(304, 116, 110, 44));
        GUI.Label(new Rect(32, 122, 260, 24),
            $"Level {level + 1} / {Levels.Length}    Moves {history.Count}" + (cleared ? "    Cleared" : ""));
        levelInput = GUI.TextField(new Rect(304, 122, 88, 27), levelInput, 5);
        if (GUI.Button(new Rect(399, 122, 48, 27), "Go"))
        {
            if (int.TryParse(levelInput, out int number) && number >= 1 && number <= Levels.Length)
                LoadLevel(number - 1);
            else
                feedback = $"Enter a level from 1 to {Levels.Length}.";
        }
        GUI.Label(new Rect(32, 153, 416, 24), "Select a tube, then select where to pour.");

        int columns = Mathf.Min(tubes.Count, 8);
        for (int i = 0; i < tubes.Count; i++)
        {
            int row = i / columns;
            int rowCount = Mathf.Min(columns, tubes.Count - row * columns);
            float x = (width - (rowCount * 52 - 6)) / 2 + i % columns * 52;
            float y = 188 + row * 158 - (i == selected ? 6 : 0);
            if (GUI.Button(new Rect(x, y, 46, 145), GUIContent.none))
                SelectTube(i);
            Fill(new Rect(x + 5, y + 5, 36, 120), i == selected
                ? new Color32(72, 92, 121, 255) : new Color32(36, 48, 68, 255));
            float layer = 116f / capacity;
            for (int j = 0; j < tubes[i].Count; j++)
            {
                int color = tubes[i][j];
                var rect = new Rect(x + 7, y + 123 - (j + 1) * layer, 32, layer - 1);
                Fill(rect, new Color32(255, 255, 255, 22));
                Texture2D fruit = fruitArt[Mathf.Clamp(color, 0, fruitArt.Length - 1)];
                DrawArt(fruit, new Rect(rect.x, rect.y, rect.width, rect.height), ScaleMode.ScaleToFit);
            }
            DrawArt(tubeArt, new Rect(x - 1, y - 8, 48, 153), ScaleMode.StretchToFill);
            GUI.Label(new Rect(x + 13, y + 125, 32, 18), (i + 1).ToString());
        }

        string status = feedback;
        if (solved && winArt != null)
        {
            DrawArt(winArt, new Rect(148, 478, 184, 55), ScaleMode.ScaleToFit);
            if (starFilledArt != null)
            {
                DrawArt(starFilledArt, new Rect(190, 428, 42, 42), ScaleMode.ScaleToFit);
                DrawArt(starFilledArt, new Rect(240, 428, 42, 42), ScaleMode.ScaleToFit);
            }
        }
        else
            GUI.Label(new Rect(32, 501, 416, 32), status);

        GUI.enabled = history.Count > 0;
        if (GUI.Button(new Rect(32, 548, 128, 58), "Undo"))
            Undo();
        GUI.enabled = true;
        if (GUI.Button(new Rect(176, 548, 128, 58), new GUIContent(restartButtonArt)))
            LoadLevel(level);
        GUI.enabled = cleared && level + 1 < Levels.Length;
        if (GUI.Button(new Rect(320, 548, 128, 58), new GUIContent(nextButtonArt)))
            LoadLevel(level + 1);
        GUI.enabled = true;
        GUI.matrix = previousMatrix;
    }

    private static void Fill(Rect rect, Color tint)
    {
        if (Event.current.type != EventType.Repaint)
            return;

        Color previous = GUI.color;
        GUI.color = tint;
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
        GUI.color = previous;
    }
}
