/**
 * Based on https://raw.githubusercontent.com/Infiziert90/SubmarineTracker/master/SubmarineTracker/Manager/HookManager.cs
 */

using Dalamud.Hooking;
using Dalamud.Memory;
using FFXIVClientStructs.FFXIV.Client.Game;
using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Lumina.Excel;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Runtime.CompilerServices;
using Lumina.Text.ReadOnly;
using Lumina.Excel.Sheets;

namespace SubDataLogger;

public class HookManager
{
    private readonly Plugin plugin;

    private const string PacketReceiverSig = "E8 ?? ?? ?? ?? E9 ?? ?? ?? ?? 44 0F B6 46 ?? 4C 8D 4E 17";
    private const string PacketReceiverSigCN = "E8 ?? ?? ?? ?? E9 ?? ?? ?? ?? 44 0F B6 46 ?? 4C 8D 4E 17";
    private delegate void PacketDelegate(uint param1, ushort param2, sbyte param3, Int64 param4, char param5);
    private readonly Hook<PacketDelegate> packetHandlerHook;
    private static ExcelSheet<Item> ItemSheet = null!;
    public static unsafe ReadOnlySeStringSpan NameToSeString(Span<byte> name) => new((byte*)Unsafe.AsPointer(ref name[0]));




    public HookManager(Plugin plugin)
    {
        this.plugin = plugin;

        ItemSheet = this.plugin.Data.GetExcelSheet<Item>()!;

        // Try to resolve the CN sig if normal one fails ...
        // Doing this because CN people use an outdated version that still uploads data
        // so trying to get them at least somewhat up to date
        nint packetReceiverPtr;
        try
        {
            packetReceiverPtr = this.plugin.SigScanner.ScanText(PacketReceiverSig);
        }
        catch (Exception)
        {
            this.plugin.Log.Error("Exception in sig scan, maybe CN client?");
            packetReceiverPtr = this.plugin.SigScanner.ScanText(PacketReceiverSigCN);
        }

        packetHandlerHook = this.plugin.Hook.HookFromAddress<PacketDelegate>(packetReceiverPtr, PacketReceiver);
        packetHandlerHook.Enable();
    }

    public void Dispose()
    {
        packetHandlerHook.Dispose();
    }

