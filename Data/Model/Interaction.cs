namespace Cryobot.Data.Model
{
    public class InteractionData
    {
        public int ReceivedCount { get; set; }
        public int GivenCount { get; set; }

        public InteractionUser[] Users { get; set; }
    }

    public class InteractionUser
    {
        public decimal Id { get; set; }
        public string DisplayName { get; set; }
        public int Count { get; set; }
    }
}
