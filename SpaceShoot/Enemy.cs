using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpaceShoot
{
    public class Enemy
    {
        public double X { get; private set; }
        public double Y { get; private set; }
        public double Size { get; private set; } = 30;
        public BoxView Visual { get; private set; }

        private double velocityX;
        private double velocityY;
        private double speed = 2.0;
        private Random random = new Random();
        private DateTime lastDirectionChange;
        private int directionChangeInterval = 2000; // Change direction every 2 seconds

        public Enemy(double x, double y)
        {
            X = x;
            Y = y;

            Visual = new BoxView
            {
                Color = Colors.Red,
                WidthRequest = Size,
                HeightRequest = Size,
                CornerRadius = Size / 2
            };
        }

        public void Update(double screenWidth, double screenHeight)
        {
            // Move in current direction
            X += velocityX;
            Y += velocityY;

            // Bounce off walls
            if (X < Size / 2 || X > screenWidth - Size / 2)
            {
                velocityX = -velocityX;
                X = Math.Clamp(X, Size / 2, screenWidth - Size / 2);
            }

            if (Y < Size / 2 || Y > screenHeight - Size / 2)
            {
                velocityY = -velocityY;
                Y = Math.Clamp(Y, Size / 2, screenHeight - Size / 2);
            }

            // Periodically change direction for more interesting movement
            if ((DateTime.Now - lastDirectionChange).TotalMilliseconds > directionChangeInterval)
            {
                ChangeDirection();
                lastDirectionChange = DateTime.Now;
            }
        }

        private void ChangeDirection()
        {
            double angle = random.NextDouble() * 2 * Math.PI;

            velocityX = Math.Cos(angle) * speed;
            velocityY = Math.Sin(angle) * speed;
        }
    }
}
