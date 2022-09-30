namespace ElasticClient;

internal class Throttler
{
    public int MessagesPerSecondLimit { get; set; }
    private int CurrentMessagesCount { get; set; }
    private bool AllowedToSend { get; set; }


    public Throttler(int messagesPerSecond)
    {
        MessagesPerSecondLimit = messagesPerSecond;
        CurrentMessagesCount = 0;
        AllowedToSend = true;
        StartTimer();
    }
    
    private void StartTimer()
    {
        var timer = new Timer(_ =>
        {
            CurrentMessagesCount = 0;
            AllowedToSend = true;
        }, null, TimeSpan.Zero, new TimeSpan(0, 0, 1));
    }
    public void WaitIfBigLoad()
    {
        CurrentMessagesCount++;
        if (CurrentMessagesCount > MessagesPerSecondLimit)
        {
            AllowedToSend = false;
            while (true)
            {
                if (AllowedToSend)
                {
                    break;
                }
            }
        }
    } 
}