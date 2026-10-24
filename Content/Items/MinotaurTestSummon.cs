using Microsoft.Xna.Framework;
using MythosOfOlympus.Content.NPCs.Bosses;
using Terraria;
using Terraria.Chat;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace MythosOfOlympus.Content.Items
{
	public class MinotaurTestSummon : ModItem
	{
		public override string Texture => $"Terraria/Images/Item_{ItemID.SuspiciousLookingEye}";

		public override void SetStaticDefaults() {
			Item.ResearchUnlockCount = 1;
			ItemID.Sets.SortingPriorityBossSpawns[Type] = 12;
		}

		public override void SetDefaults() {
			Item.width = 20;
			Item.height = 20;
			Item.maxStack = 1;
			Item.rare = ItemRarityID.Blue;
			Item.useTime = 30;
			Item.useAnimation = 30;
			Item.useStyle = ItemUseStyleID.HoldUp;
			Item.consumable = false;
		}

		public override bool CanUseItem(Player player) {
			return player.active && !player.dead;
		}

		public override bool? UseItem(Player player) {
			// Multiplayer clients request a spawn; only the server chooses its position.
			if (player.whoAmI == Main.myPlayer) {
				if (Main.netMode != NetmodeID.MultiplayerClient) {
					TrySummon(player);
				}
				else {
					ModPacket packet = Mod.GetPacket();
					packet.Write((byte)0);
					packet.Send();
				}
			}
			return true;
		}

		internal static void TrySummon(Player player) {
			if (Main.netMode == NetmodeID.MultiplayerClient) return;
			int type = ModContent.NPCType<Minotaur>();
			if (NPC.AnyNPCs(type)) {
				Notify(player, "AlreadyActive");
				return;
			}
			NPC sample = ContentSamples.NpcsByNetId[type];
			// Search 10-22 tiles away, facing side first, with nearby floor heights.
			for (int distance = 10; distance <= 22; distance += 2) {
				for (int side = 1; side >= -1; side -= 2) {
					for (int offset = 0; offset <= 24; offset++) {
						int heightOffset = offset % 2 == 0 ? offset / 2 : -(offset + 1) / 2;
						Vector2 bottom = new Vector2(
							(int)(player.Center.X / 16f) * 16 + distance * 16 * side * player.direction,
							(int)(player.Bottom.Y / 16f) * 16 + heightOffset * 16);
						Vector2 position = bottom - new Vector2(sample.width / 2f, sample.height);
						if (position.X < 16 || position.Y < 16 || bottom.X + sample.width > Main.maxTilesX * 16 - 16
							|| bottom.Y > Main.maxTilesY * 16 - 32) continue;
						if (Collision.SolidCollision(position, sample.width, sample.height)
							|| Collision.TileCollision(position, new Vector2(0, 2), sample.width, sample.height).Y >= 2f
							|| !Collision.CanHitLine(player.position, player.width, player.height, position, sample.width, sample.height)) continue;

						int index = NPC.NewNPC(player.GetSource_ItemUse(player.HeldItem), (int)bottom.X,
							(int)bottom.Y, type, Target: player.whoAmI);
						if (index >= Main.maxNPCs) {
							Notify(player, "SpawnFailed");
							return;
						}
						Main.npc[index].Bottom = bottom;
						Main.npc[index].netUpdate = true;
						Notify(player, Minotaur.CanStartFight ? "Summoned" : "Locked");
						return;
					}
				}
			}
			Notify(player, "NoSpace");
		}

		private static void Notify(Player player, string message) {
			string key = "Mods.MythosOfOlympus.Summoning." + message;
			if (Main.netMode == NetmodeID.Server) {
				ChatHelper.SendChatMessageToClient(NetworkText.FromKey(key), Color.Orange, player.whoAmI);
			}
			else Main.NewText(Language.GetTextValue(key), Color.Orange);
		}

		public override void AddRecipes() {
			// Cheap, station-free recipe for the combat prototype only.
			CreateRecipe().AddIngredient(ItemID.DirtBlock, 1).Register();
		}
	}
}
