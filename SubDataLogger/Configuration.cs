using Dalamud.Configuration;
using Dalamud.Plugin;
using System;

namespace SubDataLogger
{
    [Serializable]
    public class Configuration : IPluginConfiguration
    {
        public int Version { get; set; } = 0;

        public bool enabled = true;
        public string? name = "User";
        public string? sheetID = "";
        public string? sheetName = "";
        public string? range = "";

        // the below exist just to make saving less cumbersome
        [NonSerialized]
        private IDalamudPluginInterface? pluginInterface;

        public void Initialize(IDalamudPluginInterface pluginInterface)
        {
            this.pluginInterface = pluginInterface;
        }

        public void Save()
        {
            this.pluginInterface!.SavePluginConfig(this);
        }

        public bool Validate()
        {
            if (string.IsNullOrEmpty(this.sheetID) || string.IsNullOrEmpty(this.sheetName) || string.IsNullOrEmpty(this.range))
            {
                return false;
            }
            return true;
        }
    }
}
