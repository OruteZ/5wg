namespace FiveWG.Pickup
{
    /// <summary>
    /// 픽업을 받는 쪽. 필드의 픽업이 받는 쪽의 정체를 모르게 가른다.
    /// 무엇이 일어나는지는 받는 쪽이 정한다 — 픽업은 체력도 지갑도 모른다.
    /// </summary>
    public interface IPickupReceiver
    {
        void Receive(PickupDefinition definition);
    }
}
