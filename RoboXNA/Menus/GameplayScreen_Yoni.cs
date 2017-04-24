#region Using Statements
using System;
using System.Collections.Generic;
using System.Threading;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using System.Reflection;
#endregion

namespace RoboXNA
{
    /// <summary>
    /// This screen implements the actual game logic. It is just a
    /// placeholder to get the idea across: you'll probably want to
    /// put some more interesting gameplay in here!
    /// </summary>
    class GameplayScreen : GameScreen
    {
        #region Fields

        ContentManager content;
        SpriteFont gameFont;

        Vector2 playerPosition = new Vector2(100, 100);
        Vector2 enemyPosition = new Vector2(100, 100);

        Random random = new Random();

        // float pauseAlpha;

        private const float ROBOT_FORWARD_SPEED = 280.0f;
        private const float ROBOT_HEADING_SPEED = 120.0f;

        private const float FLOOR_WIDTH = 1024.0f;
        private const float FLOOR_HEIGHT = 1024.0f;
        private const float FLOOR_TILE_U = 4.0f;
        private const float FLOOR_TILE_V = 4.0f;

        private float CAMERA_FOVX = MathHelper.ToRadians(45.0f);
        private const float CAMERA_ASPECT = 2.0f;
        private const float CAMERA_ZFAR = 100000.0f;
        private const float CAMERA_ZNEAR = 1.0f;
        private const float CAMERA_MAX_SPRING_CONSTANT = 100.0f;
        private const float CAMERA_MIN_SPRING_CONSTANT = 1.0f;

        private const float LIGHT_RADIUS = 1024.0f;
        private const float LIGHT_SPOT_INNER_CONE = 30.0f;
        private const float LIGHT_SPOT_OUTER_CONE = 100.0f;
        public static float ROBOT_RESIZE_FACTOR = 0.3f;
        private const float SKYBOX_RESIZE_FACTOR = 40.0f;

        private GraphicsDeviceManager graphics;
        private BoundingSphere bsRobot;
        private SpriteBatch spriteBatch;
        private SpriteFont spriteFont;
        private Effect effect;
        private Effect skyboxEffect;
        private Model robotModel;
        private Model envModel;
        private Model groundModel;
        private Texture2D robotTex;
        private ThirdPersonCamera camera;
        private Entity robotEntity;
        private VertexBuffer vertexBuffer;
        private Texture2D floorColorMap;
        private Vector2 fontPos;
        private Vector4 globalAmbient;
        private Matrix[] modelTransforms;
        private Matrix[] envTransforms;
        private Matrix[] groundTransforms;
        private Light light;
        private Song robotWalk;
        private Material material;
        private KeyboardState currentKeyboardState;
        private KeyboardState prevKeyboardState;
        private int frames;
        private int framesPerSecond;
        private float robotRadius;
        private TimeSpan elapsedTime = TimeSpan.Zero;
        private bool displayHelp;
        private Song ambientSound;
        private bool inMovement;

        // Skybox variables
        private Texture2D[] skyboxTextures;
        private Model skyboxModel;

        #endregion

        /// <summary>
        /// A light. This light structure is the same as the one defined in the
        /// blinn_phong.fx file.
        /// </summary>
        private struct Light
        {
            public Vector3 Direction;
            public Vector3 Position;
            public Vector4 Ambient;
            public Vector4 Diffuse;
            public Vector4 Specular;
            public float SpotInnerConeRadians;
            public float SpotOuterConeRadians;
            public float Radius;
        }

        /// <summary>
        /// A material. This material structure is the same as the one defined
        /// in the blinn_phong.fx file.
        /// </summary>
        private struct Material
        {
            public Vector4 Ambient;
            public Vector4 Diffuse;
            public Vector4 Emissive;
            public Vector4 Specular;
            public float Shininess;
        }

        #region Initialization


        /// <summary>
        /// Constructor.
        /// </summary>
        public GameplayScreen()
        {
            TransitionOnTime = TimeSpan.FromSeconds(1.5);
            TransitionOffTime = TimeSpan.FromSeconds(0.5);
        }

