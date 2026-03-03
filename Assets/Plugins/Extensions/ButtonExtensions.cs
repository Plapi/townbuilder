using UnityEngine.Events;
using UnityEngine.UI;

namespace com.Plapamaru.Extensions
{
    public static class ButtonExtensions
    {
        public static void SetExclusiveListener(this Button button, UnityAction action)
        {
            button.onClick = new Button.ButtonClickedEvent();
            button.onClick.AddListener(action);
        }
    }
}