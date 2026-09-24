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
        public Image Visual { get; private set; }

        private readonly string[] enemySprites =
        {
            "alien1.png",
            "alien2.png",
            "alien3.png",
            "monster.png",
            "ufo.png"
        };

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

            Visual = new Image
            {
                Source = enemySprites[random.Next(enemySprites.Length)],
                WidthRequest = Size,
                HeightRequest = Size,
                Aspect = Aspect.AspectFit
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

        // Changes the enemy's direction to a new random direction.
        private void ChangeDirection()
        {
            // Generate random angle
            double angle = random.NextDouble() * 2 * Math.PI;

            // Convert to velocity components
            velocityX = Math.Cos(angle) * speed;
            velocityY = Math.Sin(angle) * speed;
        }

        // Alternative update method: Makes the enemy move towards a target (like the player).
        // Maybe different types of enemies could use this behaviour.
        // This is not currently used but demonstrates how to create homing enemies.
        public void MoveTowards(double targetX, double targetY)
        {
            double dx = targetX - X;
            double dy = targetY - Y;
            double distance = Math.Sqrt(dx * dx + dy * dy);

            if (distance > 0)
            {
                velocityX = (dx / distance) * speed;
                velocityY = (dy / distance) * speed;
            }
        }
    }
}
