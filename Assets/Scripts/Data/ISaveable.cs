using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface ISaveable
{
    void RegisterSaveData() => GameDataManager.instant.RegisterSaveData(this);
    void UnRegisterSaveData() => GameDataManager.instant.UnRegisterSaveData(this);
    void GetSaveData(Data data);
    void LoadData(Data data);
}
