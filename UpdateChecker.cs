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

        private string currentPluginVersion = "";
        private string pluginName = "";
        private string branchName = "";
        private uint productId;
        private string _storeVersion = "";
        private bool requiresUpdate = false;
        private bool loaded = false;

        public UpdateChecker(string currentPluginVersion, string pluginName, uint productId, string branch = "master")
		{
            this.pluginName = pluginName;
            this.currentPluginVersion = currentPluginVersion;
            this.productId = productId;
            branchName = branch;
        }
        public bool updateRequired(out string storeVersion)
        {
            storeVersion = _storeVersion;
            return isStoreVersionLoaded(out _) && requiresUpdate;
        }
        public bool isStoreVersionLoaded(out string storeVersion)
        {
            storeVersion = _storeVersion;
            return loaded;
        }
        public bool tryCheckPluginVersion(out string storeVersion)
        {
            loaded = false;
            requiresUpdate = false;
            storeVersion = _storeVersion;
            string newestVersion = currentPluginVersion;
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
                            _storeVersion = b.versions[y].name;
                            break;
                        }
                    }
                    break;
                }
            }
            if (_storeVersion == "")
            {
                Logger.LogError($"newest store version of {pluginName} {branchName} was not found");
                return false;
            }

            requiresUpdate = checkVersionNumbers(currentPluginVersion, _storeVersion, out newestVersion);
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
            newestVersion = currentVersion;

            string[] currentVersionSplit = currentVersion.Split('.');
            string[] foundVersionSplit = foundVersion.Split('.');
            int smallestCount = currentVersionSplit.Length > foundVersionSplit.Length ? foundVersionSplit.Length : currentVersionSplit.Length;

            for (int i = 0; i < smallestCount; i++)
            {
                if (!int.TryParse(currentVersionSplit[i], out int current))
                {
                    Logger.LogError($"Failed to parse local version number {currentVersionSplit[i]} for {pluginName}!");
                    continue;
                }
                if (!int.TryParse(foundVersionSplit[i], out int found))
                {
                    Logger.LogError($"Failed to parse remote version number {foundVersionSplit[i]} for {pluginName}!");
                    continue;
                }
                if (found > current)
                {
                    newestVersion = foundVersion;
                    return true;
                }
                else if (found < current)
                {
                    return false;
                }
            }
            return false;
        }
    }
}
