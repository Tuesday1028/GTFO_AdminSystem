namespace Hikaria.AdminSystem.Interfaces;

public interface IPauseable
{
    void PausedUpdate();

    void OnPaused();

    void OnUnpaused();
}
