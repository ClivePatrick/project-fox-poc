using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class SaveUtils
{
    /// <summary>
    /// Creates a new profile. Intended to be called at the start or after restarting
    /// </summary>
    public static void InitializeProfile()
    {
        CsvUtils.CreateSaveFile();
        SpeedrunProfile profile = new SpeedrunProfile("YOU", 0f, 0f, 0f, 0f, "");

        PlayerPrefs.SetString("PlayerName", profile.PlayerName);
        PlayerPrefs.SetFloat("Lv1Time", profile.Lv1Time);
        PlayerPrefs.SetFloat("Lv2Time", profile.Lv2Time);
        PlayerPrefs.SetFloat("Lv3Time", profile.Lv3Time);
        PlayerPrefs.SetFloat("TotalTime", profile.TotalTime);
        PlayerPrefs.SetString("PlayerContact", profile.PlayerContact);
    }

    /// <summary>
    /// Saves the time of the current level. Intended to be called at the end of each level
    /// </summary>
    /// <param name="time">in milliseconds</param>
    /// <param name="lvIndex">between 1-3 because that's how many levels we're planning to have</param>
    public static void RecordTime(float time)
    {
        // Remember to change this if we add more levels before level 1
        int levelCountBeforeLevel1 = 3; // This is conveniently 0 for now because level indices start at 1 instead of 0
        int index = SceneManager.GetActiveScene().buildIndex - levelCountBeforeLevel1;
        Debug.Log($"Index: {index}, {SceneManager.GetActiveScene().buildIndex}");

        // If this is the first time saving, initialize profile
        if (!PlayerPrefs.HasKey($"Lv{index}Time"))
        {
            InitializeProfile();
        }

        PlayerPrefs.SetFloat($"Lv{index}Time", time);
        Debug.Log($"Lv{index}Time: {PlayerPrefs.GetFloat($"Lv{index}Time")}, {time}");
    }

    /// <summary>
    /// Gets all the time records and the overall time. Intended to be called when showing the leaderboard
    /// </summary>
    /// <returns>Times in ms</returns>
    private static (float lv1Time, float lv2Time, float lv3Time, float totalTime) GetPlayerTimes()
    {
        float lv1Time = PlayerPrefs.GetFloat("Lv1Time");
        float lv2Time = PlayerPrefs.GetFloat("Lv2Time");
        float lv3Time = PlayerPrefs.GetFloat("Lv3Time");

        float totalTime = lv1Time + lv2Time + lv3Time;
        PlayerPrefs.SetFloat("TotalTime", totalTime);

        return (lv1Time, lv2Time, lv3Time, totalTime);
    }

    /// <summary>
    /// Gets player time records in formatted string. Intended to be called when showing the leaderboard
    /// </summary>
    /// <returns>Times in "00:00.000"</returns>
    public static (string lv1Time, string lv2Time, string lv3Time, string totalTime) GetPlayerTimesFormatted()
    {
        var (lv1Time, lv2Time, lv3Time, totalTime) = GetPlayerTimes();
        return (TimeFormatter.Milliseconds(lv1Time), TimeFormatter.Milliseconds(lv2Time), TimeFormatter.Milliseconds(lv3Time), TimeFormatter.Milliseconds(totalTime));
    }

    /// <summary>
    /// Saves player name. Intended to be called after player inputs their name in the leaderboard
    /// </summary>
    /// <param name="name"></param>
    public static void RecordPlayerName(string name)
    {
        string playerName = string.IsNullOrEmpty(name) ? "AAA" : name;
        PlayerPrefs.SetString("PlayerName", playerName);
    }

    /// <summary>
    /// Saves player contact. Intended to be called after player inputs their contact in the leaderboard
    /// </summary>
    /// <param name="contact"></param>
    public static void RecordPlayerContact(string contact)
    {
        string playerContact = string.IsNullOrEmpty(contact) ? "" : contact;
        PlayerPrefs.SetString("PlayerContact", playerContact);
    }

    /// <summary>
    /// Gets all save data for the current player. Intended to be called before saving to file
    /// </summary>
    /// <returns>a struct of name, contact and all the times in ms</returns>
    public static SpeedrunProfile GetPlayerData()
    {
        string playerName = PlayerPrefs.GetString("PlayerName");
        var (lv1Time, lv2Time, lv3Time, totalTime) = GetPlayerTimes();
        string playerContact = PlayerPrefs.GetString("PlayerContact");

        return new SpeedrunProfile(playerName, lv1Time, lv2Time, lv3Time, totalTime, playerContact);
    }

    public static void SaveProfile()
    {
        SpeedrunProfile speedrunProfile = GetPlayerData();
        CsvUtils.WriteToFile(speedrunProfile);
    }

    /// <summary>
    /// Load an ordered list of profiles by total time
    /// </summary>
    /// <returns></returns>
    public static List<SpeedrunProfile> LoadProfiles()
    {
        List<SpeedrunProfile> profiles = CsvUtils.ReadFromFile();

        // Also add the current profile to figure out their ranking
        profiles.Add(GetPlayerData());
        return profiles.OrderBy(x => x.TotalTime).ToList();
    }
}
