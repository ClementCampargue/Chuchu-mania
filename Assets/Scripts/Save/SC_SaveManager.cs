
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
    // Vérifie si un sticker est débloqué.
    public bool IsStickerUnlocked(SO_Sticker sticker)
    {
        if (sticker == null)
            return false;

        if (data == null)
            return sticker.unlocked;

        if (data.stickers == null)
            data.stickers = new System.Collections.Generic
                .List<SC_StickerProgress>();

        foreach (SC_StickerProgress progress in data.stickers)
        {
            if (progress.stickerId == sticker.stickerId)
                return progress.unlocked;
        }

        // Valeur par défaut définie dans le ScriptableObject.
        return sticker.unlocked;
    }


    // Débloque ou verrouille un sticker et sauvegarde le résultat.
    public void SetStickerUnlocked(
        SO_Sticker sticker, bool unlocked)
    {
        if (data == null || sticker == null)
            return;

        if (string.IsNullOrWhiteSpace(sticker.stickerId))
        {
            Debug.LogError(
                "Le sticker n'a pas de stickerId : " + sticker.name
            );
            return;
        }

        if (data.stickers == null)
            data.stickers = new System.Collections.Generic
                .List<SC_StickerProgress>();

        foreach (SC_StickerProgress progress in data.stickers)
        {
            if (progress.stickerId == sticker.stickerId)
            {
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

}
