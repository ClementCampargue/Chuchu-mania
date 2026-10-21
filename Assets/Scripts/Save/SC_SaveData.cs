
using System;
using System.Collections.Generic;

[Serializable]
public class SC_SaveData
{
    public int saveVersion = 1;
    public string saveName = "Nouvelle partie";
    public string lastPlayedDate = "";

    public int money = 0;
    public int wishes = 0;


    public List<SC_WorldProgress> worlds =
        new List<SC_WorldProgress>();

    public List<SC_LevelProgress> levels =
        new List<SC_LevelProgress>();

    public List<SC_StickerProgress> stickers =
        new List<SC_StickerProgress>();

    public List<SC_LevelRecord> levelRecords =
        new List<SC_LevelRecord>();

    public List<SC_AchievementProgress> achievements =
        new List<SC_AchievementProgress>();

    public bool firtlaunch = true;
    public bool tutorialCompleted = false;
}

[Serializable]
public class SC_AchievementProgress
{
    public bool finished;
    public bool collected;
    public string achievementId;
    public int progress;
    public bool unlocked;
}


[Serializable]
public class SC_WorldProgress
{
    public string worldId;
    public bool unlocked;

    public List<bool> spiritsUnlocked =
        new List<bool>();
}

[Serializable]
public class SC_LevelProgress
{
    public string levelId;
    public bool unlocked;
    public bool completed;
}

[Serializable]
public class SC_StickerProgress
{
    public string stickerId;
    public bool unlocked;
}

[Serializable]
public class SC_LevelRecord
{
    public string levelId;

    // -1 signifie qu'aucun record n'a encore été enregistré.
    public int bestTime = -1;
    public int bestHearts = -1;
    public int bestScore = -1;
    public int bestRank = -1;
}
