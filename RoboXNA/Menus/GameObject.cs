using System;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

using RoboXNAModel;

namespace RoboXNA.Menus
{
    class GameObject
    {
        public Model Model { get; set; }
        public Vector3 Position { get; set; }
        public BoundingSphere BoundingSphere { get; set; }
        public bool ShowBoundingSphere { get; set; }

        // Default constructor
        public GameObject()
        {
            Model = null;
            Position = Vector3.Zero;
            BoundingSphere = new BoundingSphere();
            ShowBoundingSphere = false;
        }

        /// <summary>
        /// Calculates the bounding sphere that contains the object
        /// </summary>
        /// <returns></returns>
        protected BoundingSphere CalculateBoundingSphere()
        {
            BoundingSphere mergedSphere = new BoundingSphere();
            BoundingSphere[] boundingSpheres;
            int index = 0;
            int meshCount = Model.Meshes.Count;

            boundingSpheres = new BoundingSphere[meshCount];
            if (meshCount > 0)
            {
                foreach (ModelMesh mesh in Model.Meshes)
                {
                    boundingSpheres[index++] = mesh.BoundingSphere;
                }

                mergedSphere = boundingSpheres[0];
            }

            if (meshCount > 1)
            {
                index = 1;
                do
                {
                    mergedSphere = BoundingSphere.CreateMerged(mergedSphere, boundingSpheres[index]);
                    index++;
                } while (index < Model.Meshes.Count);
            }
            mergedSphere.Center.Y = 0;
            return mergedSphere;
        }

        /// <summary>
        /// Draws the corresponding bounding sphere around the object (Mainly for debugging)
        /// </summary>
        /// <param name="view"></param>
        /// <param name="projection"></param>
        /// <param name="boundingSphereModel"></param>
        internal void DrawBoundingSphere(Matrix view, Matrix projection, GameObject boundingSphereModel)
        {
            Matrix scaleMatrix = Matrix.CreateScale(BoundingSphere.Radius);
            Matrix translateMatrix = Matrix.CreateTranslation(BoundingSphere.Center);
            Matrix worldMatrix = scaleMatrix * translateMatrix;

            foreach (ModelMesh mesh in boundingSphereModel.Model.Meshes)
            {
                foreach (BasicEffect effect in mesh.Effects)
                {
                    effect.World = worldMatrix;
                    effect.View = view;
                    effect.Projection = projection;
                }
                mesh.Draw();
            }
        }
    }

    /// <summary>
    /// Represents every obstacle in the Robots' path
    /// </summary>
    class Building : GameObject
    {
        public string BuildingType { get; set; }

        public Building()
            : base()
        {
            BuildingType = null;
        }

        public void LoadContent(ContentManager content, string modelName)
        {
            Model = content.Load<Model>(modelName);
            BuildingType = modelName;
            Position = Vector3.Down;
            BoundingSphere = CalculateBoundingSphere();

            BoundingSphere scaledSphere;
            scaledSphere = BoundingSphere;
            scaledSphere.Radius *= GameConstants.BuildingBoundingSphereFactor;
            BoundingSphere = new BoundingSphere(scaledSphere.Center, scaledSphere.Radius);
        }

        public void Draw(Matrix view, Matrix projection)
        {
            Matrix translateMatrix = Matrix.CreateTranslation(Position);
            Matrix worldMatrix = translateMatrix;

            foreach (ModelMesh mesh in Model.Meshes)
            {
                foreach (BasicEffect effect in mesh.Effects)
                {
                    effect.World = worldMatrix;
                    effect.View = view;
                    effect.Projection = projection;

                    effect.EnableDefaultLighting();
                    effect.PreferPerPixelLighting = true;

                    effect.SpecularColor = new Vector3(0.25f);
                    effect.SpecularPower = 16;
                }
                mesh.Draw();
            }
        }
    }

