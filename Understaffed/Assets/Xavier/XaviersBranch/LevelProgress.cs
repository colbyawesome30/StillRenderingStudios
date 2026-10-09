using UnityEngine;

public static class LevelProgress
{
    private static string Key(string levelName) => "Completed_" + levelName;

    public static void MarkComplete(string levelName)
    {
        PlayerPrefs.SetInt(Key(levelName), 1);
        PlayerPrefs.Save();
    }

    public static bool IsComplete(string levelName) => PlayerPrefs.GetInt(Key(levelName), 0) == 1;

    public static void ResetAll() => PlayerPrefs.DeleteAll(); // handy for testing
}