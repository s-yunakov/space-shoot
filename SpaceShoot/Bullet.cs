using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpaceShoot
{
    public class Bullet
    {
        public double X { get; private set; }
        public double Y { get; private set; }
        public BoxView Visual { get; private set; }

        public Bullet(double x, double y, double directionX, double directionY)
        {
            X = x;
            Y = y;

            Visual = new BoxView
            {
                WidthRequest = 6,
                HeightRequest = 20,
                Color = Colors.Yellow,
                CornerRadius = 3
            };
        }
    }
}
