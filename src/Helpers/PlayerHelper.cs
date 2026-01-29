using SwiftlyS2.Shared.Players;
using SwiftlyS2.Shared.Sounds;

namespace SW2_RandomSkills;

public static class PlayerHelper
{
    public static string GetPlayerName(this IPlayer player)
    {
        return player.Controller.PlayerName;
    }
    public static void SetSpeed(this IPlayer player, float speed)
    {
        if (player.PlayerPawn == null)
            return;

        player.PlayerPawn.VelocityModifier = speed;
        player.PlayerPawn.VelocityModifierUpdated();
    }
    public static void SetHealth(this IPlayer player, int health)
    {
        if (player.PlayerPawn == null)
            return;

        player.PlayerPawn.Health = health;
        player.PlayerPawn.MaxHealth = health;

        player.PlayerPawn.HealthUpdated();
        player.PlayerPawn.MaxHealthUpdated();
    }
    public static void PlaySound(this IPlayer player, string soundName, float volume)
    {
        SoundEvent sound = new SoundEvent()
        {
            Name = soundName,
            Volume = volume,
        };

        sound.Recipients.AddRecipient(player.PlayerID);
        sound.Emit();
    }
    public static void SetAmmo(this IPlayer player, int ammo, int reserve)
    {
        if (player.Pawn == null)
            return;

        var activeWeapon = player.Pawn.WeaponServices?.ActiveWeapon.Value;
        if (activeWeapon != null)
        {
            activeWeapon.Clip1 = ammo;
            activeWeapon.Clip2 = ammo;
            activeWeapon.ReserveAmmo[0] = reserve;

            activeWeapon.Clip1Updated();
            activeWeapon.Clip2Updated();
            activeWeapon.ReserveAmmoUpdated();
        }
    }
}