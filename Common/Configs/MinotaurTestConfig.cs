using System.ComponentModel;
using Terraria.ModLoader.Config;

namespace MythosOfOlympus.Common.Configs
{
	// TEMPORARY: remove this config and the CanStartFight bypass after combat testing.
	public class MinotaurTestConfig : ModConfig
	{
		public override ConfigScope Mode => ConfigScope.ServerSide;

		[DefaultValue(false)]
		// Keep the old property name so existing test configs still load.
		// This only bypasses the test summon's lock, never the physical labyrinth gate.
		public bool SkipKingSlimeRequirement { get; set; }
	}
}
