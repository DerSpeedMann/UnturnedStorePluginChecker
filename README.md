# UnturnedStore Plugin Checker

* **UpdateChecker**: allows checking for plugin updates for any plugin on https://unturnedstore.com  
* **WorkshopChecker**: allows checking if required mods are loaded and if any workshop whitelist / blacklist affects the current server ip (This can be used to whitelist or blacklist servers from using the plugin)

# Contact:
If you have questions, feature request or find any bugs, please contact me on Discord SpeedMann#7437

# Setup:
```cs
 public class Plugin : RocketPlugin<PluginConfiguration>
 {
        public static Plugin Inst;
        public static PluginConfiguration Conf;
        public static PluginChecker.UpdateChecker updateChecker;
        public static PluginChecker.WorkshopChecker workshopChecker;
        public static string PluginName = "ExamplePlugin";
        public static string Version;
        private uint productId = 1; // your unturned store product id
        
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

            PluginChecker.PluginInfoLoader.loadPluginInfo(pluginInfoLoaded, productId); // loads plugin info from UnturnedStore
            U.Events.OnPlayerConnected += onPlayerConnection;
        }
        
        protected override void Unload()
        {
            Logger.Log($"Unloading {PluginName}... ");
            U.Events.OnPlayerConnected -= onPlayerConnection;
        }
        
        private void onPlayerConnection(UnturnedPlayer player)
        {
            if (updateChecker.updateRequired(out string version) && player.isAdmin)
            {
                ChatManager.say(player.CSteamID, $"{PluginName} {version} is available please update!", Color.yellow);
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
                if (!updateChecker.tryCheckPluginVersion(out string newestVersion))
                {
                    Logger.LogError("Could not check plugin version!");
                }
                return;
            }
            Inst.Unload();
        }
        
        // is successful if all required mods are loaded and if the server ip is allowed to access the files
        private static void workshopCheckCompleted(bool success, List<WorkshopItem> workshopsData)
        {
            if (!success)
            {
                Inst.Unload();
            }
        }
 }
```

