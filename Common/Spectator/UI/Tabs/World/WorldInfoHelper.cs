using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PvPAdventure.Core.Utilities;
using ReLogic.Content;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.Localization;

namespace PvPAdventure.Common.Spectator.UI.Tabs.World;

internal static class WorldInfoHelper
{
    public static Texture2D GetWorldIcon()
    {
        return TryGetWorldIconAsset()?.Value ?? Ass.Icon_World.Value;
    }

    public static string GetNameText()
    {
        return $"Name: {Main.worldName}";
    }

    public static Texture2D GetWorldSizeIcon()
    {
        string path = Main.maxTilesX switch
        {
            <= 4200 => "Images/UI/WorldCreation/IconSizeSmall",
            <= 6400 => "Images/UI/WorldCreation/IconSizeMedium",
            _ => "Images/UI/WorldCreation/IconSizeLarge"
        };

        return Main.Assets.Request<Texture2D>(path).Value;
    }

    public static string GetWorldSizeText()
    {
        if (Main.maxTilesX <= 4200)
            return "Size: Small";

        if (Main.maxTilesX <= 6400)
            return "Size: Medium";

        if (Main.maxTilesX <= 8400)
            return "Size: Large";

        return "Size: Custom";
    }

    public static Texture2D GetWorldDifficultyIcon()
    {
        string path = Main.GameMode switch
        {
            1 => "Images/UI/WorldCreation/IconDifficultyExpert",
            2 => "Images/UI/WorldCreation/IconDifficultyMaster",
            3 => "Images/UI/WorldCreation/IconDifficultyCreative",
            _ => "Images/UI/WorldCreation/IconDifficultyNormal"
        };

        return Main.Assets.Request<Texture2D>(path).Value;
    }

    public static string GetDifficultyText()
    {
        int mode = GetEffectiveDifficultyMode();

        if (mode == 0)
            return "Difficulty: Classic";

        if (mode == 1)
            return "Difficulty: Expert";

        if (mode == 2)
            return "Difficulty: Master";

        if (mode == 3)
            return "Difficulty: Journey";

        if (mode == 4)
            return "Difficulty: Legendary";

        return $"Difficulty: Mode {mode}";
    }

    public static Color GetDifficultyColor()
    {
        int mode = GetEffectiveDifficultyMode();

        if (mode == 1)
            return Main.mcColor;

        if (mode == 2)
            return Main.hcColor;

        if (mode == 3)
            return Main.creativeModeColor;

        if (mode == 4 && typeof(Main).GetField("legendaryModeColor")?.GetValue(null) is Color legendaryColor)
            return legendaryColor;

        if (mode == 4)
            return Main.hcColor;

        return Color.White;
    }

    public static Texture2D GetWorldEvilIcon()
    {
        return Main.Assets.Request<Texture2D>(WorldGen.crimson ? "Images/UI/WorldCreation/IconEvilCrimson" : "Images/UI/WorldCreation/IconEvilCorruption").Value;
    }

    public static string GetEvilText()
    {
        return $"Evil: {(WorldGen.crimson ? "Crimson" : "Corruption")}";
    }

    public static Color GetEvilColor()
    {
        return WorldGen.crimson ? new Color(255, 120, 120) : new Color(170, 120, 255);
    }

    public static Texture2D GetWorldSeedIcon()
    {
        return Main.Assets.Request<Texture2D>("Images/UI/WorldCreation/IconRandomSeed").Value;
    }

    public static string GetSeedText()
    {
        return $"Seed: {Main.ActiveWorldFileData?.SeedText ?? "TBD"}";
    }

    public static string GetTimeText()
    {
        double time = Main.time + (Main.dayTime ? 0.0 : Main.dayLength);
        double hours = time / 3600.0 + 4.5;

        int hour = (int)hours % 12;

        if (hour == 0)
            hour = 12;

        int minute = (int)(time % 3600.0 / 60.0);
        string suffix = hours % 24.0 >= 12.0 ? "PM" : "AM";

        return $"Time: {hour}:{minute:D2}{suffix}";
    }

    public static string GetWeatherText()
    {
        string name = GetWeatherName();
        int wind = (int)Math.Round(Math.Abs(Main.windSpeedCurrent) * 60f);
        string direction = Main.windSpeedCurrent >= 0f ? "E" : "W";

        return $"Weather: {name} ({wind} mph {direction})";
    }

    public static Texture2D GetSextantIcon()
    {
        int index = (Main.bloodMoon && !Main.dayTime) || (Main.eclipse && Main.dayTime) ? 8 : 7;
        return TextureAssets.InfoIcon[index].Value;
    }

    public static string GetMoonText()
    {
        string name = Main.moonPhase switch
        {
            0 => "Full Moon",
            1 => "Waning Gibbous",
            2 => "Third Quarter",
            3 => "Waning Crescent",
            4 => "New Moon",
            5 => "Waxing Crescent",
            6 => "First Quarter",
            _ => "Waxing Gibbous"
        };

        return $"Moon Phase: {name} ({Main.moonPhase + 1}/8)";
    }

    private static Asset<Texture2D> TryGetWorldIconAsset()
    {
        try
        {
            string path = "Images/UI/Icon" + (Main.hardMode ? "Hallow" : string.Empty) + (WorldGen.crimson ? "Crimson" : "Corruption");
            return Main.Assets.Request<Texture2D>(path, AssetRequestMode.ImmediateLoad);
        }
        catch
        {
            return null;
        }
    }

    private static int GetEffectiveDifficultyMode()
    {
        int modeNumber = Main.GameMode;

        if (Main.getGoodWorld && modeNumber > 0)
            modeNumber++;

        return modeNumber;
    }

    private static string GetWeatherName()
    {
        if (Main.maxRaining >= 0.6f)
            return "Heavy Rain";

        if (Main.maxRaining >= 0.2f)
            return "Rain";

        if (Main.maxRaining > 0f)
            return "Light Rain";

        if (Main.cloudAlpha >= 0.8f)
            return "Overcast";

        if (Main.cloudAlpha >= 0.6f)
            return "Mostly Cloudy";

        if (Main.cloudAlpha >= 0.35f)
            return "Cloudy";

        if (Main.cloudAlpha >= 0.15f)
            return "Partly Cloudy";

        return "Clear";
    }
}