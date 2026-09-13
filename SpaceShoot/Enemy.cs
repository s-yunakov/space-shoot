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
            X += velocityX;
            Y += velocityY;
        }

        private void ChangeDirection()
        {
            double angle = random.NextDouble() * 2 * Math.PI;

            velocityX = Math.Cos(angle) * speed;
            velocityY = Math.Sin(angle) * speed;
        }
    }
}
