using Microsoft.Xna.Framework;
using Terraria.ID;

namespace MythosOfOlympus.Content.Tiles
{
	// Only the world system places/removes this temporary combat barrier.
	public class LabyrinthArenaSeal : LabyrinthBrick
	{
		public override string Texture => $"Terraria/Images/Tiles_{TileID.GoldBrick}";
		protected override Color MapColor => new Color(205, 145, 55);
	}
}
