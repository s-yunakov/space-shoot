namespace SpaceShoot
{
    public partial class MainPage : ContentPage
    {
        private Player player;
        private List<Enemy> enemies = new();
        private List<Bullet> bullets = new();
        private List<PowerUp> powerUps = new();
        private IDispatcherTimer gameTimer;
        private IDispatcherTimer enemySpawnTimer;
        private IDispatcherTimer powerUpSpawnTimer;

        private int score = 0;
        private int highScore = 0;
        private int lives = 3;
        private bool isGameRunning = false;
        private WeaponType currentWeapon = WeaponType.Single;

        private const int MaxBullets = 15;
        private const int MaxEnemies = 20;
        private const int MaxLives = 5;
        private double canvasWidth;
        private double canvasHeight;
        private double lastPanX = 0;
        private double lastPanY = 0;
        private DateTime playerInvulnerableUntil = DateTime.MinValue;
        private const double InvulnerabilitySeconds = 1.5;
        private DateTime tripleShotUntil = DateTime.MinValue;
        private const double TripleShotDurationSeconds = 10;
        private int lastDisplayedTripleSeconds = -1;
        private DateTime shieldUntil = DateTime.MinValue;
        private const double ShieldDurationSeconds = 5;
        private Microsoft.Maui.Controls.Shapes.Ellipse shieldVisual;

        public int Score
        {
            get { return score; }
            set
            {
                score = value;

                if (score > highScore)
                {
                    highScore = score;
                    Preferences.Default.Set("HighScore", highScore);
                }

                OnPropertyChanged();
            }
        }

        public MainPage()
        {
            InitializeComponent();
            highScore = Preferences.Default.Get("HighScore", 0);
            InitialiseTimersandGestures();
            BindingContext = this;
        }

        public void InitialiseTimersandGestures()
        {
            // Add pan gesture for continuous movement
            var panGesture = new PanGestureRecognizer();
            panGesture.PanUpdated += OnPanUpdated;
            GameCanvas.GestureRecognizers.Add(panGesture);

            // Keep tap gesture for shooting
            var tapGesture = new TapGestureRecognizer();
            tapGesture.Tapped += OnCanvasTapped;
            GameCanvas.GestureRecognizers.Add(tapGesture);

            // Setup game loop timer using DispatcherTimer (60 FPS)
            gameTimer = Dispatcher.CreateTimer();
            gameTimer.Interval = TimeSpan.FromMilliseconds(16);
            gameTimer.Tick += OnGameTick;
            gameTimer.IsRepeating = true;

            enemySpawnTimer = Dispatcher.CreateTimer();
            enemySpawnTimer.Interval = TimeSpan.FromSeconds(2);
            enemySpawnTimer.Tick += OnEnemySpawn;
            enemySpawnTimer.IsRepeating = true;

            powerUpSpawnTimer = Dispatcher.CreateTimer();
            powerUpSpawnTimer.Interval = TimeSpan.FromSeconds(12);
            powerUpSpawnTimer.Tick += OnPowerUpSpawn;
            powerUpSpawnTimer.IsRepeating = true;
        }

        protected override void OnSizeAllocated(double width, double height)
        {
            base.OnSizeAllocated(width, height);
            if (width > 0 && height > 0)
            {
                canvasWidth = width;
                canvasHeight = height - 65; // Account for header
            }
        }

        private void OnStartClicked(object sender, EventArgs e)
        {
            StartGame();
        }

        private void OnPlayAgainClicked(object sender, EventArgs e)
        {
            StartGame();
        }

        private void StartGame()
        {
            if (isGameRunning) return;

            isGameRunning = true;
            score = 0;
            lives = 3;
            currentWeapon = WeaponType.Single;
            tripleShotUntil = DateTime.MinValue;
            shieldUntil = DateTime.MinValue;
            playerInvulnerableUntil = DateTime.MinValue;
            enemySpawnTimer.Interval = TimeSpan.FromSeconds(2);
            enemies.Clear();
            bullets.Clear();
            powerUps.Clear();
            GameCanvas.Children.Clear();
            shieldVisual = null;
            GameOverOverlay.IsVisible = false;
            StartButton.IsEnabled = false;
            gameTimer.Start();
            enemySpawnTimer.Start();
            powerUpSpawnTimer.Start();

            UpdateUI();

            // Create player in center
            player = new Player(canvasWidth / 2, canvasHeight / 2);
            GameCanvas.Children.Add(player.Visual);
            AbsoluteLayout.SetLayoutBounds(player.Visual,
                new Rect(player.X - player.Size / 2, player.Y - player.Size / 2, player.Size, player.Size));


        }

        private void OnGameTick(object sender, EventArgs e)
        {
            if (!isGameRunning)
                return;

            // Check if temporary Triple Shot has expired
            if (tripleShotUntil != DateTime.MinValue &&
                DateTime.Now >= tripleShotUntil)
            {
                tripleShotUntil = DateTime.MinValue;
                lastDisplayedTripleSeconds = -1;

                UpdateWeapon();
                UpdateUI();
            }

            UpdateTripleShotCountdown();

            if (shieldUntil != DateTime.MinValue &&
                DateTime.Now >= shieldUntil)
            {
                shieldUntil = DateTime.MinValue;
            }

            UpdateShieldVisual();

            // Remove expired power-ups
            for (int i = powerUps.Count - 1; i >= 0; i--)
            {
                if (DateTime.Now >= powerUps[i].ExpiresAt)
                {
                    GameCanvas.Children.Remove(powerUps[i].Visual);
                    powerUps.RemoveAt(i);
                }
            }

            // Check collision with power-ups
            for (int i = powerUps.Count - 1; i >= 0; i--)
            {
                if (CheckCollision(
                        player.X,
                        player.Y,
                        player.Size,
                        powerUps[i].X,
                        powerUps[i].Y,
                        powerUps[i].Size))
                {
                    PowerUp powerUp = powerUps[i];

                    GameCanvas.Children.Remove(powerUp.Visual);
                    powerUps.RemoveAt(i);

                    if (powerUp.Type == PowerUpType.TripleShot)
                    {
                        tripleShotUntil = DateTime.Now.AddSeconds(
                            TripleShotDurationSeconds);

                        lastDisplayedTripleSeconds = -1;

                        UpdateWeapon();

                        _ = ShowPowerUpMessage(
                            "TRIPLE SHOT!",
                            powerUp.X,
                            powerUp.Y,
                            Colors.OrangeRed);
                    }
                    else if (powerUp.Type == PowerUpType.ExtraLife)
                    {
                        if (lives < MaxLives)
                        {
                            lives++;

                            _ = ShowPowerUpMessage(
                                "+1 LIFE",
                                powerUp.X,
                                powerUp.Y,
                                Colors.LimeGreen);
                        }
                        else
                        {
                            _ = ShowPowerUpMessage(
                                "MAX LIVES",
                                powerUp.X,
                                powerUp.Y,
                                Colors.Gold);
                        }
                    }
                    else if (powerUp.Type == PowerUpType.Shield)
                    {
                        shieldUntil = DateTime.Now.AddSeconds(
                            ShieldDurationSeconds);

                        _ = ShowPowerUpMessage(
                            "SHIELD ACTIVATED!",
                            powerUp.X,
                            powerUp.Y,
                            Colors.DeepSkyBlue);
                    }

                    UpdateUI();
                }
            }

            // Update all bullets
            for (int i = bullets.Count - 1; i >= 0; i--)
            {
                bullets[i].Update();

                AbsoluteLayout.SetLayoutBounds(
                    bullets[i].Visual,
                    new Rect(
                        bullets[i].X - 3,
                        bullets[i].Y - 10,
                        6,
                        20));

                if (!bullets[i].IsOnScreen(canvasWidth, canvasHeight))
                {
                    GameCanvas.Children.Remove(bullets[i].Visual);
                    bullets.RemoveAt(i);
                }
            }

            // Update all enemies
            for (int i = enemies.Count - 1; i >= 0; i--)
            {
                enemies[i].Update(canvasWidth, canvasHeight);

                AbsoluteLayout.SetLayoutBounds(
                    enemies[i].Visual,
                    new Rect(
                        enemies[i].X - enemies[i].Size / 2,
                        enemies[i].Y - enemies[i].Size / 2,
                        enemies[i].Size,
                        enemies[i].Size));

                // Shield destroys enemies on contact without damage
                if (DateTime.Now < shieldUntil &&
                    CheckCollision(
                        player.X,
                        player.Y,
                        player.Size,
                        enemies[i].X,
                        enemies[i].Y,
                        enemies[i].Size))
                {
                    var enemyVisual = enemies[i].Visual;
                    enemies.RemoveAt(i);

                    _ = AnimateEnemyHit(enemyVisual);
                    continue;
                }

                // Check collision with player
                if (DateTime.Now >= playerInvulnerableUntil &&
                    CheckCollision(
                        player.X,
                        player.Y,
                        player.Size,
                        enemies[i].X,
                        enemies[i].Y,
                        enemies[i].Size))
                {
                    GameCanvas.Children.Remove(enemies[i].Visual);
                    enemies.RemoveAt(i);

                    LoseLife();

                    playerInvulnerableUntil =
                        DateTime.Now.AddSeconds(InvulnerabilitySeconds);

                    continue;
                }

                // Check collision with bullets
                for (int j = bullets.Count - 1; j >= 0; j--)
                {
                    if (CheckCollision(
                            enemies[i].X,
                            enemies[i].Y,
                            enemies[i].Size,
                            bullets[j].X,
                            bullets[j].Y,
                            bullets[j].Visual.WidthRequest))
                    {
                        GameCanvas.Children.Remove(bullets[j].Visual);
                        bullets.RemoveAt(j);

                        enemies[i].Health--;

                        if (enemies[i].Health <= 0)
                        {
                            var enemyVisual = enemies[i].Visual;
                            int scoreValue = enemies[i].ScoreValue;

                            enemies.RemoveAt(i);

                            Score += scoreValue;
                            UpdateDifficulty();
                            UpdateWeapon();
                            UpdateUI();

                            _ = AnimateEnemyHit(enemyVisual);

                            break;
                        }
                    }
                }
            }
        }

        private void OnEnemySpawn(object sender, EventArgs e)
        {
            if (!isGameRunning) return;
            SpawnEnemy();
        }

        private void SpawnEnemy()
        {
            if (!isGameRunning)
                return;

            if (enemies.Count >= MaxEnemies)
                return;

            Random rand = new Random();
            double x, y;

            // Spawn at random edge of screen
            int edge = rand.Next(4);
            switch (edge)
            {
                case 0: // Top
                    x = rand.NextDouble() * canvasWidth;
                    y = 0;
                    break;
                case 1: // Right
                    x = canvasWidth;
                    y = rand.NextDouble() * canvasHeight;
                    break;
                case 2: // Bottom
                    x = rand.NextDouble() * canvasWidth;
                    y = canvasHeight;
                    break;
                default: // Left
                    x = 0;
                    y = rand.NextDouble() * canvasHeight;
                    break;
            }

            Enemy enemy;
            enemy = new Enemy(x, y);
            enemies.Add(enemy);
            GameCanvas.Children.Add(enemy.Visual);
            AbsoluteLayout.SetLayoutBounds(enemy.Visual,
                new Rect(enemy.X - enemy.Size / 2, enemy.Y - enemy.Size / 2, enemy.Size, enemy.Size));
        }

        private void OnPanUpdated(object sender, PanUpdatedEventArgs e)
        {
            if (!isGameRunning)
                return;

            switch (e.StatusType)
            {
                case GestureStatus.Started:
                    // Reset tracking at start of gesture
                    lastPanX = e.TotalX;
                    lastPanY = e.TotalY;
                    break;

                case GestureStatus.Running:
                    {
                        // Only move by the *change* in pan, not the total
                        // Divide by 2 to reduce sensitivity
                        double deltaX = (e.TotalX - lastPanX) / 2;
                        double deltaY = (e.TotalY - lastPanY) / 2;

                        lastPanX = e.TotalX;
                        lastPanY = e.TotalY;

                        double newX = player.X + deltaX;
                        double newY = player.Y + deltaY;

                        newX = Math.Clamp(
                            newX,
                            player.Size / 2,
                            canvasWidth - player.Size / 2);

                        newY = Math.Clamp(
                            newY,
                            player.Size / 2,
                            canvasHeight - player.Size / 2);

                        MovePlayer(newX, newY);
                        break;
                    }

                case GestureStatus.Completed:
                    break;
            }
        }

        private void OnCanvasTapped(object sender, TappedEventArgs e)
        {
            if (!isGameRunning)
                return;

            // Tap to shoot in direction of tap
            Point? position = e.GetPosition(GameCanvas);

            if (position.HasValue)
            {
                ShootTowards(position.Value.X, position.Value.Y);
            }
        }

        private void MovePlayer(double targetX, double targetY)
        {
            player.MoveTo(targetX, targetY);
            AbsoluteLayout.SetLayoutBounds(player.Visual,
                new Rect(player.X - player.Size / 2, player.Y - player.Size / 2, player.Size, player.Size));
        }

        private void ShootTowards(double targetX, double targetY)
        {
            if (bullets.Count >= MaxBullets)
                return;

            // Calculate direction to tap point
            double dx = targetX - player.X;
            double dy = targetY - player.Y;

            double distance = Math.Sqrt(dx * dx + dy * dy);

            if (distance <= 0)
                return;

            double directionX = dx / distance;
            double directionY = dy / distance;

            double angle = Math.Atan2(directionY, directionX) * 180 / Math.PI + 90;
            player.RotatePlayer(angle);

            switch (currentWeapon)
            {
                case WeaponType.Single:
                    ShootBullet(directionX, directionY);
                    break;

                case WeaponType.Double:
                    ShootBulletWithSpread(directionX, directionY, 10);
                    ShootBulletWithSpread(directionX, directionY, -10);
                    break;

                case WeaponType.Triple:
                    ShootBulletWithSpread(directionX, directionY, 0);
                    ShootBulletWithSpread(directionX, directionY, 10);
                    ShootBulletWithSpread(directionX, directionY, -10);
                    break;
            }
        }

        private void ShootBullet(double directionX, double directionY)
        {
            if (bullets.Count >= MaxBullets)
                return;

            Bullet bullet = new Bullet(
                player.X,
                player.Y,
                directionX,
                directionY);

            bullets.Add(bullet);
            GameCanvas.Children.Add(bullet.Visual);

            AbsoluteLayout.SetLayoutBounds(
                bullet.Visual,
                new Rect(
                    bullet.X - 3,
                    bullet.Y - 10,
                    6,
                    20));
        }

        private void ShootBulletWithSpread(
            double directionX,
            double directionY,
            double angleDegrees)
            {
                if (bullets.Count >= MaxBullets)
                    return;

                double angle = angleDegrees * Math.PI / 180.0;

                double rotatedX =
                    directionX * Math.Cos(angle) -
                    directionY * Math.Sin(angle);

                double rotatedY =
                    directionX * Math.Sin(angle) +
                    directionY * Math.Cos(angle);

                ShootBullet(rotatedX, rotatedY);
            }

        private bool CheckCollision(double x1, double y1, double size1,
                           double x2, double y2, double size2)
        {
            double distance = Math.Sqrt(Math.Pow(x2 - x1, 2) + Math.Pow(y2 - y1, 2));
            return distance < (size1 + size2) / 2;
        }

        private async Task AnimateEnemyHit(View enemyVisual)
        {
            await enemyVisual.FadeTo(0, 100);
            GameCanvas.Children.Remove(enemyVisual);
        }

        private void LoseLife()
        {
            lives--;
            UpdateUI();

            if (lives <= 0)
            {
                EndGame();
            }
        }

        private void EndGame()
        {
            isGameRunning = false;
            gameTimer?.Stop();
            enemySpawnTimer?.Stop();
            powerUpSpawnTimer?.Stop();

            GameOverOverlay.IsVisible = true;
            StartButton.IsEnabled = true;
        }

        private void UpdateDifficulty()
        {
            if (enemySpawnTimer == null)
                return;

            if (score >= 100)
            {
                enemySpawnTimer.Interval = TimeSpan.FromSeconds(1);
            }
            else if (score >= 50)
            {
                enemySpawnTimer.Interval = TimeSpan.FromSeconds(1.5);
            }
            else
            {
                enemySpawnTimer.Interval = TimeSpan.FromSeconds(2);
            }
        }

        private void UpdateWeapon()
        {
            if (DateTime.Now < tripleShotUntil)
            {
                currentWeapon = WeaponType.Triple;
                return;
            }

            if (score >= 150)
            {
                currentWeapon = WeaponType.Triple;
            }
            else if (score >= 50)
            {
                currentWeapon = WeaponType.Double;
            }
            else
            {
                currentWeapon = WeaponType.Single;
            }
        }

        private string GetWeaponName()
        {
            return currentWeapon switch
            {
                WeaponType.Single => "Weapon: Single",
                WeaponType.Double => "Weapon: Double",
                WeaponType.Triple => "Weapon: Triple",
                _ => "Weapon: Single"
            };
        }

        private void UpdateWeaponLabel()
        {
            if (tripleShotUntil != DateTime.MinValue &&
                DateTime.Now < tripleShotUntil)
            {
                int secondsRemaining = (int)Math.Ceiling(
                    (tripleShotUntil - DateTime.Now).TotalSeconds);

                WeaponLabel.Text = $"Triple Shot: {secondsRemaining}s";
            }
            else
            {
                WeaponLabel.Text = GetWeaponName();
            }

            WeaponLabel.TextColor = currentWeapon switch
            {
                WeaponType.Single => Colors.White,
                WeaponType.Double => Colors.Gold,
                WeaponType.Triple => Colors.OrangeRed,
                _ => Colors.White
            };
        }

        private async Task ShowPowerUpMessage(
            string text,
            double x,
            double y,
            Color color)
        {
            var message = new Label
            {
                Text = text,
                TextColor = color,
                FontSize = 22,
                FontAttributes = FontAttributes.Bold,
                HorizontalTextAlignment = TextAlignment.Center,
                VerticalTextAlignment = TextAlignment.Center,
                HorizontalOptions = LayoutOptions.Fill,
                VerticalOptions = LayoutOptions.Fill,
                BackgroundColor = Color.FromArgb("#CC000000"),
                WidthRequest = 220,
                HeightRequest = 55,
                Opacity = 0,
                ZIndex = 1000,
                InputTransparent = true
            };

            double left = Math.Clamp(
                x - 110,
                0,
                Math.Max(0, canvasWidth - 220));

            double top = Math.Clamp(
                y - 27,
                0,
                Math.Max(0, canvasHeight - 55));

            AbsoluteLayout.SetLayoutBounds(
                message,
                new Rect(left, top, 220, 55));

            GameCanvas.Children.Add(message);

            try
            {
                await message.FadeTo(1, 200);
                await Task.Delay(700);
                await message.TranslateTo(0, -25, 400, Easing.CubicOut);
                await message.FadeTo(0, 350);
            }
            finally
            {
                GameCanvas.Children.Remove(message);
            }
        }

        private void UpdateTripleShotCountdown()
        {
            if (tripleShotUntil == DateTime.MinValue)
                return;

            int secondsRemaining = (int)Math.Ceiling(
                (tripleShotUntil - DateTime.Now).TotalSeconds);

            if (secondsRemaining != lastDisplayedTripleSeconds)
            {
                lastDisplayedTripleSeconds = secondsRemaining;
                UpdateWeaponLabel();
            }
        }

        private void UpdateShieldVisual()
        {
            bool shieldActive =
                shieldUntil != DateTime.MinValue &&
                DateTime.Now < shieldUntil;

            if (!shieldActive)
            {
                if (shieldVisual != null)
                {
                    GameCanvas.Children.Remove(shieldVisual);
                    shieldVisual = null;
                }

                return;
            }

            if (player == null)
                return;

            if (shieldVisual == null)
            {
                shieldVisual = new Microsoft.Maui.Controls.Shapes.Ellipse
                {
                    Stroke = new SolidColorBrush(Colors.DeepSkyBlue),
                    StrokeThickness = 3,
                    Fill = new SolidColorBrush(Colors.Transparent),
                    InputTransparent = true
                };

                GameCanvas.Children.Add(shieldVisual);
            }

            double diameter = player.Size + 20;

            AbsoluteLayout.SetLayoutBounds(
                shieldVisual,
                new Rect(
                    player.X - diameter / 2,
                    player.Y - diameter / 2,
                    diameter,
                    diameter));
        }

        private void UpdateUI()
        {
            ScoreLabel.Text = $"Score : {score}";
            LivesLabel.Text = $"Lives: {lives}";
            UpdateWeaponLabel();
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            gameTimer?.Stop();
            enemySpawnTimer?.Stop();
            powerUpSpawnTimer?.Stop();
        }

        private void OnPowerUpSpawn(object sender, EventArgs e)
        {
            if (!isGameRunning)
                return;

            SpawnPowerUp();
        }

        private void SpawnPowerUp()
        {
            if (powerUps.Count >= 1)
                return;

            if (canvasWidth <= 40 || canvasHeight <= 40)
                return;

            Random rand = new Random();

            double x = 20 + rand.NextDouble() * (canvasWidth - 40);
            double y = 20 + rand.NextDouble() * (canvasHeight - 40);

            var availableTypes = new List<PowerUpType>
            {
                PowerUpType.TripleShot,
                PowerUpType.Shield
            };

            if (lives < MaxLives)
            {
                availableTypes.Add(PowerUpType.ExtraLife);
            }

            PowerUpType type =
                availableTypes[rand.Next(availableTypes.Count)];

            PowerUp powerUp = new PowerUp(x, y, type);

            powerUps.Add(powerUp);
            GameCanvas.Children.Add(powerUp.Visual);

            AbsoluteLayout.SetLayoutBounds(
                powerUp.Visual,
                new Rect(
                    powerUp.X - powerUp.Size / 2,
                    powerUp.Y - powerUp.Size / 2,
                    powerUp.Size,
                    powerUp.Size));
        }

    }
}
