using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using UnityEngine;
using UnityEngine.UIElements;

using Codice.Client.BaseCommands.BranchExplorer;
using Codice.Client.BaseCommands.BranchExplorer.Layout;

namespace Unity.PlasticSCM.Editor.Views.BranchExplorer.Game
{
    [ExcludeFromCodeCoverage]
    internal class BranchRunnerGame : VisualElement
    {
        internal event Action OnExit;

        internal BranchRunnerGame()
        {
            style.position = Position.Absolute;
            style.left = style.right = style.top = style.bottom = 0;
            focusable = true;

            generateVisualContent += OnPaint;

            BuildHUD();

            RegisterCallback<KeyDownEvent>(OnKeyDown);
            RegisterCallback<KeyUpEvent>(OnKeyUp);
        }

        internal void StartWithLayout(
            BrExLayout layout, Vector2 scrollOffset, float zoomLevel)
        {
            mRng = new System.Random(42);
            mInitialScrollOffset = scrollOffset;
            mInitialZoomLevel = zoomLevel > 0 ? zoomLevel : 1f;
            BuildLevel(layout);
            ResetPlayer();
            mState = GameState.Playing;
            UpdateHUD();

            mLastTick = DateTime.UtcNow.Ticks;
            mGameLoop = schedule.Execute(GameTick).Every(16);

            Focus();
        }

        internal void Stop()
        {
            mGameLoop?.Pause();
            mGameLoop = null;

            generateVisualContent -= OnPaint;

            UnregisterCallback<KeyDownEvent>(OnKeyDown);
            UnregisterCallback<KeyUpEvent>(OnKeyUp);
        }

        #region Level Building

        void BuildLevel(BrExLayout layout)
        {
            mPlatforms.Clear();
            mChangesets.Clear();
            mLabels.Clear();
            mEnemies.Clear();
            mFloatingTexts.Clear();
            mParticles.Clear();

            if (layout == null)
                return;

            foreach (BranchDrawInfo branch in layout.BranchDraws)
            {
                BrExRectangle b = branch.Bounds;
                string fullName = "";
                BrExBranch brExBranch = branch.Tag as BrExBranch;
                if (brExBranch != null)
                    fullName = brExBranch.Name ?? "";

                string shortName = fullName;
                int lastSlash = fullName.LastIndexOf('/');
                if (lastSlash >= 0 && lastSlash < fullName.Length - 1)
                    shortName = fullName.Substring(lastSlash + 1);

                bool isMain = fullName == "/main"
                    || fullName.EndsWith("/main")
                    || shortName == "main";

                Platform plat = new Platform
                {
                    X = b.X,
                    Y = b.Y,
                    Width = b.Width,
                    Height = Mathf.Max(b.Height, PLATFORM_MIN_HEIGHT),
                    Name = shortName,
                    IsMainBranch = isMain
                };
                mPlatforms.Add(plat);

                SpawnEnemiesOnPlatform(plat);
            }

            foreach (ChangesetDrawInfo cs in layout.ChangesetDraws)
            {
                BrExRectangle cb = cs.Bounds;
                int additions = mRng.Next(1, 120);
                int deletions = mRng.Next(0, 60);

                mChangesets.Add(new GameChangeset
                {
                    X = cb.X + cb.Width / 2f,
                    Y = cb.Y + cb.Height / 2f,
                    Radius = BrExDrawProperties.ChangesetRadius,
                    Activated = false,
                    Additions = additions,
                    Deletions = deletions,
                    IsHead = cs.IsHead
                });
            }

            if (layout.LabelDraws != null)
            {
                foreach (LabelDrawInfo lbl in layout.LabelDraws)
                {
                    BrExRectangle lb = lbl.Bounds;
                    string labelName = lbl.ScreenCaption;
                    if (string.IsNullOrEmpty(labelName))
                        labelName = lbl.Caption;
                    if (string.IsNullOrEmpty(labelName) && lbl.Labels != null && lbl.Labels.Length > 0)
                        labelName = lbl.Labels[0].Name;
                    if (string.IsNullOrEmpty(labelName))
                        labelName = "label";

                    mLabels.Add(new GameLabel
                    {
                        X = lb.X + lb.Width / 2f,
                        Y = lb.Y + lb.Height / 2f,
                        Radius = BrExDrawProperties.LabelRadius,
                        Name = labelName,
                        Collected = false
                    });
                }
            }

            float viewCenterX = mInitialScrollOffset.x / mInitialZoomLevel;
            float viewCenterY = mInitialScrollOffset.y / mInitialZoomLevel;

            Platform startPlatform = mPlatforms.Count > 0 ? mPlatforms[0] : default;
            float bestDist = float.MaxValue;

            foreach (Platform p in mPlatforms)
            {
                float pcx = p.X + p.Width / 2f;
                float pcy = p.Y;
                float dx = pcx - viewCenterX;
                float dy = pcy - viewCenterY;
                float dist = dx * dx + dy * dy;
                if (dist < bestDist)
                {
                    bestDist = dist;
                    startPlatform = p;
                }
            }

            float clampedX = Mathf.Clamp(
                viewCenterX,
                startPlatform.X + PLAYER_WIDTH,
                startPlatform.X + startPlatform.Width - PLAYER_WIDTH);
            mSpawnX = clampedX;
            mSpawnY = startPlatform.Y - PLAYER_HEIGHT - 2;
        }

        void SpawnEnemiesOnPlatform(Platform plat)
        {
            if (plat.Width < 180)
                return;

            int count = plat.Width > 500 ? 2 : 1;
            float margin = 40f;

            for (int i = 0; i < count; i++)
            {
                float offset = (i + 1f) / (count + 1f);
                float ex = plat.X + plat.Width * offset;
                int type = mRng.Next(0, 3) == 0 ? 1 : 0;

                mEnemies.Add(new Enemy
                {
                    X = ex,
                    Y = plat.Y - ENEMY_SIZE,
                    VelX = (type == 1 ? ENEMY_FAST_SPEED : ENEMY_SPEED)
                        * (mRng.Next(0, 2) == 0 ? 1 : -1),
                    PatrolLeft = plat.X + margin,
                    PatrolRight = plat.X + plat.Width - margin,
                    Alive = true,
                    DeathTimer = -1f,
                    Type = type
                });
            }
        }

        void SpawnEnemyAtChangeset(float csX, float csY)
        {
            Platform? host = null;
            float bestDist = float.MaxValue;

            foreach (Platform p in mPlatforms)
            {
                if (csX < p.X - 20 || csX > p.X + p.Width + 20)
                    continue;

                float dy = Mathf.Abs(csY - p.Y);
                if (dy < bestDist) { bestDist = dy; host = p; }
            }

            float patrolLeft, patrolRight, ey;
            if (host.HasValue)
            {
                Platform h = host.Value;
                patrolLeft = h.X + 20;
                patrolRight = h.X + h.Width - 20;
                ey = h.Y - ENEMY_SIZE;
            }
            else
            {
                patrolLeft = csX - 120;
                patrolRight = csX + 120;
                ey = csY - ENEMY_SIZE;
            }

            int type = mRng.Next(0, 2);
            float awayDir = mPlayerX < csX ? 1f : -1f;
            float spawnX = Mathf.Clamp(
                csX + awayDir * ENEMY_SPAWN_OFFSET,
                patrolLeft, patrolRight);

            mEnemies.Add(new Enemy
            {
                X = spawnX,
                Y = ey,
                VelX = (type == 1 ? ENEMY_FAST_SPEED : ENEMY_SPEED) * -awayDir,
                PatrolLeft = patrolLeft,
                PatrolRight = patrolRight,
                Alive = true,
                DeathTimer = -1f,
                Type = type,
                GraceTimer = ENEMY_GRACE_TIME
            });

            SpawnParticleBurst(spawnX, ey, 6, new Color(0.9f, 0.3f, 0.8f));
        }

        #endregion

        #region Game Loop

