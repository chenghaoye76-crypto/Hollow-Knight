using System;
using System.IO;
using System.Text;
using UnityEngine;

public enum SaveSlotStatus { Empty, Ready, Corrupt }

[Serializable]
public sealed class SaveSlotData
{
    public int version = 1;
    public int slot;
    public string scenePath;
    public bool hasPosition;
    public float x;
    public float y;
    public float z;
    public double playSeconds;
    public string savedUtc;
    public bool hasSoul;
    public int soul;
    public bool mantisLordsDefeated;
}

// 文件存取与场景/玩家分开，四个槽位互不影响。
public sealed class SaveRepository
{
    public const int SlotCount = 4;
    private readonly string directory;

    public SaveRepository(string directory)
    {
        this.directory = directory;
    }

    public SaveSlotStatus Read(int slot, out SaveSlotData data)
    {
        string path = GetPath(slot);
        if (TryReadFile(path, slot, out data) || TryReadFile(path + ".bak", slot, out data))
            return SaveSlotStatus.Ready;
        return File.Exists(path) || File.Exists(path + ".bak")
            ? SaveSlotStatus.Corrupt : SaveSlotStatus.Empty;
    }

    public void Write(SaveSlotData data)
    {
        if (data == null || !IsValid(data, data.slot))
            throw new ArgumentException("存档数据无效。");

        string path = GetPath(data.slot);
        Directory.CreateDirectory(directory);
        string temporary = path + ".tmp";
        File.WriteAllText(temporary, JsonUtility.ToJson(data, true), new UTF8Encoding(false));
        if (File.Exists(path))
        {
            // 完整写入后再原子替换；上一份有效存档保留为备份。
            bool validPrevious = TryReadFile(path, data.slot, out _);
            File.Replace(temporary, path, validPrevious ? path + ".bak" : null);
        }
        else
            File.Move(temporary, path);
    }

    public void Delete(int slot)
    {
        string path = GetPath(slot);
        File.Delete(path);
        File.Delete(path + ".bak");
        File.Delete(path + ".tmp");
    }

    private string GetPath(int slot)
    {
        if (slot < 0 || slot >= SlotCount)
            throw new ArgumentOutOfRangeException(nameof(slot));
        return Path.Combine(directory, $"slot-{slot + 1}.json");
    }

    private static bool TryReadFile(string path, int slot, out SaveSlotData data)
    {
        data = null;
        try
        {
            if (!File.Exists(path))
                return false;
            var loaded = JsonUtility.FromJson<SaveSlotData>(File.ReadAllText(path));
            if (!IsValid(loaded, slot))
                return false;
            data = loaded;
            return true;
        }
        catch (Exception error) when (error is IOException || error is UnauthorizedAccessException ||
            error is ArgumentException)
        {
            return false;
        }
    }

    private static bool IsValid(SaveSlotData data, int slot)
    {
        return data != null && data.version == 1 && data.slot == slot &&
            slot >= 0 && slot < SlotCount &&
            !string.IsNullOrEmpty(data.scenePath) && data.scenePath.StartsWith("Assets/", StringComparison.Ordinal) &&
            data.scenePath.EndsWith(".unity", StringComparison.Ordinal) && !data.scenePath.Contains("..") &&
            IsFinite(data.x) && IsFinite(data.y) && IsFinite(data.z) &&
            !double.IsNaN(data.playSeconds) && !double.IsInfinity(data.playSeconds) && data.playSeconds >= 0;
    }

    private static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
}
