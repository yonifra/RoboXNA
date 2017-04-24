#region Using Statements
using System;
using System.Text;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;

using RoboXNA.Menus;
using RoboXNAModel;
#endregion

namespace RoboXNA
{
    /// <summary>
    /// This screen implements the actual game logic. It is just a
    /// placeholder to get the idea across: you'll probably want to
    /// put some more interesting gameplay in here!
    /// </summary>
    public class GameplayScreen : GameScreen
    {
        #region Fields

        ContentManager content;
        SpriteFont gameFont;

        private SpriteBatch spriteBatch;
        private SpriteFont spriteFont;
        private Effect skyboxEffect;

        private Vector2 fontPos;
        private TimeSpan elapsedTime = TimeSpan.Zero;
        private bool displayHelp;
        private Song ambientSound;
        private bool soundEnabled;

        // Skybox variables
        private Texture2D[] skyboxTextures;
        private Model skyboxModel;

        // FuelCell Game stuff
        private GraphicsDeviceManager graphics;
        private KeyboardState prevKeyboardState = new KeyboardState();
        private KeyboardState currentKeyboardState = new KeyboardState();
        private Random random;
        Building[] buildings;
        private Camera gameCamera;

        // Our game objects
        private RobotObject robotModel;
        private AnimationPlayer animationPlayer;
        private GameObject ground;
        private GameObject boundingSphere;

        #endregion

        #region Initialization

        /// <summary>
        /// Constructor.
        /// </summary>
        public GameplayScreen()
        {
            soundEnabled = RoboXNA.Menus.RoboXNA.Default.soundActive;

            Initialize();

            TransitionOnTime = TimeSpan.FromSeconds(1.5);
            TransitionOffTime = TimeSpan.FromSeconds(0.5);

            graphics = Robo.graphics;

            random = new Random();
        }

        /// <summary>
        /// Initializes all the game variables
        /// </summary>
        private void Initialize()
        {
            ground = new GameObject();
            boundingSphere = new GameObject();
            gameCamera = new Camera();

            // Position the text.
            fontPos = new Vector2(1.0f, 1.0f);
        }

        /// <summary>
        /// Load graphics content for the game.
        /// </summary>
        public override void LoadContent()
        {
            if (content == null)
            {
                content = new ContentManager(ScreenManager.Game.Services, "Content");
            }

            ambientSound = content.Load<Song>(@"Sounds\birds");
            gameFont = content.Load<SpriteFont>(@"Fonts\gamefont");
            spriteBatch = new SpriteBatch(graphics.GraphicsDevice);
            spriteFont = content.Load<SpriteFont>(@"Fonts\DemoFont");
            skyboxEffect = content.Load<Effect>(@"Effects\effects");

            ground.Model = content.Load<Model>(@"Models\ground");
            boundingSphere.Model = content.Load<Model>(@"Models\sphere1uR");

            // Initialize buildings
            buildings = new Building[RoboXNA.Menus.RoboXNA.Default.buildingsNumber];
            int randomBuilding = random.Next(GameConstants.BuildingTypes);
            string buildingName = null;

            // Go over the different buildings, randomize their type and place them in the scene
            for (int index = 0; index < buildings.Length; index++)
            {
                switch (randomBuilding)
                {
                    case 0:
                        buildingName = "Models/House4/house4";
                        break;
                    case 1:
                        buildingName = "Models/House2/2";
                        break;
                    case 2:
                        buildingName = "Models/NewBuilding/building";
                        break;
                    case 3:
                        buildingName = "Models/House3/house3";
                        break;
                    default:
                        buildingName = "Models/House3/house3";
                        break;
                }
                buildings[index] = new Building();
                buildings[index].LoadContent(content, buildingName);
                randomBuilding = random.Next(GameConstants.BuildingTypes);
            }
            PlaceBuildings();

            //Initialize our player
            robotModel = new RobotObject();
            robotModel.LoadContent(content, "Models/dude");

            SkinningData skinningData = robotModel.Model.Tag as SkinningData;

            if (skinningData == null)
            {
                throw new InvalidOperationException("This model does not contain a SkinningData tag.");
            }

            // Create an animation player, and start decoding an animation clip.
            animationPlayer = new AnimationPlayer(skinningData);

            AnimationClip clip = skinningData.AnimationClips["Take 001"];

            animationPlayer.StartClip(clip);

            //// Set the MediaPlayer class to repeat its songs unless stopped
            MediaPlayer.IsRepeating = true;

            // once the load has finished, we use ResetElapsedTime to tell the game's
            // timing mechanism that we have just finished a very long frame, and that
            // it should not try to catch up.
            ScreenManager.Game.ResetElapsedTime();

            skyboxModel = LoadModel(@"Models\Skybox\skybox", out skyboxTextures);

            HandleSound();
        }

