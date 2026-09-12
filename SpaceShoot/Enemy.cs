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
    }
}
