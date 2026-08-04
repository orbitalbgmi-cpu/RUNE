namespace RUNE
{
    /// <summary>
    /// The contract every feature module must implement. Chat, Settings,
    /// Microphone, Plugins, LocalAI, Memory, Vision, Automation - every
    /// future capability plugs into this same shape.
    /// </summary>
    public interface IModule
    {
        string Id { get; }
        string DisplayName { get; }
        bool IsEnabled { get; }
        void Init();
        void Shutdown();
    }
}
