using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Gomoku.Controls
{
    /// <summary>
    /// LicenseCard.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class LicenseCard : UserControl
    {
        // 아이콘 - 아무거나 받을 수 있게
        public static readonly DependencyProperty IconProperty =
            DependencyProperty.Register("Icon", typeof(object), typeof(LicenseCard));
        // 라이브러리 이름
        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register("Title", typeof(string), typeof(LicenseCard));
        // 제작자
        public static readonly DependencyProperty AuthorProperty =
            DependencyProperty.Register("Author", typeof(string), typeof(LicenseCard));
        // 라이선스 링크
        public static readonly DependencyProperty UrlProperty =
            DependencyProperty.Register("Url", typeof(string), typeof(LicenseCard));
        // 링크 여는 커맨드
        public static readonly DependencyProperty CommandProperty =
            DependencyProperty.Register("Command", typeof(ICommand), typeof(LicenseCard));



        public object Icon { get => GetValue(IconProperty); set => SetValue(IconProperty, value); }
        public string Title { get => (string)GetValue(TitleProperty); set => SetValue(TitleProperty, value); }
        public string Author { get => (string)GetValue(AuthorProperty); set => SetValue(AuthorProperty, value); }
        public string Url { get => (string)GetValue(UrlProperty); set => SetValue(UrlProperty, value); }
        public ICommand Command { get => (ICommand)GetValue(CommandProperty); set => SetValue(CommandProperty, value); }

        public LicenseCard()
        {
            InitializeComponent();
        }
    }
}
