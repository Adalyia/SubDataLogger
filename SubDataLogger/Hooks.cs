using Dalamud.Hooking;
using Dalamud.Memory;
using FFXIVClientStructs.FFXIV.Client.Game.Housing;
using Lumina.Excel.GeneratedSheets;
using SubDataLogger;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Newtonsoft.Json;
using Lumina.Excel;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.Runtime.InteropServices;
using System.Text;

namespace SubDataLogger;
/**
 * Imported from https://raw.githubusercontent.com/Infiziert90/SubmarineTracker/master/SubmarineTracker/Manager/HookManager.cs
 * 
 */

public class HookManager
{
    private readonly Plugin Plugin;

    private const string PacketReceiverSig = "E8 ?? ?? ?? ?? E9 ?? ?? ?? ?? 44 0F B6 43 ?? 4C 8D 4B 17";
    private const string PacketReceiverSigCN = "E8 ?? ?? ?? ?? E9 ?? ?? ?? ?? 44 0F B6 46 ??";
    private delegate void PacketDelegate(uint param1, ushort param2, sbyte param3, Int64 param4, char param5);
    private readonly Hook<PacketDelegate> PacketHandlerHook;
    private static ExcelSheet<Item> ItemSheet = null!;

    public static readonly Dictionary<ushort, uint> PartIdToItemId = new()
    {
        // Shark
        { 1, 21792 }, // Bow
        { 2, 21793 }, // Bridge
        { 3, 21794 }, // Hull
        { 4, 21795 }, // Stern

        // Ubiki
        { 5, 21796 },
        { 6, 21797 },
        { 7, 21798 },
        { 8, 21799 },

        // Whale
        { 9, 22526 },
        { 10, 22527 },
        { 11, 22528 },
        { 12, 22529 },

        // Coelacanth
        { 13, 23903 },
        { 14, 23904 },
        { 15, 23905 },
        { 16, 23906 },

        // Syldra
        { 17, 24344 },
        { 18, 24345 },
        { 19, 24346 },
        { 20, 24347 },

        // Modified same order
        { 21, 24348 },
        { 22, 24349 },
        { 23, 24350 },
        { 24, 24351 },

        { 25, 24352 },
        { 26, 24353 },
        { 27, 24354 },
        { 28, 24355 },

        { 29, 24356 },
        { 30, 24357 },
        { 31, 24358 },
        { 32, 24359 },

        { 33, 24360 },
        { 34, 24361 },
        { 35, 24362 },
        { 36, 24363 },

        { 37, 24364 },
        { 38, 24365 },
        { 39, 24366 },
        { 40, 24367 }
    };

    public HookManager(Plugin plugin)
    {
        Plugin = plugin;
        ItemSheet = Plugin.Data.GetExcelSheet<Item>()!;

        // Try to resolve the CN sig if normal one fails ...
        // Doing this because CN people use an outdated version that still uploads data
        // so trying to get them at least somewhat up to date
        nint packetReceiverPtr;
        try
        {
            packetReceiverPtr = Plugin.SigScanner.ScanText(PacketReceiverSig);
        }
        catch (Exception)
        {
            Plugin.Log.Error("Exception in sig scan, maybe CN client?");
            packetReceiverPtr = Plugin.SigScanner.ScanText(PacketReceiverSigCN);
        }

        PacketHandlerHook = Plugin.Hook.HookFromAddress<PacketDelegate>(packetReceiverPtr, PacketReceiver);
        PacketHandlerHook.Enable();
    }

    public void Dispose()
    {
        PacketHandlerHook.Dispose();
    }

