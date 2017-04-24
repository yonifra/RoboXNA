//-----------------------------------------------------------------------------
// Copyright (c) 2008-2011 dhpoware. All Rights Reserved.
//
// Permission is hereby granted, free of charge, to any person obtaining a
// copy of this software and associated documentation files (the "Software"),
// to deal in the Software without restriction, including without limitation
// the rights to use, copy, modify, merge, publish, distribute, sublicense,
// and/or sell copies of the Software, and to permit persons to whom the
// Software is furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS
// OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS
// IN THE SOFTWARE.
//-----------------------------------------------------------------------------

using Microsoft.Xna.Framework;

namespace RoboXNA
{
    /// <summary>
    /// The Entity class tracks an object's position and orientation in 3D
    /// space.
    /// 
    /// <para>An Entity can be rotated and oriented. By default all heading
    /// changes will be about the entity's local Y axis. The
    /// ConstrainToWorldYAxis property forces all heading changes to be about
    /// the world Y axis rather that the Entity's local Y axis.</para>
    /// 
    /// <para>Changing the Entity's orientation using the Orient() method will
    /// update the direction the Entity is facing. This will also update the
    /// Entity's right, up, and forward vectors.</para>
    /// 
    /// <para>Rotating an Entity using the Rotate() method will not update the
    /// direction the Entity is facing. The Rotate() method is provided as a
    /// means to animate the Entity.</para>
    /// 
    /// <para>For example consider an Entity that represents an asteroid. The
    /// Rotate() method would be used to tumble the asteroid as it moves
    /// through space. The Orient() method would be used to alter the direction
    /// the asteroid is moving.</para>
    /// </summary>
    public class Entity
    {
        private Matrix worldMatrix;
        private Quaternion orientation;
        private Quaternion rotation;
        private Vector3 right;
        private Vector3 up;
        private Vector3 forward;
        private Vector3 position;
        private Vector3 velocity;
        private Vector3 eulerOrient;
        private Vector3 eulerRotate;
        private bool constrainedToWorldYAxis;

        #region Public Methods

        /// <summary>
        /// Constructs a new Entity object. The new Entity is located at the
        /// world origin looking down the world negative Z axis.
        /// </summary>
        public Entity()
        {
            worldMatrix = Matrix.Identity;
            orientation = Quaternion.Identity;
            rotation = Quaternion.Identity;
            
            right = Vector3.Right;
            up = Vector3.Up;
            forward = Vector3.Forward;
            
            position = Vector3.Zero;
            velocity = Vector3.Zero;
            eulerOrient = Vector3.Zero;
            eulerRotate = Vector3.Zero;
            
            constrainedToWorldYAxis = false;
        }

        /// <summary>
        /// Change the direction the entity is facing. This method directly
        /// affects the orientation of the entity's right, up, and forward
        /// axes. This method is usually called in response to the user's input
        /// if the entity is able to be moved by the user.
        /// </summary>
        /// <param name="headingDegrees">Y axis rotation.</param>
        /// <param name="pitchDegrees">X axis rotation.</param>
        /// <param name="rollDegrees">Z axis rotation.</param>
        public void Orient(float headingDegrees, float pitchDegrees, float rollDegrees)
        {
            eulerOrient.X += pitchDegrees;
            eulerOrient.Y += headingDegrees;
            eulerOrient.Z += rollDegrees;

            if (eulerOrient.X > 360.0f)
                eulerOrient.X -= 360.0f;

            if (eulerOrient.X < -360.0f)
                eulerOrient.X += 360.0f;

            if (eulerOrient.Y > 360.0f)
                eulerOrient.Y -= 360.0f;

            if (eulerOrient.Y < -360.0f)
                eulerOrient.Y += 360.0f;

            if (eulerOrient.Z > 360.0f)
                eulerOrient.Z -= 360.0f;

            if (eulerOrient.Z < -360.0f)
                eulerOrient.Z += 360.0f;
        }
        
