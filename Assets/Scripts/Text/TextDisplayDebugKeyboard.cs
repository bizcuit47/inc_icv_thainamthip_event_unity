using UnityEngine;

public class TextDisplayDebugKeyboard : MonoBehaviour
{
    // MARK: Inspector

    [SerializeField]
    [HelpButton("The display that receives the debug texts.")]
    private TextDisplayBase manager;

    [SerializeField]
    [HelpButton("Sample texts. Space shows one of them picked at random.")]
    private string[] sampleTexts =
    {
        "สวัสดีครับ ยินดีต้อนรับสู่งานของเรา",
        "Hello World",
        "ขอบคุณที่มาร่วมงานในวันนี้",
        "Welcome to the show",
        "Thai and English ภาษาไทยและอังกฤษ together",
        "ข้อความสั้น",
    };

    // MARK: Lifecycle
    private void Update()
    {
        DebugKeyboard();
    }

    // MARK: Private
    private void DebugKeyboard()
    {
        if (Input.GetKeyDown(KeyCode.Space) && sampleTexts.Length > 0)
            manager.Show(sampleTexts[Random.Range(0, sampleTexts.Length)]);

        if (Input.GetKeyDown(KeyCode.C))
            manager.Clear();
    }
}
