using System.IO;
using Microsoft.Xna.Framework;
using MythosOfOlympus.Common.Configs;
using MythosOfOlympus.Common.Systems;
using MythosOfOlympus.Content.BossBars;
using MythosOfOlympus.Content.Items;
using MythosOfOlympus.Content.Projectiles;
using Terraria;
using Terraria.Chat;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace MythosOfOlympus.Content.NPCs.Bosses
{
	public class Minotaur : ModNPC
	{
		private enum AttackState { Idle, Approach, Windup, Charge, Slam, Recover, JumpWindup, Leap }
		private const float AggroRadius = 8 * 16f;
		private const float ArenaRadius = 75 * 16f;
		// Only the server uses the home position to reset the fight.
		private Vector2 homePosition;
		private bool homeInitialized;
		private Vector2 jumpTarget;
		private int jumpCooldown;
		public bool IsFighting => State != AttackState.Idle;
		public bool IsLabyrinthGuardian { get; set; }
		// The temporary bypass does not change the world's actual boss defeat flags.
		public static bool CanStartFight => LabyrinthSystem.Unlocked
			|| ModContent.GetInstance<MinotaurTestConfig>().SkipKingSlimeRequirement;

		// ai fields are synchronized by tModLoader when netUpdate is set.
		private AttackState State {
			get => (AttackState)NPC.ai[0];
			set => NPC.ai[0] = (float)value;
		}
		private ref float Timer => ref NPC.ai[1];
		private ref float ChargeDirection => ref NPC.ai[2];
		// The charge is over during Slam, so this state can reuse the same ai slot.
		private ref float SlamsRemaining => ref NPC.ai[2];
		private bool Enraged {
			get => NPC.ai[3] == 1f;
			set => NPC.ai[3] = value ? 1f : 0f;
		}

		// Temporary vanilla art: replace this path when the Minotaur sprite is ready.
		public override string Texture => $"Terraria/Images/NPC_{NPCID.Zombie}";

		public override void SetStaticDefaults() {
			Main.npcFrameCount[Type] = Main.npcFrameCount[NPCID.Zombie];
			NPCID.Sets.MPAllowedEnemies[Type] = true;
			NPCID.Sets.BossBestiaryPriority.Add(Type);
		}

		public override void SetDefaults() {
			NPC.width = 48;
			NPC.height = 64;
			NPC.scale = 1.5f;
			NPC.lifeMax = 2400;
			NPC.damage = 24;
			NPC.defense = 8;
			NPC.knockBackResist = 0f;
			NPC.aiStyle = -1;
			NPC.boss = false;
			NPC.BossBar = ModContent.GetInstance<MinotaurBossBar>();
			NPC.dontTakeDamage = true;
			NPC.npcSlots = 1f;
			NPC.HitSound = SoundID.NPCHit1;
			NPC.DeathSound = SoundID.NPCDeath1;
			Music = -1;
		}

		public override void AI() {
			if (Main.netMode != NetmodeID.MultiplayerClient && !homeInitialized) {
				homePosition = NPC.position;
				homeInitialized = true;
			}
			NPC.boss = IsFighting;
			NPC.dontTakeDamage = !IsFighting;
			NPC.npcSlots = IsFighting ? 10f : 1f;
			Music = IsFighting ? MusicID.Boss1 : -1;
			NPC.timeLeft = 750;

			if (!IsFighting) {
				NPC.velocity.X = 0f;
				NPC.color = Color.White;
				Timer++;
				bool canEngage = IsLabyrinthGuardian ? LabyrinthSystem.Unlocked : CanStartFight;
				if (Main.netMode != NetmodeID.MultiplayerClient && canEngage && Timer >= 0f) {
					int target = FindPlayer(AggroRadius, true);
					if (target >= 0) {
						NPC.target = target;
						ChangeState(AttackState.Approach);
						// Only this idle-to-combat transition announces awakening, once per fight.
						const string key = "Mods.MythosOfOlympus.NPCs.Minotaur.Awoken";
						if (Main.netMode == NetmodeID.Server)
							ChatHelper.BroadcastChatMessage(NetworkText.FromKey(key), new Color(175, 75, 255));
						else Main.NewText(Language.GetTextValue(key), new Color(175, 75, 255));
					}
				}
				return;
			}

			if (Main.netMode != NetmodeID.MultiplayerClient) {
				int target = FindPlayer(ArenaRadius, false);
				if (target < 0 || Vector2.Distance(NPC.position, homePosition) > ArenaRadius
					|| (IsLabyrinthGuardian && !LabyrinthSystem.InArena(NPC.Center))) {
					ResetFight();
					return;
				}
				if (NPC.target != target) {
					NPC.target = target;
					NPC.netUpdate = true;
				}
			}
			if (!NPC.HasValidTarget) return;
			Player player = Main.player[NPC.target];

			if (!Enraged && NPC.life < NPC.lifeMax * 0.4f && Main.netMode != NetmodeID.MultiplayerClient) {
				Enraged = true;
				NPC.netUpdate = true;
			}
			NPC.color = Enraged ? new Color(255, 130, 100) : Color.White;
			Timer++;
			if (jumpCooldown > 0) jumpCooldown--;

			switch (State) {
				case AttackState.Approach:
					if (Main.netMode != NetmodeID.MultiplayerClient && jumpCooldown == 0 && IsGrounded()
						&& player.Center.Y < NPC.Center.Y - 64f && System.Math.Abs(player.Center.X - NPC.Center.X) < 560f) {
						jumpTarget = player.Center;
						NPC.direction = jumpTarget.X >= NPC.Center.X ? 1 : -1;
						ChangeState(AttackState.JumpWindup);
						break;
					}
					MoveTowards(player);
					if (Timer >= (Enraged ? 60 : 90) && IsGrounded()) ChangeState(AttackState.Windup);
					break;

				case AttackState.Windup:
					NPC.velocity.X *= 0.8f;
					// Lock left/right at the start of the telegraph, not during the dash.
					if (ChargeDirection == 0f && Main.netMode != NetmodeID.MultiplayerClient) {
						ChargeDirection = player.Center.X >= NPC.Center.X ? 1f : -1f;
						NPC.netUpdate = true;
					}
					if (ChargeDirection != 0f) NPC.direction = (int)ChargeDirection;
					if (!Main.dedServ && Timer % 6 == 0) {
						Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.Torch);
					}
					if (Timer >= (Enraged ? 40 : 65) && IsGrounded()) ChangeState(AttackState.Charge);
					break;

				case AttackState.Charge:
					NPC.velocity.X = ChargeDirection * (Enraged ? 13f : 9f);
					if (Timer >= (Enraged ? 28 : 36) || NPC.collideX) ChangeState(AttackState.Slam);
					break;

				case AttackState.Slam:
					NPC.velocity.X *= 0.7f;
					// Wait until grounded so the shockwaves start at the floor.
					if (Timer >= (Enraged ? 25 : 40) && IsGrounded()
						&& Main.netMode != NetmodeID.MultiplayerClient) {
						SlamGround();
						SlamsRemaining--;
						Timer = 0f;
						NPC.netUpdate = true;
						if (SlamsRemaining <= 0f) ChangeState(AttackState.Recover);
					}
					break;

				case AttackState.JumpWindup:
					NPC.velocity.X *= 0.7f;
					if (!Main.dedServ && Timer % 6 == 0)
						Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.Torch, 0f, -3f);
					if (Timer >= (Enraged ? 24 : 36) && IsGrounded() && Main.netMode != NetmodeID.MultiplayerClient) {
						// Aim at the position chosen before the windup; no midair homing.
						float gravity = System.Math.Max(NPC.gravity, 0.1f);
						float height = MathHelper.Clamp(NPC.Center.Y - jumpTarget.Y + 64f, 120f, 340f);
						float speedY = -(float)System.Math.Sqrt(2f * gravity * height);
						float speedX = (jumpTarget.X - NPC.Center.X) / (-speedY / gravity);
						float maxSpeed = Enraged ? 13f : 10f;
						ChangeState(AttackState.Leap);
						NPC.velocity = new Vector2(MathHelper.Clamp(speedX, -maxSpeed, maxSpeed), speedY);
						jumpCooldown = Enraged ? 120 : 180;
					}
					break;

				case AttackState.Leap:
					if (Timer > 6 && IsGrounded()) ChangeState(AttackState.Slam);
					else if (Timer >= 180) ChangeState(AttackState.Recover);
					break;

				case AttackState.Recover:
					MoveTowards(player);
					if (Timer >= (Enraged ? 30 : 55)) ChangeState(AttackState.Approach);
					break;
			}
			NPC.spriteDirection = NPC.direction;
		}

		// Keep the placed guardian available until the world is unloaded.
		public override bool CheckActive() => false;

		public override bool CanHitPlayer(Player target, ref int cooldownSlot) => IsFighting;

		public override bool? CanFallThroughPlatforms() => IsFighting && (State == AttackState.Leap
			|| (NPC.HasValidTarget && Main.player[NPC.target].Top.Y > NPC.Bottom.Y + 16f));

		public override void SendExtraAI(BinaryWriter writer) => writer.Write(IsLabyrinthGuardian);
		public override void ReceiveExtraAI(BinaryReader reader) => IsLabyrinthGuardian = reader.ReadBoolean();

		public override void ModifyNPCLoot(NPCLoot npcLoot) {
			// One shared weapon in every difficulty, including test-summoned bosses.
			npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<MinotaurWaraxe>()));
		}

		public override void OnKill() {
			if (IsLabyrinthGuardian) LabyrinthSystem.MarkGuardianDefeated();
		}

		private int FindPlayer(float radius, bool requireLineOfSight) {
			int target = -1;
			float closestDistance = radius;
			Vector2 homeCenter = homePosition + NPC.Size / 2f;
			for (int i = 0; i < Main.maxPlayers; i++) {
				Player candidate = Main.player[i];
				if (!candidate.active || candidate.dead || candidate.ghost
					|| Vector2.Distance(candidate.Center, homeCenter) > ArenaRadius) continue;
				if (IsLabyrinthGuardian && !LabyrinthSystem.InArena(candidate.Center)) continue;
				float distance = Vector2.Distance(NPC.Center, candidate.Center);
				if (distance > closestDistance) continue;
				if (requireLineOfSight && !Collision.CanHitLine(NPC.position, NPC.width, NPC.height,
					candidate.position, candidate.width, candidate.height)) continue;
				closestDistance = distance;
				target = i;
			}
			return target;
		}

		private void ResetFight() {
			// Called only on the server or in single player; position/life/ai are synced.
			NPC.position = homePosition;
			NPC.velocity = Vector2.Zero;
			NPC.life = NPC.lifeMax;
			NPC.color = Color.White;
			NPC.dontTakeDamage = true;
			NPC.boss = false;
			NPC.target = 255;
			Enraged = false;
			jumpCooldown = 0;
			jumpTarget = Vector2.Zero;
			ChargeDirection = 0f;
			System.Array.Clear(NPC.buffType, 0, NPC.buffType.Length);
			System.Array.Clear(NPC.buffTime, 0, NPC.buffTime.Length);
			ChangeState(AttackState.Idle);
			Timer = -180f;
			for (int i = 0; i < Main.maxProjectiles; i++) {
				Projectile wave = Main.projectile[i];
				if (wave.active && wave.type == ModContent.ProjectileType<MinotaurShockwave>()
					&& (int)wave.ai[0] == NPC.whoAmI) wave.Kill();
			}
		}

		private bool IsGrounded() {
			// Probe the floor instead of relying on vertical speed being exactly zero.
			return NPC.velocity.Y >= 0f && Collision.TileCollision(NPC.position,
				new Vector2(0f, 2f), NPC.width, NPC.height, fallThrough: CanFallThroughPlatforms() == true, fall2: true).Y < 2f;
		}

		private void MoveTowards(Player player) {
			float distanceX = player.Center.X - NPC.Center.X;
			if (System.Math.Abs(distanceX) < 48f) {
				NPC.velocity.X *= 0.8f;
				return;
			}
			NPC.direction = distanceX > 0f ? 1 : -1;
			float speed = Enraged ? 4f : 2.5f;
			NPC.velocity.X = MathHelper.Clamp(NPC.velocity.X + NPC.direction * 0.2f, -speed, speed);
			if (NPC.collideX && IsGrounded()) NPC.velocity.Y = -6f;
		}

		private void ChangeState(AttackState nextState) {
			if (Main.netMode == NetmodeID.MultiplayerClient) return;
			State = nextState;
			NPC.boss = IsFighting;
			if (IsLabyrinthGuardian) LabyrinthSystem.SetArenaSealed(nextState != AttackState.Idle);
			Timer = 0f;
			if (nextState == AttackState.Windup) ChargeDirection = 0f;
			// Next's upper bound is exclusive: 2/3/4 normally, 3/4/5 when enraged.
			if (nextState == AttackState.Slam) SlamsRemaining = Main.rand.Next(Enraged ? 3 : 2, Enraged ? 6 : 5);
			NPC.netUpdate = true;
		}

		private void SlamGround() {
			// Only the server (or single player) creates damaging projectiles.
			if (Main.netMode == NetmodeID.MultiplayerClient) return;
			for (int direction = -1; direction <= 1; direction += 2) {
				Vector2 position = NPC.Bottom + new Vector2(direction * 20f, -14f);
				Projectile.NewProjectile(NPC.GetSource_FromAI(), position,
					new Vector2(direction * (Enraged ? 8f : 6f), 0f),
					ModContent.ProjectileType<MinotaurShockwave>(), 14, 2f, Main.myPlayer, NPC.whoAmI);
			}
		}

		public override void FindFrame(int frameHeight) {
			NPC.frameCounter += System.Math.Abs(NPC.velocity.X) * 0.15f + 0.1f;
			if (NPC.frameCounter >= 1) {
				NPC.frameCounter = 0;
				NPC.frame.Y = (NPC.frame.Y + frameHeight) % (frameHeight * Main.npcFrameCount[Type]);
			}
		}
	}
}
