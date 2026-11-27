#if UNITY_EDITOR
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public static class MiniLegendsStyledMenuBuilder
{
    [MenuItem("Tools/Mini Legends/Create Styled ARAM Menu Scene")]
    public static void Generate()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        const string menuScene = "Assets/Scenes/ModeMenu_Styled.unity";
        if (File.Exists(menuScene) && !EditorUtility.DisplayDialog("Overwrite styled menu?",
                "ModeMenu_Styled.unity already exists. Recreate it? This replaces only that menu scene.", "Recreate", "Cancel")) return;
        Ensure("Assets/Scenes");
        Ensure("Assets/Resources");

        const string artPath = "Assets/MiniLegendsMenu/Art/ModeMenu_Background.png";
        TextureImporter importer = AssetImporter.GetAtPath(artPath) as TextureImporter;
        if (importer == null)
        {
            EditorUtility.DisplayDialog("Missing artwork", "Copy Assets/MiniLegendsMenu/Art/ModeMenu_Background.png into the Unity project first.", "OK");
            return;
        }
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.maxTextureSize = 2048;
        importer.mipmapEnabled = false;
        importer.SaveAndReimport();
        Sprite art = AssetDatabase.LoadAssetAtPath<Sprite>(artPath);
        if (art == null) { Debug.LogError("Failed to import menu sprite at " + artPath); return; }

        const string catalogPath = "Assets/Resources/ARAMMenuCatalog.asset";
        ARAMMenuCatalog catalog = AssetDatabase.LoadAssetAtPath<ARAMMenuCatalog>(catalogPath);
        if (catalog == null)
        {
            catalog = ScriptableObject.CreateInstance<ARAMMenuCatalog>();
            catalog.champions.Add(new ARAMMenuCatalog.ChampionInfo
            {
                displayName = "FootSoldier", role = "Xạ thủ",
                description = "Tướng đánh xa sử dụng súng và các loại đạn.",
                attackStyle = "Bắn đạn vào kẻ địch trong tầm.",
                skillQ = "Bắn đạn lớn có thể đuổi mục tiêu.", skillE = "Bắn liên tục trong thời gian ngắn."
            });
            catalog.champions.Add(new ARAMMenuCatalog.ChampionInfo
            {
                displayName = "GoblinScout", role = "Cơ động",
                description = "Tướng có khả năng nhảy tới vị trí chỉ định.",
                attackStyle = "Tấn công mục tiêu trong tầm đánh.",
                skillQ = "Nhảy tới khu vực đã chọn.",
                skillE = "Kỹ năng định hướng; sát thương chưa hoàn thiện trong mã gốc."
            });
            catalog.champions.Add(new ARAMMenuCatalog.ChampionInfo
            {
                displayName = "BladeAssasin", role = "Cận chiến",
                description = "Tướng đánh gần, có khả năng lướt và gây sát thương vùng.",
                attackStyle = "Tấn công các mục tiêu trong khu vực gần.",
                skillQ = "Gây sát thương diện rộng theo thời gian.",
                skillE = "Lướt và gây sát thương trên đường di chuyển."
            });
            AssetDatabase.CreateAsset(catalog, catalogPath);
        }

        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var controller = new GameObject("ModeMenuManager");
        MiniLegendsStyledMenu styled = controller.AddComponent<MiniLegendsStyledMenu>();
        string selectScene = FindSelectScene();
        styled.Setup(catalog, art, selectScene);
        styled.BuildLayout();
        new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), menuScene);
        AssetDatabase.SaveAssets();
        Selection.activeGameObject = controller;
        EditorUtility.DisplayDialog("Styled ARAM menu created",
            "Scene: " + menuScene + "\nChampion Select: " + selectScene +
            "\n\nAdd this scene and your Champion Select scene in Build Profiles.\nCheck the scene name on ModeMenuManager before Play.", "OK");
    }

    private static string FindSelectScene()
    {
        string[] names = AssetDatabase.FindAssets("t:Scene").Select(AssetDatabase.GUIDToAssetPath)
            .Where(p => p.ToLowerInvariant().Contains("select") && !p.ToLowerInvariant().Contains("modemenu"))
            .ToArray();
        return names.Length == 1 ? Path.GetFileNameWithoutExtension(names[0]) : "ChampionSelect";
    }

    private static void Ensure(string path)
    {
        string[] segments = path.Split('/');
        string current = segments[0];
        for (int i = 1; i < segments.Length; i++)
        {
            string next = current + "/" + segments[i];
            if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, segments[i]);
            current = next;
        }
    }
}
#endif
