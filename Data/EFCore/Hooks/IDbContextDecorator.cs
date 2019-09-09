namespace Dasync.EntityFrameworkCore.Hooks
{
    public interface IDbContextDecorator
    {
        void Decorate(IDbContextProxy dbContextProxy);
    }
}
