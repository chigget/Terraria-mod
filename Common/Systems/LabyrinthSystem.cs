using System.IO;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using MythosOfOlympus.Content.Items;
using MythosOfOlympus.Content.NPCs.Bosses;
using MythosOfOlympus.Content.Tiles;
using Terraria;
using Terraria.Chat;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using Terraria.GameContent.Generation;
using Terraria.WorldBuilding;

namespace MythosOfOlympus.Common.Systems
{
	public class LabyrinthSystem : ModSystem
	{
		public static bool Exists { get; private set; }
		public static bool Unlocked { get; private set; }
		public static bool GuardianDefeated { get; private set; }
		public static Point Origin { get; private set; }
		public static int LayoutVersion { get; private set; }
		// Worlds created before layout version 2 keep their original rooms and seal.
		private static Rectangle LocalArena => LayoutVersion >= 2 ? LabyrinthBuilder.Arena : new Rectangle(42, 24, 72, 26);
		private static Rectangle LocalSeal => LayoutVersion >= 2 ? LabyrinthBuilder.ArenaEntrance : new Rectangle(90, 23, 6, 1);
		public static Point Entrance => new Point(Origin.X + 6, Origin.Y + 57);
		public static Rectangle ArenaTiles => new Rectangle(Origin.X + LocalArena.X,
			Origin.Y + LocalArena.Y, LocalArena.Width, LocalArena.Height);
		private int spawnTimer;

		public override void ClearWorld() {
			Exists = false;
			Unlocked = false;
			GuardianDefeated = false;
			Origin = Point.Zero;
			LayoutVersion = 0;
			spawnTimer = 0;
		}

		internal static void SetLocation(Point origin, int version = 2) {
			Origin = origin;
			LayoutVersion = version;
			Exists = true;
			Unlocked = false;
			GuardianDefeated = false;
		}

		public override void ModifyWorldGenTasks(List<GenPass> tasks, ref double totalWeight) {
			// Major biomes, the dungeon and temple already exist; small decorations come next.
			int index = tasks.FindIndex(pass => pass.Name == "Life Crystals");
			if (index < 0) return;
			tasks.Insert(index, new PassLegacy("Olympian Labyrinth", (progress, configuration) => {
				progress.Message = "Olympian Labyrinth";
				LabyrinthBuilder.TryGenerate(deferPopulation: true);
			}));
		}

		public override void PostWorldGen() {
			if (!Exists && !LabyrinthBuilder.TryGenerate(deferPopulation: true)) Mod.Logger.Warn("No safe location found for the labyrinth. Use /labyrinth generate to retry in single player.");
			if (Exists && LayoutVersion >= 2) LabyrinthBuilder.FinishWorldGeneration();
		}

		public override void SaveWorldData(TagCompound tag) {
			if (!Exists) return;
			tag["labyrinthX"] = Origin.X;
			tag["labyrinthY"] = Origin.Y;
			tag["labyrinthUnlocked"] = Unlocked;
			tag["guardianDefeated"] = GuardianDefeated;
			tag["labyrinthLayoutVersion"] = LayoutVersion;
		}

		public override void LoadWorldData(TagCompound tag) {
			if (!tag.ContainsKey("labyrinthX")) return;
			Point origin = new Point(tag.GetInt("labyrinthX"), tag.GetInt("labyrinthY"));
			int version = tag.ContainsKey("labyrinthLayoutVersion") ? tag.GetInt("labyrinthLayoutVersion") : 1;
			int width = version >= 2 ? LabyrinthBuilder.Width : 120;
			int height = version >= 2 ? LabyrinthBuilder.Height : 64;
			if (origin.X < 3 || origin.Y < 3 || origin.X + width >= Main.maxTilesX - 3
				|| origin.Y + height >= Main.maxTilesY - 3) return;
			SetLocation(origin, version);
			Unlocked = tag.GetBool("labyrinthUnlocked");
			GuardianDefeated = tag.GetBool("guardianDefeated");
		}

