using MythosOfOlympus.Content.Items;
using Terraria;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;

namespace MythosOfOlympus.Common.GlobalNPCs
{
	public class KingSlimeLoot : GlobalNPC
	{
		public override void ModifyNPCLoot(NPC npc, NPCLoot npcLoot) {
			if (npc.type == NPCID.KingSlime) {
				// One shared key per kill in every difficulty, including repeat kills.
				// No difficulty condition: this drops directly, not inside a treasure bag.
				npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<LabyrinthKey>(), 1, 1, 1));
			}
		}
	}
}
