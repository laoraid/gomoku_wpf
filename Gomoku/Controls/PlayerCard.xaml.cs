using Gomoku.Models;
using Gomoku.ViewModels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Gomoku.Controls
{
    /// <summary>
    /// PlayerCard.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class PlayerCard : UserControl
    {
        public PlayerCard()
        {
            InitializeComponent();
        }

        public PlayerViewModel Player
        {
            get { return (PlayerViewModel)GetValue(PlayerProperty); }
            set { SetValue(PlayerProperty, value); }
        }

        public static readonly DependencyProperty PlayerProperty =
            DependencyProperty.Register(nameof(Player), typeof(PlayerViewModel), typeof(PlayerCard));


        public ICommand JoinGameCommand
        {
            get { return (ICommand)GetValue(JoinGameCommandProperty); }
            set { SetValue(JoinGameCommandProperty, value); }
        }

        public static readonly DependencyProperty JoinGameCommandProperty =
            DependencyProperty.Register(nameof(JoinGameCommand), typeof(ICommand), typeof(PlayerCard));

        public ICommand LeaveGameCommand
        {
            get { return (ICommand)GetValue(LeaveGameCommandProperty); }
            set { SetValue(LeaveGameCommandProperty, value); }
        }

        public static readonly DependencyProperty LeaveGameCommandProperty =
            DependencyProperty.Register(nameof(LeaveGameCommand), typeof(ICommand), typeof(PlayerCard));

        public bool IsMe
        {
            get { return (bool)GetValue(IsMeProperty); }
            set { SetValue(IsMeProperty, value); }
        }

        public static readonly DependencyProperty IsMeProperty =
            DependencyProperty.Register(nameof(IsMe), typeof(bool), typeof(PlayerCard), new PropertyMetadata(false));

        public PlayerType TeamType
        {
            get { return (PlayerType)GetValue(TeamTypeProperty); }
            set { SetValue(TeamTypeProperty, value); }
        }

        public static readonly DependencyProperty TeamTypeProperty =
            DependencyProperty.Register(nameof(TeamType), typeof(PlayerType), typeof(PlayerCard),
                new PropertyMetadata(PlayerType.Observer, OnTeamTypeChanged));

        public Brush StoneColor
        {
            get { return (Brush)GetValue(StoneColorProperty); }
            set { SetValue(StoneColorProperty, value); }
        }

        public static readonly DependencyProperty StoneColorProperty =
            DependencyProperty.Register(nameof(StoneColor), typeof(Brush), typeof(PlayerCard));



        public string PlaceHolder
        {
            get { return (string)GetValue(PlaceHolderProperty); }
            set { SetValue(PlaceHolderProperty, value); }
        }

        public static readonly DependencyProperty PlaceHolderProperty =
            DependencyProperty.Register(nameof(PlaceHolder), typeof(string), typeof(PlayerCard));

        private static void OnTeamTypeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is PlayerCard card && e.NewValue != null)
            {
                PlayerType type = (PlayerType)e.NewValue;

                if (type == PlayerType.Black)
                {
                    card.StoneColor = Brushes.Black;
                    card.PlaceHolder = "흑 대기 중...";
                }
                else if (type == PlayerType.White)
                {
                    card.StoneColor = Brushes.White;
                    card.PlaceHolder = "백 대기 중...";
                }
            }
        }

    }
}