        void ResetPlayer()
        {
            mPlayerX = mSpawnX;
            mPlayerY = mSpawnY;
            mCheckpointX = mSpawnX;
            mCheckpointY = mSpawnY;
            mPlayerVelX = 0;
            mPlayerVelY = 0;
            mIsGrounded = false;
            mFacingRight = true;
            mScore = 0;
            mCombo = 0;
            mComboTimer = 0;
            mAnimFrame = 0;
            mAnimTimer = 0;
            mDeathTimer = 0;
            mLives = 3;
            mInvincibleTimer = 0;
            mCanDoubleJump = false;
            mRunning = false;
            mCameraX = mSpawnX;
            mCameraY = mSpawnY - 100;
            mShakeTimer = 0;

            for (int i = 0; i < mChangesets.Count; i++)
            {
                GameChangeset c = mChangesets[i];
                c.Activated = false;
                mChangesets[i] = c;
            }
            for (int i = 0; i < mLabels.Count; i++)
            {
                GameLabel l = mLabels[i];
                l.Collected = false;
                mLabels[i] = l;
            }
            for (int i = 0; i < mEnemies.Count; i++)
            {
                Enemy e = mEnemies[i];
                e.Alive = true;
                e.DeathTimer = -1;
                mEnemies[i] = e;
            }
            mFloatingTexts.Clear();
            mParticles.Clear();

            mGameOverPanel.style.display = DisplayStyle.None;
        }

        void GameTick()
        {
            long now = DateTime.UtcNow.Ticks;
            float dt = (now - mLastTick) / (float)TimeSpan.TicksPerSecond;
            mLastTick = now;
            dt = Mathf.Clamp(dt, 0, 0.05f);

            if (mState == GameState.Playing)
            {
                UpdatePhysics(dt);
                UpdateEnemies(dt);
                CheckPlatformCollisions();
                CheckEnemyCollisions();
                CheckChangesetCollisions();
                CheckLabelCollisions();
                CheckDeath();
                UpdateCamera(dt);
                mAnimTimer += dt;
                if (mComboTimer > 0) mComboTimer -= dt;
                if (mComboTimer <= 0) mCombo = 0;
                if (mInvincibleTimer > 0) mInvincibleTimer -= dt;
                if (mDropTimer > 0) mDropTimer -= dt;
                TickChangesetCooldowns(dt);
            }
            else if (mState == GameState.Dying)
            {
                mDeathTimer += dt;
                mPlayerVelY += GRAVITY * dt;
                mPlayerY += mPlayerVelY * dt;
                if (mDeathTimer > 1.2f)
                {
                    if (mLives > 0)
                    {
                        Respawn();
                    }
                    else
                    {
                        mState = GameState.GameOver;
                        mGameOverPanel.style.display = DisplayStyle.Flex;
                        mFinalScoreLabel.text = string.Format("Score: {0}", mScore);
                    }
                }
            }

            UpdateEffects(dt);
            MarkDirtyRepaint();
            UpdateHUD();
        }

        void Respawn()
        {
            mPlayerX = mCheckpointX;
            mPlayerY = mCheckpointY;
            mPlayerVelX = 0;
            mPlayerVelY = 0;
            mIsGrounded = false;
            mCanDoubleJump = false;
            mInvincibleTimer = INVINCIBLE_DURATION;
            mCameraX = mPlayerX;
            mCameraY = mPlayerY;
            mState = GameState.Playing;
        }

        #endregion

        #region Physics

        void UpdatePhysics(float dt)
        {
            float speed = MOVE_SPEED;
            if (mRunning)
                speed *= RUN_SPEED_MULTIPLIER;

            mPlayerVelX = 0;
            if (mMoveLeft) mPlayerVelX -= speed;
            if (mMoveRight) mPlayerVelX += speed;

            if (mPlayerVelX > 0) mFacingRight = true;
            else if (mPlayerVelX < 0) mFacingRight = false;

            if (mJumpPressed && mIsGrounded)
            {
                float jumpVel = mRunning ? SUPER_JUMP_VELOCITY : JUMP_VELOCITY;

                mPlayerVelY = jumpVel;
                mIsGrounded = false;
                mJumpPressed = false;
                mCanDoubleJump = true;

                if (mRunning)
                {
                    SpawnParticleBurst(
                        mPlayerX + PLAYER_WIDTH / 2f,
                        mPlayerY + PLAYER_HEIGHT,
                        5, new Color(1f, 0.7f, 0.2f, 0.8f));
                }
            }
            else if (mJumpPressed && !mIsGrounded && mCanDoubleJump)
            {
                mPlayerVelY = DOUBLE_JUMP_VELOCITY;
                mCanDoubleJump = false;
                mJumpPressed = false;

                SpawnParticleBurst(
                    mPlayerX + PLAYER_WIDTH / 2f,
                    mPlayerY + PLAYER_HEIGHT,
                    4, new Color(0.5f, 0.8f, 1f, 0.8f));
            }

            if (!mIsGrounded)
                mPlayerVelY += GRAVITY * dt;

            mPlayerX += mPlayerVelX * dt;
            mPlayerY += mPlayerVelY * dt;

            float animSpeed = mRunning ? 16f : 10f;
            if (Mathf.Abs(mPlayerVelX) > 0.1f && mIsGrounded)
                mAnimFrame = (int)(mAnimTimer * animSpeed) % 4;
            else if (!mIsGrounded)
                mAnimFrame = 1;
            else
                mAnimFrame = 0;
        }

        void UpdateEnemies(float dt)
        {
            for (int i = 0; i < mEnemies.Count; i++)
            {
                Enemy e = mEnemies[i];

                if (e.DeathTimer >= 0)
                {
                    e.DeathTimer += dt;
                    mEnemies[i] = e;
                    continue;
                }

                if (!e.Alive) continue;

                if (e.GraceTimer > 0)
                    e.GraceTimer -= dt;

                e.X += e.VelX * dt;
                if (e.X <= e.PatrolLeft || e.X >= e.PatrolRight)
                    e.VelX = -e.VelX;
                e.X = Mathf.Clamp(e.X, e.PatrolLeft, e.PatrolRight);

                mEnemies[i] = e;
            }
        }

        void CheckPlatformCollisions()
        {
            if (mDropDown && mIsGrounded && mDropTimer <= 0)
            {
                mDropDown = false;
                mDropTimer = DROP_THROUGH_TIME;
                mIsGrounded = false;
                mPlayerY += 4;
                mPlayerVelY = 60;
                return;
            }

            mIsGrounded = false;

            if (mDropTimer > 0)
                return;

            float playerBottom = mPlayerY + PLAYER_HEIGHT;
            float playerLeft = mPlayerX - PLAYER_WIDTH / 2f;
            float playerRight = mPlayerX + PLAYER_WIDTH / 2f;

            foreach (Platform p in mPlatforms)
            {
                if (playerRight < p.X || playerLeft > p.X + p.Width)
                    continue;

                float platformTop = p.Y;
                if (mPlayerVelY >= 0 &&
                    playerBottom >= platformTop &&
                    playerBottom <= platformTop + p.Height + 8)
                {
                    mPlayerY = platformTop - PLAYER_HEIGHT;
                    mPlayerVelY = 0;
                    mIsGrounded = true;
                    mCheckpointX = mPlayerX;
                    mCheckpointY = mPlayerY;
                    break;
                }
            }
        }

