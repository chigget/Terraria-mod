using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using MythosOfOlympus.Content.NPCs.Bosses;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.UI.BigProgressBar;
using Terraria.ModLoader;

namespace MythosOfOlympus.Content.BossBars
{
	// Use Terraria's standard large bar, but only while the Minotaur is fighting.
	public class MinotaurBossBar : ModBossBar
	{
		public override Asset<Texture2D> GetIconTexture(ref Rectangle? iconFrame) {
			// Temporary vanilla icon until the Minotaur artwork is ready.
			return TextureAssets.NpcHeadBoss[0];
		}

		public override bool? ModifyInfo(ref BigProgressBarInfo info, ref float life,
			ref float lifeMax, ref float shield, ref float shieldMax) {
			if (info.npcIndexToAimAt < 0 || info.npcIndexToAimAt >= Main.maxNPCs) return false;
			NPC npc = Main.npc[info.npcIndexToAimAt];
			if (!npc.active || npc.life <= 0 || npc.ModNPC is not Minotaur { IsFighting: true }) return false;
			life = npc.life;
			lifeMax = npc.lifeMax;
			shield = shieldMax = 0f;
			return true;
		}
	}
}
