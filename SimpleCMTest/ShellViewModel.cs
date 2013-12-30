
namespace SimpleCMTest
{
    public class ShellViewModel : IShell
    {
        public ShellViewModel()
        {
            _i = 10;
        }

        public int _i { get; set; }
    }
}
