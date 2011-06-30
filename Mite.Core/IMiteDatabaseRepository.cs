namespace Mite.Core
{
    public interface  IMiteDatabaseRepository
    {
        MiteDatabase Create();
        void Save();
    }
}