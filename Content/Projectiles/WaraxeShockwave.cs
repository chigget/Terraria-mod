using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace MythosOfOlympus.Content.Projectiles
{
	public class WaraxeShockwave : ModProjectile
	{
		public override string Texture => "Terraria/Images/Projectile_1";

		public override void SetDefaults() {
			Projectile.width = 24;
			Projectile.height = 20;
			Projectile.friendly = true;
			Projectile.hostile = false;
			Projectile.DamageType = DamageClass.Melee;
			Projectile.penetrate = 2;
			Projectile.timeLeft = 45;
			Projectile.tileCollide = true;
			Projectile.ignoreWater = true;
			// Each wave can hit two enemies, but can only damage each enemy once.
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = -1;
		}

		public override void AI() {
			// Fly straight without gravity. Default tile collision destroys the wave on any solid surface.
			Projectile.rotation = Projectile.velocity.ToRotation();
			if (!Main.dedServ)
				Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.Torch,
					-Projectile.velocity.X * 0.2f, -Projectile.velocity.Y * 0.2f);
		}

		public override bool PreDraw(ref Color lightColor) {
			// A temporary curved slash, rotated to face the direction of travel.
			for (int i = -3; i <= 3; i++) {
				Vector2 offset = new Vector2(8f - System.Math.Abs(i) * 3f, i * 2.5f);
				Vector2 position = Projectile.Center - Main.screenPosition + offset.RotatedBy(Projectile.rotation);
				Main.EntitySpriteDraw(TextureAssets.MagicPixel.Value, position, new Rectangle(0, 0, 1, 1),
					new Color(220, 180, 100), Projectile.rotation, new Vector2(0.5f), new Vector2(4f), SpriteEffects.None, 0);
			}
			return false;
		}
	}
}
