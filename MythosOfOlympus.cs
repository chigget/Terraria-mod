using System.IO;
using MythosOfOlympus.Content.Items;
using MythosOfOlympus.Common.Systems;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace MythosOfOlympus
{
	public class MythosOfOlympus : Mod
	{
		public override void HandlePacket(BinaryReader reader, int whoAmI) {
			byte message = reader.ReadByte();
			if (Main.netMode != NetmodeID.Server || whoAmI < 0 || whoAmI >= Main.maxPlayers) return;
			Player player = Main.player[whoAmI];
			if (message == 0 && player.active && !player.dead && player.HeldItem.type == ModContent.ItemType<MinotaurTestSummon>()) {
				MinotaurTestSummon.TrySummon(player);
			}
			else if (message == 1) {
				int i = reader.ReadInt32();
				int j = reader.ReadInt32();
				LabyrinthSystem.TryUnlock(player, i, j);
			}
		}
	}
}
