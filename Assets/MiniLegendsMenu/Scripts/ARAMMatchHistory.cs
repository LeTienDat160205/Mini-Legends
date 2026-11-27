using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public static class ARAMMatchHistory
{
    [Serializable]
    public class MatchEntry
    {
        public string utcTime;
        public string mode;
        public string champion;
        public bool victory;
        public int durationSeconds;
    }

    [Serializable]
    private class MatchCollection
    {
        public List<MatchEntry> entries = new List<MatchEntry>();
    }

    private static string SavePath => Path.Combine(Application.persistentDataPath, "aram_match_history.json");

    public static List<MatchEntry> Read()
    {
        try
        {
            if (!File.Exists(SavePath)) return new List<MatchEntry>();
            MatchCollection data = JsonUtility.FromJson<MatchCollection>(File.ReadAllText(SavePath));
            return data != null && data.entries != null ? data.entries : new List<MatchEntry>();
        }
        catch (Exception e)
        {
            Debug.LogWarning("Cannot read match history: " + e.Message);
            return new List<MatchEntry>();
        }
    }

    public static void Record(bool victory, string champion, int durationSeconds)
    {
        List<MatchEntry> records = Read();
        records.Insert(0, new MatchEntry
        {
            utcTime = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm 'UTC'"),
            mode = "3v3 ARAM",
            champion = string.IsNullOrWhiteSpace(champion) ? "Unknown" : champion,
            victory = victory,
            durationSeconds = Mathf.Max(0, durationSeconds)
        });
        if (records.Count > 100) records.RemoveRange(100, records.Count - 100);
        try
        {
            File.WriteAllText(SavePath, JsonUtility.ToJson(new MatchCollection { entries = records }, true));
        }
        catch (Exception e)
        {
            Debug.LogError("Cannot save match history: " + e.Message);
        }
    }
}
