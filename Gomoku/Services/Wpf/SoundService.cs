using Gomoku.Services.Interfaces;
using System.IO;
using System.Windows;
using System.Windows.Media;

namespace Gomoku.Services.Wpf
{
    public class SoundService : ISoundService
    {
        public readonly Dictionary<SoundType, Uri> _soundPaths = new Dictionary<SoundType, Uri>();
        public SoundService()
        {
            PrepareSound(SoundType.StonePlace, "placestone.mp3");
        }

        private void PrepareSound(SoundType type, string filename)
        {
            var uri = new Uri($"pack://application:,,,/Resources/{filename}");
            // exe에 포함된 사운드 파일 경로
            var stream = Application.GetResourceStream(uri);

            if (stream == null)
                throw new FileNotFoundException($"사운드 파일을 찾을 수 없습니다: {filename}");

            string tempFolder = Path.Combine(Path.GetTempPath(), "Gomoku_wpf");
            if (!Directory.Exists(tempFolder)) Directory.CreateDirectory(tempFolder);

            string tempFilePath = Path.Combine(tempFolder, filename);

            using (var filestream = File.Create(tempFilePath))
            {
                stream.Stream.CopyTo(filestream);
                // 임시 폴더에 저장하기
            }

            _soundPaths[type] = new Uri(tempFilePath);
        }
        public void Play(SoundType soundType)
        {
            if (!_soundPaths.ContainsKey(soundType))
                throw new FileNotFoundException($"{soundType.ToString()} 파일이 없습니다.");

            Application.Current.Dispatcher.Invoke(() =>
            {
                var player = new MediaPlayer();
                player.Open(_soundPaths[soundType]);

                player.MediaEnded += (s, e) => { player.Close(); };
                player.Play();
            });
        }
    }
}
