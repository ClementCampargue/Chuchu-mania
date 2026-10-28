
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class SC_SaveManager : MonoBehaviour
{
    public static SC_SaveManager instance;

    public const int SLOT_COUNT = 3;

    public int activeSlot = -1;
    public SC_SaveData data;

    [Header("Achievements")]
    public List<SO_achievement> achievements =
        new List<SO_achievement>();

    public void ClearAllSaves()
    {
        bool allDeleted = true;

        for (int i = 0; i < SLOT_COUNT; i++)
        {
            if (!DeleteSlot(i))
            {
                allDeleted = false;
            }
        }

        data = null;
        activeSlot = -1;

        Debug.Log(
            allDeleted
                ? "Toutes les sauvegardes ont été supprimées."
                : "Certaines sauvegardes n'ont pas pu être supprimées."
        );
    }

    private void Start()
    {
        for (int i = 0; i < SLOT_COUNT; i++)
        {
            if (SlotExists(i) && LoadSlot(i))
            {
                Debug.Log("Sauvegarde chargée : " + i);
                return;
            }
        }

        data = null;
        activeSlot = -1;

        Debug.Log("Aucune sauvegarde trouvée.");
    }

    private string SavePath(int slot)
    {
        return Path.Combine(
            Application.persistentDataPath,
            "save_" + slot + ".json"
        );
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public bool SlotExists(int slot)
    {
        if (!IsValidSlot(slot))
            return false;

        return File.Exists(SavePath(slot));
    }

    public bool IsValidSlot(int slot)
    {
        return slot >= 0 && slot < SLOT_COUNT;
    }

    // Charge un emplacement existant.
    // Retourne false si aucun fichier n'existe.
    public bool LoadSlot(int slot)
    {
        if (!IsValidSlot(slot))
            return false;

        string path = SavePath(slot);

        if (!File.Exists(path))
            return false;

        try
        {
            string json = File.ReadAllText(path);
            SC_SaveData loaded =
                JsonUtility.FromJson<SC_SaveData>(json);

            if (loaded == null)
                return false;

            if (loaded.saveVersion != 1)
            {
                Debug.LogError(
                    "Version de sauvegarde incompatible : " + path
                );
                return false;
            }

            if (loaded.worlds == null)
                loaded.worlds = new System.Collections.Generic
                    .List<SC_WorldProgress>();

            if (loaded.levels == null)
                loaded.levels = new System.Collections.Generic
                    .List<SC_LevelProgress>();
            if (loaded.stickers == null)
                loaded.stickers = new System.Collections.Generic
                    .List<SC_StickerProgress>();

            if (loaded.achievements == null)
            {
                loaded.achievements =
                    new List<SC_AchievementProgress>();
            }
            foreach (SC_AchievementProgress progress in loaded.achievements)
            {
                if (progress == null)
                    continue;

                // Ancien système : un succès débloqué donnait
                // déjà sa récompense automatiquement.
                if (progress.unlocked && !progress.finished)
                {
                    progress.finished = true;
                    progress.collected = true;
                }
            }

            foreach (var world in loaded.worlds)
            {
                if (world.spiritsUnlocked == null)
                    world.spiritsUnlocked =
                        new System.Collections.Generic.List<bool>();
            }

            data = loaded;
            activeSlot = slot;

            Debug.Log("Sauvegarde chargée : slot " + slot);
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError(
                "Impossible de charger la sauvegarde : " + e.Message
            );
            return false;
        }
    }

    // Crée une nouvelle partie dans l'emplacement choisi.
    public bool CreateNewSlot(int slot, string saveName = null)
    {
        if (!IsValidSlot(slot))
            return false;

        data = new SC_SaveData();
        data.saveName = string.IsNullOrWhiteSpace(saveName)
            ? "Partie " + (slot + 1)
            : saveName;

        activeSlot = slot;
        SaveGame();

        return true;
    }

    public void SaveGame()
    {
        if (data == null || !IsValidSlot(activeSlot))
            return;
        Debug.Log("SAVE SLOT : " + activeSlot);
        Debug.Log("SAVE PATH : " + SavePath(activeSlot));
        Debug.Log("SAVE DATA : " + JsonUtility.ToJson(data, true));
        try
        {
            data.lastPlayedDate = DateTime.Now.ToString("o");

            string json = JsonUtility.ToJson(data, true);
            string path = SavePath(activeSlot);
            string tempPath = path + ".tmp";

            File.WriteAllText(tempPath, json);

            if (File.Exists(path))
            {
                string backupPath = path + ".bak";

                try
                {
                    File.Replace(tempPath, path, backupPath);
                    if (File.Exists(backupPath))
                        File.Delete(backupPath);
                }
                catch
                {
                    if (File.Exists(tempPath))
                        File.Delete(tempPath);

                    File.WriteAllText(path, json);
                }
            }
            else
            {
                File.Move(tempPath, path);
            }
        }
        catch (Exception e)
        {
            Debug.LogError(
                "Erreur de sauvegarde : " + e.Message
            );
        }
    }


    public bool DeleteSlot(int slot)
    {
        if (!IsValidSlot(slot))
            return false;

        string path = SavePath(slot);

        try
        {
            if (File.Exists(path))
                File.Delete(path);

            if (activeSlot == slot)
            {
                data = null;
                activeSlot = -1;
            }

            return true;
        }
        catch (Exception e)
        {
            Debug.LogError(
                "Erreur de suppression : " + e.Message
            );
            return false;
        }
    }

    private SC_WorldProgress GetWorldProgress(
        SO_world world, bool create)
    {
        if (data == null || world == null)
            return null;

        foreach (SC_WorldProgress progress in data.worlds)
        {
            if (progress.worldId == world.worldId)
                return progress;
        }

        if (!create)
            return null;

        SC_WorldProgress newProgress = new SC_WorldProgress();
        newProgress.worldId = world.worldId;
        newProgress.unlocked = world.startUnlocked;

        for (int i = 0; i < world.spirits.Count; i++)
        {
            bool initiallyUnlocked =
                world.defaultSpiritsUnlocked != null &&
                i < world.defaultSpiritsUnlocked.Count &&
                world.defaultSpiritsUnlocked[i];

            newProgress.spiritsUnlocked.Add(initiallyUnlocked);
        }

        data.worlds.Add(newProgress);
        return newProgress;
    }

    public bool IsWorldUnlocked(SO_world world)
    {
        if (world == null)
            return false;

        if (data == null)
            return world.startUnlocked;

        SC_WorldProgress progress =
            GetWorldProgress(world, false);

        return progress != null
            ? progress.unlocked
            : world.startUnlocked;
    }
    public int GetCompletedLevelsCount()
    {
        if (data == null || data.levels == null)
            return 0;

        int count = 0;

        foreach (SC_LevelProgress progress in data.levels)
        {
            if (progress.completed)
                count++;
        }

        return count;
    }

    public void SetWorldUnlocked(SO_world world, bool unlocked)
    {
        SC_WorldProgress progress =
            GetWorldProgress(world, true);

        if (progress == null)
            return;

        progress.unlocked = unlocked;
        SaveGame();
    }

    public bool IsSpiritUnlocked(SO_world world, int index)
    {
        if (world == null ||
            index < 0 ||
            index >= world.spirits.Count)
            return false;

        SC_WorldProgress progress =
            GetWorldProgress(world, false);

        if (progress == null ||
            index >= progress.spiritsUnlocked.Count)
        {
            return world.defaultSpiritsUnlocked != null &&
                   index < world.defaultSpiritsUnlocked.Count &&
                   world.defaultSpiritsUnlocked[index];
        }

        return progress.spiritsUnlocked[index];
    }

    public void SetSpiritUnlocked(
        SO_world world, int index, bool unlocked)
    {
        if (world == null || index < 0 ||
            index >= world.spirits.Count)
            return;

        SC_WorldProgress progress =
            GetWorldProgress(world, true);

        if (progress == null)
            return;

        while (progress.spiritsUnlocked.Count <= index)
            progress.spiritsUnlocked.Add(false);

        progress.spiritsUnlocked[index] = unlocked;
        SaveGame();
    }

    public bool IsLevelCompleted(SO_level level)
    {
        if (data == null || level == null)
            return false;

        foreach (SC_LevelProgress progress in data.levels)
        {
            if (progress.levelId == level.levelId)
                return progress.completed;
        }

        return false;
    }

    public void SetLevelCompleted(SO_level level)
    {
        if (data == null || level == null)
            return;

        foreach (SC_LevelProgress progress in data.levels)
        {
            if (progress.levelId == level.levelId)
            {
                progress.completed = true;
                SaveGame();
                return;
            }
        }

        SC_LevelProgress newProgress = new SC_LevelProgress();
        newProgress.levelId = level.levelId;
        newProgress.completed = true;

        data.levels.Add(newProgress);
        SaveGame();
    }

    public void AddWish(int amount)
    {
        if (data == null)
            return;

        data.wishes = Mathf.Max(0, data.wishes + amount);
        SaveGame();
    }

    public void SpendWish(int amount)
    {
        if (data == null)
            return;

        data.wishes = Mathf.Max(0, data.wishes - amount);
        SaveGame();
    }

    public void Setmoney(int amount)
    {
        if (data == null)
            return;

        data.money = Mathf.Max(0, amount);
        SaveGame();
    }

    public void Addmoney(int amount)
    {
        if (data == null)
            return;
       AddAchievementProgress(AchievementType.Win1000coins, amount);

        data.money = Mathf.Max(0, data.money + amount);
        SaveGame();
    }

    public void Spendmoney(int amount)
    {
        if (data == null)
            return;

        data.money = Mathf.Max(0, data.money - amount);
        SaveGame();
    }
    public bool IsLevelUnlocked(SO_level level)
    {
        if (level == null)
            return false;

        if (data == null)
            return level.startUnlocked;

        foreach (SC_LevelProgress progress in data.levels)
        {
            if (progress.levelId == level.levelId)
                return progress.unlocked || progress.completed;
        }

        return level.startUnlocked;
    }
    public void SetLevelUnlocked(SO_level level, bool unlocked)
    {
        if (data == null || level == null)
            return;

        foreach (SC_LevelProgress progress in data.levels)
        {
            if (progress.levelId == level.levelId)
            {
                progress.unlocked = unlocked;
                SaveGame();
                return;
            }
        }

        SC_LevelProgress newProgress = new SC_LevelProgress
        {
            levelId = level.levelId,
            unlocked = unlocked,
            completed = false
        };

        data.levels.Add(newProgress);
        SaveGame();
    }

    // Vérifie exclusivement l'état enregistré du sticker.
    public bool IsStickerUnlocked(SO_Sticker sticker)
    {
        if (sticker == null)
            return false;

        if (string.IsNullOrWhiteSpace(sticker.stickerId))
        {
            Debug.LogError(
                "stickerId manquant : " + sticker.name
            );
            return false;
        }

        // Sans sauvegarde active, aucun sticker n'est débloqué.
        if (data == null)
            return false;

        if (data.stickers == null)
            return false;

        foreach (SC_StickerProgress progress in data.stickers)
        {
            if (progress == null)
                continue;

            if (progress.stickerId == sticker.stickerId)
                return progress.unlocked;
        }

        // Sticker absent de la sauvegarde = verrouillé.
        return false;
    }

    // Définit l'état du sticker dans la sauvegarde active.
    public void SetStickerUnlocked(
        SO_Sticker sticker,
        bool unlocked)
    {
        if (data == null)
        {
            Debug.LogWarning(
                "Impossible de modifier un sticker : " +
                "aucune sauvegarde active."
            );
            return;
        }

        if (sticker == null)
            return;

        if (string.IsNullOrWhiteSpace(sticker.stickerId))
        {
            Debug.LogError(
                "stickerId manquant : " + sticker.name
            );
            return;
        }

        if (data.stickers == null)
        {
            data.stickers = new List<SC_StickerProgress>();
        }

        foreach (SC_StickerProgress progress in data.stickers)
        {
            if (progress == null)
                continue;

            if (progress.stickerId == sticker.stickerId)
            {
                if (progress.unlocked == unlocked)
                    return;

                progress.unlocked = unlocked;
                SaveGame();
                return;
            }
        }

        data.stickers.Add(new SC_StickerProgress
        {
            stickerId = sticker.stickerId,
            unlocked = unlocked
        });

        SaveGame();
    }

    // Débloque un sticker.
    public void UnlockSticker(SO_Sticker sticker)
    {
        SetStickerUnlocked(sticker, true);
    }

    // Verrouille un sticker.
    public void LockSticker(SO_Sticker sticker)
    {
        SetStickerUnlocked(sticker, false);
    }

    public SC_LevelRecord GetLevelRecord(SO_level level)
    {
        if (data == null || level == null)
            return null;

        if (data.levelRecords == null)
            data.levelRecords = new List<SC_LevelRecord>();

        foreach (SC_LevelRecord record in data.levelRecords)
        {
            if (record.levelId == level.levelId)
                return record;
        }

        return null;
    }

    public void SaveLevelRecord(
        SO_level level,
        int elapsedTime,
        int hearts,
        int score,
        int rankIndex)
    {
        if (data == null || level == null)
            return;

        if (string.IsNullOrWhiteSpace(level.levelId))
        {
            Debug.LogError(
                "Impossible de sauvegarder le record : levelId manquant."
            );
            return;
        }

        if (data.levelRecords == null)
            data.levelRecords = new List<SC_LevelRecord>();

        SC_LevelRecord record = GetLevelRecord(level);

        // Premier résultat enregistré pour ce niveau.
        if (record == null)
        {
            record = new SC_LevelRecord
            {
                levelId = level.levelId,
                bestTime = Mathf.Max(0, elapsedTime),
                bestHearts = Mathf.Max(0, hearts),
                bestScore = Mathf.Max(0, score),
                bestRank = Mathf.Max(0, rankIndex)
            };

            data.levelRecords.Add(record);
        }
        else
        {
            // Le temps est mesuré en secondes : plus petit = meilleur.
            if (elapsedTime >= 0f &&
                (record.bestTime < 0f ||
                 elapsedTime < record.bestTime))
            {
                record.bestTime = elapsedTime;
            }

            // Plus de cœurs = meilleur record.
            if (hearts > record.bestHearts)
                record.bestHearts = hearts;

            // Plus de points = meilleur record.
            if (score > record.bestScore)
                record.bestScore = score;

            // Un index supérieur représente un meilleur rang.
            if (rankIndex > record.bestRank)
                record.bestRank = rankIndex;
        }

        // Le niveau est également marqué comme terminé.
        SetLevelCompleted(level);

        // Enregistre tous les records dans le slot actif.
        SaveGame();

        Debug.Log(
            "Record sauvegardé pour " + level.levelId +
            " | Temps : " + record.bestTime +
            " | Cœurs : " + record.bestHearts +
            " | Score : " + record.bestScore +
            " | Rang : " + record.bestRank
        );
    }

    private SO_achievement GetAchievementDefinition(AchievementType type)
    {
        if (achievements == null)
            return null;

        foreach (SO_achievement achievement in achievements)
        {
            if (achievement != null && achievement.type == type)
                return achievement;
        }

        return null;
    }

    private SC_AchievementProgress GetAchievementProgress(
        SO_achievement achievement,
        bool create)
    {
        if (data == null || achievement == null)
            return null;

        if (string.IsNullOrWhiteSpace(achievement.achievementId))
        {
            Debug.LogError(
                "achievementId manquant : " + achievement.name
            );
            return null;
        }

        if (data.achievements == null)
            data.achievements = new List<SC_AchievementProgress>();

        foreach (SC_AchievementProgress progress in data.achievements)
        {
            if (progress != null &&
                progress.achievementId == achievement.achievementId)
            {
                return progress;
            }
        }

        if (!create)
            return null;

        SC_AchievementProgress newProgress = new SC_AchievementProgress
        {
            achievementId = achievement.achievementId,
            progress = 0,
            unlocked = false,
            finished = false,
            collected = false
        };

        data.achievements.Add(newProgress);
        return newProgress;
    }

    // Le succès a-t-il été terminé ?
    public bool IsAchievementFinished(SO_achievement achievement)
    {
        SC_AchievementProgress progress =
            GetAchievementProgress(achievement, false);

        return progress != null && progress.finished;
    }

    // La récompense est-elle disponible ?


    public bool CollectAchievementReward(
        SO_achievement achievement,
        bool useJoker = false)
    {
        if (data == null)
        {
            Debug.LogError("Aucune sauvegarde active !");
            return false;
        }

        if (achievement == null)
        {
            Debug.LogError("Achievement null !");
            return false;
        }

        Debug.Log("Achievements avant : " + data.achievements.Count);

        // IMPORTANT : true crée l'entrée si elle n'existe pas.
        SC_AchievementProgress progress =
            GetAchievementProgress(achievement, true);

        if (progress == null)
        {
            Debug.LogError(
                "Impossible de créer la progression : "
                + achievement.achievementId
            );
            return false;
        }

        if (progress.collected)
        {
            Debug.LogWarning("Récompense déjà récupérée !");
            return false;
        }

        if (!progress.finished && !useJoker)
        {
            Debug.LogWarning("Succès non terminé : joker nécessaire.");
            return false;
        }

        // Avec un joker, le succès peut être validé sans objectif atteint.
        progress.finished = true;
        progress.unlocked = true;

        if (achievement.sticker_reward != null)
        {
            UnlockSticker(achievement.sticker_reward);
        }

        progress.collected = true;

        SaveGame();

        Debug.Log("Achievements après : " + data.achievements.Count);
        Debug.Log(
            "Succès enregistré : " +
            JsonUtility.ToJson(progress, true)
        );

        return true;
    }


    // La récompense a-t-elle déjà été récupérée ?
    public bool IsAchievementCollected(SO_achievement achievement)
    {
        SC_AchievementProgress progress =
            GetAchievementProgress(achievement, false);

        return progress != null && progress.collected;
    }

    // Progression actuelle.
    public int GetAchievementProgressValue(
        SO_achievement achievement)
    {
        SC_AchievementProgress progress =
            GetAchievementProgress(achievement, false);

        return progress != null ? progress.progress : 0;
    }

    // Progression complète du succès.
    public SC_AchievementProgress GetAchievementCollectionProgress(
        SO_achievement achievement)
    {
        return GetAchievementProgress(achievement, false);
    }

    // Ajoute de la progression au succès.
    public void AddAchievementProgress(
        AchievementType type,
        int amount = 1)
    {
        if (data == null || amount <= 0)
            return;

        SO_achievement achievement = GetAchievementDefinition(type);

        if (achievement == null)
        {
            Debug.LogWarning(
                "Aucun succès configuré pour : " + type
            );
            return;
        }

        SC_AchievementProgress progress =
            GetAchievementProgress(achievement, true);

        if (progress == null || progress.finished)
            return;

        int objectif = Mathf.Max(1, achievement.objectif);

        progress.progress = (int)Math.Min(
            (long)progress.progress + amount,
            (long)objectif
        );

        if (progress.progress >= objectif)
        {
            UnlockAchievement(achievement);
            return;
        }
        Debug.Log("achievement " + achievement + " " + progress.progress);
        SaveGame();
    }

    // Termine immédiatement un succès.
    public void CompleteAchievement(AchievementType type)
    {
        if (data == null)
            return;

        SO_achievement achievement = GetAchievementDefinition(type);

        if (achievement == null)
        {
            Debug.LogWarning(
                "Aucun succès configuré pour : " + type
            );
            return;
        }

        UnlockAchievement(achievement);
    }

    // Termine le succès, mais ne récupère PAS la récompense.
    public void UnlockAchievement(SO_achievement achievement)
    {
        if (data == null || achievement == null)
            return;

        SC_AchievementProgress progress =
            GetAchievementProgress(achievement, true);

        if (progress == null || progress.finished)
            return;

        progress.progress = Mathf.Max(
            progress.progress,
            Mathf.Max(1, achievement.objectif)
        );

        progress.unlocked = true;
        progress.finished = true;
        progress.collected = false;

        SaveGame();

        Debug.Log(
            "Succès terminé : " + achievement.achievementId
        );

        // La récompense sera attribuée au moment de la récupération.
    }

    // Récupère la récompense une seule fois.
    public bool CollectAchievementReward(
        SO_achievement achievement)
    {
        if (data == null || achievement == null)
            return false;

        SC_AchievementProgress progress =
            GetAchievementProgress(achievement, false);

        // Impossible de récupérer un succès non terminé
        // ou dont la récompense a déjà été récupérée.
        if (progress == null ||
            !progress.finished ||
            progress.collected)
        {
            return false;
        }

        // Attribue la récompense.
        if (achievement.sticker_reward != null)
        {
            UnlockSticker(achievement.sticker_reward);
        }

        // Marque la récompense comme récupérée.
        progress.collected = true;

        SaveGame();

        Debug.Log(
            "Récompense récupérée : " + achievement.achievementId
        );

        return true;
    }

    // Compatibilité avec les appels existants.
    // Ne permet pas de récupérer une récompense sans la terminer.
    public void SetAchievementCollected(
        SO_achievement achievement,
        bool collected)
    {
        if (!collected)
            return;

        CollectAchievementReward(achievement);
    }

    // Compatibilité avec le code existant.
    public bool IsAchievementUnlocked(SO_achievement achievement)
    {
        return IsAchievementFinished(achievement);
    }

}
