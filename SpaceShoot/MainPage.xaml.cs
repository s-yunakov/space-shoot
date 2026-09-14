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

            // Setup game loop timer using DispatcherTimer (60 FPS)
            gameTimer = Dispatcher.CreateTimer();
            gameTimer.Interval = TimeSpan.FromMilliseconds(16);
            gameTimer.Tick += OnGameTick;
            gameTimer.IsRepeating = true;
        }
    }
}