    class RobotObject : GameObject
    {
        public float ForwardDirection { get; set; }
        public int MaxRange { get; set; }
        public bool IsWalking { get; set; }

        public RobotObject()
            : base()
        {
            ForwardDirection = 0.0f;
            MaxRange = GameConstants.MaxRange;
            IsWalking = true;
        }

        /// <summary>
        /// Loads the content needed for the character model
        /// </summary>
        public void LoadContent(ContentManager content, string modelName)
        {
            Model = content.Load<Model>(modelName);
            BoundingSphere = CalculateBoundingSphere();

            BoundingSphere scaledSphere;
            scaledSphere = BoundingSphere;
            scaledSphere.Radius *= GameConstants.CharacterBoundingSphereFactor * GameConstants.CharacterSize;
            BoundingSphere = new BoundingSphere(scaledSphere.Center, scaledSphere.Radius);
        }

        /// <summary>
        /// Draws the character model
        /// </summary>
        public void Draw(Matrix view, Matrix projection, AnimationPlayer animationPlayer)
        {
            Matrix worldMatrix = Matrix.Identity;
            Matrix rotationYMatrix = Matrix.CreateRotationY(ForwardDirection);
            Matrix translateMatrix = Matrix.CreateTranslation(Position);

            worldMatrix = rotationYMatrix * translateMatrix;

            Matrix[] bones = animationPlayer.GetSkinTransforms();

            // Render and draw the model
            foreach (ModelMesh mesh in Model.Meshes)
            {
                foreach (SkinnedEffect effect in mesh.Effects)
                {
                    effect.SetBoneTransforms(bones);

                    effect.World = worldMatrix;
                    effect.View = view;
                    effect.Projection = projection;

                    effect.EnableDefaultLighting();

                    effect.SpecularColor = new Vector3(0.25f);
                    effect.SpecularPower = 16;
                }
                mesh.Draw();
            }
        }

        /// <summary>
        /// Updates the animation according to the game time (this enables the walking animation)
        /// </summary>
        public void Update(KeyboardState keyboardState, Building[] buildings, GameTime gameTime, AnimationPlayer animationPlayer)
        {
            Vector3 futurePosition = Position;
            float turnAmount = 0;

            // Update the robot position according to the keyboard input
            if (keyboardState.IsKeyDown(Keys.A) || keyboardState.IsKeyDown(Keys.Left))
            {
                turnAmount = 1;
            }
            else if (keyboardState.IsKeyDown(Keys.D) || keyboardState.IsKeyDown(Keys.Right))
            {
                turnAmount = -1;
            }
            ForwardDirection += turnAmount * GameConstants.TurnSpeed;
            Matrix orientationMatrix = Matrix.CreateRotationY(ForwardDirection);
            Vector3 movement = Vector3.Zero;

            if (keyboardState.IsKeyDown(Keys.W) || keyboardState.IsKeyDown(Keys.Up))
            {
                IsWalking = true;
                movement.Z = 1;
            }
            else if (keyboardState.IsKeyDown(Keys.S) || keyboardState.IsKeyDown(Keys.Down))
            {
                IsWalking = true;
                movement.Z = -1;
            }
            else
            {
                IsWalking = false;
            }

            Vector3 speed = Vector3.Transform(movement, orientationMatrix);

            // For demonstration purposes, if the user presses either of the "Shift" buttons, make the character
            // go faster on the scene.
            if (keyboardState.IsKeyDown(Keys.LeftShift) || keyboardState.IsKeyDown(Keys.RightShift))
            {
                speed *= GameConstants.Velocity + GameConstants.RunningSpeed;
            }
            else
            {
                speed *= GameConstants.Velocity;
            }

            futurePosition = Position + speed;

            if (ValidateMovement(futurePosition, buildings))
            {
                Position = futurePosition;

                BoundingSphere updatedSphere;
                updatedSphere = BoundingSphere;

                updatedSphere.Center.X = Position.X;
                updatedSphere.Center.Z = Position.Z;
                BoundingSphere = new BoundingSphere(updatedSphere.Center, updatedSphere.Radius);
            }

            if (IsWalking)
            {
                animationPlayer.Update(gameTime.ElapsedGameTime, true,
                    Matrix.CreateScale(GameConstants.CharacterSize) * Matrix.CreateRotationY((float)Math.PI));
            }
            else
            {
                animationPlayer.Update(TimeSpan.Zero, true,
                    Matrix.CreateScale(GameConstants.CharacterSize) * Matrix.CreateRotationY((float)Math.PI));
            }
        }

