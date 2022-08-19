using Rocket.Core.Logging;
using SDG.Unturned;
using SpeedMann.UpdateChecker.Models;
using SpeedMann.UpdateChecker.Models.UStore;
using Steamworks;
using System.Collections.Generic;

namespace SpeedMann.UpdateChecker
{
    internal class WorkshopChecker
    {
        private static uint serverIp;
        private string pluginName;
        private uint productId;

        public delegate void WorkshopCheckCompletion(bool success, List<WorkshopItemData> workshopsData);

        private WorkshopCheckCompletion onCompletion;
        private UGCQueryHandle_t queryHandle;
        private Dictionary<ulong, WorkshopItemData> workshopItemDict;
        private List<WorkshopItemData> workshopData;

        private bool loaded = false;
        private bool valid = false;
        public WorkshopChecker(string pluginName, uint productId, Dictionary<ulong, WorkshopItemData> workshopItems = null)
        {
            if(workshopItems == null)
            {
                workshopItems = new Dictionary<ulong, WorkshopItemData>();
            }
            workshopItemDict = workshopItems;
            this.pluginName = pluginName;
            this.productId = productId;
        }
        public void loadWorkshopItems()
        {
            PluginInfoLoader.loadPluginInfo(loadedWorkshopInfo, productId);
        }
        private void loadedWorkshopInfo(bool success, Product pluginInfo)
        {
            if (success && pluginInfo?.workshopItems != null)
            {
                foreach (WorkshopItem item in pluginInfo?.workshopItems)
                {
                    if (!workshopItemDict.ContainsKey(item.workshopFileId))
                    {
                        workshopItemDict.Add(item.workshopFileId, new WorkshopItemData(item.workshopFileId, item.workshopFileId.ToString(), item.required));
                    }
                }
            }
        }
        public bool checkWorkshopItem(ulong workshopId, string displaySting, bool required, WorkshopCheckCompletion calledMethod)
        {
            return checkWorkshopItems(new List<WorkshopItemData> { new WorkshopItemData(workshopId, displaySting, required) }, calledMethod);
        }
        public bool checkWorkshopItems(List<WorkshopItemData> workshopItems, WorkshopCheckCompletion calledMethod)
        {
            onCompletion = calledMethod;

            CallResult<SteamUGCQueryCompleted_t> queryCompleted = CallResult<SteamUGCQueryCompleted_t>.Create(new CallResult<SteamUGCQueryCompleted_t>.APIDispatchDelegate(onQueryCompleted));
            PublishedFileId_t[] ids = new PublishedFileId_t[workshopItems.Count];
            for (int i = 0; i < workshopItems.Count; i++)
            {
                if (workshopItems[i].required && !WorkshopDownloadConfig.getOrLoad().File_IDs.Contains(workshopItems[i].id))
                {
                    CommandWindow.LogError($"{workshopItems[i].displayText} is not present in the WorkshopDownloadConfig.json!");
                    return false;
                }
                ids[i] = new PublishedFileId_t(workshopItems[i].id);
            }

            workshopData = workshopItems;
            uint ip;
            if (!SteamGameServer.GetPublicIP().TryGetIPv4Address(out ip))
            {
                UnturnedLog.warn("Unable to determine server IP for workshop check");
                return false;
            }
            serverIp = ip;
            queryHandle = SteamGameServerUGC.CreateQueryUGCDetailsRequest(ids, (uint)ids.Length);

            SteamGameServerUGC.SetReturnKeyValueTags(queryHandle, true);
            SteamGameServerUGC.SetReturnChildren(queryHandle, true);

            uint query_Cache_Max_Age_Seconds = WorkshopDownloadConfig.get().Query_Cache_Max_Age_Seconds;
            if (query_Cache_Max_Age_Seconds > 0U)
            {
                SteamGameServerUGC.SetAllowCachedResponse(queryHandle, query_Cache_Max_Age_Seconds);
            }
            SteamAPICall_t hAPICall = SteamGameServerUGC.SendQueryUGCRequest(queryHandle);
            queryCompleted.Set(hAPICall, null);

            return true;
        }
        private void onQueryCompleted(SteamUGCQueryCompleted_t callback, bool ioFailure)
        {
            bool valid = false;

            if (callback.m_handle != queryHandle || ioFailure)
            {
                onCompletion?.Invoke(valid, workshopData);
                return;
            }

            valid = true;
            for (uint i = 0; i < workshopData.Count; i++)
            {
                WorkshopItemData item = workshopData[(int)i];
                SteamUGCDetails_t steamUGCDetails_t;
                if (!SteamGameServerUGC.GetQueryUGCResult(queryHandle, i, out steamUGCDetails_t))
                {
                    valid = false;
                    Logger.LogWarning($"Workshop query unable to get details for {item.displayText} [{item.id}] of {pluginName}");
                    continue;
                }

                string text = string.Concat(new object[]
                {
                                steamUGCDetails_t.m_nPublishedFileId,
                                " '",
                                steamUGCDetails_t.m_rgchTitle,
                                "'"
                });
                if (steamUGCDetails_t.m_eResult != EResult.k_EResultOK)
                {
                    valid = false;
                    Logger.LogWarning(string.Format("Error {0} checking workshop file {1}", steamUGCDetails_t.m_eResult, text));
                    continue;
                }

                EWorkshopDownloadRestrictionResult restrictionResult = WorkshopDownloadRestrictions.getRestrictionResult(queryHandle, i, serverIp);
                switch (restrictionResult)
                {
                    case EWorkshopDownloadRestrictionResult.NoRestrictions:
                        item.result = WorkshopResult.NoRestrictions;
                        break;
                    case EWorkshopDownloadRestrictionResult.NotWhitelisted:
                        valid = false;
                        item.result = WorkshopResult.NotWhitelisted;
                        Logger.LogError($"Not authorized in the IP whitelist for {item.displayText} [{item.id}] of {pluginName}");
                        break;
                    case EWorkshopDownloadRestrictionResult.Blacklisted:
                        valid = false;
                        item.result = WorkshopResult.Blacklisted;
                        Logger.LogError($"Blocked in IP blacklist for {item.displayText} [{item.id}] of {pluginName}");
                        break;
                    case EWorkshopDownloadRestrictionResult.Allowed:
                        item.result = WorkshopResult.Allowed;
                        break;
                    case EWorkshopDownloadRestrictionResult.Banned:
                        valid = false;
                        item.result = WorkshopResult.Banned;
                        Logger.LogError($"Workshop file {item.displayText} [{item.id}] of {pluginName} is banned");
                        break;
                    case EWorkshopDownloadRestrictionResult.PrivateVisibility:
                        valid = false;
                        item.result = WorkshopResult.PrivateVisibility;
                        Logger.LogError($"Workshop file {item.displayText} [{item.id}] of {pluginName} is private");
                        break;
                    default:
                        valid = false;
                        item.result = WorkshopResult.Unknown;
                        Logger.LogError($"Unknown restriction result '{restrictionResult}' for '{item.displayText}'");
                        break;
                }
            }
            this.valid = valid;
            loaded = true;
            onCompletion?.Invoke(valid, workshopData);
        }
        public bool areWorkshopItemsValid()
        {
            return areWorkshopItemsLoaded() && valid;
        }
        public bool areWorkshopItemsLoaded()
        {
            return loaded;
        }
    }
}

