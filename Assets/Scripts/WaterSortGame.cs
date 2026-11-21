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
        const float height = 800;
        Matrix4x4 previousMatrix = GUI.matrix;
        float scale = Mathf.Min(Screen.width / width, Screen.height / height);
        GUI.matrix = Matrix4x4.TRS(
            new Vector3((Screen.width - width * scale) / 2, (Screen.height - height * scale) / 2, 0),
            Quaternion.identity, new Vector3(scale, scale, 1));
        DrawArt(backgroundArt, new Rect(0, 0, width, height), ScaleMode.ScaleAndCrop);
        Fill(new Rect(0, 0, width, height), new Color32(10, 40, 70, 24));
        DrawArt(topPanelArt, new Rect(0, 0, width, 118));

        var heading = new GUIStyle(GUI.skin.label)
        {
            fontSize = 29,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
        heading.normal.textColor = Color.white;
        GUI.Label(new Rect(30, 22, 420, 48), "Chromatic Pour", heading);

        var tabStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = 15,
            fontStyle = FontStyle.Bold
        };
        tabStyle.normal.textColor = Color.white;
        tabStyle.hover.textColor = Color.white;
        for (int i = 0; i < Groups.Length; i++)
        {
            GUI.enabled = i != group;
            if (GUI.Button(new Rect(24 + i * 108, 100, 104, 38), Groups[i], tabStyle))
                OpenGroup(i);
        }
        GUI.enabled = true;

        var infoStyle = new GUIStyle(GUI.skin.label) { fontSize = 17 };
        infoStyle.normal.textColor = Color.white;
        GUI.Label(new Rect(24, 151, 220, 28), $"Level {level + 1} / {Levels.Length}", infoStyle);
        GUI.Label(new Rect(238, 151, 90, 28), $"Moves {history.Count}", infoStyle);

        DrawArt(levelFrameArt, new Rect(324, 145, 104, 44));
        var inputStyle = new GUIStyle(GUI.skin.textField)
        {
            fontSize = 17,
            alignment = TextAnchor.MiddleCenter
        };
        levelInput = GUI.TextField(new Rect(333, 153, 76, 28), levelInput, 5, inputStyle);
        if (GUI.Button(new Rect(432, 149, 48, 34), "Go", tabStyle))
        {
            if (int.TryParse(levelInput, out int number) && number >= 1 && number <= Levels.Length)
                LoadLevel(number - 1);
            else
                feedback = $"Enter a level from 1 to {Levels.Length}.";
        }
        GUI.Label(new Rect(24, 190, 432, 26), "Select a tube, then select where to pour.", infoStyle);

        int columns = Mathf.Min(tubes.Count, 5);
        const float tubeWidth = 54;
        const float tubeHeight = 160;
        const float tubeGap = 10;
        const float rowGap = 24;
        for (int i = 0; i < tubes.Count; i++)
        {
            int row = i / columns;
            int rowCount = Mathf.Min(columns, tubes.Count - row * columns);
            float rowWidth = rowCount * tubeWidth + (rowCount - 1) * tubeGap;
            float x = (width - rowWidth) / 2 + i % columns * (tubeWidth + tubeGap);
            float y = 224 + row * (tubeHeight + rowGap) - (i == selected ? 7 : 0);
            if (GUI.Button(new Rect(x, y, tubeWidth, tubeHeight), GUIContent.none))
                SelectTube(i);
            Fill(new Rect(x, y, tubeWidth, tubeHeight), i == selected
                ? new Color32(255, 208, 94, 100) : new Color32(13, 44, 72, 135));
            DrawArt(tubeArt, new Rect(x + 3, y - 4, 48, 153), ScaleMode.StretchToFill);
            float layer = 132f / capacity;
            for (int j = 0; j < tubes[i].Count; j++)
            {
                int color = tubes[i][j];
                var rect = new Rect(x + 8, y + 143 - (j + 1) * layer, 38, layer + 2);
                Texture2D fruit = fruitArt[Mathf.Clamp(color, 0, fruitArt.Length - 1)];
                DrawArt(fruit, new Rect(rect.x, rect.y, rect.width, rect.height), ScaleMode.ScaleToFit);
            }
            Fill(new Rect(x + 8, y + 8, 38, 3), new Color32(255, 255, 255, 210));
            Fill(new Rect(x + 8, y + 10, 3, 128), new Color32(255, 255, 255, 130));
            Fill(new Rect(x + 43, y + 10, 3, 128), new Color32(255, 255, 255, 90));
            var tubeNumber = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 14 };
            tubeNumber.normal.textColor = Color.white;
            GUI.Label(new Rect(x, y + 143, tubeWidth, 20), (i + 1).ToString(), tubeNumber);
        }

        string status = feedback;
        if (solved && winArt != null)
        {
            DrawArt(winArt, new Rect(148, 590, 184, 55), ScaleMode.ScaleToFit);
            if (starFilledArt != null)
            {
                DrawArt(starFilledArt, new Rect(190, 540, 42, 42), ScaleMode.ScaleToFit);
                DrawArt(starFilledArt, new Rect(240, 540, 42, 42), ScaleMode.ScaleToFit);
            }
        }
        else
            GUI.Label(new Rect(24, 590, 432, 32), status, infoStyle);

        var actionStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = 18,
            fontStyle = FontStyle.Bold
        };
        actionStyle.normal.textColor = Color.white;
        GUI.enabled = history.Count > 0;
        if (GUI.Button(new Rect(24, 714, 128, 58), "Undo", actionStyle))
            Undo();
        GUI.enabled = true;
        if (GUI.Button(new Rect(168, 710, 144, 64), new GUIContent(restartButtonArt)))
            LoadLevel(level);
        GUI.enabled = cleared && level + 1 < Levels.Length;
        if (GUI.Button(new Rect(328, 710, 128, 64), new GUIContent(nextButtonArt)))
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