        /// <summary>
        /// Used to load the skybox model and textures
        /// </summary>
        /// <param name="assetName">The name of the skybox model</param>
        /// <param name="textures">An array of textures that will be mapped to the skybox model</param>
        /// <returns></returns>
        private Model LoadModel(string assetName, out Texture2D[] textures)
        {

            Model newModel = content.Load<Model>(assetName);
            textures = new Texture2D[newModel.Meshes.Count];
            int i = 0;

            foreach (ModelMesh mesh in newModel.Meshes)
            {
                foreach (BasicEffect currentEffect in mesh.Effects)
                {
                    textures[i++] = currentEffect.Texture;
                }
            }

            foreach (ModelMesh mesh in newModel.Meshes)
            {
                foreach (ModelMeshPart meshPart in mesh.MeshParts)
                {
                    meshPart.Effect = skyboxEffect.Clone();
                }
            }

            return newModel;
        }

        /// <summary>
        /// Places buildings across the level according to the number of buildings chosen in the options window 
        /// </summary>
        private void PlaceBuildings()
        {
            int min = GameConstants.MinDistance;
            int max = GameConstants.MaxDistance;
            Vector3 tempCenter;

            // Place buildings
            foreach (Building building in buildings)
            {
                building.Position = GenerateRandomPosition(min, max);
                tempCenter = building.BoundingSphere.Center;
                tempCenter.X = building.Position.X;
                tempCenter.Y = 0;
                tempCenter.Z = building.Position.Z;
                building.BoundingSphere = new BoundingSphere(tempCenter, building.BoundingSphere.Radius);
            }
        }

        private Vector3 GenerateRandomPosition(int min, int max)
        {
            int xValue, zValue;
            do
            {
                xValue = random.Next(min, max);
                zValue = random.Next(min, max);

                if (random.Next(100) % 2 == 0)
                {
                    xValue *= -1;
                }

                if (random.Next(100) % 2 == 0)
                {
                    zValue *= -1;
                }

            } while (IsOccupied(xValue, zValue));

            return new Vector3(xValue, 0, zValue);
        }

