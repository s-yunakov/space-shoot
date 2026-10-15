using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpaceShoot
{
    internal class PowerUp
    {
        public double X { get; private set; }
        public double Y { get; private set; }
        public double Size { get; private set; } = 40;

        public PowerUpType Type { get; private set; }

        public DateTime ExpiresAt { get; private set; }

        public View Visual { get; private set; }

        public PowerUp(double x, double y, PowerUpType type)
        {
            X = x;
            Y = y;
            Type = type;

            ExpiresAt = DateTime.Now.AddSeconds(8);

            if (type == PowerUpType.ExtraLife)
            {
                Visual = new Label
                {
                    Text = "❤️",
                    FontSize = 30,
                    WidthRequest = Size,
                    HeightRequest = Size,
                    HorizontalTextAlignment = TextAlignment.Center,
                    VerticalTextAlignment = TextAlignment.Center,
                    BackgroundColor = Colors.Transparent
                };
            }
            else if (type == PowerUpType.Shield)
            {
                Visual = new Label
                {
                    Text = "🛡️",
                    FontSize = 30,
                    WidthRequest = Size,
                    HeightRequest = Size,
                    HorizontalTextAlignment = TextAlignment.Center,
                    VerticalTextAlignment = TextAlignment.Center,
                    BackgroundColor = Colors.Transparent
                };
            }
            else
            {
                Visual = new Image
                {
                    Source = "ufo.png",
                    WidthRequest = Size,
                    HeightRequest = Size,
                    Aspect = Aspect.AspectFit
                };
            }
        }
    }
}
