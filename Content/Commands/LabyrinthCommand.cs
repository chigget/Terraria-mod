using MythosOfOlympus.Common.Systems;
using MythosOfOlympus.Content.Items;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace MythosOfOlympus.Content.Commands
{
	// Temporary helper for finding the entrance and adding it to an old test world.
	public class LabyrinthCommand : ModCommand
	{
		public override string Command => "labyrinth";
		public override CommandType Type => CommandType.Chat | CommandType.Console;
		public override string Usage => "/labyrinth [locate|generate|tp|key]";
		public override string Description => Language.GetTextValue("Mods.MythosOfOlympus.Labyrinth.CommandDescription");

		public override void Action(CommandCaller caller, string input, string[] args) {
			string action = args.Length == 0 ? "locate" : args[0];
			if (args.Length > 1 || (action != "locate" && action != "generate" && action != "tp" && action != "key")) {
				caller.Reply(Usage);
				return;
			}
			// Temporary shortcut: test the real key interaction without fighting King Slime.
			if (action == "key") {
				if (Main.netMode != NetmodeID.SinglePlayer) {
					caller.Reply(Language.GetTextValue("Mods.MythosOfOlympus.Labyrinth.SinglePlayerOnly"));
					return;
				}
				caller.Player.QuickSpawnItem(new EntitySource_Misc("LabyrinthTestCommand"), ModContent.ItemType<LabyrinthKey>(), 1);
				caller.Reply(Language.GetTextValue("Mods.MythosOfOlympus.Labyrinth.TestKeyGranted"));
				return;
			}
			if (action == "generate") {
				if (Main.netMode != NetmodeID.SinglePlayer) {
					caller.Reply(Language.GetTextValue("Mods.MythosOfOlympus.Labyrinth.SinglePlayerOnly"));
					return;
				}
				if (!LabyrinthSystem.Exists && !LabyrinthBuilder.TryGenerate()) {
					caller.Reply(Language.GetTextValue("Mods.MythosOfOlympus.Labyrinth.NoSafeLocation"));
					return;
				}
			}
			if (!LabyrinthSystem.Exists) {
				caller.Reply(Language.GetTextValue("Mods.MythosOfOlympus.Labyrinth.NotGenerated"));
				return;
			}
			if (action == "tp") {
				if (Main.netMode != NetmodeID.SinglePlayer) {
					caller.Reply(Language.GetTextValue("Mods.MythosOfOlympus.Labyrinth.SinglePlayerOnly"));
					return;
				}
				Player player = caller.Player;
				var destination = new Microsoft.Xna.Framework.Vector2((LabyrinthSystem.Origin.X + 3) * 16f - player.width / 2f,
					(LabyrinthSystem.Origin.Y + 60) * 16f - player.height);
				if (Collision.SolidCollision(destination, player.width, player.height)) {
					caller.Reply(Language.GetTextValue("Mods.MythosOfOlympus.Labyrinth.EntranceBlocked"));
					return;
				}
				player.Teleport(destination, TeleportationStyleID.RodOfDiscord);
				player.velocity = Microsoft.Xna.Framework.Vector2.Zero;
			}
			var entrance = LabyrinthSystem.Entrance;
			caller.Reply(Language.GetTextValue("Mods.MythosOfOlympus.Labyrinth.Location", entrance.X, entrance.Y,
				entrance.X - Main.spawnTileX, entrance.Y - Main.spawnTileY));
			string status = LabyrinthSystem.GuardianDefeated ? "Defeated" : LabyrinthSystem.Unlocked ? "Open" : "Locked";
			caller.Reply(Language.GetTextValue("Mods.MythosOfOlympus.Labyrinth." + status));
		}
	}
}
