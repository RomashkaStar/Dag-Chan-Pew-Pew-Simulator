public interface IDamageable
{
    // Любой объект с этим интерфейсом
    // обязан уметь получать урон.
    void TakeDamage(float damage);
}