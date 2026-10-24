using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MythosOfOlympus.Content.NPCs.Bosses;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace MythosOfOlympus.Content.Projectiles
{
	public class MinotaurShockwave : ModProjectile
	{
		public override string Texture => "Terraria/Images/Projectile_1";

		public override void SetDefaults() {
			Projectile.width = 32;
			Projectile.height = 28;
			Projectile.hostile = true;
			Projectile.friendly = false;
			Projectile.penetrate = -1;
			Projectile.timeLeft = 90;
			Projectile.tileCollide = true;
			Projectile.ignoreWater = true;
			CooldownSlot = ImmunityCooldownID.Bosses;
		}

		public override void OnSpawn(IEntitySource source) {
			if (!Main.dedServ && Projectile.velocity.X > 0f) {
				SoundEngine.PlaySound(SoundID.Item14, Projectile.Center);
			}
		}

		public override void AI() {
			// ai[0] stores the source boss so its waves disappear when it is gone.
			int bossIndex = (int)Projectile.ai[0];
			if (bossIndex < 0 || bossIndex >= Main.maxNPCs || !Main.npc[bossIndex].active
				|| Main.npc[bossIndex].type != ModContent.NPCType<Minotaur>()
				|| Main.npc[bossIndex].ModNPC is not Minotaur { IsFighting: true }) {
				Projectile.Kill();
				return;
			}
			Projectile.velocity.Y = MathHelper.Min(Projectile.velocity.Y + 0.4f, 10f);
			if (!Main.dedServ) {
				Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.Dirt,
					-Projectile.velocity.X * 0.3f, -2f);
			}
		}

		public override bool OnTileCollide(Vector2 oldVelocity) {
			// Stop at a wall, but continue moving across the floor.
			if (Projectile.velocity.X != oldVelocity.X) return true;
			return false;
		}

		public override bool PreDraw(ref Color lightColor) {
			// Simple visible earth spikes; the hitbox matches this 32 x 28 rectangle.
			Texture2D pixel = TextureAssets.MagicPixel.Value;
			Color color = new Color(205, 155, 85);
			for (int i = 0; i < 4; i++) {
				int height = i == 1 || i == 2 ? 28 : 18;
				Vector2 position = Projectile.position - Main.screenPosition + new Vector2(i * 8, 28 - height);
				Main.EntitySpriteDraw(pixel, position, new Rectangle(0, 0, 1, 1), color,
					0f, Vector2.Zero, new Vector2(8, height), SpriteEffects.None, 0);
			}
			return false;
		}
	}
}