        /// <summary>
        /// Rotate() does not change the direction the entity is facing. This
        /// method allows the entity to freely spin around without affecting
        /// its orientation and its right, up, and forwards vectors. For
        /// example, if this entity is a planet then Rotate() is used to spin
        /// the planet on its Y axis. If this entity is an asteroid, then
        /// Rotate() is used to tumble the asteroid as it moves in space.
        /// </summary>
        /// <param name="headingDegrees">Y axis rotation.</param>
        /// <param name="pitchDegrees">X axis rotation.</param>
        /// <param name="rollDegrees">Z axis rotation.</param>
        public void Rotate(float headingDegrees, float pitchDegrees, float rollDegrees)
        {
            eulerRotate.X += pitchDegrees;
            eulerRotate.Y += headingDegrees;
            eulerRotate.Z += rollDegrees;

            if (eulerRotate.X > 360.0f)
                eulerRotate.X -= 360.0f;

            if (eulerRotate.X < -360.0f)
                eulerRotate.X += 360.0f;

            if (eulerRotate.Y > 360.0f)
                eulerRotate.Y -= 360.0f;

            if (eulerRotate.Y < -360.0f)
                eulerRotate.Y += 360.0f;

            if (eulerRotate.Z > 360.0f)
                eulerRotate.Z -= 360.0f;

            if (eulerRotate.Z < -360.0f)
                eulerRotate.Z += 360.0f;
        }

        /// <summary>
        /// Call this method once per frame to update the internal state of the
        /// Entity object.
        /// </summary>
        /// <param name="gameTime">The elapsed game time.</param>
        public void Update(GameTime gameTime)
        {      
            float elapsedTimeSec = (float)gameTime.ElapsedGameTime.TotalSeconds;

            Vector3 velocityElapsed;
            Vector3 eulerOrientElapsed;
            Vector3 eulerRotateElapsed;
            Vector3 oldPos;
            Vector3 heading;
            Quaternion temp;

            velocityElapsed = velocity * elapsedTimeSec;
            eulerOrientElapsed = eulerOrient * elapsedTimeSec;
            eulerRotateElapsed = eulerRotate * elapsedTimeSec;
            
            // Update the entity's position.
            ExtractAxes();

            oldPos = position;

            position += right * velocityElapsed.X;
            position += up * velocityElapsed.Y;
            position += forward * velocityElapsed.Z;

            heading = position - oldPos;
            heading.Normalize();

            // Update the entity's orientation.
            
            temp = EulerToQuaternion(orientation, eulerOrientElapsed.Y,
                    eulerOrientElapsed.X, eulerOrientElapsed.Z);

            // When moving backwards invert rotations to match direction of travel.
            
            if (Vector3.Dot(heading, forward) < 0.0f)
                temp = Quaternion.Inverse(temp);

            orientation = Quaternion.Concatenate(orientation, temp);
            orientation.Normalize();

            // Update the entity's free rotation.
            
            temp = EulerToQuaternion(rotation, eulerRotateElapsed.Y,
                    eulerRotateElapsed.X, eulerRotateElapsed.Z);
            
            rotation = Quaternion.Concatenate(rotation, temp);
            rotation.Normalize();

            // Update the entity's world matrix.
            
            temp = Quaternion.Concatenate(rotation, orientation);
            temp.Normalize();
            
            Matrix.CreateFromQuaternion(ref temp, out worldMatrix);

            // We take into consideration the robot resize factor that might be dynamic, so we divide the x and z
            // coordinates of the robot in the resize factor and set it in the last column of the World Matrix
            worldMatrix.M41 = position.X / GameplayScreen.ROBOT_RESIZE_FACTOR;
            worldMatrix.M42 = position.Y;
            worldMatrix.M43 = position.Z / GameplayScreen.ROBOT_RESIZE_FACTOR;

            // Clear the cached Euler rotations and velocity for this frame.

            velocity.X = velocity.Y = velocity.Z = 0.0f;
            eulerOrient.X = eulerOrient.Y = eulerOrient.Z = 0.0f;
            eulerRotate.X = eulerRotate.Y = eulerRotate.Z = 0.0f;
        }