		public override void NetSend(BinaryWriter writer) {
			writer.Write(Exists);
			writer.Write(Origin.X);
			writer.Write(Origin.Y);
			writer.Write(Unlocked);
			writer.Write(GuardianDefeated);
			writer.Write(LayoutVersion);
		}

		public override void NetReceive(BinaryReader reader) {
			Exists = reader.ReadBoolean();
			Origin = new Point(reader.ReadInt32(), reader.ReadInt32());
			Unlocked = reader.ReadBoolean();
			GuardianDefeated = reader.ReadBoolean();
			LayoutVersion = reader.ReadInt32();
		}

		public static bool InArena(Vector2 position) => Exists && ArenaTiles.Contains(position.ToTileCoordinates());

		// An interrupted fight must never leave a saved world permanently sealed.
		public override void PostWorldLoad() => SetArenaSealed(false);

		public static void SetArenaSealed(bool closed) {
			if (Main.netMode == NetmodeID.MultiplayerClient || !Exists) return;
			Rectangle doorway = LocalSeal;
			doorway.Offset(Origin);
			Rectangle doorwayPixels = new Rectangle(doorway.X * 16, doorway.Y * 16, doorway.Width * 16, doorway.Height * 16);
			if (closed) {
				// Move a player caught in the closing doorway just inside the arena.
				for (int i = 0; i < Main.maxPlayers; i++) {
					Player player = Main.player[i];
					if (!player.active || player.dead || !player.Hitbox.Intersects(doorwayPixels)) continue;
					Vector2 destination = LayoutVersion >= 2
						? new Vector2(doorwayPixels.Right + 16, ArenaTiles.Bottom * 16 - player.height)
						: new Vector2(player.position.X, doorwayPixels.Bottom + 16);
					if (Collision.SolidCollision(destination, player.width, player.height)) return;
					player.Teleport(destination, TeleportationStyleID.RodOfDiscord);
					player.velocity = Vector2.Zero;
					if (Main.netMode == NetmodeID.Server) NetMessage.SendData(MessageID.TeleportEntity,
						number: 0, number2: i, number3: destination.X, number4: destination.Y, number5: TeleportationStyleID.RodOfDiscord);
				}
			}
			int sealType = ModContent.TileType<LabyrinthArenaSeal>();
			bool changed = false;
			for (int x = doorway.Left; x < doorway.Right; x++) {
				for (int y = doorway.Top; y < doorway.Bottom; y++) {
					Tile tile = Main.tile[x, y];
					if (closed) {
						if (tile.HasTile && tile.TileType != sealType) {
							// Existing solid blocks already close the opening. Remove decorations normally, with drops.
							if (Main.tileSolid[tile.TileType] && !Main.tileSolidTop[tile.TileType] && !tile.IsActuated) continue;
							WorldGen.KillTile(x, y);
							if (tile.HasTile) continue;
						}
						if (tile.HasTile && tile.TileType == sealType && !tile.IsActuated) continue;
						tile.ClearTile();
						tile.HasTile = true;
						tile.TileType = (ushort)sealType;
						changed = true;
					}
					else if (tile.HasTile && tile.TileType == sealType) {
						tile.ClearTile();
						changed = true;
					}
				}
			}
			if (!changed) return;
			WorldGen.RangeFrame(doorway.Left - 1, doorway.Top - 1, doorway.Right + 1, doorway.Bottom + 1);
			if (Main.netMode == NetmodeID.Server) NetMessage.SendTileSquare(-1,
				doorway.X - 1, doorway.Y - 1, doorway.Width + 2, doorway.Height + 2);
		}

