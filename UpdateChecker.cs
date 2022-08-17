using Newtonsoft.Json;
using SpeedMann.UpdateChecker.Models;
using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Logger = Rocket.Core.Logging.Logger;


namespace SpeedMann.UpdateChecker
{
	public class UpdateChecker
	{
        private const string ApiUrl = "https://unturnedstore.com/api/products/";
        private const string StoreUrl = "https://unturnedstore.com/products/";

        private static bool Initialized = false;
		public static string Version = "";

        private string currentPluginVersion = "";
        private string pluginName = "";
        private string branchName = "";
        private string productId = "";
        private string storeVersion = "";
        private bool requiresUpdate = false;
        private bool loaded = false;

        public UpdateChecker(string currentPluginVersion, string pluginName, string storeId, string branch = "master", bool startCheck = true)
		{
            if (Initialized)
            {
				Version = readFileVersion();
				Initialized = true;
			}
            this.pluginName = pluginName;
            this.currentPluginVersion = currentPluginVersion;
            productId = storeId;
            branchName = branch;
            
            if(startCheck) _ = tryCheckPluginVersion();
        }

        public async Task<bool> tryCheckPluginVersion()
        {
            loaded = false;
            requiresUpdate = false;

            if (!await getVersionAsync())
            {
                Logger.LogError($"Could not check newest plugin version for {pluginName}!");
                return false;
            }

            requiresUpdate = checkVersion(currentPluginVersion, storeVersion, out string newestVersion);
            loaded = true;

            if (requiresUpdate)
            {
                Logger.LogWarning($"{pluginName} {newestVersion} is available, update now: {StoreUrl + productId}");
            }
            else
            {
                Logger.Log($"{pluginName} {currentPluginVersion} is up to date");
            }
            return true;
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
        private async Task<bool> getVersionAsync()
        {
            WebRequest wr = WebRequest.Create(ApiUrl + productId);
            wr.Method = "GET";

            try
            {
                WebResponse response = await wr.GetResponseAsync();
                Stream dataStream = response.GetResponseStream();
                JsonTextReader reader = new JsonTextReader(new StreamReader(dataStream));
                var serializer = new JsonSerializer();

                Product deserializedProduct = serializer.Deserialize<Product>(reader);

                foreach (Branch b in deserializedProduct.branches)
                {
                    if (b.name == branchName)
                    {
                        for (int y = b.versions.Count - 1; y >= 0; y--)
                        {
                            if (b.versions[y].isEnabled)
                            {
                                storeVersion = b.versions[y].name;
                                return true;
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Logger.LogException(e, "Exeption checking plugin version!");
            }
            return false;
        }

        private bool checkVersion(string currentVersion, string foundVersion, out string newestVersion)
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
        private static string readFileVersion()
        {
            System.Reflection.Assembly assembly = System.Reflection.Assembly.GetExecutingAssembly();
            System.Diagnostics.FileVersionInfo fvi = System.Diagnostics.FileVersionInfo.GetVersionInfo(assembly.Location);
            return fvi.FileVersion;
        }
    }
}
