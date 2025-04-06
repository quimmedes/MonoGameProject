using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;

namespace MonoGameProject.Camera
{
    public class FreeCamera
    {
        private Vector3 _position;
        private Vector3 _target;
        private Vector3 _up;
        private Vector3 _right;
        private Vector3 _forward;

        private float _yaw;
        private float _pitch;
        private float _moveSpeed;
        private float _rotationSpeed;
        private float _nearPlane;
        private float _farPlane;
        private float _aspectRatio;
        private float _fieldOfView;

        private MouseState _prevMouseState;
        private KeyboardState _prevKeyboardState;

        private Matrix _view;
        private Matrix _projection;

        public Vector3 Position => _position;
        public Vector3 Forward => _forward;
        public Matrix View => _view;
        public Matrix Projection => _projection;

        public FreeCamera(GraphicsDevice graphicsDevice, Vector3 position, Vector3 target, float moveSpeed = 50.0f, float rotationSpeed = 0.005f)
        {
            _position = position;
            _target = target;
            _up = Vector3.Up;
            _moveSpeed = moveSpeed;
            _rotationSpeed = rotationSpeed;
            _nearPlane = 0.1f;
            _farPlane = 3000f;
            _aspectRatio = graphicsDevice.Viewport.AspectRatio;
            _fieldOfView = MathHelper.PiOver4;

            // Calculate initial forward, right, and up vectors
            _forward = Vector3.Normalize(_target - _position);
            _right = Vector3.Normalize(Vector3.Cross(_forward, Vector3.Up));
            _up = Vector3.Normalize(Vector3.Cross(_right, _forward));

            // Calculate initial yaw and pitch
            _yaw = (float)Math.Atan2(_forward.X, _forward.Z);
            _pitch = (float)Math.Asin(_forward.Y);

            // Initialize previous mouse and keyboard states
            _prevMouseState = Mouse.GetState();
            _prevKeyboardState = Keyboard.GetState();

            // Calculate view and projection matrices
            UpdateViewMatrix();
            UpdateProjectionMatrix();
        }

        public void Update(GameTime gameTime, Terrain.TerrainManager terrainManager)
        {
            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
            MouseState currentMouseState = Mouse.GetState();
            KeyboardState currentKeyboardState = Keyboard.GetState();

            // Handle mouse rotation
            if (currentMouseState.RightButton == ButtonState.Pressed)
            {
                float deltaX = currentMouseState.X - _prevMouseState.X;
                float deltaY = currentMouseState.Y - _prevMouseState.Y;

                _yaw -= deltaX * _rotationSpeed;
                _pitch -= deltaY * _rotationSpeed;

                // Clamp pitch to avoid gimbal lock
                _pitch = MathHelper.Clamp(_pitch, -MathHelper.PiOver2 + 0.1f, MathHelper.PiOver2 - 0.1f);

                // Update forward vector based on yaw and pitch
                _forward.X = (float)(Math.Sin(_yaw) * Math.Cos(_pitch));
                _forward.Y = (float)Math.Sin(_pitch);
                _forward.Z = (float)(Math.Cos(_yaw) * Math.Cos(_pitch));
                _forward = Vector3.Normalize(_forward);

                // Recalculate right and up vectors
                _right = Vector3.Normalize(Vector3.Cross(_forward, Vector3.Up));
                _up = Vector3.Normalize(Vector3.Cross(_right, _forward));
            }

            // Handle keyboard movement
            Vector3 movement = Vector3.Zero;

            if (currentKeyboardState.IsKeyDown(Keys.W))
                movement += _forward;
            if (currentKeyboardState.IsKeyDown(Keys.S))
                movement -= _forward;
            if (currentKeyboardState.IsKeyDown(Keys.A))
                movement -= _right;
            if (currentKeyboardState.IsKeyDown(Keys.D))
                movement += _right;
            if (currentKeyboardState.IsKeyDown(Keys.Space))
                movement += Vector3.Up;
            if (currentKeyboardState.IsKeyDown(Keys.LeftShift))
                movement -= Vector3.Up;

            // Normalize movement vector if not zero
            if (movement != Vector3.Zero)
            {
                movement.Normalize();
                movement *= _moveSpeed * deltaTime;
                _position += movement;
            }

            // Adjust camera height based on terrain if Q is pressed (terrain following)
            if (currentKeyboardState.IsKeyDown(Keys.Q) && terrainManager != null)
            {
                float terrainHeight = terrainManager.GetHeightAt(_position.X, _position.Z);
                _position.Y = terrainHeight + 2.0f; // 2 units above terrain
            }

            // Adjust movement speed with mouse wheel
            int scrollDelta = currentMouseState.ScrollWheelValue - _prevMouseState.ScrollWheelValue;
            if (scrollDelta != 0)
            {
                _moveSpeed += scrollDelta * 0.01f;
                _moveSpeed = MathHelper.Clamp(_moveSpeed, 1.0f, 200.0f);
            }

            // Update view matrix
            UpdateViewMatrix();

            // Update previous states
            _prevMouseState = currentMouseState;
            _prevKeyboardState = currentKeyboardState;
        }

        private void UpdateViewMatrix()
        {
            _target = _position + _forward;
            _view = Matrix.CreateLookAt(_position, _target, _up);
        }

        private void UpdateProjectionMatrix()
        {
            _projection = Matrix.CreatePerspectiveFieldOfView(_fieldOfView, _aspectRatio, _nearPlane, _farPlane);
        }

        public void SetAspectRatio(float aspectRatio)
        {
            _aspectRatio = aspectRatio;
            UpdateProjectionMatrix();
        }
    }
}