        /// <summary>
        /// Rescales a set of Matrices representing the model's bones to another scale
        /// </summary>
        /// <param name="bones">The array of bone matrices to scale</param>
        /// <param name="p">The new scale factor</param>
        /// <returns></returns>
        private Matrix[] RescaleBones(Matrix[] bones, float p)
        {
            Matrix[] localMatrixArray = bones;

            for (int i = 0; i < bones.Length; i++)
            {
                localMatrixArray[i] *= Matrix.CreateScale(p);
            }

            return localMatrixArray;
        }

        /// <summary>
        /// Makes sure that the future position is valid for movement.
        /// </summary>
        private bool ValidateMovement(Vector3 futurePosition, Building[] buildings)
        {
            BoundingSphere futureBoundingSphere = BoundingSphere;
            futureBoundingSphere.Center.X = futurePosition.X;
            futureBoundingSphere.Center.Z = futurePosition.Z;

            //Don't allow off-terrain walking
            if ((Math.Abs(futurePosition.X) > MaxRange) || (Math.Abs(futurePosition.Z) > MaxRange))
                return false;

            //Don't allow walking through a building
            if (CheckForBuildingCollision(futureBoundingSphere, buildings))
                return false;

            return true;
        }

        /// <summary>
        /// Checks whether we're colliding with buildings in the scene.
        /// </summary>
        /// <param name="robotBoundingSphere">The characters' bounding sphere</param>
        /// <param name="buildings">Buildings in the scene</param>
        /// <returns>True if colliding, false otherwise</returns>
        private bool CheckForBuildingCollision(BoundingSphere robotBoundingSphere, Building[] buildings)
        {
            for (int curBuilding = 0; curBuilding < buildings.Length; curBuilding++)
            {
                if (robotBoundingSphere.Intersects(buildings[curBuilding].BoundingSphere))
                    return true;
            }
            return false;
        }
    }


    class Camera
    {
        public Vector3 AvatarHeadOffset { get; set; }
        public Vector3 TargetOffset { get; set; }
        public Matrix ViewMatrix { get; set; }
        public Matrix ProjectionMatrix { get; set; }

        public Camera()
        {
            AvatarHeadOffset = new Vector3(0, 7, -15);
            TargetOffset = new Vector3(0, 5, 0);
            ViewMatrix = Matrix.Identity;
            ProjectionMatrix = Matrix.Identity;
        }

        public void Update(float avatarYaw, Vector3 position, float aspectRatio)
        {
            Matrix rotationMatrix = Matrix.CreateRotationY(avatarYaw);

            Vector3 transformedheadOffset = Vector3.Transform(AvatarHeadOffset, rotationMatrix);
            Vector3 transformedReference = Vector3.Transform(TargetOffset, rotationMatrix);

            Vector3 cameraPosition = position + transformedheadOffset;
            Vector3 cameraTarget = position + transformedReference;

            //Calculate the camera's view and projection matrices based on current values.
            ViewMatrix = Matrix.CreateLookAt(cameraPosition, cameraTarget, Vector3.Up);
            ProjectionMatrix = Matrix.CreatePerspectiveFieldOfView(MathHelper.ToRadians(GameConstants.ViewAngle),
                aspectRatio, GameConstants.NearClip, GameConstants.FarClip);
        }
    }
}
