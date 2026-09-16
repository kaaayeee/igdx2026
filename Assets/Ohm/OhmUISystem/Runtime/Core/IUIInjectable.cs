namespace Ohm.UISystem
{
    /// <summary>Implemented by screens that accept a typed data payload via ChangeUI.</summary>
    public interface IUIInjectable<TData>
    {
        void Inject(TData data);
    }
}