    private unsafe void PacketReceiver(uint param1, ushort param2, sbyte param3, Int64 param4, char param5)
    {
        PacketHandlerHook.Original(param1, param2, param3, param4, param5);

        // We only care about voyage Result
        if (param1 != 721343)
            return;

        try
        {
            var instance = HousingManager.Instance();
            if (instance == null || instance->WorkshopTerritory == null)
                return;

            var current = instance->WorkshopTerritory->Submersible.DataPointerListSpan[4];
            if (current.Value == null)
                return;

            var sub = current.Value;

            var lootList = GetLootList(sub->GatheredDataSpan)!;
            var charName = Plugin.ClientState.LocalPlayer!.Name;
            
            var subName = MemoryHelper.ReadSeStringNullTerminated((nint)sub->Name).ToString();
            var subLevel = sub->RankId;
            var managedArray = new byte[5];
            Marshal.Copy((nint)sub->CurrentExplorationPoints, managedArray, 0, 5);
            StringBuilder sb = new StringBuilder();
            
            for (int i = 0; i < 5; i++)
            {
                if (managedArray[i] != 0)
                {
                    sb.Append(SiteToLetter(managedArray[i]));
                }
            }
            var routeCode = sb.ToString();
            var hullName = !PartIdToItemId.TryGetValue(sub->HullId, out uint itemId) ? "None?" : Plugin.Data.GetExcelSheet<Item>()!.GetRow(itemId)!.Name;
            var hullIdentifier = PartToIdentifier(sub->HullId);
            var sternName = !PartIdToItemId.TryGetValue(sub->SternId, out itemId) ? "None?" : Plugin.Data.GetExcelSheet<Item>()!.GetRow(itemId)!.Name;
            var sternIdentifier = PartToIdentifier(sub->SternId);
            var bowName = !PartIdToItemId.TryGetValue(sub->BowId, out itemId) ? "None?" : Plugin.Data.GetExcelSheet<Item>()!.GetRow(itemId)!.Name;
            var bowIdentifier = PartToIdentifier(sub->BowId);
            var bridgeName = !PartIdToItemId.TryGetValue(sub->BridgeId, out itemId) ? "None?" : Plugin.Data.GetExcelSheet<Item>()!.GetRow(itemId)!.Name;
            var bridgeIdentifier = PartToIdentifier(sub->BridgeId);
            var buildIdentifier = $"{hullIdentifier}{sternIdentifier}{bowIdentifier}{bridgeIdentifier}";
            buildIdentifier = buildIdentifier.Contains('+') ? string.Concat(buildIdentifier.Replace("+", ""), "+") : buildIdentifier;
            var totalEarnings = 0;
            var time = DateTime.Now.ToUniversalTime();
            Plugin.Log.Info("--------- BEGIN SUB ---------");
            Plugin.Log.Info(string.Format("Character: {0}", charName));
            Plugin.Log.Info(string.Format("Sub Name: {0}", subName));
            Plugin.Log.Info(string.Format("Sub Level: {0}", subLevel));
            Plugin.Log.Info(string.Format("Route Code: {0}", routeCode));
            Plugin.Log.Info(string.Format("Hull: {0}", hullName));
            Plugin.Log.Info(string.Format("Stern: {0}", sternName));
            Plugin.Log.Info(string.Format("Bow: {0}", bowName));
            Plugin.Log.Info(string.Format("Bridge: {0}", bridgeName));
            Plugin.Log.Info(string.Format("Build Identifier: {0}", buildIdentifier));
            Plugin.Log.Info(string.Format("Time: {0} {1}", time.ToLongDateString(), time.ToLongTimeString()));

            Plugin.Log.Info("Loot:");
            foreach (DetailedLoot loot in lootList)
            {
                if (loot.PrimaryItem.Name.ToString().Contains("Salvage"))
                {
                    Plugin.Log.Info(string.Format("- {0} x {1} with a price of {2:n} per unit (Total: {3:n})", loot.PrimaryItem.Name, loot.PrimaryCount, loot.PrimaryItem.PriceLow, loot.PrimaryCount * loot.PrimaryItem.PriceLow));
                    totalEarnings += (int)(loot.PrimaryCount * loot.PrimaryItem.PriceLow);
                }
                if (loot.ValidAdditional && loot.AdditionalItem.Name.ToString().Contains("Salvage"))
                {
                    Plugin.Log.Info(string.Format("- {0} x {1} with a price of {2:n} per unit (Total: {3:n})", loot.AdditionalItem.Name, loot.AdditionalCount, loot.AdditionalItem.PriceLow, loot.AdditionalCount * loot.AdditionalItem.PriceLow));
                    totalEarnings += (int)(loot.AdditionalCount * loot.AdditionalItem.PriceLow);
                }

            }
            Plugin.Log.Info(string.Format("Total Earnings: {0:n}", totalEarnings));
            Plugin.Log.Info("---------- END SUB ----------");
        }
        catch (Exception e)
        {
            Plugin.Log.Error(e.Message);
            Plugin.Log.Error(e.StackTrace ?? "Unknown");
        }
    }

