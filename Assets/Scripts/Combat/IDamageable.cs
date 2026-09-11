namespace HY3RIDOrigins.Combat
{
    public interface IDamageable
    {
        void TakeDamage(float amount, bool fromFront = false);
    }
}
