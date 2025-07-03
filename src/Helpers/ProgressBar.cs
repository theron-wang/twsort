namespace TWSort.Helpers;
public class ProgressBar
{
    private const int BAR_LENGTH = 25;
    private const char BLOCK = '█';

    private int _last = -1;
    private int _leftInitialPosition;
    private int _topInitialPosition;
    private int _total;

    public void Initialize(int total)
    {
        (_leftInitialPosition, _topInitialPosition) = Console.GetCursorPosition();
        _total = total;
        Update(0);
    }

    public void Update(int progress)
    {
        if (progress == _last) return;

        var (left, top) = Console.GetCursorPosition();

        if (left != _leftInitialPosition || top != _topInitialPosition)
        {
            Console.SetCursorPosition(_leftInitialPosition, _topInitialPosition);
        }

        _last = Math.Max(0, progress);

        var percentage = (double)progress / _total;
        var filledLength = (int)(BAR_LENGTH * percentage);
        var bar = new string(BLOCK, filledLength) + new string('-', BAR_LENGTH - filledLength);

        Console.Write($"\r[{bar}] {progress} of {_total} done ({percentage:P0})");
        Console.SetCursorPosition(left, top);
    }
}
