using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using ReLogic.Graphics;
using System;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.GameInput;
using Terraria.ModLoader;

namespace PvPAdventure.Common.Spectator.Drawers.Inventory;

public static class BuffDrawer
{
    public static void DrawBuffs(Player player)
    {
        int num = -1;
        int num2 = 11;
        for (int i = 0; i < Player.maxBuffs; i++)
        {
            if (player.buffType[i] > 0)
            {
                _ = player.buffType[i];
                int x = 32 + i * 38;
                int num3 = 76;
                int num4 = i;
                while (num4 >= num2)
                {
                    num4 -= num2;
                    x = 32 + num4 * 38;
                    num3 += 50;
                }
                num = DrawBuffIcon(player, i, x, num3);
            }
            else
            {
                Main.buffAlpha[i] = 0.4f;
            }
        }
    }

    public static int DrawBuffIcon(Player player, int buffSlotOnPlayer, int x, int y)
    {
        int num = player.buffType[buffSlotOnPlayer];
        if (num == 0)
        {
            return -1;
        }
        Color color = new Color(Main.buffAlpha[buffSlotOnPlayer], Main.buffAlpha[buffSlotOnPlayer], Main.buffAlpha[buffSlotOnPlayer], Main.buffAlpha[buffSlotOnPlayer]);
        Asset<Texture2D> obj = TextureAssets.Buff[num];
        Texture2D texture = obj.Value;
        Vector2 drawPosition = new Vector2(x, y);
        int width = obj.Width();
        int height = obj.Height();
        Vector2 textPosition = new Vector2(x, y + height);
        Rectangle sourceRectangle = new Rectangle(0, 0, width, height);
        Rectangle mouseRectangle = new Rectangle(x, y, width, height);
        Color drawColor = color;
        BuffDrawParams drawParams = new BuffDrawParams(texture, drawPosition, textPosition, sourceRectangle, mouseRectangle, drawColor);
        bool num2 = !BuffLoader.PreDraw(Main.spriteBatch, num, buffSlotOnPlayer, ref drawParams);
        BuffDrawParams buffDrawParams = drawParams;
        //(texture, drawPosition, textPosition, sourceRectangle, mouseRectangle, drawColor) = (BuffDrawParams)(ref buffDrawParams);
        if (!num2)
        {
            Main.spriteBatch.Draw(texture, drawPosition, sourceRectangle, drawColor, 0f, default(Vector2), 1f, SpriteEffects.None, 0f);
        }
        BuffLoader.PostDraw(Main.spriteBatch, num, buffSlotOnPlayer, drawParams);
        if (Main.TryGetBuffTime(buffSlotOnPlayer, out var buffTimeValue) && buffTimeValue > 2)
        {
            string text = Lang.LocalizedDuration(new TimeSpan(0, 0, buffTimeValue / 60), abbreviated: true, showAllAvailableUnits: false);
            Main.spriteBatch.DrawString(FontAssets.ItemStack.Value, text, textPosition, color, 0f, default(Vector2), 0.8f, SpriteEffects.None, 0f);
        }
        if (mouseRectangle.Contains(new Point(Main.mouseX, Main.mouseY)))
        {
            //drawBuffText = buffSlotOnPlayer;
            Main.buffAlpha[buffSlotOnPlayer] += 0.1f;
            bool flag = Main.mouseRight && Main.mouseRightRelease;
            if (PlayerInput.UsingGamepad)
            {
                flag = Main.mouseLeft && Main.mouseLeftRelease && Main.playerInventory;
                if (Main.playerInventory)
                {
                    Main.player[Main.myPlayer].mouseInterface = true;
                }
            }
            else
            {
                Main.player[Main.myPlayer].mouseInterface = true;
            }
            if (flag)
            {
                flag &= BuffLoader.RightClick(num, buffSlotOnPlayer);
            }
            if (flag)
            {
                Main.TryRemovingBuff(buffSlotOnPlayer, num);
            }
        }
        else
        {
            Main.buffAlpha[buffSlotOnPlayer] -= 0.05f;
        }
        if (Main.buffAlpha[buffSlotOnPlayer] > 1f)
        {
            Main.buffAlpha[buffSlotOnPlayer] = 1f;
        }
        else if ((double)Main.buffAlpha[buffSlotOnPlayer] < 0.4)
        {
            Main.buffAlpha[buffSlotOnPlayer] = 0.4f;
        }
        if (PlayerInput.UsingGamepad && !Main.playerInventory)
        {
            //drawBuffText = -1;
        }
        //return drawBuffText;
        return -1;
    }
}