        private void Initialize()
        {
            graphics = Robo.graphics;

            // Setup frame buffer.
            graphics.SynchronizeWithVerticalRetrace = false;
            graphics.PreferMultiSampling = true;
            graphics.ApplyChanges();

            // Position the text.
            fontPos = new Vector2(1.0f, 1.0f);

            // Setup the initial input states.
            currentKeyboardState = Keyboard.GetState();

            // Setup a spot light.
            globalAmbient = new Vector4(0.0f, 0.0f, 0.0f, 1.0f);
            light.Direction = Vector3.Down;
            light.Position = new Vector3(0.0f, LIGHT_RADIUS * 0.5f, 0.0f);
            light.Ambient = Color.White.ToVector4();
            light.Diffuse = Color.White.ToVector4();
            light.Specular = Color.White.ToVector4();
            light.SpotInnerConeRadians = MathHelper.ToRadians(LIGHT_SPOT_INNER_CONE);
            light.SpotOuterConeRadians = MathHelper.ToRadians(LIGHT_SPOT_OUTER_CONE);
            light.Radius = LIGHT_RADIUS;

            // Setup a Lambert material.
            material.Ambient = new Vector4(0.2f, 0.2f, 0.2f, 1.0f);
            material.Diffuse = new Vector4(0.8f, 0.8f, 0.8f, 1.0f);
            material.Emissive = new Vector4(0.0f, 0.0f, 0.0f, 1.0f);
            material.Specular = new Vector4(0.0f, 0.0f, 0.0f, 1.0f);
            material.Shininess = 0.0f;

            // Create floor geometry.
            CreateFloor();

            // Determine the radius of the ball model.           
            BoundingSphere bounds = new BoundingSphere();

            foreach (ModelMesh mesh in robotModel.Meshes)
            {
                bounds = BoundingSphere.CreateMerged(bounds, mesh.BoundingSphere);
            }

            bsRobot = bounds.Transform(Matrix.Identity * ROBOT_RESIZE_FACTOR);

            robotRadius = bounds.Radius * (ROBOT_RESIZE_FACTOR * 2);

            // Setup the robot entity.
            robotEntity = new Entity();
            robotEntity.ConstrainToWorldYAxis = true;
            //robotEntity.Position = new Vector3(0.0f, 1.0f + (robotRadius * ROBOT_RESIZE_FACTOR), 0.0f);
            robotEntity.Position = new Vector3(0.0f, 400.0f, 0.0f);
            // Setup the camera.
            camera = new ThirdPersonCamera();
            camera.Perspective(CAMERA_FOVX, CAMERA_ASPECT,
                CAMERA_ZNEAR, CAMERA_ZFAR);
            camera.LookAt(new Vector3(0.0f, robotRadius * 6.0f, robotRadius * -12.0f),
                robotEntity.Position, Vector3.Up);
        }

