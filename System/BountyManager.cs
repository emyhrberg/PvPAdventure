using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Chat;
using Terraria.DataStructures;
using Terraria.Enums;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using Terraria.UI;

namespace PvPAdventure.System;

[Autoload(Side = ModSide.Both)]
public class BountyManager : ModSystem
{
    // Any interaction with claims will increment this, ensuring the client is interacting with the correct state.
    public int TransactionId { get; private set; }
    private readonly Dictionary<Team, IList<Page>> _bounties = new();
    public IReadOnlyDictionary<Team, IList<Page>> Bounties => _bounties;

    public UIBountyShop UiBountyShop { get; private set; }

    public sealed class Page(IList<Item[]> bounties)
    {
        public IList<Item[]> Bounties { get; } = bounties;
    }

    public sealed class Transaction(int id, byte team, byte pageIndex, byte bountyIndex)
        : IPacket<Transaction>
    {
        public int Id { get; } = id;
        public byte Team { get; } = team;
        public byte PageIndex { get; } = pageIndex;
        public byte BountyIndex { get; } = bountyIndex;

        public static Transaction Deserialize(BinaryReader reader)
        {
            var transactionId = reader.ReadInt32();
            var team = reader.ReadByte();
            var pageIndex = reader.ReadByte();
            var bountyIndex = reader.ReadByte();

            return new(transactionId, team, pageIndex, bountyIndex);
        }

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(Id);
            writer.Write(Team);
            writer.Write(PageIndex);
            writer.Write(BountyIndex);
        }
    }

    public class ClaimEntitySource : IEntitySource
    {
        public string Context => null;
    }

    private class UIItemSlotScalable(Item[] itemArray, int itemIndex, int itemSlotContext)
        : UIItemSlot(itemArray, itemIndex, itemSlotContext)
    {
        public float InventoryScale { get; set; }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            var previousInventoryScale = Main.inventoryScale;

            try
            {
                Main.inventoryScale = InventoryScale;
                base.DrawSelf(spriteBatch);
            }
            finally
            {
                Main.inventoryScale = previousInventoryScale;
            }
        }
    }

    public class UIBountyShop(BountyManager bountyManager) : UIState
    {
        public override void OnInitialize()
        {
            Invalidate();
        }

        // FIXME: We could be smarter, but is that worth it?
        public void Invalidate()
        {
            RemoveAllChildren();

            var root = new UIElement
            {
                HAlign = 0.5f,
                Top = { Pixels = 200 },
                Width = { Percent = 0.35f },
                Height = { Percent = 0.5f }
            };

            root.Append(new UIPanel
            {
                Width = { Percent = 1.0f },
                Height = { Percent = 1.0f }
            });

            root.Append(new UITextPanel<string>("Bounty Shop", large: true)
            {
                HAlign = 0.5f,
                PaddingLeft = 15.0f,
                PaddingRight = 15.0f,
                PaddingTop = 15.0f,
                PaddingBottom = 15.0f,
                Top = { Pixels = -30.0f },
            });

            var bountyList = new UIList
            {
                Width = { Percent = 1.0f },
                Height = { Percent = 1.0f },
                PaddingTop = 60.0f,
                PaddingBottom = 30.0f, PaddingLeft = 30.0f, PaddingRight = 30.0f,
            };

            root.Append(bountyList);

            var teamBounties = bountyManager.Bounties[(Team)Main.LocalPlayer.team];
            // FIXME: indentation cause we append root late
            if (teamBounties.Count != 0)
            {
                var page = teamBounties[0];

                var i = 0;

                foreach (var items in page.Bounties)
                {
                    var elementForBounty = new UIElement
                    {
                        Width = { Pixels = 600.0f },
                        Height = { Pixels = 50.0f },
                        Top = { Pixels = i * 50.0f },
                    };
                    bountyList.Add(elementForBounty);

                    var button = new UIKeybindingSimpleListItem(() => "Claim", new Color(73, 94, 171, 255) * 0.9f)
                    {
                        // FIXME: Not sure why these don't look right as they do in the controls panel, scaling seems odd.
                        Width = { Pixels = 75.0f },
                        Height = { Pixels = 35.0f }
                    };

                    // FIXME: dumb
                    var i1 = (byte)i;
                    button.OnLeftClick += (evt, element) =>
                    {
                        // FIXME: wtf??
                        var packet = ModContent.GetInstance<BountyManager>().Mod.GetPacket();
                        // FIXME: no magic
                        packet.Write((byte)0);
                        new Transaction(bountyManager.TransactionId, (byte)Main.LocalPlayer.team, 0, i1).Serialize(
                            packet);
                        packet.Send();
                    };

                    elementForBounty.Append(button);
                    var leftOffset = 0.0f;

                    foreach (var item in items)
                    {
                        var itemSlot = new UIItemSlotScalable([item], 0, ItemSlot.Context.ChestItem)
                        {
                            Left = { Pixels = button.Width.Pixels + leftOffset },
                            InventoryScale = 0.8f,
                        };
                        elementForBounty.Append(itemSlot);

                        leftOffset += itemSlot.Width.Pixels;
                    }

                    i++;
                }
            }

            Append(root);
        }
    }

    public override void Load()
    {
        if (Main.netMode != NetmodeID.Server)
            UiBountyShop = new UIBountyShop(this);
    }

    public override void ClearWorld()
    {
        foreach (var team in Enum.GetValues<Team>())
            _bounties[team] = new List<Page>();

        var config = ModContent.GetInstance<AdventureConfig>();

        foreach (var team in Enum.GetValues<Team>())
        {
            if (team == Team.None)
                continue;

            var page = new Page(new List<Item[]>());

            foreach (var bounty in config.Bounties)
            {
                var items = bounty.Items.Select(configItem =>
                    new Item(configItem.Item.Type, configItem.Stack, configItem.Prefix.Type)).ToArray();

                if (items.Length > 0)
                    page.Bounties.Add(items);
            }

            if (page.Bounties.Count > 0)
                _bounties[team].Add(page);
        }
    }

    public override void NetSend(BinaryWriter writer)
    {
        writer.Write(Bounties.Count);

        foreach (var (team, pages) in Bounties)
        {
            writer.Write((int)team);
            writer.Write(pages.Count);

            foreach (var page in pages)
            {
                writer.Write(page.Bounties.Count);

                foreach (var bounty in page.Bounties)
                {
                    writer.Write(bounty.Length);
                    foreach (var item in bounty)
                        ItemIO.Send(item, writer, true);
                }
            }
        }

        writer.Write(TransactionId);
    }

    public override void NetReceive(BinaryReader reader)
    {
        _bounties.Clear();

        var numberOfTeams = reader.ReadInt32();
        for (var i = 0; i < numberOfTeams; i++)
        {
            var team = (Team)reader.ReadInt32();
            var numberOfPages = reader.ReadInt32();
            var pages = new List<Page>();

            for (var j = 0; j < numberOfPages; j++)
            {
                var numberOfBounties = reader.ReadInt32();
                var page = new Page(new List<Item[]>());

                for (var k = 0; k < numberOfBounties; k++)
                {
                    var numberOfItems = reader.ReadInt32();
                    var items = new Item[numberOfItems];

                    for (var l = 0; l < numberOfItems; l++)
                        items[l] = ItemIO.Receive(reader, true);

                    page.Bounties.Add(items);
                }

                pages.Add(page);
            }

            _bounties[team] = pages;
        }

        TransactionId = reader.ReadInt32();

        UiBountyShop.Invalidate();
    }

    public void AwardToTeam(Team team)
    {
        var eligibleBounties = ModContent.GetInstance<AdventureConfig>().Bounties
            .Where(IsBountyAvailable)
            .Select(bounty => bounty.Items)
            .Select(items => items.Select(item => new Item(item.Item.Type, item.Stack, item.Prefix.Type)).ToArray())
            .ToList();

        _bounties[team].Add(new Page(eligibleBounties));

        NetMessage.SendData(MessageID.WorldData);

        foreach (var player in Main.ActivePlayers)
        {
            if ((Team)player.team == team)
                ChatHelper.SendChatMessageToClient(NetworkText.FromLiteral("Your team has been awarded a claim!"),
                    Color.White, player.whoAmI);
        }
    }

    // FIXME: We could be MUCH smarter.
    public override bool HijackGetData(ref byte messageType, ref BinaryReader reader, int playerNumber)
    {
        if (Main.netMode == NetmodeID.Server)
            return false;

        if (messageType is MessageID.PlayerTeam)
            Main.QueueMainThreadAction(() => UiBountyShop.Invalidate());

        return false;
    }

    // FIXME: We could be MUCH smarter.
    public override bool HijackSendData(int whoAmI, int msgType, int remoteClient, int ignoreClient, NetworkText text,
        int number,
        float number2, float number3, float number4, int number5, int number6, int number7)
    {
        if (Main.netMode == NetmodeID.Server)
            return false;

        if (msgType == MessageID.PlayerTeam)
            Main.QueueMainThreadAction(() => UiBountyShop.Invalidate());

        return false;
    }

    public void IncrementTransactionId() => TransactionId++;

    private static bool IsBountyAvailable(AdventureConfig.Bounty bounty)
    {
        // This set requires pre-hardmode, but the world is hardmode.
        if (bounty.Conditions.WorldProgression == AdventureConfig.Condition.WorldProgressionState.PreHardmode &&
            Main.hardMode)
            return false;

        // This set requires hardmode, but the world is pre-hardmode.
        if (bounty.Conditions.WorldProgression == AdventureConfig.Condition.WorldProgressionState.Hardmode &&
            !Main.hardMode)
            return false;

        // This set requires Skeletron Prime to be defeated, but it is not.
        if (bounty.Conditions.SkeletronPrimeDefeated && !NPC.downedMechBoss3)
            return false;

        // This set requires The Twins to be defeated, but it is not.
        if (bounty.Conditions.TwinsDefeated && !NPC.downedMechBoss2)
            return false;

        // This set requires The Destroyer to be defeated, but it is not.
        if (bounty.Conditions.DestroyerDefeated && !NPC.downedMechBoss1)
            return false;

        // This set requires Plantera to be defeated, but it is not.
        if (bounty.Conditions.PlanteraDefeated && !NPC.downedPlantBoss)
            return false;

        // This set requires Golem to be defeated, but it is not.
        if (bounty.Conditions.GolemDefeated && !NPC.downedGolemBoss)
            return false;

        return true;
    }
}