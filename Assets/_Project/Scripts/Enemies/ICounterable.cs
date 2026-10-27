namespace Platformer.Enemies
{
    /// <summary>
    /// 카운터(GDD 4.5)를 받을 수 있는 적. 공격 직전 신호를 보내는 동안 판정이 열린다.
    /// </summary>
    public interface ICounterable
    {
        /// <summary>지금 카운터하면 성공하는지 (공격 직전 신호 중)</summary>
        bool IsCounterWindowOpen { get; }
    }
}