        private void CreateFloor()
        {
            float w = FLOOR_WIDTH * 0.5f;
            float h = FLOOR_HEIGHT * 0.5f;

            Vector3[] positions =
            {
                new Vector3(-w, 0.0f, -h),
                new Vector3( w, 0.0f, -h),
                new Vector3(-w, 0.0f,  h),
                new Vector3( w, 0.0f,  h)
            };

            Vector2[] texCoords =
            {
                new Vector2(0.0f,         0.0f),
                new Vector2(FLOOR_TILE_U, 0.0f),
                new Vector2(0.0f,         FLOOR_TILE_V),
                new Vector2(FLOOR_TILE_U, FLOOR_TILE_V)
            };

            VertexPositionNormalTexture[] vertices =
            {
                new VertexPositionNormalTexture(
                    positions[0], Vector3.Up, texCoords[0]),
                new VertexPositionNormalTexture(
                    positions[1], Vector3.Up, texCoords[1]),
                new VertexPositionNormalTexture(
                    positions[2], Vector3.Up, texCoords[2]),
                new VertexPositionNormalTexture(
                    positions[3], Vector3.Up, texCoords[3]),
            };

            vertexBuffer = new VertexBuffer(graphics.GraphicsDevice,
                typeof(VertexPositionNormalTexture), vertices.Length,
                BufferUsage.WriteOnly);

            vertexBuffer.SetData(vertices);
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
        /// Load graphics content for the game.
        /// </summary>
        public override void LoadContent()
        {
            graphics = Robo.graphics;

            if (content == null)
            {
                content = new ContentManager(ScreenManager.Game.Services, "Content");
            }

            gameFont = content.Load<SpriteFont>(@"Fonts\gamefont");

            spriteBatch = new SpriteBatch(graphics.GraphicsDevice);
            spriteFont = content.Load<SpriteFont>(@"Fonts\DemoFont");
            floorColorMap = content.Load<Texture2D>(@"Textures\floor_decal_map");
            effect = content.Load<Effect>(@"Effects\blinn_phong");
            skyboxEffect = content.Load<Effect>(@"Effects\effects");
            robotModel = content.Load<Model>(@"Models\Android");
            envModel = content.Load<Model>(@"Models\univ");
            groundModel = content.Load<Model>(@"Models\ground");
            robotTex = content.Load<Texture2D>(@"Textures\green");
            robotWalk = content.Load<Song>(@"Sounds\robotWalk");

            // Set the MediaPlayer class to repeat its songs unless stopped
            MediaPlayer.IsRepeating = true;

            // once the load has finished, we use ResetElapsedTime to tell the game's
            // timing mechanism that we have just finished a very long frame, and that
            // it should not try to catch up.
            ScreenManager.Game.ResetElapsedTime();

            skyboxModel = LoadModel(@"Models\Skybox\skybox", out skyboxTextures);

            // Initialize the game objects
            Initialize();
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
            base.Update(gameTime, otherScreenHasFocus, false);

            if (!IsActive)
                return;

            ProcessKeyboard();
            UpdateRobot(gameTime);
            UpdateFrameRate(gameTime);
            //HandleSound();            // Comment / Uncomment to disable / enable sound effects
        }

        /// <summary>
        /// Handles all the sound related issues for the robot walking sounds and ambient sounds
        /// </summary>
        private void HandleSound()
        {
            if (inMovement)
            {
                if (MediaPlayer.State != MediaState.Playing)
                {
                    MediaPlayer.Play(robotWalk);
                }
            }
            else
            {
                if (MediaPlayer.State == MediaState.Playing)
                {
                    MediaPlayer.Stop();
                }
            }
        }

        /// <summary>
        /// Checks the keyboard state and updates the robot position and speed accordingly
        /// </summary>
        /// <param name="gameTime"></param>
        private void UpdateRobot(GameTime gameTime)
        {
            float heading = 0.0f;
            float forwardSpeed = 0.0f;

            if (currentKeyboardState.IsKeyDown(Keys.W) ||
                currentKeyboardState.IsKeyDown(Keys.Up))
            {
                forwardSpeed = -ROBOT_FORWARD_SPEED;
            }

            if (currentKeyboardState.IsKeyDown(Keys.S) ||
                currentKeyboardState.IsKeyDown(Keys.Down))
            {
                forwardSpeed = ROBOT_FORWARD_SPEED;
            }

            if (currentKeyboardState.IsKeyDown(Keys.D) ||
                currentKeyboardState.IsKeyDown(Keys.Right))
            {
                if (currentKeyboardState.IsKeyDown(Keys.Down) || (currentKeyboardState.IsKeyDown(Keys.Up))
                    || (currentKeyboardState.IsKeyDown(Keys.W)) || (currentKeyboardState.IsKeyDown(Keys.S)))
                {
                    heading = ROBOT_HEADING_SPEED;
                }
                else
                {
                    heading = -ROBOT_HEADING_SPEED;
                }
            }

            if (currentKeyboardState.IsKeyDown(Keys.A) ||
                currentKeyboardState.IsKeyDown(Keys.Left))
            {
                if (currentKeyboardState.IsKeyDown(Keys.Down) || (currentKeyboardState.IsKeyDown(Keys.Up))
                    || (currentKeyboardState.IsKeyDown(Keys.W)) || (currentKeyboardState.IsKeyDown(Keys.S)))
                {
                    heading = -ROBOT_HEADING_SPEED;
                }
                else
                {
                    heading = ROBOT_HEADING_SPEED;
                }
            }

            // If we have a key pressed, change the "inMovement" flag to true
            Keys[] k = currentKeyboardState.GetPressedKeys();
            if (k.Length > 0)
            {
                inMovement = true;
            }
            else
            {
                inMovement = false;
            }

            float floorBoundaryZ = FLOOR_HEIGHT * 0.5f - robotRadius;
            float floorBoundaryX = FLOOR_WIDTH * 0.5f - robotRadius;
            float elapsedTimeSec = (float)gameTime.ElapsedGameTime.TotalSeconds;
            float velocity = forwardSpeed * elapsedTimeSec;
            Vector3 newRobotPos = robotEntity.Position + robotEntity.Forward * velocity;

            // Collision detection check
            if (IsHit(newRobotPos, envModel))
            {
                forwardSpeed = 0.0f;
            }

            // I removed this "if" statement so that the robot will not stop at any point. IMPLEMENT COLLISION DETECTION HERE!
            //if ((newBallPos.Z > floorBoundaryZ) || (newBallPos.Z < -floorBoundaryZ) || (newBallPos.X > floorBoundaryX) || (newBallPos.X < -floorBoundaryX))
            //{
            //    forwardSpeed = 0.0f;
            //}

            // Update the robots's state.
            robotEntity.Velocity = new Vector3(0.0f, 0.0f, 3.0f * forwardSpeed);
            robotEntity.Orient(heading, 0.0f, 0.0f);
            robotEntity.Update(gameTime);

            // Then move the camera based on where the robot has moved to.
            // When the robot is moving backwards rotations are inverted to
            // match the direction of travel. Consequently the camera's
            // rotation needs to be inverted as well.
            camera.Rotate((forwardSpeed >= 0.0f) ? heading : -heading, 0.0f);
            camera.LookAt(newRobotPos);
            camera.Update(gameTime);
        }

        /// <summary>
        /// Used for collision detection. Checks if we hit the environmental model with our robot
        /// </summary>
        /// <param name="newRobotPos"></param>
        /// <param name="envModel"></param>
        /// <returns></returns>
        private bool IsHit(Vector3 newRobotPos, Model envModel)
        {
            newRobotPos.Y -= 200;
            foreach (ModelMesh m in envModel.Meshes)
            {
                if (m.BoundingSphere.Intersects(new BoundingSphere(newRobotPos, 10.0f)))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Updates the game frame rate count to be displayed on the on-screen counter
        /// </summary>
        /// <param name="gameTime"></param>
        private void UpdateFrameRate(GameTime gameTime)
        {
            elapsedTime += gameTime.ElapsedGameTime;

            if (elapsedTime > TimeSpan.FromSeconds(1))
            {
                elapsedTime -= TimeSpan.FromSeconds(1);
                framesPerSecond = frames;
                frames = 0;
            }
        }

        /// <summary>
        /// Increments the number of frames by one for each call to the Draw() method
        /// </summary>
        private void IncrementFrameCounter()
        {
            ++frames;
        }

        /// <summary>
        /// Draws the floor of the world
        /// </summary>
        private void DrawFloor()
        {
            //graphics.GraphicsDevice.SetVertexBuffer(vertexBuffer);

            //// Set shader matrix parameters.
            //effect.Parameters["worldMatrix"].SetValue(Matrix.Identity);
            //effect.Parameters["worldInverseTransposeMatrix"].SetValue(Matrix.Identity);
            //effect.Parameters["worldViewProjectionMatrix"].SetValue(camera.ViewProjectionMatrix);

            //// Set the shader camera position parameter.
            //effect.Parameters["cameraPos"].SetValue(camera.Position);

            //// Set the shader global ambiance parameters.
            //effect.Parameters["globalAmbient"].SetValue(globalAmbient);

            //// Set the shader lighting parameters.
            //effect.Parameters["light"].StructureMembers["dir"].SetValue(light.Direction);
            //effect.Parameters["light"].StructureMembers["pos"].SetValue(light.Position);
            //effect.Parameters["light"].StructureMembers["ambient"].SetValue(light.Ambient);
            //effect.Parameters["light"].StructureMembers["diffuse"].SetValue(light.Diffuse);
            //effect.Parameters["light"].StructureMembers["specular"].SetValue(light.Specular);
            //effect.Parameters["light"].StructureMembers["spotInnerCone"].SetValue(light.SpotInnerConeRadians);
            //effect.Parameters["light"].StructureMembers["spotOuterCone"].SetValue(light.SpotOuterConeRadians);
            //effect.Parameters["light"].StructureMembers["radius"].SetValue(light.Radius);

            //// Set the shader material parameters.
            //effect.Parameters["material"].StructureMembers["ambient"].SetValue(material.Ambient);
            //effect.Parameters["material"].StructureMembers["diffuse"].SetValue(material.Diffuse);
            //effect.Parameters["material"].StructureMembers["emissive"].SetValue(material.Emissive);
            //effect.Parameters["material"].StructureMembers["specular"].SetValue(material.Specular);
            //effect.Parameters["material"].StructureMembers["shininess"].SetValue(material.Shininess);

            //// Bind the texture map to the shader.
            //effect.Parameters["colorMapTexture"].SetValue(floorColorMap);

            //effect.CurrentTechnique = effect.Techniques["PerPixelSpotLighting"];

            //foreach (EffectPass pass in effect.CurrentTechnique.Passes)
            //{
            //    pass.Apply();
            //    graphics.GraphicsDevice.DrawPrimitives(PrimitiveType.TriangleStrip, 0, 2);
            //}

            if (groundTransforms == null)
            {
                groundTransforms = new Matrix[groundModel.Bones.Count];
            }

            groundModel.CopyAbsoluteBoneTransformsTo(groundTransforms);

            foreach (ModelMesh m in groundModel.Meshes)
            {
                foreach (BasicEffect e in m.Effects)
                {
                    e.PreferPerPixelLighting = true;
                    e.EnableDefaultLighting();

                    // The original environment model is much smaller than we want, so we increase its size by a constant factor
                    e.World = groundTransforms[m.ParentBone.Index] * Matrix.CreateScale(30.0f);
                    e.View = camera.ViewMatrix;
                    e.Projection = camera.ProjectionMatrix;
                }

                m.Draw();
            }
        }

        /// <summary>
        /// Draws the environment models
        /// </summary>
        private void DrawEnvironment()
        {
            if (envTransforms == null)
            {
                envTransforms = new Matrix[envModel.Bones.Count];
            }

            envModel.CopyAbsoluteBoneTransformsTo(envTransforms);

            foreach (ModelMesh m in envModel.Meshes)
            {
                foreach (BasicEffect e in m.Effects)
                {
                    e.PreferPerPixelLighting = true;
                    e.EnableDefaultLighting();

                    // The original environment model is much smaller than we want, so we increase its size by a constant factor
                    e.World = envTransforms[m.ParentBone.Index] * Matrix.CreateScale(30.0f);
                    e.View = camera.ViewMatrix;
                    e.Projection = camera.ProjectionMatrix;
                }

                m.Draw();
            }
        }

        /// <summary>
        /// Draws the robot for each frame in the game
        /// </summary>
        private void DrawRobot()
        {
            if (modelTransforms == null)
            {
                modelTransforms = new Matrix[robotModel.Bones.Count];
            }

            robotModel.CopyAbsoluteBoneTransformsTo(modelTransforms);

            foreach (ModelMesh m in robotModel.Meshes)
            {
                foreach (BasicEffect e in m.Effects)
                {
                    e.PreferPerPixelLighting = true;
                    e.EnableDefaultLighting();
                    e.TextureEnabled = true;
                    e.World = modelTransforms[m.ParentBone.Index] * robotEntity.WorldMatrix * Matrix.CreateScale(ROBOT_RESIZE_FACTOR);
                    e.View = camera.ViewMatrix;
                    e.Projection = camera.ProjectionMatrix;
                    e.Texture = robotTex;
                }

                m.Draw();
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
                buffer.AppendFormat("FPS: {0}\n", framesPerSecond);
                buffer.AppendLine("Press W or UP to move the robot forwards");
                buffer.AppendLine("Press S or DOWN to move the robot backwards");
                buffer.AppendLine("Press D or RIGHT to turn the robot to the right");
                buffer.AppendLine("Press A or LEFT to turn the robot to the left");
                buffer.AppendLine();
                buffer.AppendLine("Press SPACE to enable and disable the camera's spring system");
                buffer.AppendLine("Press + and - to change the camera's spring constant");
                buffer.AppendLine("Press ALT + ENTER to toggle full screen");
                buffer.AppendLine("Press ESCAPE for menu");
                buffer.AppendLine();
                buffer.AppendLine("Press H to hide");
            }
            else
            {
                buffer.AppendLine("Press H to show help");
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

            if (KeyJustPressed(Keys.H))
                displayHelp = !displayHelp;

            if (KeyJustPressed(Keys.Space))
                camera.EnableSpringSystem = !camera.EnableSpringSystem;

            if (currentKeyboardState.IsKeyDown(Keys.LeftAlt) ||
                currentKeyboardState.IsKeyDown(Keys.RightAlt))
            {
                if (KeyJustPressed(Keys.Enter))
                    ToggleFullScreen();
            }

            if (KeyJustPressed(Keys.Add))
            {
                float springConstant = camera.SpringConstant + 0.1f;

                springConstant = Math.Min(CAMERA_MAX_SPRING_CONSTANT, springConstant);
                camera.SpringConstant = springConstant;
            }

            if (KeyJustPressed(Keys.Subtract))
            {
                float springConstant = camera.SpringConstant - 0.1f;

                springConstant = Math.Max(CAMERA_MIN_SPRING_CONSTANT, springConstant);
                camera.SpringConstant = springConstant;
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

            camera.Perspective(CAMERA_FOVX, CAMERA_ASPECT,
                CAMERA_ZNEAR, CAMERA_ZFAR);
        }

        /// <summary>
        /// Lets the game respond to player input. Unlike the Update method,
        /// this will only be called when the gameplay screen is active.
        /// </summary>
        public override void HandleInput(InputState input)
        {
            if (input == null)
                throw new ArgumentNullException("input");

            // Look up inputs for the active player profile.
            int playerIndex = (int)ControllingPlayer.Value;

            KeyboardState keyboardState = input.CurrentKeyboardStates[playerIndex];
            GamePadState gamePadState = input.CurrentGamePadStates[playerIndex];

            // The game pauses either if the user presses the pause button, or if
            // they unplug the active gamepad. This requires us to keep track of
            // whether a gamepad was ever plugged in, because we don't want to pause
            // on PC if they are playing with a keyboard and have no gamepad at all!
            bool gamePadDisconnected = !gamePadState.IsConnected &&
                                       input.GamePadWasConnected[playerIndex];

            if (input.IsPauseGame(ControllingPlayer) || gamePadDisconnected)
            {
                ScreenManager.AddScreen(new PauseMenuScreen(), ControllingPlayer);
            }
            else
            {
                // Otherwise move the player position.
                Vector2 movement = Vector2.Zero;

                if (keyboardState.IsKeyDown(Keys.Left))
                    movement.X--;

                if (keyboardState.IsKeyDown(Keys.Right))
                    movement.X++;

                if (keyboardState.IsKeyDown(Keys.Up))
                    movement.Y--;

                if (keyboardState.IsKeyDown(Keys.Down))
                    movement.Y++;

                Vector2 thumbstick = gamePadState.ThumbSticks.Left;

                movement.X += thumbstick.X;
                movement.Y -= thumbstick.Y;

                if (movement.Length() > 1)
                    movement.Normalize();

                playerPosition += movement * 2;
            }
        }


        /// <summary>
        /// Draws the gameplay screen.
        /// </summary>
        public override void Draw(GameTime gameTime)
        {
            if (!IsActive)
                return;

            graphics.GraphicsDevice.Clear(Color.Black);

            graphics.GraphicsDevice.BlendState = BlendState.Opaque;
            graphics.GraphicsDevice.DepthStencilState = DepthStencilState.Default;
            graphics.GraphicsDevice.SamplerStates[0] = SamplerState.LinearWrap;

            DrawSkybox();
            DrawRobot();
            DrawFloor();
            DrawEnvironment();
            DrawText();

            base.Draw(gameTime);
            IncrementFrameCounter();
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
                    Matrix worldMatrix = Matrix.CreateScale(SKYBOX_RESIZE_FACTOR) * skyboxTransforms[mesh.ParentBone.Index] * Matrix.CreateTranslation(robotEntity.Position);
                    Matrix projMatrix = camera.ProjectionMatrix;
                    Matrix viewMatrix = camera.ViewMatrix;
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
