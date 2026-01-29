namespace Gomoku.Services.Wpf.Media
{
    public enum SoundType
    { // 승리, 패배, 입장, 퇴장 사운드 추가?
        StonePlace
    }
    public interface ISoundService
    {
        void Play(SoundType soundType);
    }
}
