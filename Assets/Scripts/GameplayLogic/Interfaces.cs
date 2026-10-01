using UnityEngine;

public enum WeaponType { Default, MailLauncher }

public interface IShootable
{
    void OnShot(RaycastHit hit, WeaponType weapon);
}

public interface IInteractable
{
    void Interact();
}
