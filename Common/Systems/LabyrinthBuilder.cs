using Microsoft.Xna.Framework;
using MythosOfOlympus.Content.Tiles;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace MythosOfOlympus.Common.Systems
{
	// Coordinates below are relative to the top-left corner, measured in tiles.
	internal static class LabyrinthBuilder
	{
		internal const int Width = 276;
		internal const int Height = 132;
		private const int PassageHeight = 5;
		internal static readonly Rectangle Arena = new Rectangle(196, 96, 78, 32);
		internal static readonly Rectangle Gate = new Rectangle(6, 54, 2, 6);
		internal static readonly Rectangle ArenaEntrance = new Rectangle(195, 122, 1, 6);

		internal static bool TryGenerate(bool deferPopulation = false) {
			if (Main.netMode == NetmodeID.MultiplayerClient || LabyrinthSystem.Exists) return false;
			int freeChests = 0;
			foreach (Chest chest in Main.chest) if (chest == null) freeChests++;
			if (freeChests < 4) return false;
			int top = (int)Main.worldSurface + 45;
			int bottom = Main.maxTilesY - Height - 250;
			// Bounded search. Reject structures rather than overwriting them to force a placement.
			for (int attempt = 0; attempt < 6000 && bottom > top; attempt++) {
				int x = WorldGen.genRand.Next(250, Main.maxTilesX - Width - 250);
				int y = WorldGen.genRand.Next(top, bottom);
				Rectangle area = new Rectangle(x - 3, y - 3, Width + 6, Height + 6);
				if (!IsSafeArea(area)) continue;
				if (WorldGen.gen && !Terraria.WorldBuilding.GenVars.structures.CanPlace(area)) continue;
				Build(new Point(x, y), deferPopulation);
				if (WorldGen.gen) Terraria.WorldBuilding.GenVars.structures.AddProtectedStructure(area);
				return true;
			}
			return false;
		}

		private static bool IsSafeArea(Rectangle area) {
			for (int x = area.Left; x < area.Right; x++) {
				for (int y = area.Top; y < area.Bottom; y++) {
					Tile tile = Main.tile[x, y];
					// Strict natural-terrain whitelist: no chests, furniture, wires, or special biomes.
					if (tile.RedWire || tile.BlueWire || tile.GreenWire || tile.YellowWire
						|| (tile.LiquidAmount > 0 && tile.LiquidType != LiquidID.Water)) return false;
					if (tile.WallType != WallID.None && tile.WallType != WallID.DirtUnsafe
						&& tile.WallType != WallID.CaveUnsafe && tile.WallType != WallID.Cave2Unsafe
						&& tile.WallType != WallID.CaveWall && tile.WallType != WallID.CaveWall2
						&& tile.WallType != WallID.Cave3Unsafe && tile.WallType != WallID.Cave4Unsafe
						&& tile.WallType != WallID.Cave5Unsafe && tile.WallType != WallID.Cave6Unsafe
						&& tile.WallType != WallID.Cave7Unsafe && tile.WallType != WallID.Cave8Unsafe
						&& tile.WallType != WallID.DirtUnsafe1 && tile.WallType != WallID.DirtUnsafe2
						&& tile.WallType != WallID.DirtUnsafe3 && tile.WallType != WallID.DirtUnsafe4
						&& tile.WallType != WallID.AmethystUnsafe && tile.WallType != WallID.TopazUnsafe
						&& tile.WallType != WallID.SapphireUnsafe && tile.WallType != WallID.EmeraldUnsafe
						&& tile.WallType != WallID.RubyUnsafe && tile.WallType != WallID.DiamondUnsafe
						&& tile.WallType != WallID.RocksUnsafe1 && tile.WallType != WallID.RocksUnsafe2
						&& tile.WallType != WallID.RocksUnsafe3 && tile.WallType != WallID.RocksUnsafe4) return false;
					if (tile.HasTile && tile.TileType != TileID.Dirt && tile.TileType != TileID.Stone
						&& tile.TileType != TileID.ClayBlock && tile.TileType != TileID.Silt
						&& tile.TileType != TileID.Mud && tile.TileType != TileID.Sand
						&& tile.TileType != TileID.Copper && tile.TileType != TileID.Tin
						&& tile.TileType != TileID.Iron && tile.TileType != TileID.Lead
						&& tile.TileType != TileID.Silver && tile.TileType != TileID.Tungsten
						&& tile.TileType != TileID.Gold && tile.TileType != TileID.Platinum
						&& tile.TileType != TileID.Pots && tile.TileType != TileID.SmallPiles
						&& tile.TileType != TileID.LargePiles && tile.TileType != TileID.LargePiles2
						&& tile.TileType != TileID.Stalactite && tile.TileType != TileID.Cobweb
						&& tile.TileType != TileID.Amethyst && tile.TileType != TileID.Topaz
						&& tile.TileType != TileID.Sapphire && tile.TileType != TileID.Emerald
						&& tile.TileType != TileID.Ruby && tile.TileType != TileID.Diamond
						&& tile.TileType != TileID.ExposedGems && tile.TileType != TileID.LongMoss
						&& tile.TileType != TileID.ImmatureHerbs && tile.TileType != TileID.MatureHerbs
						&& tile.TileType != TileID.BloomingHerbs && tile.TileType != TileID.Seaweed
						&& tile.TileType != TileID.WaterDrip && !Main.tileMoss[tile.TileType]) return false;
				}
			}
			return true;
		}

		private static void Build(Point origin, bool deferPopulation) {
			int brick = ModContent.TileType<LabyrinthBrick>();
			for (int x = 0; x < Width; x++) {
				for (int y = 0; y < Height; y++) {
					Tile tile = Main.tile[origin.X + x, origin.Y + y];
					tile.ClearEverything();
					tile.HasTile = true;
					tile.TileType = (ushort)brick;
					tile.WallType = WallID.MarbleBlock;
				}
			}
			LabyrinthSystem.SetLocation(origin);
			// During world generation keep a solid shell so vanilla cannot add chests or traps inside.
			// The rooms are carved after the remaining vanilla generation passes finish.
			if (!deferPopulation) Populate(origin);
		}

		internal static void FinishWorldGeneration() => Populate(LabyrinthSystem.Origin);

		private static void Populate(Point origin) {
			int brick = ModContent.TileType<LabyrinthBrick>();
			// Points mark the floor of a landing. Diagonal links become one-tile stairs.
			// Main route: entry -> upper gallery -> middle stairs -> lower hall -> arena.
			Carve(origin, new Rectangle(0, 54, 21, 6));
			Passage(origin, new Point(20, 60), new Point(68, 28));
			Passage(origin, new Point(68, 28), new Point(116, 28));
			Passage(origin, new Point(116, 28), new Point(164, 60));
			Passage(origin, new Point(164, 60), new Point(116, 92));
			Passage(origin, new Point(116, 92), new Point(164, 124));
			Passage(origin, new Point(164, 124), new Point(196, 128));
			// Branches include treasure dead ends and a loop through the middle floor.
			Passage(origin, new Point(20, 28), new Point(68, 28));
			Passage(origin, new Point(116, 28), new Point(164, 28));
			Passage(origin, new Point(68, 60), new Point(164, 60));
			Passage(origin, new Point(20, 60), new Point(68, 92));
			Passage(origin, new Point(68, 92), new Point(116, 92));
			Passage(origin, new Point(68, 92), new Point(20, 124));
			Passage(origin, new Point(20, 92), new Point(68, 124));
			Passage(origin, new Point(68, 124), new Point(164, 124));
			Point[] treasureRooms = { new Point(20, 28), new Point(164, 28), new Point(68, 60), new Point(20, 124) };
			foreach (Point room in treasureRooms) Carve(origin, new Rectangle(room.X - 8, room.Y - 7, 16, 7));
			Carve(origin, Arena);
			// The side doorway matches the seal exactly, leaving no gap above it.
			for (int x = 190; x < 196; x++) {
				for (int y = 118; y < 122; y++) {
					Tile tile = Main.tile[origin.X + x, origin.Y + y];
					tile.HasTile = true;
					tile.TileType = (ushort)brick;
				}
			}
			Carve(origin, new Rectangle(190, 122, 6, 6));
			for (int x = Gate.Left; x < Gate.Right; x++) {
				for (int y = Gate.Top; y < Gate.Bottom; y++) {
					Tile tile = Main.tile[origin.X + x, origin.Y + y];
					tile.HasTile = true;
					tile.TileType = (ushort)ModContent.TileType<LabyrinthGate>();
				}
			}
			for (int x = Arena.Left + 6; x < Arena.Right - 4; x += 12)
				WorldGen.PlaceTile(origin.X + x, origin.Y + Arena.Bottom - 6, TileID.Torches, mute: true);
			// Recessed dart traps use ordinary player-triggered pressure plates and hidden wire.
			DartTrap(origin, 11, 34, 28, true);
			DartTrap(origin, 172, 145, 28, false);
			DartTrap(origin, 59, 88, 60, true);
			DartTrap(origin, 11, 26, 124, true);
			foreach (Point room in treasureRooms) PlaceTreasure(origin, room);
			PlacePots(origin);
			WorldGen.RangeFrame(origin.X - 1, origin.Y - 1, origin.X + Width + 1, origin.Y + Height + 1);
		}

		private static void Carve(Point origin, Rectangle room) {
			for (int x = room.Left; x < room.Right; x++) {
				for (int y = room.Top; y < room.Bottom; y++) {
					Tile tile = Main.tile[origin.X + x, origin.Y + y];
					tile.HasTile = false;
				}
			}
		}

		private static void Passage(Point origin, Point start, Point end) {
			if (start.X > end.X) (start, end) = (end, start);
			for (int x = start.X; x <= end.X; x++) {
				// Six flat tiles at each end make room for landings and intersections.
				float progress = MathHelper.Clamp((x - start.X - 6f) / (end.X - start.X - 12f), 0f, 1f);
				int floor = start.Y + (int)System.Math.Round((end.Y - start.Y) * progress);
				Carve(origin, new Rectangle(x, floor - PassageHeight, 1, PassageHeight));
				if (x > 10 && x % 12 == 0)
					WorldGen.PlaceTile(origin.X + x, origin.Y + floor - 4, TileID.Torches, mute: true);
			}
		}

		private static void DartTrap(Point origin, int wallX, int plateX, int floor, bool faceRight) {
			int trapY = floor - 2;
			Tile trap = Main.tile[origin.X + wallX, origin.Y + trapY];
			trap.ClearTile();
			trap.HasTile = true;
			trap.TileType = TileID.Traps;
			trap.TileFrameX = (short)(faceRight ? 18 : 0);
			trap.TileFrameY = 0;
			WorldGen.PlaceTile(origin.X + plateX, origin.Y + floor - 1, TileID.PressurePlates, mute: true, style: 2);
			// Keep each trap's circuit separate, routed below the walking surface.
			for (int x = System.Math.Min(wallX, plateX); x <= System.Math.Max(wallX, plateX); x++) {
				Tile wire = Main.tile[origin.X + x, origin.Y + floor + 1];
				wire.RedWire = true;
			}
			for (int y = trapY; y <= floor + 1; y++) {
				Tile wire = Main.tile[origin.X + wallX, origin.Y + y];
				wire.RedWire = true;
			}
			for (int y = floor - 1; y <= floor + 1; y++) {
				Tile wire = Main.tile[origin.X + plateX, origin.Y + y];
				wire.RedWire = true;
			}
		}

		private static void PlacePots(Point origin) {
			// Vanilla breakable pots provide their normal drops; no custom art or loot system yet.
			Point[] spots = {
				new Point(14, 28), new Point(24, 28), new Point(98, 28),
				new Point(158, 28), new Point(168, 28),
				new Point(62, 60), new Point(72, 60), new Point(100, 60),
				new Point(76, 92), new Point(100, 92),
				new Point(14, 124), new Point(24, 124), new Point(98, 124), new Point(148, 124)
			};
			foreach (Point spot in spots) {
				WorldGen.PlaceTile(origin.X + spot.X, origin.Y + spot.Y - 1,
					TileID.Pots, mute: true, style: WorldGen.genRand.Next(3));
			}
		}

		private static void PlaceTreasure(Point origin, Point room) {
			// PlaceChest expects the bottom-left tile; the next row is solid floor.
			int index = WorldGen.PlaceChest(origin.X + room.X - 1, origin.Y + room.Y - 1,
				TileID.Containers, notNearOtherChests: false, style: 1);
			if (index < 0) {
				ModContent.GetInstance<LabyrinthSystem>().Mod.Logger.Warn("Could not place a labyrinth treasure chest.");
				return;
			}
			Chest chest = Main.chest[index];
			int[] potions = { ItemID.IronskinPotion, ItemID.RegenerationPotion, ItemID.SwiftnessPotion, ItemID.SpelunkerPotion };
			int[] loot = { potions[WorldGen.genRand.Next(potions.Length)], ItemID.LesserHealingPotion,
				ItemID.SilverCoin, ItemID.Torch, ItemID.RecallPotion };
			int[] stacks = { 2, WorldGen.genRand.Next(4, 7), WorldGen.genRand.Next(20, 41), 20, 2 };
			for (int slot = 0; slot < loot.Length; slot++) {
				chest.item[slot].SetDefaults(loot[slot]);
				chest.item[slot].stack = stacks[slot];
			}
		}
	}
}
