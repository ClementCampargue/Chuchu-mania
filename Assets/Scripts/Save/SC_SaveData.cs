
using System;
using System.Collections.Generic;

[Serializable]
public class SC_SaveData
{
    public int saveVersion = 1;
    public string saveName = "Nouvelle partie";
    public string lastPlayedDate = "";

    public int money = 0;

    public List<SC_WorldProgress> worlds =
        new List<SC_WorldProgress>();

    public List<SC_LevelProgress> levels =
        new List<SC_LevelProgress>();

    public List<SC_StickerProgress> stickers =
        new List<SC_StickerProgress>();

    public bool tutorialCompleted = false;
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


