using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace MythosOfOlympus.Content.Items
{
	public class LabyrinthKey : ModItem
	{
		// Temporary vanilla art until the labyrinth key has its own sprite.
		public override string Texture => $"Terraria/Images/Item_{ItemID.GoldenKey}";

		public override void SetStaticDefaults() {
			Item.ResearchUnlockCount = 1;
		}

		public override void SetDefaults() {
			Item.width = 14;
			Item.height = 20;
			Item.maxStack = Item.CommonMaxStack;
			Item.rare = ItemRarityID.Blue;
			// Right-click the gate with this in the inventory. The server consumes one.
			// The item itself has no left-click action.
			Item.consumable = false;
		}
	}
}
