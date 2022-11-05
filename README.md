# UnturnedStore Plugin Checker

* **UpdateChecker**: check for plugin updates on https://unturnedstore.com (also works for paid plugins) 
* **WorkshopChecker**: check if required mods are loaded and if any workshop whitelist / blacklist affects the current server ip (can be used to whitelist or blacklist servers from using the plugin)

# Contact:
If you have questions, feature request or find any bugs, please contact me on Discord SpeedMann#7437

# Setup:
```cs
using SpeedMann.PluginChecker.Models.UStore;

 public class Plugin : RocketPlugin<PluginConfiguration>
 {
        public static Plugin Inst;
        public static PluginConfiguration Conf;
        public const string PluginName = "ExamplePlugin";
        public static string Version { get; private set; }
        
        private static PluginChecker.UpdateChecker updateChecker;
        private static PluginChecker.WorkshopChecker workshopChecker;
        private uint productId = 1; // your unturned store product id
        private List<WorkshopItem> workshopItems = new List<WorkshopItem>() // The backup workshop ids used if no connection to UnturnedStore can be established
        {
            new WorkshopItem(1, true) 
        };
        
        //you can use debug builds to test on a local server with active workshop ip whitelists
        #if DEBUG
            private const bool workshopCheck = false;
        #else
            private const bool workshopCheck = true;
        #endif
        
        protected override void Load()
        {
            Inst = this;
            Conf = Configuration.Instance;
            Version = readFileVersion(); 
            
            updateChecker = new PluginChecker.UpdateChecker(Version, PluginName, productId);
            workshopChecker = new PluginChecker.WorkshopChecker(PluginName, productId);
            
            U.Events.OnPlayerConnected += onPlayerConnection;
            Level.onPostLevelLoaded += onPostLevelLoaded;

            if (Level.isLoaded) 
            {
                OnPostLevelLoaded(0);
            }
        }
        
        protected override void Unload()
        {
            Logger.Log($"Unloading {PluginName}... ");
            U.Events.OnPlayerConnected -= onPlayerConnection;
            Level.onPostLevelLoaded -= onPostLevelLoaded;
        }
        private void OnPostLevelLoaded(int level)
        {
            PluginChecker.PluginInfoLoader.loadPluginInfo(pluginInfoLoaded, productId); // loads plugin info from UnturnedStore
        }
        private void onPlayerConnection(UnturnedPlayer player)
        {
            if (player.isAdmin)
            {
                if(updateChecker.updateRequired(out string storeVersion))
                {
                    ChatManager.say(player.CSteamID, $"{PluginName} {storeVersion} is available please update!", Color.yellow);
                }
                if (workshopChecker.isWorkshopCheckCompleted())
                {
                    List<WorkshopItem> missingItems = workshopChecker.getMissingWorkshopItems();
                    StringBuilder assetIds = new StringBuilder();
                    foreach (WorkshopItem item in missingItems)
                    {
                        assetIds.Append(item.workshopFileId + " ");
                    }
                    if(missingItems.Count > 0)
                    {
                        ChatManager.say(player.CSteamID, $"You are missing the following required Workshop items for {PluginName}:\n{assetIds}", Color.yellow);
                    }
                }
            }
        }
        
        /*
        / Reads the current assembly version (set in AssemblyInfo.cs)
        / This needs to correspond to the UnturnedStore version!
        */
        private static string readFileVersion()
        {
            System.Reflection.Assembly assembly = System.Reflection.Assembly.GetExecutingAssembly();
            System.Diagnostics.FileVersionInfo fvi = System.Diagnostics.FileVersionInfo.GetVersionInfo(assembly.Location);
            return fvi.FileVersion;
        }
        
        private static void pluginInfoLoaded(bool success, Product pluginInfo)
        {
            if (success)
            {
                if(workshopCheck)
                {
                    // automatically check all workshop items set on UnturnedStore
                    workshopChecker.tryAddStoreWorkshopItems(); // add all workshop items set on unturned store
                    workshopChecker.checkSetWorkshopItems(workshopCheckCompleted); // checks all set workshop items

                    // for manual workshop checking use:
                    // bool checkWorkshopItem(ulong workshopId, bool required, WorkshopCheckCompletion calledMethod)
                    // bool checkWorkshopItems(List<WorkshopItem> workshopItems, WorkshopCheckCompletion calledMethod)
                }
                
                // check plugin version
                if (!updateChecker.tryCheckPluginVersion(out string storeVersion))
                {
                    Logger.LogError("Could not check plugin version!");
                }
                return;
            }
            
            Logger.LogError("Could not check plugin version!");
            workshopChecker.checkWorkshopItems(workshopItems, workshopCheckCompleted);
        }
        
        // is successful if the server ip is allowed to access all workshop files
        private static void workshopCheckCompleted(bool success, List<WorkshopItem> workshopItems)
        {
            // the WorkshopItem list contains the results of every single workshop item checked
            foreach (WorkshopItem item in workshopItems)
            {
                if (!item.enabled && item.required)
                {
                    Logger.LogWarning($"The mod [{item.workshopFileId}] is required for {PluginName}");
                }
            }
            if (!success)
            {   
                unloadRocketPlugin();
            }
        }
        private static void unloadRocketPlugin()
        {
            RocketPlugin p = (RocketPlugin)R.Plugins.GetPlugins().Where(pl => pl.Name.ToLower().Contains(PluginName.ToLower())).FirstOrDefault();
            p.UnloadPlugin();
        }
 }
```

