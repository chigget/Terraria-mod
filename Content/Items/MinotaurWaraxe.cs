using Microsoft.Xna.Framework;
using MythosOfOlympus.Content.Projectiles;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace MythosOfOlympus.Content.Items
{
	public class MinotaurWaraxe : ModItem
	{
		public override string Texture => $"Terraria/Images/Item_{ItemID.WarAxeoftheNight}";

		public override void SetStaticDefaults() => Item.ResearchUnlockCount = 1;

		public override void SetDefaults() {
			Item.width = 40;
			Item.height = 40;
			Item.damage = 20;
			Item.DamageType = DamageClass.Melee;
			Item.knockBack = 5f;
			Item.useTime = 30;
			Item.useAnimation = 30;
			Item.useStyle = ItemUseStyleID.Swing;
			Item.UseSound = SoundID.Item1;
			Item.autoReuse = true;
			Item.rare = ItemRarityID.Blue;
			Item.value = Item.sellPrice(silver: 30);
			Item.shoot = ModContent.ProjectileType<WaraxeShockwave>();
			Item.shootSpeed = 6f;
			// A combat weapon: leave axe power at zero so swinging does not chop trees.
		}

		public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position,
			Vector2 velocity, int type, int damage, float knockback) {
			// Shoot runs once per swing. Only the owning player rolls and creates the wave.
			if (player.whoAmI != Main.myPlayer || !Main.rand.NextBool(4)) return false;
			// The Shoot velocity already points toward the owner's cursor, including upward aim.
			if (velocity.LengthSquared() < 0.001f) velocity = new Vector2(player.direction * Item.shootSpeed, 0f);
			Vector2 direction = velocity.SafeNormalize(new Vector2(player.direction, 0f));
			Vector2 center = player.MountedCenter + direction * 20f;
			Vector2 size = new Vector2(24, 20);
			if (Collision.SolidCollision(center - size / 2f, (int)size.X, (int)size.Y)
				|| !Collision.CanHitLine(player.Center, 1, 1, center, 1, 1)) return false;
			Projectile.NewProjectile(source, center, velocity, type,
				System.Math.Max(1, (int)(damage * 0.7f)), knockback * 1.2f, player.whoAmI);
			return false;
		}
	}
}
