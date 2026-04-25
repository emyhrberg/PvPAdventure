using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;
using Terraria.UI;

namespace PvPAdventure.Common.Spectator;

[Autoload(Side = ModSide.Client)]
internal sealed class SpectateCameraFade : ModSystem
{
    private const float FadeDistanceTiles = 70f;
    private const int FadeTicks = 42;
    private static readonly float FadeDistancePixelsSq = FadeDistanceTiles * 16f * FadeDistanceTiles * 16f;
    private static int fadeTicksLeft;

    public static void SetScreenPosition(Vector2 position)
    {
        if (Vector2.DistanceSquared(Main.screenPosition, position) >= FadeDistancePixelsSq)
            fadeTicksLeft = FadeTicks;

        Main.screenPosition = position;
    }

    public override void UpdateUI(GameTime gameTime)
    {
        if (fadeTicksLeft > 0)
            fadeTicksLeft--;
    }

    public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
    {
        int index = layers.FindIndex(layer => layer.Name == "Vanilla: Mouse Text");
        if (index < 0)
            return;

        layers.Insert(index, new LegacyGameInterfaceLayer(
            "PvPAdventure: Spectate Camera Fade",
            DrawFade,
            InterfaceScaleType.UI));
    }

    private static bool DrawFade()
    {
        if (fadeTicksLeft <= 0)
            return true;

        float progress = fadeTicksLeft / (float)FadeTicks;
        float alpha = progress * progress;
        Main.spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(0, 0, Main.screenWidth, Main.screenHeight), Color.Black * alpha);
        return true;
    }
}