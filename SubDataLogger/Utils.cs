using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SubDataLogger;

public class Utils
{
    public static readonly Dictionary<ushort, uint> PartIdToItemId = new()
    {
        // Shark
        { 1, 21792 }, // Bow
        { 2, 21793 }, // Bridge
        { 3, 21794 }, // Hull
        { 4, 21795 }, // Stern

        // Unkiu
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