		public override void PostUpdateWorld() {
			if (Main.netMode == NetmodeID.MultiplayerClient || !Exists) return;
			bool fighting = false;
			for (int i = 0; i < Main.maxNPCs; i++) {
				if (Main.npc[i].active && Main.npc[i].ModNPC is Minotaur { IsLabyrinthGuardian: true, IsFighting: true }) {
					fighting = true;
					break;
				}
			}
			SetArenaSealed(fighting);
			if (!Unlocked || GuardianDefeated) return;
			if (++spawnTimer < 60) return;
			spawnTimer = 0;
			if (NPC.AnyNPCs(ModContent.NPCType<Minotaur>())) return;
			for (int i = 0; i < Main.maxPlayers; i++) {
				Player player = Main.player[i];
				if (!player.active || player.dead || !InArena(player.Center)) continue;
				Vector2 bottom = new Vector2((ArenaTiles.Left + 28) * 16f, ArenaTiles.Bottom * 16f);
				int index = NPC.NewNPC(new EntitySource_Misc("LabyrinthGuardian"), (int)bottom.X,
					(int)bottom.Y, ModContent.NPCType<Minotaur>(), Target: i);
				if (index < Main.maxNPCs) {
					NPC npc = Main.npc[index];
					npc.Bottom = bottom;
					((Minotaur)npc.ModNPC).IsLabyrinthGuardian = true;
					npc.netUpdate = true;
				}
				break;
			}
		}

		public static void MarkGuardianDefeated() {
			if (Main.netMode == NetmodeID.MultiplayerClient) return;
			GuardianDefeated = true;
			SetArenaSealed(false);
			if (Main.netMode == NetmodeID.Server) NetMessage.SendData(MessageID.WorldData);
		}

		public static void TryUnlock(Player player, int i, int j) {
			if (Main.netMode == NetmodeID.MultiplayerClient || !Exists || Unlocked
				|| !player.active || player.dead || player.ghost) return;
			Rectangle gate = LabyrinthBuilder.Gate;
			gate.Offset(Origin);
			if (!gate.Contains(i, j) || Vector2.Distance(player.Center, new Vector2(i * 16 + 8, j * 16 + 8)) > 7 * 16f) return;
			Tile clicked = Main.tile[i, j];
			if (!clicked.HasTile || clicked.TileType != ModContent.TileType<LabyrinthGate>()) return;
			int slot = -1;
			for (int k = 0; k < 58; k++) {
				if (player.inventory[k].type == ModContent.ItemType<LabyrinthKey>() && player.inventory[k].stack > 0) {
					slot = k;
					break;
				}
			}
			if (slot < 0) {
				Notify(player, "NeedKey");
				return;
			}
			// This transaction runs on the server. A second request sees Unlocked and consumes nothing.
			player.inventory[slot].stack--;
			if (player.inventory[slot].stack == 0) player.inventory[slot].TurnToAir();
			Unlocked = true;
			for (int x = gate.Left; x < gate.Right; x++) {
				for (int y = gate.Top; y < gate.Bottom; y++) {
					Tile tile = Main.tile[x, y];
					tile.HasTile = false;
				}
			}
			WorldGen.RangeFrame(gate.Left - 1, gate.Top - 1, gate.Right + 1, gate.Bottom + 1);
			if (Main.netMode == NetmodeID.Server) {
				NetMessage.SendData(MessageID.SyncEquipment, number: player.whoAmI, number2: slot);
				NetMessage.SendTileSquare(-1, gate.X - 1, gate.Y - 1, gate.Width + 2, gate.Height + 2);
				NetMessage.SendData(MessageID.WorldData);
				ChatHelper.BroadcastChatMessage(NetworkText.FromKey("Mods.MythosOfOlympus.Labyrinth.Opened"), Color.Gold);
			}
			else Notify(player, "Opened");
		}

		private static void Notify(Player player, string message) {
			string key = "Mods.MythosOfOlympus.Labyrinth." + message;
			if (Main.netMode == NetmodeID.Server) ChatHelper.SendChatMessageToClient(NetworkText.FromKey(key), Color.Gold, player.whoAmI);
			else Main.NewText(Language.GetTextValue(key), Color.Gold);
		}
	}
}
