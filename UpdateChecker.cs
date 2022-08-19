using Newtonsoft.Json;
using SpeedMann.PluginChecker.Models;
using SpeedMann.PluginChecker.Models.UStore;
using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Logger = Rocket.Core.Logging.Logger;


namespace SpeedMann.PluginChecker
{
	public class UpdateChecker
	{
        public delegate void UpdateCheckCompletion(bool success, string newestVersion);

        private const string StoreUrl = "https://unturnedstore.com/products/";

        private UpdateCheckCompletion OnCheckCompletion;
        private string currentPluginVersion = "";
        private string pluginName = "";
        private string branchName = "";
        private uint productId;
        private string storeVersion = "";
        private bool requiresUpdate = false;
        private bool loaded = false;

        public UpdateChecker(string currentPluginVersion, string pluginName, uint productId, string branch = "master")
		{
            this.pluginName = pluginName;
            this.currentPluginVersion = currentPluginVersion;
            this.productId = productId;
            branchName = branch;
        }

        public bool tryCheckPluginVersion(out string newestVersion)
        {
            loaded = false;
            requiresUpdate = false;
            newestVersion = currentPluginVersion;
            if (PluginInfoLoader.tryGetPluginInfo(productId, out Product pluginInfo))
            {
                checkVerionInner(pluginInfo, out newestVersion);
                return true;
            }
            return false;
        }
        private bool checkVerionInner(Product pluginInfo, out string newestVersion)
        {
            newestVersion = "";
            foreach (Branch b in pluginInfo.branches)
            {
                if (b.name == branchName)
                {
                    for (int y = b.versions.Count - 1; y >= 0; y--)
                    {
                        if (b.versions[y].isEnabled)
                        {
                            storeVersion = b.versions[y].name;
                            break;
                        }
                    }
                    break;
                }
            }
            if (storeVersion == "")
            {
                Logger.LogError($"newest store version of {pluginName} {branchName} was not found");
                return false;
            }

            requiresUpdate = checkVersionNumbers(currentPluginVersion, storeVersion, out newestVersion);
            loaded = true;

            if (requiresUpdate)
            {
                Logger.LogWarning($"{pluginName} {newestVersion} is available, update now: {StoreUrl + productId}");
                return false;
            }
            Logger.Log($"{pluginName} {currentPluginVersion} is up to date");
            return true;
        }
        private bool checkVersionNumbers(string currentVersion, string foundVersion, out string newestVersion)
        {

            string[] currentVersionSplit = currentVersion.Split('.');
            string[] newestVersionSplit = foundVersion.Split('.');
            int smallestCount = currentVersionSplit.Length > newestVersionSplit.Length ? newestVersionSplit.Length : currentVersionSplit.Length;

            for (int i = 0; i < smallestCount; i++)
            {
                if (!int.TryParse(currentVersionSplit[i], out int current))
                {
                    Logger.LogError($"Failed to parse local version number {currentVersionSplit[i]} for {pluginName}!");
                    continue;
                }
                if (!int.TryParse(newestVersionSplit[i], out int newest))
                {
                    Logger.LogError($"Failed to parse remote version number {newestVersionSplit[i]} for {pluginName}!");
                    continue;
                }
                if (newest > current)
                {
                    newestVersion = foundVersion;
                    return true;
                }
            }
            newestVersion = currentVersion;
            return false;
        }
        public bool updateRequired(out string foundVersion)
        {
            return isStoreVersionLoaded(out foundVersion) && requiresUpdate;
        }
        public bool isStoreVersionLoaded(out string foundVersion)
        {
            foundVersion = storeVersion;
            return loaded;
        }
        
    }
}
