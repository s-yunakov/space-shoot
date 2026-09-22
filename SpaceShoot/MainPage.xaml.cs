namespace SpaceShoot
{
    public partial class MainPage : ContentPage
    {
        private Player player;
        private List<Enemy> enemies = new();
        private List<Bullet> bullets = new();
        private IDispatcherTimer gameTimer;
        private IDispatcherTimer enemySpawnTimer;

        private int score = 0;
        private int lives = 3;
        private bool isGameRunning = false;

        private const int MaxBullets = 5;
        private double canvasWidth;
        private double canvasHeight;
        private double lastPanX = 0;
        private double lastPanY = 0;

        public int Score
        {
            get { return score; }
            set
            {
                score = value;
                OnPropertyChanged();
            }
        }

        public MainPage()
        {
            InitializeComponent();
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
            enemies.Clear();
            bullets.Clear();
            GameCanvas.Children.Clear();
            GameOverOverlay.IsVisible = false;
            StartButton.IsEnabled = false;
            gameTimer.Start();
            //enemySpawnTimer.Start();

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

            // Update all bullets

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

                // Check collision with player
                if (CheckCollision(player.X, player.Y, player.Size,
                                 enemies[i].X, enemies[i].Y, enemies[i].Size))
                {
                    GameCanvas.Children.Remove(enemies[i].Visual);
                    enemies.RemoveAt(i);
                    LoseLife();
                    continue;
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

        private bool CheckCollision(double x1, double y1, double size1,
                           double x2, double y2, double size2)
        {
            double distance = Math.Sqrt(Math.Pow(x2 - x1, 2) + Math.Pow(y2 - y1, 2));
            return distance < (size1 + size2) / 2;
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

            GameOverOverlay.IsVisible = true;
        }

        private void UpdateUI()
        {
            ScoreLabel.Text = $"Score : {score}";
            LivesLabel.Text = $"Lives: {lives}";
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            gameTimer?.Stop();
            enemySpawnTimer?.Stop();
        }

    }
}
