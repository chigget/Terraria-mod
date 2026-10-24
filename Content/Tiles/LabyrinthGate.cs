using Microsoft.Xna.Framework;
using MythosOfOlympus.Common.Systems;
using MythosOfOlympus.Content.Items;
using Terraria;
using Terraria.ID;

namespace MythosOfOlympus.Content.Tiles
{
	public class LabyrinthGate : LabyrinthBrick
	{
		public override string Texture => $"Terraria/Images/Tiles_{TileID.GoldBrick}";
		protected override Color MapColor => new Color(225, 175, 55);

		public override void SetStaticDefaults() {
			base.SetStaticDefaults();
			Main.tileLighted[Type] = true;
		}

		public override bool RightClick(int i, int j) {
			if (Main.netMode == NetmodeID.MultiplayerClient) {
				var packet = Mod.GetPacket();
				packet.Write((byte)1);
				packet.Write(i);
				packet.Write(j);
				packet.Send();
			}
			else LabyrinthSystem.TryUnlock(Main.LocalPlayer, i, j);
			return true;
		}

		public override void MouseOver(int i, int j) {
			Main.LocalPlayer.noThrow = 2;
			Main.LocalPlayer.cursorItemIconEnabled = true;
			Main.LocalPlayer.cursorItemIconID = Terraria.ModLoader.ModContent.ItemType<LabyrinthKey>();
		}

		public override void ModifyLight(int i, int j, ref float r, ref float g, ref float b) {
			r = 0.35f;
			g = 0.25f;
			b = 0.05f;
		}
	}
}
