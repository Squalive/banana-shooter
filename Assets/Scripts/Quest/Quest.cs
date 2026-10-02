using System;
using Level;
using Manager;
using Multiplayer;
using Steamworks;
using UnityEngine;
using Web;

namespace Quest
{
    public class Quest
    {
        public int Id;
        public Action<int,int> ProgressChanged;
        public Action<int> OnClaim;
        
        public bool IsClaim=false;

        public int Progress = 0;
        
        public QuestObject QuestObj;

        public Quest(int id,bool isAchieve, int progress, QuestObject questObj)
        {
            Id = id;
            IsClaim = isAchieve;
            Progress = progress;
            QuestObj = questObj;
        }

        public bool IsReached()
        {
            return Progress >= QuestObj.requiredAmount;
        }

        public void AddProgress(QuestType q)
        {
            if (QuestObj.questType == q)
            {
                ++Progress;
                ProgressChanged?.Invoke(Id,Progress);
            }
        }

        public void Claim()
        {
            if (IsReached() && !IsClaim)
            {
                LevelManager.Instance.expWaitToAdd += QuestObj.expReward;
                LevelManager.Instance.StoreExp();

                bool crate = false;
                foreach (var reward in QuestObj.rewards)
                {
                    switch (reward)
                    {
                        case QuestRewardType.Crate1:
                            crate = true;
                            // InventoryManager.Instance.HandleQueue.Enqueue(InventoryManager.InventoryHandleType.ItemDrop);
                            // SteamInventory.TriggerItemDrop(out InventoryManager.Instance.inventoryHandle,(SteamItemDef_t)181);
                            break;
                        
                    }
                }
                
                OnClaim?.Invoke(Id);

                int type = 0;

                switch (QuestObj.expReward)
                {
                    case 15000:
                        type = 1;
                        break;
                    case 20000:
                        type = 2;
                        break;
                    case 50000:
                        type = 3;
                        break;
                }

                HttpClient.Post(EndPoint.RequestDailyReward + $"?key=daily_reward_&steamid={SteamUser.GetSteamID().m_SteamID}&crate={crate}&type={type}", null);
            }
        }
    }
}