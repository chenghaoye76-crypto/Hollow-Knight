using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

[DefaultExecutionOrder(-50)]
public class GameDataManager : MonoBehaviour
{
    public static GameDataManager instant;

    int GameDataID;

    public Data data;
    public List<ISaveable> ISaveableList;

    string JsonPath;

    private void Awake()
    {
        if (instant == null)
            instant = this;
        else
            Destroy(this);

        ISaveableList = new List<ISaveable>();
        if (PlayerPrefs.HasKey("GameDataNum"))
            GameDataID = PlayerPrefs.GetInt("GameDataNum");
        else
            GameDataID = 1;
#if UNITY_ANDROID
        JsonPath = Path.Combine(Application.persistentDataPath,"GameData","GameData" + GameDataID.ToString() + ".json");
#else
        JsonPath = Application.streamingAssetsPath + @"/Json/GameData/GameData" + GameDataID.ToString() + ".json";
#endif
        ReadSavedData();
    }

    public void RegisterSaveData(ISaveable saveable)
    {
        if (!ISaveableList.Contains(saveable))
        {
            ISaveableList.Add(saveable);
        }
    }

    public void UnRegisterSaveData(ISaveable saveable)
    {
        if (ISaveableList.Contains(saveable))
        {
            ISaveableList.Remove(saveable);
        }
    }

    private void ReadSavedData()
    {
        if (File.Exists(JsonPath))
        {
            string stringData = File.ReadAllText(JsonPath);
            Data jsonData = JsonConvert.DeserializeObject<Data>(stringData);
            data = jsonData;

            if (!data.packageData.packageGoodTypes.Contains(PackageGoodType.Focus))
                data.packageData.packageGoodTypes.Add(PackageGoodType.Focus);
        }
        else
            data = new Data();
    }


    public void Save(bool saveIcon = true)
    {
        foreach (var saveable in ISaveableList)
        {
            saveable.GetSaveData(data);
        }
        data.gameTime = TimeManager.Instant.gameTime;
        if (saveIcon)
            GamePanel.instance.SaveIconShow();

        string jsonData = JsonConvert.SerializeObject(data, Formatting.Indented);
        File.WriteAllText(JsonPath, jsonData);
#if UNITY_EDITOR
        AssetDatabase.Refresh();//刷新资源
#endif
    }

    public void Load()
    {
        foreach (var saveable in ISaveableList)
        {
            saveable.LoadData(data);
        }
    }
}