    private unsafe void PacketReceiver(uint param1, ushort param2, sbyte param3, Int64 param4, char param5)
    {
        packetHandlerHook.Original(param1, param2, param3, param4, param5);

        // We only care about voyage Result
        if (param1 != 721343 || !this.plugin.Configuration.Validate())
            return;

        try
        {
            var instance = HousingManager.Instance();
            if (instance == null || instance->WorkshopTerritory == null)
                return;

            var current = instance->WorkshopTerritory->Submersible.DataPointers[4];
            if (current.Value == null)
                return;

            // Get the sub data
            var sub = current.Value; // HousingWorkshopSubmarine
            var lootList = GetLootList(sub->GatheredData)!; // List<DetailedLoot>
            var charName = plugin.ClientState.LocalPlayer!.Name; // Character Name as an SeString
            var subName = NameToSeString(sub->Name).ExtractText(); // Submarine Name as string
            var subLevel = sub->RankId; // Submarine Level as int
            StringBuilder sb = new StringBuilder(); // StringBuilder to hold the route code
            foreach (var point in sub->CurrentExplorationPoints)
            {
                if (point > 0)
                {
                    sb.Append(Utils.SiteToLetter(point));
                }
                
            }
            
            var routeCode = sb.ToString(); // Route Code as string

            // Sub Parts
            var hullName = !Utils.PartIdToItemId.TryGetValue(sub->HullId, out uint itemId) ? "None?" : plugin.Data.GetExcelSheet<Item>()!.GetRow(itemId)!.Name;
            var hullIdentifier = Utils.PartToIdentifier(sub->HullId);
            var sternName = !Utils.PartIdToItemId.TryGetValue(sub->SternId, out itemId) ? "None?" : plugin.Data.GetExcelSheet<Item>()!.GetRow(itemId)!.Name;
            var sternIdentifier = Utils.PartToIdentifier(sub->SternId);
            var bowName = !Utils.PartIdToItemId.TryGetValue(sub->BowId, out itemId) ? "None?" : plugin.Data.GetExcelSheet<Item>()!.GetRow(itemId)!.Name;
            var bowIdentifier = Utils.PartToIdentifier(sub->BowId);
            var bridgeName = !Utils.PartIdToItemId.TryGetValue(sub->BridgeId, out itemId) ? "None?" : plugin.Data.GetExcelSheet<Item>()!.GetRow(itemId)!.Name;
            var bridgeIdentifier = Utils.PartToIdentifier(sub->BridgeId);
            var buildIdentifier = $"{hullIdentifier}{sternIdentifier}{bowIdentifier}{bridgeIdentifier}";
            buildIdentifier = buildIdentifier.Contains('+') ? string.Concat(buildIdentifier.Replace("+", ""), "+") : buildIdentifier;

            // Loot 
            var totalEarnings = 0;
            var time = DateTime.Now.ToUniversalTime().ToString("MM/dd/yyyy HH:mm:ss");
            plugin.Log.Info("--------- BEGIN SUB ---------");
            plugin.Log.Info(string.Format("Character: {0}", charName));
            plugin.Log.Info(string.Format("Sub Name: {0}", subName));
            plugin.Log.Info(string.Format("Sub Level: {0}", subLevel));
            plugin.Log.Info(string.Format("Route Code: {0}", routeCode));
            plugin.Log.Info(string.Format("Hull: {0}", hullName));
            plugin.Log.Info(string.Format("Stern: {0}", sternName));
            plugin.Log.Info(string.Format("Bow: {0}", bowName));
            plugin.Log.Info(string.Format("Bridge: {0}", bridgeName));
            plugin.Log.Info(string.Format("Build Identifier: {0}", buildIdentifier));
            plugin.Log.Info(string.Format("Time: {0}", time));

            plugin.Log.Info("Loot:");
            foreach (DetailedLoot loot in lootList)
            {
                if (IsSalvageItem(loot.PrimaryItem.Name.ToString()))
                {
                    plugin.Log.Info(string.Format("- {0} x {1} with a price of {2:n} per unit (Total: {3:n})", loot.PrimaryItem.Name, loot.PrimaryCount, loot.PrimaryItem.PriceLow, loot.PrimaryCount * loot.PrimaryItem.PriceLow));
                    totalEarnings += (int)(loot.PrimaryCount * loot.PrimaryItem.PriceLow);
                }
                if (loot.ValidAdditional && IsSalvageItem(loot.AdditionalItem.Name.ToString()))
                {
                    plugin.Log.Info(string.Format("- {0} x {1} with a price of {2:n} per unit (Total: {3:n})", loot.AdditionalItem.Name, loot.AdditionalCount, loot.AdditionalItem.PriceLow, loot.AdditionalCount * loot.AdditionalItem.PriceLow));
                    totalEarnings += (int)(loot.AdditionalCount * loot.AdditionalItem.PriceLow);
                }

            }
            plugin.Log.Info(string.Format("Total Earnings: {0:n}", totalEarnings));
            plugin.Log.Info("---------- END SUB ----------");
            if (!IsSalvageRoute(routeCode))
            {
                plugin.Log.Info("Not a salvage route, not uploading.");
                return;
            }
            if (totalEarnings == 0 && subLevel < 79)
            {
                plugin.Log.Info("No salvage items found, not uploading.");
                return;
            }
            

            var payload = new Payload(this.plugin.Configuration.name, charName.ToString(), subName, time, subLevel.ToString(), routeCode, buildIdentifier, $"{routeCode}{buildIdentifier}", hullName.ToString(), sternName.ToString(), bowName.ToString(), bridgeName.ToString(), totalEarnings);
            var t = new Thread(() => this.plugin.UploadManager!.UploadData(payload, plugin));
            t.Start();
        }
        catch (Exception e)
        {
            plugin.Log.Error(e.Message);
            plugin.Log.Error(e.StackTrace ?? "Unknown");
        }
    }

    public static bool IsSalvageItem(String s)
    {
        String[] salvageItemNames =
        {
            "Extravagant Salvaged Bracelet",
            "Extravagant Salvaged Earring",
            "Extravagant Salvaged Necklace",
            "Extravagant Salvaged Ring",
            "Salvaged Bracelet",
            "Salvaged Earring",
            "Salvaged Necklace",
            "Salvaged Ring"
        };
        return salvageItemNames.Contains(s);
    }

    public static bool IsSalvageRoute(String s)
    {
        String[] salvageRoutes =
        {
            "M",
            "R",
            "O",
            "J",
            "Z"
        };
        return salvageRoutes.Any(s.Contains);
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
}