        private bool IsOccupied(int xValue, int zValue)
        {
            foreach (GameObject currentObj in buildings)
            {
                if (((int)(MathHelper.Distance(xValue, currentObj.Position.X)) < GameConstants.Spacing) &&
                    ((int)(MathHelper.Distance(zValue, currentObj.Position.Z)) < GameConstants.Spacing))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Unload graphics content used by the game.
        /// </summary>
        public override void UnloadContent()
        {
            content.Unload();
        }

        #endregion

        #region Update and Draw

        /// <summary>
        /// Updates the state of the game. This method checks the GameScreen.IsActive
        /// property, so the game will stop updating when the pause menu is active,
        /// or if you tab away to a different application.
        /// </summary>
        public override void Update(GameTime gameTime, bool otherScreenHasFocus,
                                                       bool coveredByOtherScreen)
        {
            base.Update(gameTime, otherScreenHasFocus, coveredByOtherScreen);

            if (!IsActive)
                return;

            ProcessKeyboard();

            robotModel.Update(currentKeyboardState, buildings, gameTime, animationPlayer);
            float aspectRatio = graphics.GraphicsDevice.Viewport.AspectRatio;
            gameCamera.Update(robotModel.ForwardDirection, robotModel.Position, aspectRatio);
        }

        /// <summary>
        /// Handles all the sound related issues for the robot walking sounds and ambient sounds
        /// </summary>
        private void HandleSound()
        {
            // Check if the user wanted sound or not in the options menu, and if he did, play the ambient sound
            if (RoboXNA.Menus.RoboXNA.Default.soundActive)
            {
                Song ourSong = content.Load<Song>(@"Sounds\birds");
                MediaPlayer.Volume = 100.0f;
                MediaPlayer.Play(ourSong);
            }
        }

        /// <summary>
        /// Draws the on-screen text
        /// </summary>
        private void DrawText()
        {
            StringBuilder buffer = new StringBuilder();

            if (displayHelp)
            {
                buffer.AppendLine("Press 'W' or UP to move the character forward");
                buffer.AppendLine("Press 'S' or DOWN to move the character backward");
                buffer.AppendLine("Press 'D' or RIGHT to turn the character to the right");
                buffer.AppendLine("Press 'A' or LEFT to turn the character to the left");
                buffer.AppendLine("Press SHIFT to make the character run");
                buffer.AppendLine("Press ALT + ENTER to enable / disable fullscreen mode");
                buffer.AppendLine();
                buffer.AppendLine("Press 'B' to toggle bounding sphere visibility");
                buffer.AppendLine("Press ESCAPE for menu");
                buffer.AppendLine();
                buffer.AppendLine("Press 'H' to hide");
            }
            else
            {
                buffer.AppendLine("Press 'H' to show help");
            }

            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
            spriteBatch.DrawString(spriteFont, buffer.ToString(), fontPos, Color.Yellow);
            spriteBatch.End();
        }

        private bool KeyJustPressed(Keys key)
        {
            return currentKeyboardState.IsKeyDown(key) && prevKeyboardState.IsKeyUp(key);
        }

        /// <summary>
        /// Handles keyboard input
        /// </summary>
        private void ProcessKeyboard()
        {
            prevKeyboardState = currentKeyboardState;
            currentKeyboardState = Keyboard.GetState();

            // Hide / Show the help text
            if (KeyJustPressed(Keys.H))
                displayHelp = !displayHelp;

            // Hide / Show the bounding spheres for all objects
            if (KeyJustPressed(Keys.B))
            {
                robotModel.ShowBoundingSphere = !robotModel.ShowBoundingSphere;

                foreach (Building building in buildings)
                {
                    building.ShowBoundingSphere = !building.ShowBoundingSphere;
                }
            }

            // Allows the game to exit
            if (KeyJustPressed(Keys.Escape))
            {
                ScreenManager.AddScreen(new PauseMenuScreen(), ControllingPlayer);
            }

            if (currentKeyboardState.IsKeyDown(Keys.LeftAlt) ||
                currentKeyboardState.IsKeyDown(Keys.RightAlt))
            {
                if (KeyJustPressed(Keys.Enter))
                    ToggleFullScreen();
            }
        }

        /// <summary>
        /// Allows the user to toggle between fullscreen mode and windowed mode
        /// </summary>
        private void ToggleFullScreen()
        {
            int newWidth = 0;
            int newHeight = 0;

            graphics.IsFullScreen = !graphics.IsFullScreen;

            if (graphics.IsFullScreen)
            {
                newWidth = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Width;
                newHeight = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Height;
            }
            else
            {
                newWidth = graphics.PreferredBackBufferWidth;
                newHeight = graphics.PreferredBackBufferHeight;
            }

            graphics.ApplyChanges();
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        public override void Draw(GameTime gameTime)
        {
            if (!IsActive)
                return;

            // Place the skybox drawing after this statement
            graphics.GraphicsDevice.Clear(Color.Black);

            DrawSkybox();

            // These lines handle the depth factor of the 3D world
            graphics.GraphicsDevice.BlendState = BlendState.Opaque;
            graphics.GraphicsDevice.DepthStencilState = DepthStencilState.Default;
            graphics.GraphicsDevice.SamplerStates[0] = SamplerState.LinearWrap;

            DrawTerrain(ground.Model);

            // Draw the buildings in the game
            foreach (Building building in buildings)
            {
                building.Draw(gameCamera.ViewMatrix, gameCamera.ProjectionMatrix);

                if (building.ShowBoundingSphere)
                {
                    building.DrawBoundingSphere(gameCamera.ViewMatrix, gameCamera.ProjectionMatrix, boundingSphere);
                }
            }

            // Draw the robot and its bounding sphere
            robotModel.Draw(gameCamera.ViewMatrix, gameCamera.ProjectionMatrix, animationPlayer);

            if (robotModel.ShowBoundingSphere)
            {
                robotModel.DrawBoundingSphere(gameCamera.ViewMatrix, gameCamera.ProjectionMatrix, boundingSphere);
            }

            DrawText();

            base.Draw(gameTime);
        }

        /// <summary>
        /// Draws the game terrain, a simple grid.
        /// </summary>
        /// <param name="model">Model representing the game playing field.</param>
        private void DrawTerrain(Model model)
        {
            foreach (ModelMesh mesh in model.Meshes)
            {
                foreach (BasicEffect effect in mesh.Effects)
                {
                    effect.EnableDefaultLighting();
                    effect.PreferPerPixelLighting = true;
                    effect.World = Matrix.Identity;

                    // Use the matrices provided by the game camera
                    effect.View = gameCamera.ViewMatrix;
                    effect.Projection = gameCamera.ProjectionMatrix;
                }
                mesh.Draw();
            }
        }

        /// <summary>
        /// Draws the environmental skybox
        /// </summary>
        private void DrawSkybox()
        {
            SamplerState ss = new SamplerState();
            ss.AddressU = TextureAddressMode.Clamp;
            ss.AddressV = TextureAddressMode.Clamp;
            graphics.GraphicsDevice.SamplerStates[0] = ss;

            DepthStencilState dss = new DepthStencilState();
            dss.DepthBufferEnable = false;
            graphics.GraphicsDevice.DepthStencilState = dss;

            Matrix[] skyboxTransforms = new Matrix[skyboxModel.Bones.Count];
            skyboxModel.CopyAbsoluteBoneTransformsTo(skyboxTransforms);
            int i = 0;

            foreach (ModelMesh mesh in skyboxModel.Meshes)
            {
                foreach (Effect currentEffect in mesh.Effects)
                {
                    Matrix worldMatrix = skyboxTransforms[mesh.ParentBone.Index] * Matrix.CreateTranslation(robotModel.Position);
                    Matrix projMatrix = gameCamera.ProjectionMatrix;
                    Matrix viewMatrix = gameCamera.ViewMatrix;
                    currentEffect.CurrentTechnique = currentEffect.Techniques["Textured"];
                    currentEffect.Parameters["xWorld"].SetValue(worldMatrix);
                    currentEffect.Parameters["xView"].SetValue(viewMatrix);
                    currentEffect.Parameters["xProjection"].SetValue(projMatrix);
                    currentEffect.Parameters["xTexture"].SetValue(skyboxTextures[i++]);
                }
                mesh.Draw();
            }

            dss = new DepthStencilState();
            dss.DepthBufferEnable = true;
            graphics.GraphicsDevice.DepthStencilState = dss;
        }

        #endregion
    }
}
