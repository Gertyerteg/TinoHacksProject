﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using TinoHacksGame.States;

namespace TinoHacksGame.Sprites {
    /// <summary>
    /// A player sprite within the game.
    /// </summary>
    public class Player : Sprite {
        /// <summary>
        /// The maximum slowfall speed.
        /// </summary>
        public const float SLOWFALL = 2.5f;
        /// <summary>
        /// The maximum fastfall speed.
        /// </summary>
        public const float FASTFALL = 14f;
        /// <summary>
        /// The maximum walking speed.
        /// </summary>
        public const float WALK = 0.5f;
        /// <summary>
        /// The maximum dashing speed.
        /// </summary>
        public const float DASH = 1.5f;
        /// <summary>
        /// The maximum number of jumps.
        /// </summary>
        public const int MAXJUMPS = 2;
        public int numJumps = 0;
        /// <summary>
        /// If the player is on the ground or not.
        /// </summary>
        public bool IsFloating {
            get;
            set;
        }

        private Texture2D walkRightTexture, walkLeftTexture, dashRightTexture, dashLeftTexture,
            idleTexture, idleLeftTexture, fastFallTexture, slowFallTexture, jumpLeftTexture, jumpRightTexture;

        public bool AisUP = true;
        private bool wasLeft = false;

        /// <summary>
        /// Whether the <c>Player</c> is fast falling.
        /// </summary>
        public bool FastFalling = false;
        private bool firstTapDown = false;
        private bool secondTapNotDown = false;
        private float fastFallTimer = 0f;

        private bool dashing = false;
        private int firstTapSide = 0;
        private int secondTapNotSide = 0;
        private float dashTimer = 0f;

        /// <summary>
        /// The speed in which the player moves at.
        /// </summary>
        public const float SPEED = 0.1f;

        private float rotation;
        private float jumpTimer;
        private float walkAnimationTimer, dashAnimationTimer, jumpAnimationTimer;
        private int walkAnimationFrameNumber, dashAnimationFrameNumber, jumpAnimationFrame;
        private bool isJumping;

        /// <summary>
        /// The player number.
        /// </summary>
        public int index;

        /// <summary>
        /// Creates a new instance of <c>Player</c>
        /// </summary>
        public Player(GameState state, int index) : base(state)
        {
            this.index = index;

            IsFloating = false;
        }


        /// <summary>
        /// Updates the <c>Player</c>'s logic and conditional checking.
        /// </summary>
        /// <param name="gameTime"></param>
        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            Size = Texture.Bounds.Size;
            GamePadState gamePadState = GamePad.GetState(index);
            rotation += gamePadState.ThumbSticks.Right.X * MathHelper.Pi / 10;

            // animations
            if (walkLeftTexture == null)
            {
                walkLeftTexture = state.Content.Load<Texture2D>("Move_Left");
                walkRightTexture = state.Content.Load<Texture2D>("Move_Right");
                idleTexture = state.Content.Load<Texture2D>("Idle");
                dashLeftTexture = state.Content.Load<Texture2D>("Dash_Left");
                dashRightTexture = state.Content.Load<Texture2D>("Dash_Right");
                jumpRightTexture = state.Content.Load<Texture2D>("Jump_Right");
                jumpLeftTexture = state.Content.Load<Texture2D>("Jump_Left");
                idleLeftTexture = state.Content.Load<Texture2D>("Idle_Left");
            }

            float left = gamePadState.ThumbSticks.Left.X;
            if (Math.Abs(left) > 0)
            {
                walkAnimationTimer += (float)gameTime.ElapsedGameTime.TotalMilliseconds;
                if (walkAnimationTimer >= 250f)
                {
                    walkAnimationTimer = 0f;
                    walkAnimationFrameNumber = (walkAnimationFrameNumber + 1) % 4;
                }
            }

            if (left > 0)
                wasLeft = false;
            else if (left < 0)
                wasLeft = true;

            if (isJumping)
            {
                jumpAnimationTimer += (float)gameTime.ElapsedGameTime.TotalMilliseconds;

                if (jumpAnimationTimer > 200f)
                    jumpAnimationFrame = 1;
            }

            if (dashing)
            {
                dashTimer += (float)gameTime.ElapsedGameTime.TotalMilliseconds;

                if (dashTimer >= 100f)
                {
                    dashTimer = 0f;
                    if (dashAnimationFrameNumber == 0)
                        dashAnimationFrameNumber = 1;
                    else
                        dashAnimationFrameNumber = 0;
                }
            }

            //dashing
            if (gamePadState.ThumbSticks.Left.X == -1) {
                if (secondTapNotSide == -1 && dashTimer < 50f) {
                    dashing = true;
                    firstTapSide = 0;
                    secondTapNotSide = 0;
                }
                else firstTapSide = -1;
                dashTimer = 0f;
            }
            else if (gamePadState.ThumbSticks.Left.X == 1) {
                if (secondTapNotSide == 1 && dashTimer < 50f) {
                    dashing = true;
                    firstTapSide = 0;
                    secondTapNotSide = 0;
                }
                else firstTapSide = 1;
                dashTimer = 0f;
            }
            else if (firstTapSide != 0) {
                secondTapNotSide = firstTapSide;
            }
            //left/right movement
            if (left < 0 && Velocity.X > 0)
                Velocity += new Vector2(-0.25f, 0);
            else if (left > 0 && Velocity.X < 0)
                Velocity += new Vector2(0.25f, 0);
            else
                Velocity = new Vector2(Math.Max(Math.Min(dashing ? DASH : WALK, Velocity.X + left * SPEED),
                    -(dashing ? DASH : WALK)), Velocity.Y);
            dashTimer += (float)gameTime.ElapsedGameTime.TotalMilliseconds;

