using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Plugin;
using System.IO;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using SubDataLogger.Windows;
using Dalamud.Game;
using System.Threading;
using System;
using System.Runtime.CompilerServices;

namespace SubDataLogger
{
    public sealed class Plugin : IDalamudPlugin
    {
        // Plugin properties
        public string Name => "Sub Data Logger";
        private const string CommandName = "/sdata";
        
        // Dalamud services
        public IDalamudPluginInterface PluginInterface { get; init; }
        public ICommandManager CommandManager { get; init; }
        public ISigScanner SigScanner { get; init; }
        public IPluginLog Log { get; init; }
        public IGameInteropProvider Hook { get; init; }
        public IToastGui ToastGui { get; init; }
        public IClientState ClientState { get; init; }
        public IDataManager Data { get; init; }

        // Plugin configuration
        public Configuration Configuration { get; init; }

        // Window system
        public WindowSystem WindowSystem = new("SubDataLogger");
        public ConfigWindow ConfigWindow { get; init; }

        // Stuff
        public HookManager? HookManager = null!;
        public UploadManager? UploadManager = null!;

        public Plugin(
            IDalamudPluginInterface pluginInterface,
            ICommandManager commandManager,
            ISigScanner sigScanner,
            IPluginLog log,
            IGameInteropProvider hook,
            IToastGui toastGui,
            IClientState clientState,
            IDataManager data)
        {
            this.PluginInterface = pluginInterface;
            this.CommandManager = commandManager;
            this.SigScanner = sigScanner;
            this.Log = log;
            this.Hook = hook;
            this.ToastGui = toastGui;
            this.ClientState = clientState;
            this.Data = data;
            this.UploadManager = new UploadManager(this);
            


            this.Configuration = this.PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
            this.Configuration.Initialize(this.PluginInterface);

            ConfigWindow = new ConfigWindow(this);
            
            WindowSystem.AddWindow(ConfigWindow);

           

            this.CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
            {
                HelpMessage = "Opens the config window"
            });

            this.PluginInterface.UiBuilder.Draw += DrawUI;
            this.PluginInterface.UiBuilder.OpenConfigUi += DrawConfigUI;

            this.HookManager = new HookManager(this);
            this.ClientState.Login += OnLogin;
            this.ClientState.Logout += OnLogout;
            var sheetValidationThread = new Thread(() => this.UploadManager!.ValidateConfig());
            sheetValidationThread.Start();
        }

        public void Dispose()
        {
            this.WindowSystem.RemoveAllWindows();
            
            this.ConfigWindow.Dispose();
            this.HookManager.Dispose();
            this.UploadManager?.Dispose();
            this.CommandManager.RemoveHandler(CommandName);
        }

        private void OnLogin()
        {
            if (this.UploadManager == null)
            {
                this.UploadManager = new UploadManager(this);
                var sheetValidationThread = new Thread(() => this.UploadManager!.ValidateConfig());
                sheetValidationThread.Start();
            }
        }

        private void OnLogout()
        {
            this.UploadManager?.Dispose();
            this.UploadManager = null;
        }

        private void OnCommand(string command, string args)
        {
            ConfigWindow.IsOpen = true;
        }

        private void DrawUI()
        {
            this.WindowSystem.Draw();
        }

        public void DrawConfigUI()
        {
            ConfigWindow.IsOpen = true;
        }
    }
}
