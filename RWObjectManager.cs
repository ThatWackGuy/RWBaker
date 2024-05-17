using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Text.RegularExpressions;
using RWBaker.Props;
using RWBaker.Tiles;

namespace RWBaker;

public class RWObjectManager
{
    public delegate void ObjectsChangedEvent();

    public readonly List<TileInfo> Tiles = new();
    public string TileLoadLogs = "";
    public event ObjectsChangedEvent TilesChanged = () => { };

    public readonly List<Prop> Props = new();
    public string PropLoadLogs = "";
    public event ObjectsChangedEvent PropsChanged = () => { };

    public string GraphicsDir;
    public string TileLastSearched;
    public string TileLastUsed;
    public bool TileUseUnlit;
    public float TileUseRain;

    public string PropsDir;
    public string PropLastSearched;
    public string PropLastUsed;
    public bool PropUseUnlit;
    public float PropUseRain;

    public RWObjectManager(UserData userData)
    {
        GraphicsDir = userData.SavedGraphicsDir;
        TileLastSearched = userData.TileLastSearched;
        TileLastUsed = userData.TileLastUsed;
        TileUseUnlit = userData.TileUseUnlit;
        TileUseRain = userData.TileUseRain;

        PropsDir = userData.SavedPropsDir;
        PropLastSearched = userData.PropLastSearched;
        PropLastUsed = userData.PropLastUsed;
        PropUseUnlit = userData.PropUseUnlit;
        PropUseRain = userData.PropUseRain;
    }

    public void GetTiles(string path)
    {
        Tiles.Clear();
        TileLoadLogs = "";

        if (path == "")
        {
            TileLoadLogs = "NO GRAPHICS DIRECTORY GIVEN";
            return;
        }

        if (!Directory.Exists(path))
        {
            TileLoadLogs = "GRAPHICS FOLDER DOES NOT EXIST!";
            return;
        }

        if (!File.Exists($"{path}/init.txt"))
        {
            TileLoadLogs = "GRAPHICS INIT DOES NOT EXIST!";
            return;
        }

        GraphicsDir = path;

        string[] initLines = File.ReadAllLines(path + "/init.txt");

        string lastCategory = "Uncategorized";
        Vector3 lastColor = new(255, 255, 255);

        foreach (string line in initLines)
        {
            // empty line or comment
            if (Regex.IsMatch(line, "^\\s*$") || line.StartsWith("--")) continue;

            // category definition
            if (Regex.IsMatch(line, "-\\[\"(.+?)\",\\s*color\\s*\\(((?:\\s*[0-9]+\\s*,?){3})\\)\\]"))
            {
                GroupCollection categoryInfo = Regex.Match(line, "-\\[\"(.+?)\",\\s*color\\s*\\(((?:\\s*[0-9]+\\s*,?){3})\\)\\]").Groups;
                lastCategory = categoryInfo[1].Value;
                string[] colorsNums = categoryInfo[2].Value.Replace(" ", "").Split(',');
                lastColor.X = int.Parse(colorsNums[0]);
                lastColor.Y = int.Parse(colorsNums[1]);
                lastColor.Z = int.Parse(colorsNums[2]);

                continue;
            }

            TileInfo tileObject = new(this, line, lastCategory, lastColor);
            Tiles.Add(tileObject);
        }

        TilesChanged();
    }

    public void GetProps(string path)
    {
        Props.Clear();
        PropLoadLogs = "";

        if (path == "")
        {
            PropLoadLogs = "NO PROPS DIRECTORY GIVEN";
            return;
        }

        if (!Directory.Exists(path))
        {
            PropLoadLogs = "PROPS FOLDER DOES NOT EXIST!";
            return;
        }

        if (!File.Exists(path + "/init.txt"))
        {
            PropLoadLogs = "PROPS INIT DOES NOT EXIST!";
            return;
        }

        PropsDir = path;

        string[] initLines = File.ReadAllLines(path + "/init.txt");

        string lastCategory = "Uncategorized";
        Vector3 lastColor = new(255, 255, 255);

        foreach (string line in initLines)
        {
            // empty line or comment
            if (Regex.IsMatch(line, "^\\s*$") || line.StartsWith("--")) continue;

            // category definition
            if (Regex.IsMatch(line, "-\\[\"(.+?)\",\\s*color\\s*\\(((?:\\s*[0-9]+\\s*,?){3})\\)\\]"))
            {
                GroupCollection categoryInfo = Regex.Match(line, "-\\[\"(.+?)\",\\s*color\\s*\\(((?:\\s*[0-9]+\\s*,?){3})\\)\\]").Groups;
                lastCategory = categoryInfo[1].Value;
                string[] colorsNums = categoryInfo[2].Value.Replace(" ", "").Split(',');
                lastColor.X = int.Parse(colorsNums[0]);
                lastColor.Y = int.Parse(colorsNums[1]);
                lastColor.Z = int.Parse(colorsNums[2]);

                continue;
            }

            // get prop type to be constructed
            bool defaultType = false;
            string typeStr = Regex.Match(line, "#tp *: *\"(.*?)\"").Groups[1].Value;
            if (!RWUtils.LingoEnum(typeStr, PropType.Standard, out PropType propType))
            {
                defaultType = true;
            }

            Prop prop = propType switch
            {
                PropType.Standard or PropType.VariedStandard => new StandardProp(this, propType, line, lastCategory, lastColor),
                PropType.Soft or PropType.VariedSoft => new SoftProp(this, propType, line, lastCategory, lastColor),
                PropType.SimpleDecal or PropType.VariedDecal => new DecalProp(this, propType, line, lastCategory, lastColor),
                PropType.Antimatter => new AntimatterProp(this, propType, line, lastCategory, lastColor),

                _ => throw new Exception("How? See RWObjectManager.cs line 188")
            };

            if (defaultType) prop.LogWarning($"Couldn't parse tile type '{typeStr}'. Defaulting to Standard.");

            Props.Add(prop);
        }

        PropsChanged();
    }
}