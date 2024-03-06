using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using Google.Apis.Sheets.v4.Data;
using FFXIVClientStructs.Havok;

namespace SubDataLogger;

public class UploadManager
{
    public SheetsService Service { get; private set; }
    static readonly string[] Scopes = { SheetsService.Scope.Spreadsheets };
    private Plugin plugin;

    public UploadManager(Plugin plugin)
    {
        this.plugin = plugin;
        InitializeService(this.plugin.Name);
        this.plugin.Log.Info("Google Sheets Service initialized");
/*        var stuff = Service!.Spreadsheets.Values.Get("1qgWvEcKiU_jsmi66f4jeMMVnzhXB2FmCTB2WL6maJt0", "C2").Execute().Values;
*//*        foreach (var value in stuff)
        {
            plugin.Log.Info(value[0].ToString());
        }*/

    }
    private void InitializeService(string appName)
    {
        var credential = GetCredentialsFromFile();
        Service = new SheetsService(new BaseClientService.Initializer()
        {
            HttpClientInitializer = credential,
            ApplicationName = appName
        });

    }

    private GoogleCredential GetCredentialsFromFile()
    {
        GoogleCredential credential;
        var assembly = Assembly.GetExecutingAssembly();
        using (var stream = assembly.GetManifestResourceStream(assembly.GetManifestResourceNames().Single(str => str.EndsWith("google.json"))))
        {
            credential = GoogleCredential.FromStream(stream).CreateScoped(Scopes);
        }

        return credential;
    }
    public void UploadData(Payload payload, Plugin plugin)
    {
        plugin.Log.Info($"Beginning upload to {plugin.Configuration.sheetID}");
        try {
            var data = new ValueRange();
            data.Values = [new List<object> { payload.GUID, payload.Name, payload.CharacterName, payload.SubName, payload.Timestamp, payload.SubLevel, payload.SubRoute, payload.SubBuild, payload.VoyageSig, payload.SubHull, payload.SubStern, payload.SubBow, payload.SubBridge, payload.Earnings }];
            var request = Service!.Spreadsheets.Values.Append(data, $"{plugin.Configuration.sheetID}", $"{plugin.Configuration.sheetName}!{plugin.Configuration.range}");
            request.ValueInputOption = SpreadsheetsResource.ValuesResource.AppendRequest.ValueInputOptionEnum.USERENTERED;
            var response = request.Execute();
            
            plugin.Log.Info($"Upload complete, {response.Updates.UpdatedRows} row(s) added");
        }
        catch (Exception e)
        {
            plugin.Log.Error(e.Message);
        }


    }



    public void Dispose()
    {
        Service.Dispose();
        this.plugin.Log.Info("Google Sheets Service disposed");
    }
}