        #endregion

        #region Private Methods
        
        /// <summary>
        /// Construct a quaternion from an Euler transformation. We do this
        /// rather than using the standard XNA math routines to support
        /// constraining heading changes to the world Y axis.
        /// </summary>
        /// <param name="q">The quaternion to apply the rotations to.</param>
        /// <param name="headingDegrees">Y axis rotation.</param>
        /// <param name="pitchDegrees">X axis rotation.</param>
        /// <param name="rollDegrees">Z axis rotation.</param>
        /// <returns>
        /// A quaternion containing the initially passed in quaternion with
        /// the Euler transformation applied to it.
        /// </returns>
        private Quaternion EulerToQuaternion(Quaternion q,
                                             float headingDegrees,
                                             float pitchDegrees,
                                             float rollDegrees)
        {
            Matrix m = Matrix.CreateFromQuaternion(q);
            
            Quaternion result = Quaternion.Identity;
            Quaternion rotation = Quaternion.Identity;
            
            Vector3 localXAxis = new Vector3(m.M11, m.M12, m.M13);
            Vector3 localYAxis = new Vector3(m.M21, m.M22, m.M23);
            Vector3 localZAxis = new Vector3(m.M31, m.M32, m.M33);
            
            float heading = MathHelper.ToRadians(headingDegrees);
            float pitch = MathHelper.ToRadians(pitchDegrees);
            float roll = MathHelper.ToRadians(rollDegrees);
            
            if (heading != 0.0f)
            {
                Vector3 yAxis = (constrainedToWorldYAxis) ? Vector3.Up : localYAxis;
                
                Quaternion.CreateFromAxisAngle(ref yAxis, heading, out rotation);
                result = Quaternion.Concatenate(result, rotation);
            }
            
            if (pitch != 0.0f)
            {
                Quaternion.CreateFromAxisAngle(ref localXAxis, pitch, out rotation);
                result = Quaternion.Concatenate(result, rotation);
            }
            
            if (roll != 0.0f)
            {
                Quaternion.CreateFromAxisAngle(ref localZAxis, roll, out rotation);
                result = Quaternion.Concatenate(result, rotation);
            }
            
            return result;
        }
        
        /// <summary>
        /// Extracts the local axes from the Entity's current orientation.
        /// </summary>
        private void ExtractAxes()
        {
            Matrix m = Matrix.CreateFromQuaternion(orientation);

            right = new Vector3(m.M11, m.M12, m.M13);
            right.Normalize();

            up = new Vector3(m.M21, m.M22, m.M23);
            up.Normalize();

            forward = new Vector3(-m.M31, -m.M32, -m.M33);
            forward.Normalize();
        }
        
        #endregion

        #region Properties
        
        /// <summary>
        /// Constraining rotations to the world Y axis means that all heading
        /// changes are applied to the world Y axis rather than the entity's
        /// local Y axis.
        /// </summary>
        public bool ConstrainToWorldYAxis
        {
            get { return constrainedToWorldYAxis; }
            set { constrainedToWorldYAxis = value; }
        }
        
        public Vector3 Forward
        {
            get { return forward; }
        }
        
        public Vector3 Position
        {
            get { return position; }
            set { position = value; }
        }
        
        public Vector3 Right
        {
            get { return right; }
        }
        
        public Vector3 Up
        {
            get { return up; }
        }
        
        public Vector3 Velocity
        {
            get { return velocity; }
            set { velocity = value; }
        }
        
        public Matrix WorldMatrix
        {
            get { return worldMatrix; }
            
            set
            {
                worldMatrix = value;
                Quaternion.CreateFromRotationMatrix(ref worldMatrix, out orientation);
                position = new Vector3(worldMatrix.M41, worldMatrix.M42, worldMatrix.M43);
                ExtractAxes();
            }
        }
        
        #endregion
    }
}