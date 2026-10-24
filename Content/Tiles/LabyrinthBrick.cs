using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace MythosOfOlympus.Content.Tiles
{
	public class LabyrinthBrick : ModTile
	{
		public override string Texture => $"Terraria/Images/Tiles_{TileID.MarbleBlock}";
		protected virtual Color MapColor => new Color(190, 185, 175);

		public override void SetStaticDefaults() {
			Main.tileSolid[Type] = true;
			Main.tileBlockLight[Type] = true;
			TileID.Sets.CanBeClearedDuringGeneration[Type] = false;
			TileID.Sets.CanBeClearedDuringOreRunner[Type] = false;
			DustType = DustID.Stone;
			AddMapEntry(MapColor, CreateMapEntryName());
		}

		public override bool CanKillTile(int i, int j, ref bool blockDamaged) => false;
		public override bool CanExplode(int i, int j) => false;
		public override bool CanReplace(int i, int j, int tileTypeBeingPlaced) => false;
		public override bool Slope(int i, int j) => false;
	}
}