            //air and ground friction
            if (gamePadState.ThumbSticks.Left.Length() == 0) {
                float coeff = IsFloating ? 0.02f : 0.4f;
                if (Velocity.X >= 0.1 || Velocity.X <= -0.1) Velocity -= new Vector2(Velocity.X * coeff, 0);
                else {
                    Velocity = new Vector2(0.0f, Velocity.Y);
                    dashing = false;
                    firstTapSide = 0;
                    secondTapNotSide = 0;
                }
            }

            //ground detection
            foreach (Platform p in state.currentStage.Platforms) {
                Rectangle rect = GetDrawRectangle();
                Rectangle rect2 = p.GetDrawRectangle();
                if (rect.Intersects(rect2))
                {
                    if (rect.Bottom <= rect2.Bottom && Velocity.Y > 0)
                    {
                        Position = new Vector2(Position.X, rect2.Top - Origin.Y * GameState.SCALE);
                        FastFalling = false;
                        IsFloating = false;
                        numJumps = 0;
                        jumpTimer = 0f;
                    }
                    break;
                }
                else
                    IsFloating = true;
            }

            //jump
            if (gamePadState.IsButtonDown(Buttons.A))

            {
                isJumping = true;
                if (AisUP && numJumps < MAXJUMPS) {
                    jumpTimer = 0f;
                    numJumps++;
                    FastFalling = false;
                    AisUP = false;
                    Velocity = new Vector2(Velocity.X, -1.5f);
                    IsFloating = true;
                }
                else if (IsFloating) {
                    Console.WriteLine("lolz" + jumpTimer);
                    if (jumpTimer < 125f) Velocity = new Vector2(Velocity.X, -1.55f);
                    else if (jumpTimer < 150f) Velocity = new Vector2(Velocity.X, -1.55f);
                }
                jumpTimer += (float)gameTime.ElapsedGameTime.TotalMilliseconds;
            }
            else if (gamePadState.IsButtonUp(Buttons.A)) {
                AisUP = true;
                jumpTimer = 200f;
                isJumping = false;
            }
            //fast falling
            if (IsFloating && gamePadState.ThumbSticks.Left.Y == -1) {
                if (secondTapNotDown && fastFallTimer < 50f) {
                    FastFalling = true;
                    firstTapDown = false;
                    secondTapNotDown = false;
                }
                else firstTapDown = true;
                fastFallTimer = 0f;
            }
            else if (firstTapDown) secondTapNotDown = true;
            //gravity
            if (FastFalling && IsFloating) {
                Velocity += new Vector2(0, FASTFALL * GameState.GRAVITY);
            }
            else if (IsFloating) {
                Velocity += new Vector2(0, SLOWFALL * GameState.GRAVITY);
                jumpTimer += (float)gameTime.ElapsedGameTime.TotalMilliseconds;
                fastFallTimer += (float)gameTime.ElapsedGameTime.TotalMilliseconds;
            }
            else {
                numJumps = 0;
                Velocity = new Vector2(Velocity.X, 0.0f);
                fastFallTimer = 0f;
                firstTapDown = false;
                secondTapNotDown = false;
                FastFalling = false;
            }
        }

        /// <summary>
        /// Draws the <c>Player</c> to the screen.
        /// </summary>
        /// <param name="spriteBatch"></param>
        public override void Draw(SpriteBatch spriteBatch)
        {
            base.Draw(spriteBatch);

            if (idleTexture != null)
            Origin = new Vector2(idleTexture.Width / 2f, idleTexture.Height / 2f);

            bool left = Velocity.X < 0f;

            Texture2D drawnTexture = idleTexture;

            if (wasLeft)
                drawnTexture = idleLeftTexture;
            
            if (dashing)
            {
                if (left)
                    drawnTexture = dashLeftTexture;
                else
                    drawnTexture = dashRightTexture;
            }
            else if (Math.Abs(Velocity.X) > 0)
            {
                if (!left)
                    drawnTexture = walkRightTexture;
                else
                    drawnTexture = walkLeftTexture;
            }

            if (isJumping)
            {
                if (!left)
                    drawnTexture = jumpRightTexture;
                else
                    drawnTexture = jumpLeftTexture;
            }
            //else if (IsFloating)
            //{
            //    if (!FastFalling)
            //        drawnTexture = slowFallTexture;
            //    else
            //        drawnTexture = fastFallTexture;
            //}

            if (drawnTexture != null)
            {
                if (drawnTexture == walkLeftTexture || drawnTexture == walkRightTexture)
                    spriteBatch.Draw(drawnTexture, Position, new Rectangle(29 * walkAnimationFrameNumber, 0, 29, 44), Color.White, rotation,
                        Origin, GameState.SCALE, SpriteEffects.None, 0f);
                else if (drawnTexture == dashLeftTexture || dashRightTexture == drawnTexture)
                    spriteBatch.Draw(drawnTexture, Position, new Rectangle(34 * dashAnimationFrameNumber, 0, 34, 50), Color.White, rotation,
                        Origin, GameState.SCALE, SpriteEffects.None, 0f);
                else if (drawnTexture == jumpLeftTexture || drawnTexture == jumpRightTexture)
                    spriteBatch.Draw(drawnTexture, Position, new Rectangle(28 * jumpAnimationFrame, 0, 28, 42), Color.White, rotation,
                        Origin, GameState.SCALE, SpriteEffects.None, 0f);
                else
                    spriteBatch.Draw(drawnTexture, Position, null, Color.White, rotation,
                        Origin, GameState.SCALE, SpriteEffects.None, 0f);
            }
            
        }
    }

}