        void CheckEnemyCollisions()
        {
            if (mInvincibleTimer > 0) return;

            float playerBottom = mPlayerY + PLAYER_HEIGHT;
            float playerCX = mPlayerX;
            float playerCY = mPlayerY + PLAYER_HEIGHT / 2f;

            for (int i = 0; i < mEnemies.Count; i++)
            {
                Enemy e = mEnemies[i];
                if (!e.Alive || e.DeathTimer >= 0 || e.GraceTimer > 0) continue;

                float dx = playerCX - e.X;
                float dy = playerCY - e.Y;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);

                if (dist > ENEMY_SIZE + PLAYER_WIDTH / 2f)
                    continue;

                bool stompedFromAbove = mPlayerVelY > 0 &&
                    playerBottom < e.Y + ENEMY_SIZE * 0.7f;

                if (stompedFromAbove)
                {
                    e.Alive = false;
                    e.DeathTimer = 0;
                    mEnemies[i] = e;

                    mPlayerVelY = STOMP_BOUNCE;
                    mIsGrounded = false;

                    mCombo++;
                    mComboTimer = COMBO_WINDOW;
                    int points = 50 * mCombo;
                    mScore += points;

                    string msg = STOMP_MESSAGES[mRng.Next(STOMP_MESSAGES.Length)];
                    if (mCombo > 1)
                        msg = string.Format("x{0} COMBO! {1}", mCombo, msg);
                    AddFloatingText(e.X, e.Y - 10, msg,
                        new Color(1f, 0.6f, 0.1f), 1.5f);
                    AddFloatingText(e.X, e.Y + 8,
                        string.Format("+{0}", points), Color.white, 1.2f);

                    SpawnParticleBurst(e.X, e.Y, 8,
                        e.Type == 1
                            ? new Color(0.9f, 0.2f, 0.15f)
                            : new Color(0.6f, 0.4f, 0.2f));
                    TriggerShake(4f, 0.15f);
                }
                else
                {
                    KillPlayer();
                }
            }
        }

        void TickChangesetCooldowns(float dt)
        {
            for (int i = 0; i < mChangesets.Count; i++)
            {
                GameChangeset c = mChangesets[i];
                if (c.Cooldown <= 0) continue;
                c.Cooldown -= dt;
                mChangesets[i] = c;
            }
        }

