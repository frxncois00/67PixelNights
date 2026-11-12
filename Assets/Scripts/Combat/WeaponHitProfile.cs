using UnityEngine;

// Presentation only: weapon damage, attack timing and physical knockback are unchanged.
public enum WeaponHitKind { Slash, Cleave, Stab, Thrust, Slam, Scoop, Arrow }

public readonly struct WeaponHitProfile
{
    public readonly WeaponHitKind kind;
    public readonly float duration, compression, recoil, tilt, burstSize;
    public readonly int sparks;
    public readonly Color accent;

    WeaponHitProfile(WeaponHitKind kind, float duration, float compression, float recoil,
        float tilt, float burstSize, int sparks, Color accent)
    {
        this.kind = kind;
        this.duration = duration;
        this.compression = compression;
        this.recoil = recoil;
        this.tilt = tilt;
        this.burstSize = burstSize;
        this.sparks = sparks;
        this.accent = accent;
    }

    public static WeaponHitKind ForWeapon(ItemDefinition item)
    {
        if (!item) return WeaponHitKind.Slash;
        if (item.rangedWeapon) return WeaponHitKind.Arrow;
        switch (item.Id)
        {
            case "wood_longsword":
            case "basic_axe": return WeaponHitKind.Cleave;
            case "wood_knife": return WeaponHitKind.Stab;
            case "wood_spear": return WeaponHitKind.Thrust;
            case "wood_hammer": return WeaponHitKind.Slam;
            case "wood_shovel": return WeaponHitKind.Scoop;
            default: return WeaponHitKind.Slash;
        }
    }

    public static WeaponHitProfile For(WeaponHitKind kind)
    {
        Color gold = new Color(1f, 0.72f, 0.18f);
        Color ivory = new Color(0.73f, 0.92f, 1f);
        switch (kind)
        {
            case WeaponHitKind.Cleave: return new WeaponHitProfile(kind, .30f, .24f, .13f, 12f, 1.15f, 9, gold);
            case WeaponHitKind.Stab: return new WeaponHitProfile(kind, .17f, .12f, .07f, 5f, .65f, 4, ivory);
            case WeaponHitKind.Thrust: return new WeaponHitProfile(kind, .24f, .17f, .15f, 6f, .80f, 6, ivory);
            case WeaponHitKind.Slam: return new WeaponHitProfile(kind, .34f, .30f, .07f, 4f, 1.30f, 12, new Color(1f, .58f, .16f));
            case WeaponHitKind.Scoop: return new WeaponHitProfile(kind, .29f, .20f, .09f, 11f, .95f, 7, new Color(1f, .81f, .43f));
            case WeaponHitKind.Arrow: return new WeaponHitProfile(kind, .19f, .10f, .08f, 4f, .60f, 4, ivory);
            default: return new WeaponHitProfile(kind, .25f, .19f, .10f, 9f, .95f, 7, gold);
        }
    }
}
