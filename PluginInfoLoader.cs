using Newtonsoft.Json;
using Rocket.Core.Logging;
using SpeedMann.UpdateChecker.Models;
using SpeedMann.UpdateChecker.Models.UStore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace SpeedMann.UpdateChecker
{
    public class PluginInfoLoader
    {
        private const string ApiUrl = "https://unturnedstore.com/api/products/";

        public delegate void PluginQuerryCompletion(bool success, Product pluginInfo);

        private static Dictionary<uint, Product> LoadedProducts = new Dictionary<uint, Product>();

        public static bool tryGetPluginInfo(uint pluginId, out Product pluginInfo)
        {
            pluginInfo = null;
            if (isPluginInfoLoaded(pluginId))
            {
                pluginInfo = LoadedProducts[pluginId];
                return true;
            }
            return false;
        }
        public static bool isPluginInfoLoaded(uint pluginId)
        {
            return LoadedProducts.ContainsKey(pluginId);
        }

        public static void loadPluginInfo(PluginQuerryCompletion calledMethod, uint productId)
        {
            if (isPluginInfoLoaded(productId))
            {
                calledMethod.Invoke(true, LoadedProducts[productId]);
                return;
            }
            loadProductAsync(calledMethod, productId);
        }

        private static async void loadProductAsync(PluginQuerryCompletion calledFunction, uint productId)
        {
            WebRequest wr = WebRequest.Create(ApiUrl + productId);
            wr.Method = "GET";
            Product deserializedProduct;
            try
            {
                WebResponse response = await wr.GetResponseAsync();
                Stream dataStream = response.GetResponseStream();
                JsonTextReader reader = new JsonTextReader(new StreamReader(dataStream));
                var serializer = new JsonSerializer();

                deserializedProduct = serializer.Deserialize<Product>(reader);
            }
            catch (Exception e)
            {
                calledFunction?.Invoke(false, null);
                Logger.LogException(e, $"Exeption loading product {productId} from UnturnedStore!");
                return;
            }
            calledFunction?.Invoke(true, deserializedProduct);
        }

        
    }
}