    public static List<DetailedLoot>? GetLootList(Span<HousingWorkshopSubmarineGathered> data)
    {
        var list = new List<DetailedLoot>();
        if (data[0].ItemIdPrimary == 0)
            return null;

        foreach (var val in data.ToArray().Where(val => val.Point > 0))
        {
            list.Add(new DetailedLoot(val));
        }
        return list;


    }

    public class DetailedLoot
    {
        public bool Valid;
        public int Rank;
        public int Surv;
        public int Ret;
        public int Fav;

        public uint PrimarySurvProc;
        public uint AdditionalSurvProc;
        public uint PrimaryRetProc;
        public uint AdditionalRetProc;
        public uint FavProc;

        public uint Sector;
        public uint Unlocked = 0;

        public uint Primary;
        public ushort PrimaryCount;
        public bool PrimaryHQ;

        public uint Additional;
        public ushort AdditionalCount;
        public bool AdditionalHQ;
        public DateTime Date = DateTime.MinValue;

        [JsonConstructor]
        public DetailedLoot() { }

        public DetailedLoot(HousingWorkshopSubmarineGathered data)
        {
            Valid = true;


            Sector = data.Point;
            Unlocked = data.UnlockedPoint;

            Primary = data.ItemIdPrimary;
            PrimaryCount = data.ItemCountPrimary;
            PrimaryHQ = data.ItemHQPrimary;
            PrimarySurvProc = data.SurveyLinePrimary;
            PrimaryRetProc = data.YieldLinePrimary;

            Additional = data.ItemIdAdditional;
            AdditionalCount = data.ItemCountAdditional;
            AdditionalHQ = data.ItemHQAdditional;
            AdditionalSurvProc = data.SurveyLineAdditional;
            AdditionalRetProc = data.YieldLineAdditional;
            FavProc = data.FavorLine;

            Date = DateTime.Now;

        }

        [JsonIgnore] public Item PrimaryItem => ItemSheet.GetRow(Primary)!;
        [JsonIgnore] public Item AdditionalItem => ItemSheet.GetRow(Additional)!;
        [JsonIgnore] public bool ValidAdditional => Additional > 0;
    }

    public static string SiteToLetter(uint num)
    {

        var index = (int)(num - 1);  // 0 indexed

        const string letters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";

        var value = "";

        if (index >= letters.Length)
            value += letters[(index / letters.Length) - 1];

        value += letters[index % letters.Length];

        return value;
    }

    public static string PartToIdentifier(ushort partId)
    {
        return ((partId - 1) / 4) switch
        {
            0 => "S",
            1 => "U",
            2 => "W",
            3 => "C",
            4 => "Y",

            5 => $"{PartToIdentifier((ushort)(partId - 20))}+",
            6 => $"{PartToIdentifier((ushort)(partId - 20))}+",
            7 => $"{PartToIdentifier((ushort)(partId - 20))}+",
            8 => $"{PartToIdentifier((ushort)(partId - 20))}+",
            9 => $"{PartToIdentifier((ushort)(partId - 20))}+",
            _ => "Unknown"
        };
    }

    public static string ProcToText(uint proc)
    {
        return proc switch
        {
            // Surveillance Procs
            4 => "T3 High",
            5 => "T2 High",
            6 => "T1 High",
            7 => "T2 Mid",
            8 => "T1 Mid",
            9 => "T1 Low",

            // Retrieval Procs
            14 => "Optimal",
            15 => "Normal",
            16 => "Poor",

            // Favor Procs
            18 => "Yes",
            19 => "CSCFIFFE",
            20 => "Too Low",

            _ => "Unknown"
        };
    }
}
