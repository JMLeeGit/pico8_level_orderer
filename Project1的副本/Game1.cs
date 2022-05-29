using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.Diagnostics;

namespace Project1
{
    public class Game1 : Game
    {
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;

        public Level TestLevel;
        public const int ScreenW = 640;
        public const int ScreenH = 640;

        public Game1()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
        }

        protected override void Initialize()
        {
            // TODO: Add your initialization logic here
            Window.Title = "level sorter";
            _graphics.PreferredBackBufferWidth = ScreenW;
            _graphics.PreferredBackBufferHeight = ScreenH;
            _graphics.ApplyChanges();

            TestLevel = new Level(_graphics.GraphicsDevice);
            Pico8.Init(_graphics.GraphicsDevice);
            // Simple tests
            // Array copy test
            /*byte[,] arr1 = { { 0, 1, 2, 3 }, { 0, 1, 2, 3 } };
            byte[,] arr2 = { { 3, 4, 5, 6 }, { 3, 4, 5, 6 } };
            byte[,] buffer = new byte[arr1.GetLength(0) + arr2.GetLength(0), arr1.GetLength(1)];

            Array.Copy(arr1, buffer, arr1.Length);
            Array.Copy(arr2, 0, buffer, arr1.Length, arr2.Length);*/

            base.Initialize();
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);

            // TODO: use this.Content to load your game content here
        }

        protected override void Update(GameTime gameTime)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            // TODO: Add your update logic here

            Pico8.Update();

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(new Color(99, 99, 102));
            // TODO: Add your drawing code here
            _spriteBatch.Begin(samplerState: SamplerState.PointClamp);
            Pico8.Draw(_graphics.GraphicsDevice, _spriteBatch);
            _spriteBatch.End();
            base.Draw(gameTime);
        }
    }
}