        void CheckChangesetCollisions()
        {
            float px = mPlayerX;
            float py = mPlayerY + PLAYER_HEIGHT / 2f;

            for (int i = 0; i < mChangesets.Count; i++)
            {
                GameChangeset c = mChangesets[i];
                if (c.Activated || c.Cooldown > 0) continue;

                float dx = px - c.X;
                float dy = py - c.Y;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);

                if (dist > c.Radius + PLAYER_WIDTH * 0.4f)
                    continue;

                bool foundBug = mRng.Next(100) < BUG_CHANCE_PERCENT;

                if (foundBug)
                {
                    c.Cooldown = CHANGESET_COOLDOWN;
                    mChangesets[i] = c;

                    string warning = BUG_FOUND_MESSAGES[mRng.Next(BUG_FOUND_MESSAGES.Length)];
                    AddFloatingText(c.X, c.Y - 30, warning,
                        new Color(1f, 0.3f, 0.15f), 2.2f);

                    SpawnEnemyAtChangeset(c.X, c.Y);
                    SpawnParticleBurst(c.X, c.Y, 6, new Color(1f, 0.2f, 0.1f));
                    TriggerShake(6f, 0.2f);
                }
                else
                {
                    c.Activated = true;
                    mChangesets[i] = c;

                    int points = (c.Additions + c.Deletions) * 5;
                    mScore += points;

                    AddFloatingText(c.X, c.Y - 36, "REVIEWED OK!",
                        new Color(0.2f, 0.95f, 0.4f), 2f);
                    AddFloatingText(c.X - 26, c.Y - 14,
                        string.Format("+{0}", c.Additions),
                        new Color(0.2f, 0.9f, 0.3f), 1.4f);
                    AddFloatingText(c.X + 26, c.Y - 14,
                        string.Format("-{0}", c.Deletions),
                        new Color(0.95f, 0.25f, 0.2f), 1.4f);
                    AddFloatingText(c.X, c.Y + 10,
                        string.Format("+{0}", points), Color.white, 1.0f);

                    SpawnDiffParticles(c.X, c.Y, c.Additions, c.Deletions);
                    TriggerShake(3f, 0.1f);
                }
            }
        }

        void CheckLabelCollisions()
        {
            float px = mPlayerX;
            float py = mPlayerY + PLAYER_HEIGHT / 2f;

            for (int i = 0; i < mLabels.Count; i++)
            {
                GameLabel l = mLabels[i];
                if (l.Collected) continue;

                float dx = px - l.X;
                float dy = py - l.Y;
                if (dx * dx + dy * dy > (l.Radius + 10) * (l.Radius + 10))
                    continue;

                l.Collected = true;
                mLabels[i] = l;

                mScore += LABEL_POINTS;
                AddFloatingText(l.X, l.Y - 25,
                    string.Format("TAGGED! +{0}", LABEL_POINTS),
                    new Color(0.2f, 0.95f, 0.85f), 2f);
                AddFloatingText(l.X, l.Y - 8, l.Name,
                    new Color(1f, 1f, 1f, 0.8f), 1.6f);

                SpawnParticleBurst(l.X, l.Y, 12, new Color(0.2f, 0.95f, 0.85f));
                TriggerShake(2f, 0.08f);
            }
        }

        void CheckDeath()
        {
            float vh = resolvedStyle.height;
            if (float.IsNaN(vh)) vh = 600;

            if (mPlayerY > mCameraY + vh)
                KillPlayer();
        }

        void KillPlayer()
        {
            if (mState != GameState.Playing) return;

            mLives--;
            mState = GameState.Dying;
            mPlayerVelY = JUMP_VELOCITY * 0.6f;
            mDeathTimer = 0;
            TriggerShake(8f, 0.3f);
            SpawnParticleBurst(mPlayerX, mPlayerY + PLAYER_HEIGHT / 2f, 10,
                new Color(0.9f, 0.15f, 0.15f));
        }

        #endregion

        #region Effects

        void AddFloatingText(float x, float y, string text, Color color, float lifetime)
        {
            if (mFloatingTexts.Count > 20)
                mFloatingTexts.RemoveAt(0);

            mFloatingTexts.Add(new FloatingText
            {
                X = x, Y = y, Text = text,
                TextColor = color, Timer = 0, Lifetime = lifetime
            });
        }

        void SpawnParticleBurst(float x, float y, int count, Color color)
        {
            for (int i = 0; i < count; i++)
            {
                float angle = (i / (float)count) * Mathf.PI * 2f
                    + (float)mRng.NextDouble() * 0.5f;
                float speed = 80 + (float)mRng.NextDouble() * 120;
                mParticles.Add(new GameParticle
                {
                    X = x, Y = y,
                    VelX = Mathf.Cos(angle) * speed,
                    VelY = Mathf.Sin(angle) * speed - 60,
                    ParticleColor = color,
                    Timer = 0,
                    Lifetime = 0.6f + (float)mRng.NextDouble() * 0.4f,
                    Size = 2f + (float)mRng.NextDouble() * 3f
                });
            }
        }

        void SpawnDiffParticles(float x, float y, int additions, int deletions)
        {
            int greenCount = Mathf.Min(additions / 8 + 2, 8);
            int redCount = Mathf.Min(deletions / 8 + 1, 6);

            for (int i = 0; i < greenCount; i++)
            {
                float angle = -Mathf.PI / 2f + (float)(mRng.NextDouble() - 0.5) * Mathf.PI;
                float speed = 60 + (float)mRng.NextDouble() * 100;
                mParticles.Add(new GameParticle
                {
                    X = x - 10 + (float)mRng.NextDouble() * 20,
                    Y = y,
                    VelX = Mathf.Cos(angle) * speed - 30,
                    VelY = Mathf.Sin(angle) * speed,
                    ParticleColor = new Color(0.15f, 0.85f, 0.25f),
                    Timer = 0, Lifetime = 0.8f + (float)mRng.NextDouble() * 0.3f,
                    Size = 3f + (float)mRng.NextDouble() * 2f
                });
            }
            for (int i = 0; i < redCount; i++)
            {
                float angle = -Mathf.PI / 2f + (float)(mRng.NextDouble() - 0.5) * Mathf.PI;
                float speed = 60 + (float)mRng.NextDouble() * 100;
                mParticles.Add(new GameParticle
                {
                    X = x + (float)mRng.NextDouble() * 20 - 10,
                    Y = y,
                    VelX = Mathf.Cos(angle) * speed + 30,
                    VelY = Mathf.Sin(angle) * speed,
                    ParticleColor = new Color(0.9f, 0.2f, 0.15f),
                    Timer = 0, Lifetime = 0.8f + (float)mRng.NextDouble() * 0.3f,
                    Size = 3f + (float)mRng.NextDouble() * 2f
                });
            }
        }

        void TriggerShake(float intensity, float duration)
        {
            mShakeIntensity = intensity;
            mShakeTimer = duration;
            mShakeDuration = duration;
        }

        void UpdateEffects(float dt)
        {
            for (int i = mFloatingTexts.Count - 1; i >= 0; i--)
            {
                FloatingText ft = mFloatingTexts[i];
                ft.Timer += dt;
                ft.Y -= 40 * dt;
                if (ft.Timer >= ft.Lifetime) { mFloatingTexts.RemoveAt(i); continue; }
                mFloatingTexts[i] = ft;
            }
            for (int i = mParticles.Count - 1; i >= 0; i--)
            {
                GameParticle p = mParticles[i];
                p.Timer += dt;
                p.X += p.VelX * dt;
                p.Y += p.VelY * dt;
                p.VelY += 200 * dt;
                if (p.Timer >= p.Lifetime) { mParticles.RemoveAt(i); continue; }
                mParticles[i] = p;
            }
            if (mShakeTimer > 0) mShakeTimer -= dt;
        }

        void UpdateCamera(float dt)
        {
            float targetX = mPlayerX;
            float targetY = mPlayerY - 100;
            float smooth = 1f - Mathf.Pow(0.003f, dt);
            mCameraX = Mathf.Lerp(mCameraX, targetX, smooth);
            mCameraY = Mathf.Lerp(mCameraY, targetY, smooth);
        }

        #endregion

        #region Input

        void OnKeyDown(KeyDownEvent evt)
        {
            switch (evt.keyCode)
            {
                case KeyCode.LeftArrow: case KeyCode.A:
                    mMoveLeft = true; break;
                case KeyCode.RightArrow: case KeyCode.D:
                    mMoveRight = true; break;
                case KeyCode.DownArrow: case KeyCode.S:
                    mDropDown = true; break;
                case KeyCode.Z:
                    mRunning = true; break;
                case KeyCode.Space: case KeyCode.UpArrow: case KeyCode.W:
                    mJumpPressed = true;
                    if (mState == GameState.GameOver)
                    {
                        ResetPlayer();
                        mState = GameState.Playing;
                    }
                    break;
                case KeyCode.Escape:
                    OnExit?.Invoke(); break;
            }
            evt.StopPropagation();
        }

        void OnKeyUp(KeyUpEvent evt)
        {
            switch (evt.keyCode)
            {
                case KeyCode.LeftArrow: case KeyCode.A:
                    mMoveLeft = false; break;
                case KeyCode.RightArrow: case KeyCode.D:
                    mMoveRight = false; break;
                case KeyCode.DownArrow: case KeyCode.S:
                    mDropDown = false; break;
                case KeyCode.Z:
                    mRunning = false; break;
                case KeyCode.Space: case KeyCode.UpArrow: case KeyCode.W:
                    mJumpPressed = false; break;
            }
            evt.StopPropagation();
        }

        #endregion

        #region Rendering

        void OnPaint(MeshGenerationContext ctx)
        {
            float vw = resolvedStyle.width;
            float vh = resolvedStyle.height;
            if (float.IsNaN(vw) || float.IsNaN(vh) || vw <= 0 || vh <= 0)
                return;

#if UNITY_2022_1_OR_NEWER
            Painter2D p = ctx.painter2D;

            float shakeX = 0, shakeY = 0;
            if (mShakeTimer > 0 && mShakeDuration > 0)
            {
                float progress = mShakeTimer / mShakeDuration;
                shakeX = Mathf.Sin(mShakeTimer * 60) * mShakeIntensity * progress;
                shakeY = Mathf.Cos(mShakeTimer * 45) * mShakeIntensity * progress * 0.7f;
            }

            DrawBackground(p, vw, vh);
            DrawStars(p, vw, vh);
            DrawPlatforms(p, vw, vh, shakeX, shakeY);
            DrawChangesets(p, vw, vh, shakeX, shakeY);
            DrawLabels(p, vw, vh, shakeX, shakeY);
            DrawEnemies(p, vw, vh, shakeX, shakeY);
            DrawParticles(p, vw, vh, shakeX, shakeY);
            DrawPlayer(p, vw, vh, shakeX, shakeY);
            DrawFloatingTexts(p, vw, vh, shakeX, shakeY);
#endif
        }

        float SX(float wx, float vw, float shake)
        {
            return (wx - mCameraX) + vw / 2f + shake;
        }

        float SY(float wy, float vh, float shake)
        {
            return (wy - mCameraY) + vh / 2f + shake;
        }

        void DrawBackground(Painter2D p, float vw, float vh)
        {
            p.fillColor = new Color(0.06f, 0.07f, 0.16f, 0.97f);
            FillRect(p, 0, 0, vw, vh);

            p.fillColor = new Color(0.04f, 0.08f, 0.22f, 0.35f);
            p.BeginPath();
            p.MoveTo(new Vector2(0, vh * 0.55f));
            p.LineTo(new Vector2(vw, vh * 0.65f));
            p.LineTo(new Vector2(vw, vh));
            p.LineTo(new Vector2(0, vh));
            p.ClosePath();
            p.Fill();
        }

        void DrawStars(Painter2D p, float vw, float vh)
        {
            for (int i = 0; i < 40; i++)
            {
                float x = ((i * 137 + 29) % 997) / 997f * vw;
                float y = ((i * 269 + 47) % 991) / 991f * vh;
                float px = (mCameraX * 0.015f) % vw;
                float py = (mCameraY * 0.015f) % vh;
                x = ((x - px) % vw + vw) % vw;
                y = ((y - py) % vh + vh) % vh;

                float twinkle = 0.5f + 0.5f * Mathf.Sin(mAnimTimer * (2 + i % 3) + i);
                float alpha = (0.2f + ((i * 173) % 100) / 160f) * twinkle;
                float size = 1f + (i % 3) * 0.5f;

                p.fillColor = new Color(1, 1, 1, alpha);
                FillRect(p, x - size, y - size, size * 2, size * 2);
            }
        }

        void DrawPlatforms(Painter2D p, float vw, float vh, float sx, float sy)
        {
            foreach (Platform plat in mPlatforms)
            {
                float px = SX(plat.X, vw, sx);
                float py = SY(plat.Y, vh, sy);

                if (px + plat.Width < -50 || px > vw + 50 ||
                    py + plat.Height < -50 || py > vh + 50) continue;

                Color body = plat.IsMainBranch
                    ? new Color(0.15f, 0.55f, 0.25f)
                    : new Color(0.25f, 0.38f, 0.72f);

                Color top = plat.IsMainBranch
                    ? new Color(0.25f, 0.75f, 0.30f)
                    : new Color(0.40f, 0.55f, 0.88f);

                Color bottom = new Color(body.r * 0.5f, body.g * 0.5f, body.b * 0.5f);

                p.fillColor = body;
                FillRect(p, px, py, plat.Width, plat.Height);

                p.fillColor = top;
                FillRect(p, px, py, plat.Width, 3);

                p.fillColor = bottom;
                FillRect(p, px, py + plat.Height - 2, plat.Width, 2);

                if (plat.IsMainBranch)
                {
                    p.fillColor = new Color(0.22f, 0.82f, 0.18f);
                    float worldStart = plat.X + 6;
                    float worldEnd = plat.X + plat.Width - 6;
                    float screenLeft = -10f;
                    float screenRight = vw + 10f;
                    float camOff = mCameraX - vw / 2f;
                    float wStart = Mathf.Max(worldStart, camOff + screenLeft);
                    wStart = worldStart + Mathf.Ceil((wStart - worldStart) / 16f) * 16f;
                    float wEnd = Mathf.Min(worldEnd, camOff + screenRight);

                    for (float wx = wStart; wx < wEnd; wx += 16)
                    {
                        float gx = SX(wx, vw, sx);
                        p.BeginPath();
                        p.MoveTo(new Vector2(gx, py));
                        p.LineTo(new Vector2(gx + 5, py - 7));
                        p.LineTo(new Vector2(gx + 10, py));
                        p.ClosePath();
                        p.Fill();
                    }
                }
            }
        }

        void DrawChangesets(Painter2D p, float vw, float vh, float shx, float shy)
        {
            int drawn = 0;
            foreach (GameChangeset cs in mChangesets)
            {
                float cx = SX(cs.X, vw, shx);
                float cy = SY(cs.Y, vh, shy);
                if (cx < -40 || cx > vw + 40 || cy < -40 || cy > vh + 40) continue;
                if (++drawn > MAX_VISIBLE_ENTITIES) break;

                float r = cs.Radius;

                if (cs.Activated)
                {
                    p.fillColor = new Color(0.2f, 0.2f, 0.2f, 0.5f);
                    p.BeginPath();
                    p.Arc(new Vector2(cx, cy), r, 0, 360);
                    p.ClosePath();
                    p.Fill();

                    p.strokeColor = new Color(0.4f, 0.9f, 0.4f, 0.6f);
                    p.lineWidth = 2;
                    p.BeginPath();
                    p.MoveTo(new Vector2(cx - 5, cy));
                    p.LineTo(new Vector2(cx - 1, cy + 5));
                    p.LineTo(new Vector2(cx + 6, cy - 4));
                    p.Stroke();
                }
                else if (cs.Cooldown > 0)
                {
                    float pulse = 0.6f + 0.4f * Mathf.Sin(mAnimTimer * 8f);
                    p.fillColor = new Color(0.3f, 0.08f, 0.05f, 0.7f);
                    p.BeginPath();
                    p.Arc(new Vector2(cx, cy), r, 0, 360);
                    p.ClosePath();
                    p.Fill();

                    p.strokeColor = new Color(1f, 0.25f, 0.15f, pulse);
                    p.lineWidth = 2.5f;
                    p.BeginPath();
                    p.Arc(new Vector2(cx, cy), r, 0, 360);
                    p.ClosePath();
                    p.Stroke();

                    p.strokeColor = new Color(1f, 0.3f, 0.15f, pulse * 0.8f);
                    p.lineWidth = 2;
                    float s = r * 0.35f;
                    p.BeginPath();
                    p.MoveTo(new Vector2(cx - s, cy - s));
                    p.LineTo(new Vector2(cx + s, cy + s));
                    p.Stroke();
                    p.BeginPath();
                    p.MoveTo(new Vector2(cx + s, cy - s));
                    p.LineTo(new Vector2(cx - s, cy + s));
                    p.Stroke();
                }
                else
                {
                    float glow = 0.6f + 0.4f * Mathf.Sin(mAnimTimer * 2.5f + cs.X * 0.02f);
                    Color glowColor = new Color(0.2f, 0.75f, 0.95f, 0.15f * glow);
                    p.fillColor = glowColor;
                    p.BeginPath();
                    p.Arc(new Vector2(cx, cy), r + 6, 0, 360);
                    p.ClosePath();
                    p.Fill();

                    p.fillColor = new Color(0.12f, 0.14f, 0.22f);
                    p.BeginPath();
                    p.Arc(new Vector2(cx, cy), r, 0, 360);
                    p.ClosePath();
                    p.Fill();

                    Color borderColor = cs.IsHead
                        ? new Color(0.3f, 0.95f, 0.5f)
                        : new Color(0.25f, 0.7f, 0.95f);
                    p.strokeColor = borderColor;
                    p.lineWidth = 2.5f;
                    p.BeginPath();
                    p.Arc(new Vector2(cx, cy), r, 0, 360);
                    p.ClosePath();
                    p.Stroke();

                    p.fillColor = borderColor;
                    p.BeginPath();
                    p.Arc(new Vector2(cx, cy), r * 0.6f, 0, 360);
                    p.ClosePath();
                    p.Fill();
                }
            }
        }

        void DrawLabels(Painter2D p, float vw, float vh, float shx, float shy)
        {
            int drawn = 0;
            foreach (GameLabel lbl in mLabels)
            {
                float lx = SX(lbl.X, vw, shx);
                float ly = SY(lbl.Y, vh, shy);
                if (lx < -40 || lx > vw + 40 || ly < -40 || ly > vh + 40) continue;
                if (++drawn > MAX_VISIBLE_ENTITIES) break;

                if (lbl.Collected)
                {
                    p.fillColor = new Color(0.1f, 0.85f, 0.8f, 0.15f);
                    p.BeginPath();
                    p.Arc(new Vector2(lx, ly), lbl.Radius * 0.7f, 0, 360);
                    p.ClosePath();
                    p.Fill();
                    continue;
                }

                float pulse = 1f + 0.08f * Mathf.Sin(mAnimTimer * 4f + lbl.X * 0.03f);
                float r = lbl.Radius * pulse;

                p.fillColor = new Color(0.1f, 0.85f, 0.8f, 0.12f);
                p.BeginPath();
                p.Arc(new Vector2(lx, ly), r + 8, 0, 360);
                p.ClosePath();
                p.Fill();

                p.fillColor = new Color(0.1f, 0.78f, 0.75f);
                p.BeginPath();
                p.Arc(new Vector2(lx, ly), r * 0.5f, 0, 360);
                p.ClosePath();
                p.Fill();

                p.strokeColor = new Color(0.2f, 0.95f, 0.85f);
                p.lineWidth = 3;
                p.BeginPath();
                p.Arc(new Vector2(lx, ly), r * 0.5f, 0, 360);
                p.ClosePath();
                p.Stroke();

                p.strokeColor = new Color(0.2f, 0.95f, 0.85f, 0.4f);
                p.lineWidth = 1.5f;
                p.BeginPath();
                p.Arc(new Vector2(lx, ly), r, 0, 360);
                p.ClosePath();
                p.Stroke();
            }
        }

        void DrawEnemies(Painter2D p, float vw, float vh, float shx, float shy)
        {
            for (int i = 0; i < mEnemies.Count; i++)
            {
                Enemy e = mEnemies[i];
                float ex = SX(e.X, vw, shx);
                float ey = SY(e.Y, vh, shy);
                if (ex < -30 || ex > vw + 30 || ey < -30 || ey > vh + 30) continue;

                if (e.DeathTimer >= 0)
                {
                    float squish = Mathf.Clamp01(1f - e.DeathTimer * 4f);
                    float alpha = Mathf.Clamp01(1f - e.DeathTimer * 2f);
                    if (alpha <= 0) continue;
                    DrawEnemySquished(p, ex, ey, e.Type, squish, alpha);
                    continue;
                }

                if (!e.Alive) continue;

                if (e.GraceTimer > 0 && ((int)(mAnimTimer * 10) % 2 == 0))
                    continue;

                DrawEnemyAlive(p, ex, ey, e.Type, e.VelX > 0);
            }
        }

        void DrawEnemyAlive(Painter2D p, float cx, float cy, int type, bool facingRight)
        {
            float r = ENEMY_SIZE;
            float bob = Mathf.Sin(mAnimTimer * 6) * 2;

            if (type == 0)
            {
                p.fillColor = new Color(0.55f, 0.35f, 0.15f);
                p.BeginPath();
                p.Arc(new Vector2(cx, cy + bob), r, 0, 360);
                p.ClosePath();
                p.Fill();

                p.fillColor = new Color(0.45f, 0.28f, 0.10f);
                p.BeginPath();
                p.Arc(new Vector2(cx, cy + bob), r, 180, 360);
                p.ClosePath();
                p.Fill();

                DrawEnemyEyes(p, cx, cy + bob, r, facingRight, false);

                p.strokeColor = new Color(0.35f, 0.22f, 0.08f);
                p.lineWidth = 2;
                float legAnim = Mathf.Sin(mAnimTimer * 8) * 3;
                p.BeginPath();
                p.MoveTo(new Vector2(cx - 4, cy + bob + r - 2));
                p.LineTo(new Vector2(cx - 6 - legAnim, cy + bob + r + 4));
                p.Stroke();
                p.BeginPath();
                p.MoveTo(new Vector2(cx + 4, cy + bob + r - 2));
                p.LineTo(new Vector2(cx + 6 + legAnim, cy + bob + r + 4));
                p.Stroke();
            }
            else
            {
                p.fillColor = new Color(0.85f, 0.18f, 0.12f);
                p.BeginPath();
                p.Arc(new Vector2(cx, cy + bob), r, 0, 360);
                p.ClosePath();
                p.Fill();

                int spikes = 6;
                p.fillColor = new Color(0.95f, 0.3f, 0.15f);
                for (int s = 0; s < spikes; s++)
                {
                    float angle = (s / (float)spikes) * Mathf.PI * 2f + mAnimTimer * 2f;
                    float spikeX = cx + Mathf.Cos(angle) * (r + 4);
                    float spikeY = cy + bob + Mathf.Sin(angle) * (r + 4);
                    p.BeginPath();
                    p.Arc(new Vector2(spikeX, spikeY), 3f, 0, 360);
                    p.ClosePath();
                    p.Fill();
                }

                DrawEnemyEyes(p, cx, cy + bob, r, facingRight, true);
            }
        }

        void DrawEnemyEyes(
            Painter2D p, float cx, float cy, float r,
            bool facingRight, bool angry)
        {
            float dir = facingRight ? 1f : -1f;
            float eyeOff = r * 0.3f;

            p.fillColor = Color.white;
            p.BeginPath();
            p.Arc(new Vector2(cx - eyeOff * 0.5f + dir * 2, cy - r * 0.2f), 3.5f, 0, 360);
            p.ClosePath();
            p.Fill();
            p.BeginPath();
            p.Arc(new Vector2(cx + eyeOff * 0.5f + dir * 2, cy - r * 0.2f), 3.5f, 0, 360);
            p.ClosePath();
            p.Fill();

            p.fillColor = Color.black;
            p.BeginPath();
            p.Arc(new Vector2(cx - eyeOff * 0.5f + dir * 3, cy - r * 0.2f), 2f, 0, 360);
            p.ClosePath();
            p.Fill();
            p.BeginPath();
            p.Arc(new Vector2(cx + eyeOff * 0.5f + dir * 3, cy - r * 0.2f), 2f, 0, 360);
            p.ClosePath();
            p.Fill();

            if (angry)
            {
                p.strokeColor = new Color(0.4f, 0f, 0f);
                p.lineWidth = 2;
                p.BeginPath();
                p.MoveTo(new Vector2(cx - eyeOff - 2, cy - r * 0.55f));
                p.LineTo(new Vector2(cx - 1, cy - r * 0.35f));
                p.Stroke();
                p.BeginPath();
                p.MoveTo(new Vector2(cx + eyeOff + 2, cy - r * 0.55f));
                p.LineTo(new Vector2(cx + 1, cy - r * 0.35f));
                p.Stroke();
            }
        }

        void DrawEnemySquished(
            Painter2D p, float cx, float cy, int type, float squish, float alpha)
        {
            Color c = type == 1
                ? new Color(0.85f, 0.18f, 0.12f, alpha)
                : new Color(0.55f, 0.35f, 0.15f, alpha);
            float h = ENEMY_SIZE * 2 * squish;
            float w = ENEMY_SIZE * 2 * (2f - squish);

            p.fillColor = c;
            FillRect(p, cx - w / 2, cy + ENEMY_SIZE - h, w, Mathf.Max(h, 2));
        }

        void DrawPlayer(Painter2D p, float vw, float vh, float shx, float shy)
        {
            float px = SX(mPlayerX, vw, shx);
            float py = SY(mPlayerY, vh, shy);

            if (mState == GameState.Dying)
            {
                float progress = Mathf.Clamp01(mDeathTimer / 1.2f);
                DrawCharacter(p, px, py, 1f, 1f - progress, true);
                return;
            }

            if (mRunning && mIsGrounded &&
                Mathf.Abs(mPlayerVelX) > 0.1f)
            {
                float dustAlpha = 0.15f + 0.1f * Mathf.Sin(mAnimTimer * 20f);
                float dustX = px - (mFacingRight ? 8f : -8f);
                float dustY = py + PLAYER_HEIGHT;
                p.fillColor = new Color(0.8f, 0.7f, 0.5f, dustAlpha);
                for (int i = 0; i < 3; i++)
                {
                    float offset = i * 6f * (mFacingRight ? -1f : 1f);
                    float r = 3f - i * 0.8f;
                    FillRect(p, dustX + offset - r, dustY - i * 2f - r, r * 2, r * 2);
                }
            }

            bool blink = mInvincibleTimer > 0 &&
                ((int)(mAnimTimer * 12) % 2 == 0);

            if (!blink)
                DrawCharacter(p, px, py, 1f, 1f, false);
        }

        void DrawCharacter(
            Painter2D p, float cx, float cy, float scale, float alpha, bool dead)
        {
            float hw = PLAYER_WIDTH / 2f * scale;
            float h = PLAYER_HEIGHT * scale;
            float top = cy;

            Color skin = WithAlpha(dead ? new Color(0.6f, 0.3f, 0.3f) : new Color(1f, 0.82f, 0.65f), alpha);
            Color hat = WithAlpha(dead ? new Color(0.5f, 0.1f, 0.1f) : new Color(0.90f, 0.12f, 0.12f), alpha);
            Color shirt = hat;
            Color pants = WithAlpha(dead ? new Color(0.15f, 0.1f, 0.3f) : new Color(0.15f, 0.15f, 0.65f), alpha);
            Color shoe = WithAlpha(new Color(0.35f, 0.18f, 0.08f), alpha);

            float flip = mFacingRight ? 1f : -1f;
            bool legFwd = mAnimFrame % 2 == 1;

            float hatBottom = top + h * 0.18f;
            p.fillColor = hat;
            p.BeginPath();
            p.MoveTo(new Vector2(cx - hw * 0.9f, hatBottom));
            p.LineTo(new Vector2(cx - hw * 0.9f, top + h * 0.06f));
            p.LineTo(new Vector2(cx + hw * 1.1f * flip, top));
            p.LineTo(new Vector2(cx + hw * 1.1f, top + h * 0.06f));
            p.LineTo(new Vector2(cx + hw * 0.9f, hatBottom));
            p.ClosePath();
            p.Fill();

            float headCY = hatBottom + h * 0.10f;
            float headR = h * 0.12f;
            p.fillColor = skin;
            p.BeginPath();
            p.Arc(new Vector2(cx, headCY), headR, 0, 360);
            p.ClosePath();
            p.Fill();

            float eyeX = cx + hw * 0.2f * flip;
            float eyeY = headCY - headR * 0.15f;
            float er = 2.2f * scale;
            p.fillColor = WithAlpha(Color.white, alpha);
            FillRect(p, eyeX - er, eyeY - er, er * 2, er * 2);
            float pr = 1.3f * scale;
            p.fillColor = WithAlpha(Color.black, alpha);
            FillRect(p, eyeX + 0.5f * flip - pr, eyeY - pr, pr * 2, pr * 2);

            float bodyTop = headCY + headR - 1;
            float bodyH = h * 0.30f;
            p.fillColor = shirt;
            FillRect(p, cx - hw * 0.7f, bodyTop, hw * 1.4f, bodyH);

            float btnR = 1.5f * scale;
            p.fillColor = WithAlpha(new Color(1f, 0.85f, 0f), alpha);
            FillRect(p, cx - btnR, bodyTop + bodyH * 0.35f - btnR, btnR * 2, btnR * 2);
            FillRect(p, cx - btnR, bodyTop + bodyH * 0.65f - btnR, btnR * 2, btnR * 2);

            float armY = bodyTop + 2;
            float armLen = hw * 0.8f;
            float armSwing = legFwd ? hw * 0.3f : -hw * 0.2f;
            p.strokeColor = skin;
            p.lineWidth = 3f * scale;
            p.BeginPath(); p.MoveTo(new Vector2(cx + hw * 0.7f, armY)); p.LineTo(new Vector2(cx + hw * 0.7f + armLen * 0.6f, armY + armLen * 0.7f + armSwing)); p.Stroke();
            p.BeginPath(); p.MoveTo(new Vector2(cx - hw * 0.7f, armY)); p.LineTo(new Vector2(cx - hw * 0.7f - armLen * 0.6f, armY + armLen * 0.7f - armSwing)); p.Stroke();

            float pantsTop = bodyTop + bodyH;
            float pantsH = h * 0.12f;
            p.fillColor = pants;
            FillRect(p, cx - hw * 0.65f, pantsTop, hw * 1.3f, pantsH);

            float legTop = pantsTop + pantsH;
            float legH = h * 0.13f;
            float legOff = hw * 0.28f;
            float legStep = legFwd ? legOff * 0.5f : -legOff * 0.3f;
            p.fillColor = pants;
            FillRect(p, cx - legOff - 2.5f * scale, legTop, 5 * scale, legH);
            FillRect(p, cx + legOff - 2.5f * scale, legTop, 5 * scale, legH);

            float shoeTop = legTop + legH;
            float shoeH = h * 0.07f;
            float shoeW = hw * 0.45f;
            p.fillColor = shoe;
            FillRect(p, cx - legOff + legStep - shoeW * 0.2f, shoeTop, shoeW * 1.2f, shoeH);
            FillRect(p, cx + legOff - legStep - shoeW, shoeTop, shoeW * 1.2f, shoeH);
        }

        void DrawParticles(Painter2D p, float vw, float vh, float shx, float shy)
        {
            foreach (GameParticle pt in mParticles)
            {
                float px = SX(pt.X, vw, shx);
                float py = SY(pt.Y, vh, shy);
                if (px < -20 || px > vw + 20 || py < -20 || py > vh + 20)
                    continue;

                float alpha = 1f - pt.Timer / pt.Lifetime;
                p.fillColor = WithAlpha(pt.ParticleColor, alpha);
                float s = pt.Size * alpha;
                FillRect(p, px - s / 2, py - s / 2, s, s);
            }
        }

        void DrawFloatingTexts(Painter2D p, float vw, float vh, float shx, float shy)
        {
            foreach (FloatingText ft in mFloatingTexts)
            {
                float fx = SX(ft.X, vw, shx);
                float fy = SY(ft.Y, vh, shy);
                if (fx < -200 || fx > vw + 200 || fy < -50 || fy > vh + 50)
                    continue;

                float alpha = 1f - ft.Timer / ft.Lifetime;
                float scale = 1f + ft.Timer * 0.3f;

                Color c = WithAlpha(ft.TextColor, alpha);

                float pixelSize = 2f * scale;
                float charW = pixelSize * 5f;
                float textH = pixelSize * 5f;

                float bgW = ft.Text.Length * charW;
                float bgH = textH;
                p.fillColor = WithAlpha(new Color(0, 0, 0, 0.5f), alpha);
                FillRect(p, fx - bgW / 2 - 4, fy - bgH / 2 - 3, bgW + 8, bgH + 6);

                float startX = fx - (ft.Text.Length * charW) / 2f;
                float startY = fy - textH / 2f;
                for (int ci = 0; ci < ft.Text.Length; ci++)
                {
                    char ch = ft.Text[ci];
                    if (ch != ' ')
                    {
                        DrawTinyChar(p, startX + ci * charW, startY, pixelSize, ch, c);
                    }
                }
            }
        }

        void DrawTinyChar(Painter2D p, float x, float y, float s, char c, Color col)
        {
            uint glyph = GetGlyph(char.ToUpper(c));
            if (glyph == 0)
            {
                p.fillColor = col;
                FillRect(p, x + s, y + s * 2f, s, s);
                return;
            }

            p.fillColor = col;
            for (int row = 0; row < 5; row++)
            {
                int rowBits = (int)((glyph >> ((4 - row) * 4)) & 0xF);
                for (int bit = 0; bit < 4; bit++)
                {
                    if ((rowBits & (8 >> bit)) != 0)
                        FillRect(p, x + bit * s, y + row * s, s, s);
                }
            }
        }

        static uint GetGlyph(char c)
        {
            switch (c)
            {
                case 'A': return 0x69F99;
                case 'B': return 0xE9E9E;
                case 'C': return 0x78887;
                case 'D': return 0xE999E;
                case 'E': return 0xF8E8F;
                case 'F': return 0xF8E88;
                case 'G': return 0x78B97;
                case 'H': return 0x99F99;
                case 'I': return 0xE444E;
                case 'J': return 0x31196;
                case 'K': return 0x9ACA9;
                case 'L': return 0x8888F;
                case 'M': return 0x9FF99;
                case 'N': return 0x9DB99;
                case 'O': return 0x69996;
                case 'P': return 0xE9E88;
                case 'Q': return 0x69961;
                case 'R': return 0xE9EA9;
                case 'S': return 0x7861E;
                case 'T': return 0xF4444;
                case 'U': return 0x99996;
                case 'V': return 0x99966;
                case 'W': return 0x999F6;
                case 'X': return 0x99699;
                case 'Y': return 0x99644;
                case 'Z': return 0xF168F;
                case '0': return 0x69996;
                case '1': return 0x26227;
                case '2': return 0x6924F;
                case '3': return 0xE161E;
                case '4': return 0x99F11;
                case '5': return 0xF8E1E;
                case '6': return 0x68E96;
                case '7': return 0xF1244;
                case '8': return 0x69696;
                case '9': return 0x69716;
                case '+': return 0x04E40;
                case '-': return 0x00E00;
                case '!': return 0x44404;
                case '.': return 0x00004;
                case '/': return 0x12480;
                default:  return 0x00000;
            }
        }

        static void FillRect(Painter2D p, float x, float y, float w, float h)
        {
            p.BeginPath();
            p.MoveTo(new Vector2(x, y));
            p.LineTo(new Vector2(x + w, y));
            p.LineTo(new Vector2(x + w, y + h));
            p.LineTo(new Vector2(x, y + h));
            p.ClosePath();
            p.Fill();
        }

        static Color WithAlpha(Color c, float alpha)
        {
            return new Color(c.r, c.g, c.b, c.a * alpha);
        }

        #endregion

        #region HUD

        void BuildHUD()
        {
            mScoreLabel = new Label("Score: 0");
            mScoreLabel.style.position = Position.Absolute;
            mScoreLabel.style.left = 20;
            mScoreLabel.style.top = 12;
            mScoreLabel.style.fontSize = 22;
            mScoreLabel.style.color = Color.white;
            mScoreLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            Add(mScoreLabel);

            mLivesLabel = new Label("");
            mLivesLabel.style.position = Position.Absolute;
            mLivesLabel.style.right = 20;
            mLivesLabel.style.top = 12;
            mLivesLabel.style.fontSize = 22;
            mLivesLabel.style.color = new Color(0.95f, 0.2f, 0.2f);
            mLivesLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            Add(mLivesLabel);

            mStatsLabel = new Label("");
            mStatsLabel.style.position = Position.Absolute;
            mStatsLabel.style.left = 20;
            mStatsLabel.style.top = 38;
            mStatsLabel.style.fontSize = 13;
            mStatsLabel.style.color = new Color(1f, 1f, 1f, 0.65f);
            Add(mStatsLabel);

            Label titleLabel = new Label("BRANCH RUNNER");
            titleLabel.style.position = Position.Absolute;
            titleLabel.style.top = 12;
            titleLabel.style.left = 0;
            titleLabel.style.right = 0;
            titleLabel.style.unityTextAlign = TextAnchor.UpperCenter;
            titleLabel.style.fontSize = 13;
            titleLabel.style.color = new Color(1, 1, 1, 0.4f);
            titleLabel.style.letterSpacing = 4;
            Add(titleLabel);

            Label instrLabel = new Label(
                "\u2190\u2192 Move  |  Z Run  |  SPACE Jump  |  Z+JUMP Super Jump  |  \u2193 Drop  |  Jump in air = Double Jump!  |  ESC Quit");
            instrLabel.style.position = Position.Absolute;
            instrLabel.style.bottom = 10;
            instrLabel.style.left = 0;
            instrLabel.style.right = 0;
            instrLabel.style.unityTextAlign = TextAnchor.LowerCenter;
            instrLabel.style.fontSize = 12;
            instrLabel.style.color = new Color(1, 1, 1, 0.35f);
            Add(instrLabel);

            mGameOverPanel = new VisualElement();
            mGameOverPanel.style.position = Position.Absolute;
            mGameOverPanel.style.left = mGameOverPanel.style.right = 0;
            mGameOverPanel.style.top = mGameOverPanel.style.bottom = 0;
            mGameOverPanel.style.justifyContent = Justify.Center;
            mGameOverPanel.style.alignItems = Align.Center;
            mGameOverPanel.style.backgroundColor = new Color(0, 0, 0, 0.7f);
            mGameOverPanel.style.display = DisplayStyle.None;

            Label goTitle = new Label("GAME OVER");
            goTitle.style.fontSize = 48;
            goTitle.style.color = new Color(0.95f, 0.15f, 0.15f);
            goTitle.style.unityFontStyleAndWeight = FontStyle.Bold;
            goTitle.style.letterSpacing = 6;
            mGameOverPanel.Add(goTitle);

            mFinalScoreLabel = new Label("Score: 0");
            mFinalScoreLabel.style.fontSize = 26;
            mFinalScoreLabel.style.color = Color.white;
            mFinalScoreLabel.style.marginTop = 12;
            mGameOverPanel.Add(mFinalScoreLabel);

            Label restartLabel = new Label("Press SPACE to restart  |  ESC to quit");
            restartLabel.style.fontSize = 16;
            restartLabel.style.color = new Color(1, 1, 1, 0.6f);
            restartLabel.style.marginTop = 24;
            mGameOverPanel.Add(restartLabel);

            Add(mGameOverPanel);
        }

        void UpdateHUD()
        {
            mScoreLabel.text = string.Format("Score: {0}", mScore);

            string hearts = "";
            for (int i = 0; i < mLives; i++) hearts += "\u2764 ";
            mLivesLabel.text = hearts;

            int csActivated = 0;
            foreach (GameChangeset c in mChangesets) { if (c.Activated) csActivated++; }
            int lblCollected = 0;
            foreach (GameLabel l in mLabels) { if (l.Collected) lblCollected++; }
            int enemiesAlive = 0;
            foreach (Enemy e in mEnemies) { if (e.Alive && e.DeathTimer < 0) enemiesAlive++; }

            mStatsLabel.text = string.Format(
                "Changesets: {0}/{1}  |  Labels: {2}/{3}  |  Bugs alive: {4}",
                csActivated, mChangesets.Count,
                lblCollected, mLabels.Count,
                enemiesAlive);
        }

        #endregion

        #region Data Structures

        struct Platform
        {
            public float X, Y, Width, Height;
            public string Name;
            public bool IsMainBranch;
        }

        struct GameChangeset
        {
            public float X, Y, Radius;
            public bool Activated;
            public int Additions, Deletions;
            public bool IsHead;
            public float Cooldown;
        }

        struct GameLabel
        {
            public float X, Y, Radius;
            public string Name;
            public bool Collected;
        }

        struct Enemy
        {
            public float X, Y;
            public float VelX;
            public float PatrolLeft, PatrolRight;
            public bool Alive;
            public float DeathTimer;
            public int Type;
            public float GraceTimer;
        }

        struct FloatingText
        {
            public float X, Y;
            public string Text;
            public Color TextColor;
            public float Timer, Lifetime;
        }

        struct GameParticle
        {
            public float X, Y;
            public float VelX, VelY;
            public Color ParticleColor;
            public float Timer, Lifetime, Size;
        }

        enum GameState { Playing, Dying, GameOver }

        #endregion

        #region Fields

        GameState mState;
        float mPlayerX, mPlayerY;
        float mPlayerVelX, mPlayerVelY;
        bool mIsGrounded;
        bool mFacingRight = true;
        int mScore;
        int mLives;
        int mCombo;
        float mComboTimer;
        int mAnimFrame;
        float mAnimTimer;
        float mDeathTimer;
        float mCameraX, mCameraY;
        float mSpawnX, mSpawnY;
        float mCheckpointX, mCheckpointY;
        float mInvincibleTimer;
        Vector2 mInitialScrollOffset;
        float mInitialZoomLevel;
        float mShakeTimer, mShakeIntensity, mShakeDuration;

        bool mMoveLeft, mMoveRight, mJumpPressed, mDropDown;
        bool mRunning;
        bool mCanDoubleJump;
        float mDropTimer;

        readonly List<Platform> mPlatforms = new List<Platform>();
        readonly List<GameChangeset> mChangesets = new List<GameChangeset>();
        readonly List<GameLabel> mLabels = new List<GameLabel>();
        readonly List<Enemy> mEnemies = new List<Enemy>();
        readonly List<FloatingText> mFloatingTexts = new List<FloatingText>();
        readonly List<GameParticle> mParticles = new List<GameParticle>();

        Label mScoreLabel, mLivesLabel, mStatsLabel;
        VisualElement mGameOverPanel;
        Label mFinalScoreLabel;

        long mLastTick;
        IVisualElementScheduledItem mGameLoop;
        System.Random mRng;

        #endregion

        #region Constants

        const float GRAVITY = 950f;
        const float JUMP_VELOCITY = -500f;
        const float DOUBLE_JUMP_VELOCITY = -420f;
        const float SUPER_JUMP_VELOCITY = -700f;
        const float STOMP_BOUNCE = -320f;
        const float MOVE_SPEED = 300f;
        const float RUN_SPEED_MULTIPLIER = 1.8f;
        const float PLAYER_WIDTH = 24f;
        const float PLAYER_HEIGHT = 36f;
        const float PLATFORM_MIN_HEIGHT = 14f;
        const float ENEMY_SIZE = 12f;
        const float ENEMY_SPEED = 55f;
        const float ENEMY_FAST_SPEED = 95f;
        const float COMBO_WINDOW = 3f;
        const float INVINCIBLE_DURATION = 2f;
        const float DROP_THROUGH_TIME = 0.25f;
        const int LABEL_POINTS = 500;
        const int BUG_CHANCE_PERCENT = 30;
        const float ENEMY_SPAWN_OFFSET = 60f;
        const float ENEMY_GRACE_TIME = 1.2f;
        const float CHANGESET_COOLDOWN = 1.5f;
        const int MAX_VISIBLE_ENTITIES = 60;

        static readonly string[] STOMP_MESSAGES =
        {
            "BUG FIXED!", "SQUASHED!", "RESOLVED!",
            "PATCHED!", "DEBUGGED!", "HOTFIXED!"
        };

        static readonly string[] BUG_FOUND_MESSAGES =
        {
            "BUG FOUND!", "REGRESSION!", "BROKEN BUILD!",
            "NULL REF!", "OFF BY ONE!", "RACE CONDITION!",
            "MEMORY LEAK!", "STACK OVERFLOW!"
        };

        #endregion
    }
}
