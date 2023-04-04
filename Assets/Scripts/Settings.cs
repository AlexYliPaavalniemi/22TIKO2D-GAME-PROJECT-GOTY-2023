using System.Linq;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UIElements;

namespace Game
{
    /// <summary>Settings dialog.</summary>
    public class Settings : MonoBehaviour
    {
        /// <summary>Root visual element.</summary>
        private VisualElement rootVisual;

        /// <summary>Canvas to hide when this dialog is shown.</summary>
        [SerializeField]
        private Canvas mainCanvas;

        /// <summary>Mixer for audio.</summary>
        [SerializeField]
        AudioMixer mixer;

        private void Start()
        {
            this.rootVisual = this.GetComponent<UIDocument>().rootVisualElement;

            // Get the music slider.
            Slider musicSlider = this.rootVisual.Query<Slider>("Music");

            // Chagne music volume when slider chages.
            musicSlider.RegisterValueChangedCallback(
                (value) =>
                {
                    // Set volume.
                    this.mixer.SetFloat("MusicVolume", Linear2dB(value.newValue));

                    // Save to player prefs.
                    PlayerPrefs.SetFloat("MusicVolume", value.newValue);
                }
            );

            // Set the volume.
            musicSlider.value = PlayerPrefs.GetFloat("MusicVolume", 1.0f);

            // Get speed buttons.
            string[] buttonNames = { "SpeedHalf", "SpeedOne", "SpeedTwo", "SpeedThree" };
            float[] buttonSpeeds = { 0.5f, 1.0f, 2.0f, 3.0f };
            Button[] buttons = buttonNames
                .Select((name) => this.rootVisual.Query<Button>(name).First())
                .ToArray();

            // Get current saved speed setting.
            float currentSpeed = PlayerPrefs.GetFloat("BattleSpeed", 1.0f);

            // Set click callbacks.
            for (int i = 0; i < buttons.Length; i++)
            {
                int iCopy = i; // Copy by value.
                buttons[i].clicked += () =>
                {
                    for (int j = 0; j < buttons.Length; j++)
                    {
                        // Active if this is the current button.
                        buttons[j].EnableInClassList("active", iCopy == j);
                    }

                    PlayerPrefs.SetFloat("BattleSpeed", buttonSpeeds[iCopy]);
                };

                // If the speed matches, set this button as active.
                buttons[i].EnableInClassList(
                    "active",
                    Mathf.Approximately(buttonSpeeds[i], currentSpeed)
                );
            }

            // Hide when closed.
            this.rootVisual.Query<Button>("Close").First().clicked += () =>
            {
                this.SetVisible(false);

                // Save settings when closed.
                PlayerPrefs.Save();
            };

            // Hide at the start.
            this.SetVisible(false);
        }

        /// <summary>Convert linear audio to decibels.
        public static float Linear2dB(float amount)
        {
            amount = Mathf.Clamp01(amount);
            return Mathf.Approximately(amount, 0) ? -80 : (Mathf.Log10(amount) * 20);
        }

        /// <summary>Show or hide this settings dialog.</summary>
        public void SetVisible(bool visible)
        {
            this.rootVisual.visible = visible;
            this.mainCanvas.enabled = !visible;
        }
    }
}
