namespace MainServer.Service.AI
{
    public interface IAIService
    {
        Task<string> GenerateStoryText(string prompt);
        Task<string> EnrichStory(string baseStory);
        Task<byte[]> GenerateAudio(string text, IFormFile audioFile);        //10-30 임강묵 수정
        Task<string> TranslateText(string text, string targetLanguage = "en");
        Task<string> CallCallbackHook(string data);
    }
}
