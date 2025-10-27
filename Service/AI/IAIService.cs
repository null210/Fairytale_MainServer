namespace MainServer.Service.AI
{
    public interface IAIService
    {
        Task<string> GenerateStoryText(string prompt);
        Task<string> EnrichStory(string baseStory);
        Task<byte[]> GenerateAudio(string text);
        Task<string> TranslateText(string text, string targetLanguage = "en");
        Task<string> CallCallbackHook(string data);
    }
}
