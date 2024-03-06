using System;

namespace SubDataLogger;

public class Payload(string name, string characterName, string subName, DateTime timestamp, string subLevel, string subRoute, string subBuild, string voyageSig, string subHull, string subStern, string subBow, string subBridge, double earnings)
{
    public string? GUID { get; set; } = Guid.NewGuid().ToString();
    public string? Name { get; set; } = name;
    public string? CharacterName { get; set; } = characterName;
    public string? SubName { get; set; } = subName;
    public DateTime? Timestamp { get; set; } = timestamp;
    public string? SubLevel { get; set; } = subLevel;
    public string? SubRoute { get; set; } = subRoute;
    public string? SubBuild { get; set; } = subBuild;
    public string? VoyageSig { get; set; } = voyageSig;
    public string? SubHull { get; set; } = subHull;
    public string? SubStern { get; set; } = subStern;
    public string? SubBow { get; set; } = subBow;
    public string? SubBridge { get; set; } = subBridge;
    public double? Earnings { get; set; } = earnings;

    
    public override String ToString() => $"GUID: {GUID}, Name: {Name}, Character: {CharacterName}, Sub: {SubName}, Timestamp: {Timestamp}, Level: {SubLevel}, Route: {SubRoute}, Build: {SubBuild}, Voyage: {VoyageSig}, Hull: {SubHull}, Stern: {SubStern}, Bow: {SubBow}, Bridge: {SubBridge}, Earnings: {Earnings}";

}

