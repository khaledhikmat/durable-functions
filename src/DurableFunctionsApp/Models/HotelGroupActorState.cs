namespace DurableFunctionsApp.Models
{
    public class HotelGroupActorState
    {
        // Input
        public string Code { get; set; } = "";
        public int MaxMinutesToRun { get; set; } = 60 * 60;
        public int MinutesBetweenInvocations { get; set; } = 30;
        public int MembershipsToFetchInEachIteration { get; set; } = 50;
        public int MillisToDelayBetweenEachRefresh{ get; set; } = 2000;
        public bool IsTest { get; set; } = false;
        public int MaxTestCounter { get; set; } = 10;

        // Running
        public int UpTimeInMinutes { get; set; } = 0;
        public int RefreshCounter { get; set; } = 0;
        public string EndReason { get; set; } = "";
    }
}
